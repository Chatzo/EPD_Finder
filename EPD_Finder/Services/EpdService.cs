using EPD_Finder.Services.IServices;
using HtmlAgilityPack;
using Microsoft.Playwright;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EPD_Finder.Services
{
    public class EpdService : IEpdService
    {
        private readonly HttpClient _client;
        private readonly ILogger<EpdService> _logger;
        private const string BaseUrl = "https://www.e-nummersok.se/infoDocs/EPD/";
        public EpdService(HttpClient client, ILogger<EpdService> logger)
        {
            _client = client;
            _client.DefaultRequestHeaders.Add("User-Agent", "C# App");
            _logger = logger;
        }
        public List<string> ParseInput(string eNumbers, IFormFile file)
        {
            var list = new List<string>();

            // Från textarea
            if (!string.IsNullOrWhiteSpace(eNumbers))
            {
                list.AddRange(eNumbers
                    .Split(new[] { ',', ';', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim()));
            }

            // Från fil (Excel eller CSV)
            if (file != null && file.Length > 0)
            {
                using var stream = file.OpenReadStream();
                if (file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    using var reader = new StreamReader(stream);
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (!string.IsNullOrWhiteSpace(line))
                            list.Add(line.Trim());
                    }
                }
                else if (file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
                    var ws = workbook.Worksheets.First();
                    foreach (var row in ws.RowsUsed())
                    {
                        var val = row.Cell(1).GetValue<string>();
                        if (!string.IsNullOrWhiteSpace(val))
                            list.Add(val.Trim());
                    }
                }
            }

            return list.Distinct().ToList();
        }

        public async Task<string> TryGetEpdLink(string eNumber)
        {
            //from AHlsell
            string pdfUrl = await TryGetAhlsellEpdLink(eNumber);
            if (await IsLinkValid(pdfUrl))
            {
                return pdfUrl;
            }
            //from e-nummersok
            //pdfUrl = await TryGetEnummersokEpdLink(eNumber);
            //if (await IsLinkValid(pdfUrl))
            //{
            //    return pdfUrl;
            //}
            throw new ArgumentException("Ej hittad");
        }
        //private async Task<string> GetEnummersokUrl(string eNumber)
        //{
        //    if (string.IsNullOrWhiteSpace(eNumber))
        //        return null;

        //    try
        //    {
        //        string apiUrl = $"https://www.e-nummersok.se/ApiSearch/Suggest?ActiveOnly=true&PicsOnly=true&Query={eNumber}&Sort=1";
        //        var response = await _client.GetStringAsync(apiUrl);

        //        using var doc = JsonDocument.Parse(response);
        //        var root = doc.RootElement;

        //        if (root.GetProperty("Status").GetString() != "OK")
        //            return null;

        //        var suggestions = root.GetProperty("Data").GetProperty("ProductSuggestionRows");
        //        if (suggestions.GetArrayLength() == 0)
        //            return null;

        //        var firstProduct = suggestions[0];
        //        string url = firstProduct.GetProperty("Url").GetString()!;
        //        return url;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Fel vid hämtning av URL för e-nummer {Enummer}", eNumber);
        //        return null;
        //    }
        //}

        //private async Task<string?> GetSupplierIdFromEnumberWithAsync(string url, string eNumber)
        //{
        //    if (string.IsNullOrWhiteSpace(url))
        //        return null;

        //    try
        //    {
        //        // Plocka ut sista numret i urlen med regex
        //        var match = Regex.Match(url, @"-(\d+)$");
        //        return match.Success ? match.Groups[1].Value : null;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Fel vid hämtning av sista numret från produkt-URL för e-nummer {Enummer}", eNumber);
        //        return null;
        //    }
        //}

        private async Task<bool> IsLinkValid(string url)
        {
            try
            {
                // Check if the link exists
                var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
        //private async Task<string> TryGetEnummersokEpdLink(string eNumber)
        //{
        //    var url = await GetEnummersokUrl(eNumber);
        //    var supplierID = await GetSupplierIdFromEnumberWithAsync(url, eNumber);
        //    if (supplierID == null)
        //        throw new ArgumentException("Ej hittad");
        //    string pdfUrl = $"{BaseUrl}EPD_{supplierID}_{eNumber}.pdf";
        //    if (await IsLinkValid(pdfUrl))
        //    {
        //        return pdfUrl;
        //    }
        //    pdfUrl = $"{BaseUrl}EPD_{eNumber}.pdf";
        //    return pdfUrl;
        //}
        private async Task<string> TryGetAhlsellEpdLink(string eNumber)
        {
            string productUrl = await TryGetAhlsellProductUrl(eNumber);
            string epdUrl = await TryGetEPDLinkFromAhlsellProductPage(productUrl);
            return epdUrl;
        }
        private async Task<string> TryGetAhlsellProductUrl(string eNumber)
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

            // 2. Parse HTML
            var searchDoc = new HtmlDocument();
            searchDoc.LoadHtml(searchHtml);

            // 3. Hitta första produktlänk
            var productNode = searchDoc.DocumentNode.SelectSingleNode("//a[contains(@href, '/products/')]");
            if (productNode == null)
            {
                _logger.LogError("Ingen produkt hittades i sökresultatet.");
                return null;
            }

            // 4. Returnera full URL till produktsidan
            string productUrl = "https://www.ahlsell.se" + productNode.GetAttributeValue("href", "");
            return productUrl;
        }
        private async Task<string> TryGetEPDLinkFromAhlsellProductPage(string productUrl)
        {
            if(string.IsNullOrWhiteSpace(productUrl))
            throw new ArgumentException("Produkt-URL måste anges.", nameof(productUrl));

            // 1. Hämta produktsidan
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

            // 2. Parse HTML
            var productDoc = new HtmlDocument();
            productDoc.LoadHtml(productHtml);

            // 3. Leta efter EPD-länk (länk som innehåller 'infoDocs/EPD')
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
