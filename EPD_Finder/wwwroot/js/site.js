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
});