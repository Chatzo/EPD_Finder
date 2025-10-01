$(function () {
    initInputSwitch();
    initFormSubmit();
    initCopyButtons();
});

// Global variables
var totalLinks = 0;
var foundLinks = 0;
var failedLinks = 0;
var currentSSE = null;

// Switch input type
function initInputSwitch() {
    $("input[name='inputType']").change(function () {
        if ($(this).val() === "file") {
            // Hide textarea and reset
            $("#textInputDiv").hide();
            $("#enumbersText")
                .prop("required", false)
                .val("")
                .get(0).setCustomValidity("");

            // Show file input and set required
            $("#fileInputDiv").show();
            $("#fileInput")
                .prop("required", true)
                .get(0).setCustomValidity("");

            showCheckmark();
        } else {
            // Hide file input and reset
            $("#fileInputDiv").hide();
            $("#fileInput")
                .prop("required", false)
                .val("")
                .get(0).setCustomValidity("");
            $("#fileCheck").hide();

            // Show textarea and set required
            $("#textInputDiv").show();
            $("#enumbersText")
                .prop("required", true)
                .get(0).setCustomValidity("");
        }
    });
}

function reset() {
    if (currentSSE) {
        currentSSE.close();
        currentSSE = null;
    }

    // Reset counters
    totalLinks = 0;
    foundLinks = 0;
    failedLinks = 0;

    // Clear previous results
    $("#results").empty();
    $("#resultsInput").val("[]");
    $("#linksCounter").html("");
    $("#downloadForm").hide();
    $("#progressBar").css("width", "0%");
    $("#progressBarText").text("0 %");
    $("#progressText").html("");
}
// Form submission
function initFormSubmit() {
    $("#epdForm").submit(function (e) {
        e.preventDefault();
        reset();
        var formData = new FormData(this);
        var selectedSources = [];
        $('input[name="sources"]:checked').each(function () {
            selectedSources.push($(this).val());
        });
        selectedSources.forEach(src => formData.append('sources', src));
        $.ajax({
            url: "/Home/CreateJob",
            type: "POST",
            data: formData,
            processData: false,
            contentType: false,
            success: function (res) {
                totalLinks = res.eNumbers.length;
                foundLinks = 0;
                failedLinks = 0;
                updateLoadingbar();
                preCreateRows(res.eNumbers, res.jobId);
            },
            error: function (xhr) {
                var msg = xhr.responseJSON?.message || "Fel vid skapande av jobb";
                alert(msg);
            }
        });
    });
}

// Pre-create table rows
function preCreateRows(eNumbers, jobId) {
    $("#outputTable").show();
    $("#loadingContainer").show();
    $("#linksCounter").show();

    eNumbers.forEach(num => {
        var row = $("<tr>").attr("data-enumber", num);
        row.append($("<td>").css("position", "relative").text(num));
        row.append($("<td>").css("position", "relative").text(""));
        row.append($("<td>").css("position", "relative").text("Hämtar..."));
        row.append($("<td>").css("position", "relative"));   
        $("#results").append(row);
    });

    startSSE(jobId);
}

// SSE
function startSSE(jobId) {
    var url = "/Home/GetResultsStream?jobId=" + jobId;
    currentSSE = new EventSource(url);
    currentSSE.onmessage = function (e) {
        var result = JSON.parse(e.data);
        var row = $("#results").find(`tr[data-enumber='${result.ENumber}']`);

        // Clear Source + EPD columns
        row.find("td").slice(1).empty();

        if (result.EpdLink && result.EpdLink.startsWith("http")) {
            foundLinks++;

            var sourceTd = row.find("td").eq(1);
            if (result.Source === "Ahlsell") {
                sourceTd.text(result.Source || "").removeClass("text-warning").addClass("text-info");
            } else if (result.Source) {
                sourceTd.text(result.Source).removeClass("text-info").addClass("text-warning");
            } else {
                sourceTd.text("").removeClass("text-info text-warning");
            }

            var epdTd = row.find("td").eq(2);
            epdTd.append(
                $("<a>").addClass("epdLink fs-6 text-wrap").attr("href", result.EpdLink).attr("target", "_blank").text(result.EpdLink)
            );
            var copyTd = row.find("td").eq(3);
            copyTd.empty().append(
                $("<button>").addClass("copyBtn").css({ border: "none", background: "none", cursor: "pointer" })
                    .append($("<img>").attr("src", "/images/copy-white.svg").addClass("ms-2"))
            );
        } else {
            failedLinks++;
            row.find("td").eq(2).text(result.EpdLink || "Ej hittad").addClass("text-danger");
            row.find("td").eq(3).empty();
        }

        updateLoadingbar()

        // Update hidden input
        var existing = $("#resultsInput").val();
        var arr = existing ? JSON.parse(existing) : [];
        arr.push(result);
        $("#resultsInput").val(JSON.stringify(arr));
    };

    currentSSE.addEventListener("done", function () {
        currentSSE.close();
        currentSSE = null;
        $("#downloadForm").show();
    });

    currentSSE.onerror = function () {
        $("#results").append("<tr><td colspan='2'>Fel vid hämtning</td></tr>");
        currentSSE.close();
        currentSSE = null;
    };
}

function updateLoadingbar() {
    var percent = Math.round(((foundLinks + failedLinks) / totalLinks) * 100);
    $("#progressBar").css("width", percent + "%");
    $("#progressBarText").text(`${percent} %`);
    $("#progressText").html(`
        <span class="me-3">Hämtade ${foundLinks + failedLinks} av ${totalLinks} länkar</span>
        <span class="text-success me-3">Hittade: ${foundLinks}</span>
        <span class="text-danger me-3">Misslyckade: ${failedLinks}</span>
    `);

    if (percent >= 0 && percent <= 11) {
        document.getElementById("loadingContainer").scrollIntoView({
            behavior: "smooth",
            block: "start"
        });
    }
}
$("#downloadExcelForm").submit(function (e) {
    e.preventDefault();
    var data = collectResultsFromTable();

    $.ajax({
        url: "/Home/DownloadExcel",
        type: "POST",
        contentType: "application/json",
        data: data,
        xhrFields: { responseType: 'blob' },
        success: function (blob) {
            var link = document.createElement('a');
            link.href = window.URL.createObjectURL(blob);
            link.download = "epd_links.xlsx";
            link.click();
        },
        error: function () {
            alert("Fel vid skapande av Excel-fil");
        }
    });
});
function collectResultsFromTable() {
    var arr = [];
    $("#results tr").each(function () {
        var en = $(this).find("td:first").text().trim();
        var source = $(this).find("td").eq(1).text().trim();
        var link = $(this).find("td").eq(2).find("a").attr("href") || $(this).find("td:last").text().trim();
        arr.push({ ENumber: en, Source: source, EpdLink: link });
    });
    return JSON.stringify(arr);
}

$("input[name='inputType']").change(function () {
    // Scroll the container div into view
    document.querySelector(".mx-auto").scrollIntoView({
        behavior: "smooth",
        block: "center"
    });
});

function showCheckmark() {
    // Show green checkmark when a file is selected
    $("#fileInput").on("change", function () {
        if (this.files.length > 0) {
            $("#fileCheck").show();
        } else {
            $("#fileCheck").hide();
        }
    });
}

// Copy buttons
function initCopyButtons() {
    $("#results").on("click", ".copyBtn", function () {
        var btn = $(this);
        var row = btn.closest("tr");
        var link = row.find("a.epdLink").attr("href");
        if (!link) return;

        navigator.clipboard.writeText(link).then(function () {
            let floatMsg = $("<span>")
                .addClass("copyCheckFloating show")
                .text("Kopierat ✔")
                .appendTo("body");

            // positionera mitt över knappen
            let rect = btn[0].getBoundingClientRect();
            floatMsg.css({
                left: rect.left + window.scrollX + rect.width / 2 + 20 + "px",
                top: rect.top + window.scrollY + "px",
                transform: "translateX(-50%)" 
            });

            setTimeout(() => floatMsg.remove(), 1200);
        }).catch(function (err) {
            console.error("Kunde inte kopiera: ", err);
        });
    });
}