using HtmlAgilityPack;
using System.Text.Json;

namespace EPD_Finder.Services
{
    public class OnninenSearch
    {
        private readonly HttpClient _client;
        private readonly ILogger<EpdService> _logger;
        public OnninenSearch(HttpClient client, ILogger<EpdService> logger)
        {
            _client = client;
            _logger = logger;
        }
        public async Task<string> TryGetEpdLink(string eNumber)
        {
            string productCode = await TryGetProductCode(eNumber);
            if (string.IsNullOrEmpty(productCode))
                return null;
            string epdUrl = await TryGetEPDLinkFromProduct(productCode);
            return epdUrl;
        }
        private async Task<string> TryGetProductCode(string eNumber)
        {
            if (string.IsNullOrWhiteSpace(eNumber))
                throw new ArgumentException("E-nummer måste anges.", nameof(eNumber));

            string quickSearchUrl = $"https://www.onninen.se/rest/v2/search/suggestions?term={eNumber}";

            string json;
            try
            {
                json = await _client.GetStringAsync(quickSearchUrl);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"Fel vid hämtning av sökresultat: {ex.Message}");
                return null;
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var firstProduct = root.GetProperty("products").EnumerateArray().FirstOrDefault();
            if (firstProduct.ValueKind != JsonValueKind.Undefined)
            {
                string code = firstProduct.GetProperty("code").GetString();
                return code;
            }

            return null;
        }

        private async Task<string> TryGetEPDLinkFromProduct(string productCode)
        {
            if (string.IsNullOrWhiteSpace(productCode))
                throw new ArgumentException("Produktkod måste anges.", nameof(productCode));

            string apiUrl = $"https://www.onninen.se/rest/v1/product/{productCode}";

            try
            {
                var json = await _client.GetStringAsync(apiUrl);

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Leta efter dokumentfält
                if (root.TryGetProperty("documents", out var docs))
                {
                    foreach (var docItem in docs.EnumerateArray())
                    {
                        if (docItem.TryGetProperty("documents", out var innerDocs))
                        {
                            foreach (var innerDoc in innerDocs.EnumerateArray())
                            {
                                if (innerDoc.TryGetProperty("name", out var name) &&
                                    name.GetString()?.Contains("Miljövarudeklaration", StringComparison.OrdinalIgnoreCase) == true &&
                                    innerDoc.TryGetProperty("link", out var link))
                                {
                                    return link.GetString();
                                }
                            }
                        }
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"Fel vid hämtning från produkt-API: {ex.Message}");
                return null;
            }

            _logger.LogError("Ingen EPD-länk hittades i produktens API-data.");
            return null;
        }
    }
}
