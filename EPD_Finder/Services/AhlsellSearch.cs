using HtmlAgilityPack;

namespace EPD_Finder.Services
{
    public class AhlsellSearch
    {
        private readonly HttpClient _client;
        private readonly ILogger<EpdService> _logger;
        public AhlsellSearch(HttpClient client, ILogger<EpdService> logger)
        {
            _client = client;
            _logger = logger;
        }
        public async Task<string> TryGetEpdLink(string eNumber)
        {
            string productUrl = await TryGetProductUrl(eNumber);
            string epdUrl = await TryGetEPDLinkFromProductPage(productUrl);
            return epdUrl;
        }
        private async Task<string> TryGetProductUrl(string eNumber)
        {
            if (string.IsNullOrWhiteSpace(eNumber))
                throw new ArgumentException("E-nummer måste anges.", nameof(eNumber));

            string quickSearchUrl = $"https://www.ahlsell.se/QuickSearch?parameters.SearchPhrase={eNumber}";

            string searchHtml;
            try
            {
                searchHtml = await _client.GetStringAsync(quickSearchUrl);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"Fel vid hämtning av sökresultat: {ex.Message}");
                return null;
            }

            var searchDoc = new HtmlDocument();
            searchDoc.LoadHtml(searchHtml);

            var productNode = searchDoc.DocumentNode.SelectSingleNode("//a[contains(@href, '/products/')]");
            if (productNode == null)
            {
                _logger.LogError("Ingen produkt hittades i sökresultatet.");
                return null;
            }

            string productUrl = "https://www.ahlsell.se" + productNode.GetAttributeValue("href", "");
            return productUrl;
        }
        private async Task<string> TryGetEPDLinkFromProductPage(string productUrl)
        {
            if (string.IsNullOrWhiteSpace(productUrl))
                throw new ArgumentException("Produkt-URL måste anges.", nameof(productUrl));

            string productHtml;
            try
            {
                productHtml = await _client.GetStringAsync(productUrl);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"Fel vid hämtning av produktsidan: {ex.Message}");
                return null;
            }

            var productDoc = new HtmlDocument();
            productDoc.LoadHtml(productHtml);

            var epdNode = productDoc.DocumentNode
                .SelectSingleNode("//a[contains(@href, 'infoDocs/EPD')]");

            if (epdNode != null)
            {
                string epdUrl = epdNode.GetAttributeValue("href", "");
                return epdUrl.StartsWith("http") ? epdUrl : "https://www.e-nummersok.se" + epdUrl;
            }
            _logger.LogError("Ingen EPD-länk hittades på produktsidan.");
            return null;
        }
    }
}
