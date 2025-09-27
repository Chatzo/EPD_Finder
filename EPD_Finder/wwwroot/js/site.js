$(function () {
    initInputSwitch();
    initFormSubmit();
    initCopyButtons();
});

// Byt mellan text och fil
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

        // Skicka alltid via CreateJob för att få jobId
        var formData = new FormData(this);
        $.ajax({
            url: "/Home/CreateJob",
            type: "POST",
            data: formData,
            processData: false,
            contentType: false,
            success: function (res) {
                startSSE(res.jobId);
            },
            error: function (xhr) {
                var msg = xhr.responseJSON?.message || "Fel vid skapande av jobb";
                alert(msg);
            }
        });
    });
}

// Starta SSE
function startSSE(jobId) {
    var url = "/Home/GetResultsStream?jobId=" + jobId;
    var evtSource = new EventSource(url);

    evtSource.onmessage = function (e) {
        var result = JSON.parse(e.data);
        addResultRow(result);
    };

    evtSource.addEventListener("done", function () {
        evtSource.close();
    });

    evtSource.onerror = function () {
        $("#results").append("<tr><td colspan='2'>Fel vid hämtning</td></tr>");
        evtSource.close();
    };
}

// Lägg till rad i tabellen och uppdatera hidden input
function addResultRow(result) {
    var row = $("<tr>");
    row.append($("<td>").text(result.ENumber));

    if (result.EpdLink && result.EpdLink.startsWith("http")) {
        var cell = $("<td>").css("position", "relative");
        cell.append(
            $("<a>").addClass("epdLink").attr("href", result.EpdLink).attr("target", "_blank").text(result.EpdLink)
        );
        cell.append(
            $("<button>").addClass("copyBtn").css({ border: "none", background: "none", cursor: "pointer" })
                .append($("<img>").attr("src", "/images/copy.svg"))
                .append($("<span>").addClass("copyCheck text-end").text("Kopierat ✔"))
        );
        row.append(cell);
    } else {
        row.append($("<td>").css("color", "red").text(result.EpdLink));
    }

    $("#results").append(row);

    var existing = $("#resultsInput").val();
    var arr = existing ? JSON.parse(existing) : [];
    arr.push(result);
    $("#resultsInput").val(JSON.stringify(arr));
}

// Copy-knappar
function initCopyButtons() {
    $("#results").on("click", ".copyBtn", function () {
        var btn = $(this);
        var link = btn.closest("td").find("a.epdLink").attr("href");
        var check = btn.closest("td").find(".copyCheck");

        navigator.clipboard.writeText(link).then(function () {
            check.removeClass("show");
            void check[0].offsetWidth; // reflow för animation
            check.addClass("show");
        }).catch(function (err) {
            console.error("Kunde inte kopiera: ", err);
        });
    });
}
