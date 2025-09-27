$(function () {
    $("#epdForm").submit(function (e) {
        e.preventDefault(); // förhindra standardformulär
        var formData = new FormData(this);

        $.ajax({
            url: '/Home/Results',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (html) {
                $("#results").html(html); // infoga partial view
            },
            error: function () {
                $("#results").html("<p>Fel vid hämtning av resultat.</p>");
            }
        });
    });

    $("#results").on("click", ".copyBtn", function () {
        var btn = $(this);
        var link = btn.closest("td").find("a.epdLink").attr("href");
        var check = btn.closest("td").find(".copyCheck");

        navigator.clipboard.writeText(link).then(function () {
            // Ta bort klassen för att kunna animera igen
            check.removeClass("show");
            void check[0].offsetWidth; // tvinga reflow
            check.addClass("show");
        }).catch(function (err) {
            console.error("Kunde inte kopiera: ", err);
        });
    });
});