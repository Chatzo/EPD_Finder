$(function () {
    initInputSwitch();
    initFormSubmit();
    initCopyButtons();
});

// Global counter
var totalLinks = 0;
var foundLinks = 0;
var failedLinks = 0;

// Switch input type
function initInputSwitch() {
    $("input[name='inputType']").change(function () {
        if ($(this).val() === "file") {
            $("#fileInputDiv").show();
            $("#textInputDiv").hide();
        } else {
            $("#fileInputDiv").hide();
            $("#textInputDiv").show();
        }
    });
}

// Form submission
function initFormSubmit() {
    $("#epdForm").submit(function (e) {
        e.preventDefault();
        $("#results").empty();
        $("#resultsInput").val("[]");
        $("#linksCounter").remove();

        var formData = new FormData(this);
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
                // Add counter display
                $("#epdForm").after(`<div id="linksCounter" class="mt-2"></div>`);
                updateCounter();
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
    eNumbers.forEach(num => {
        var row = $("<tr>").attr("data-enumber", num);
        row.append($("<td>").text(num));
        row.append($("<td>").text("Hämtar..."));
        $("#results").append(row);
    });

    startSSE(jobId);
}

// SSE
function startSSE(jobId) {
    var url = "/Home/GetResultsStream?jobId=" + jobId;
    var evtSource = new EventSource(url);

    evtSource.onmessage = function (e) {
        var result = JSON.parse(e.data);
        var row = $("#results").find(`tr[data-enumber='${result.ENumber}']`);
        row.find("td:last").empty();

        if (result.EpdLink && result.EpdLink.startsWith("http")) {
            foundLinks++;
            var cell = $("<td>").css("position", "relative");
            cell.append(
                $("<a>").addClass("epdLink").attr("href", result.EpdLink).attr("target", "_blank").text(result.EpdLink)
            );
            cell.append(
                $("<button>").addClass("copyBtn").css({ border: "none", background: "none", cursor: "pointer" })
                    .append($("<img>").attr("src", "/images/copy.svg"))
                    .append($("<span>").addClass("copyCheck text-end").text("Kopierat ✔"))
            );
            row.find("td:last").replaceWith(cell);
        } else {
            failedLinks++;
            row.find("td:last").text(result.EpdLink).css("color", "red");
        }

        // Update counter with colors
        updateCounter();

        // Update hidden input
        var existing = $("#resultsInput").val();
        var arr = existing ? JSON.parse(existing) : [];
        arr.push(result);
        $("#resultsInput").val(JSON.stringify(arr));
    };

    evtSource.addEventListener("done", function () {
        evtSource.close();
        $("#linksCounter").append("<strong>( Sökning slutförd ✅ )</strong>");
        $("#downloadForm").show();
    });

    evtSource.onerror = function () {
        $("#results").append("<tr><td colspan='2'>Fel vid hämtning</td></tr>");
        evtSource.close();
    };
}

// Update counter display
function updateCounter() {
    $("#linksCounter").html(`
        <hr/>
        <span style="margin-right: 15px;"><strong>Sökresultat från ${totalLinks} e-nummer: </strong></span>
        <span style="color:green; margin-right: 15px;"><strong>Hittade: ${foundLinks}</strong></span>
        <span style="color:red; margin-right: 15px;"><strong>Misslyckade: ${failedLinks}</strong></span>
    `);
}

// Copy buttons
function initCopyButtons() {
    $("#results").on("click", ".copyBtn", function () {
        var btn = $(this);
        var link = btn.closest("td").find("a.epdLink").attr("href");
        var check = btn.closest("td").find(".copyCheck");

        navigator.clipboard.writeText(link).then(function () {
            check.removeClass("show");
            void check[0].offsetWidth;
            check.addClass("show");
        }).catch(function (err) {
            console.error("Kunde inte kopiera: ", err);
        });
    });
}
