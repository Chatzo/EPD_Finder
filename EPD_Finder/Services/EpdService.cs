using EPD_Finder.Models;
using EPD_Finder.Services.IServices;
using HtmlAgilityPack;

namespace EPD_Finder.Services
{
    public class EpdService : IEpdService
    {
        private readonly HttpClient _client;
        private const string BaseUrl = "https://www.e-nummersok.se/infoDocs/EPD/";
        public EpdService(HttpClient client)
        {
            _client = client;
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
        //public async Task<List<ArticleResult>> GetEpdLinksAsync(List<string> eNumbers)
        //{
        //    var results = new List<ArticleResult>();

        //    // Hämta EPD-indexsidan
        //    var html = await _client.GetStringAsync(BaseUrl);
        //    var doc = new HtmlDocument();
        //    doc.LoadHtml(html);

        //    // Hämta alla länkar
        //    var links = doc.DocumentNode.SelectNodes("//a[contains(@href,'/infoDocs/EPD/')]");

        //    if (links == null) return results;

        //    foreach (var en in eNumbers)
        //    {
        //        var match = links
        //            .Select(a => a.GetAttributeValue("href", ""))
        //            .FirstOrDefault(href => href.Contains(en));

        //        if (match != null)
        //        {
        //            results.Add(new ArticleResult
        //            {
        //                ENumber = en,
        //                EpdLink = match.StartsWith("/") ? BaseUrl + match : match
        //            });
        //        }
        //    }

        //    return results;
        //}
        public async Task<ArticleResult> GetEpdLinkByEnumberAsync(string enumber)
        {
            var result = new ArticleResult
            {
                ENumber = enumber,
                EpdLink = "Ej hittad"
            };

            try
            {
                // Hämta indexsidan med alla EPD-länkar
                var html = await _client.GetStringAsync(BaseUrl);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Hämta alla <a>-taggar med EPD-länkar
                var links = doc.DocumentNode.SelectNodes("//a[@href]");

                if (links != null)
                {
                    // Leta efter länken som innehåller e-numret
                    var match = links
                        .Select(a => a.GetAttributeValue("href", ""))
                        .FirstOrDefault(href => href.Contains(enumber));

                    if (match != null)
                    {
                        // Absolut URL
                        result.EpdLink = match.StartsWith("/") ? BaseUrl + match : match;
                    }
                }
            }
            catch (Exception ex)
            {
                result.EpdLink = "Fel vid hämtning: " + ex.Message;
            }

            return result;
        }
        //    public async Task<ArticleResult> ScrapeEnumber(string eNumber)
        //    {
        //        var result = new ArticleResult
        //        {
        //            ENumber = eNumber,
        //            EpdLink = "Ej Hittad"
        //        };

        //        try
        //        {
        //            var searchUrl = $"https://www.e-nummersok.se/Search?searchText={eNumber}";
        //            var html = await _client.GetStringAsync(searchUrl);

        //            var doc = new HtmlAgilityPack.HtmlDocument();
        //            doc.LoadHtml(html);

        //            // Hämta första länken som innehåller /infoDocs/EPD/
        //            var linkNode = doc.DocumentNode
        //                .SelectNodes("//a[@href]")
        //                ?.Select(a => a.GetAttributeValue("href", ""))
        //                .FirstOrDefault(href => href.Contains("/infoDocs/EPD/"));

        //            if (!string.IsNullOrEmpty(linkNode))
        //            {
        //                // Gör absolut URL om den börjar med /
        //                result.EpdLink = linkNode.StartsWith("/")
        //                    ? "https://www.e-nummersok.se" + linkNode
        //                    : linkNode;
        //            }
        //            else
        //            {
        //                result.EpdLink = "EPD saknas / Ej Hittad";
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            result.EpdLink = "Fel vid hämtning: " + ex.Message;
        //        }

        //        return result;
        //    }
    }
}
