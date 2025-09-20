using EPD_Finder.Models;
using EPD_Finder.Services.IServices;

namespace EPD_Finder.Services
{
    public class EpdService : IEpdService
    {
        private readonly HttpClient _client;
        public EpdService(HttpClient client)
        {
            _client = client;
        }
        public List<string> ParseInput(string enNumbers, IFormFile file)
        {
            var list = new List<string>();

            // Från textarea
            if (!string.IsNullOrWhiteSpace(enNumbers))
            {
                list.AddRange(enNumbers
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

            // Unika värden
            return list.Distinct().ToList();
        }
        public async Task<ArticleResult> ScrapeEnumber(string enNumber)
        {
            var result = new ArticleResult
            {
                ArticleNumber = enNumber,
                ENumber = enNumber,
                Status = "Ej implementerad"
            };

            try
            {
                var searchUrl = $"https://www.e-nummersok.se/Search?searchText={enNumber}";
                var html = await _client.GetStringAsync(searchUrl);

                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(html);

                var links = doc.DocumentNode.SelectNodes("//a[contains(@href,'/infoDocs/EPD/')]")
                    ?.Select(a => a.GetAttributeValue("href", ""))
                    .Select(l => l.StartsWith("/") ? "https://www.e-nummersok.se" + l : l)
                    .ToList() ?? new List<string>();

                if (links.Any())
                {
                    result.EpdLinks = links;
                    result.Status = "Hittad";
                }
                else
                {
                    result.Status = "EPD saknas / fallback behövs";
                }
            }
            catch (Exception ex)
            {
                result.Status = "Fel vid hämtning: " + ex.Message;
            }

            return result;
        }
    }
}
