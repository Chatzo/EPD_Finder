using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Vml;
using EPD_Finder.Models;
using EPD_Finder.Services.IServices;

namespace EPD_Finder.Services
{
    public class EpdService : IEpdService
    {
        private readonly HttpClient _client;
        private readonly ILogger<EpdService> _logger;
        private readonly AhlsellSearch _ahlsell;
        private readonly EnummersokSearch _enummersok;
        private readonly OnninenSearch _onninen;
        private readonly SolarSearch _solar;

        public EpdService(HttpClient client, ILogger<EpdService> logger)
        {
            _client = client;
             _logger = logger;
            _client.Timeout = TimeSpan.FromSeconds(30);
            _client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                "AppleWebKit/537.36 (KHTML, like Gecko) " +
                "Chrome/120.0 Safari/537.36"
            );
            _ahlsell = new AhlsellSearch(_client, _logger);
            _enummersok = new EnummersokSearch(_client, _logger);
            _onninen = new OnninenSearch(_client, _logger);
            _solar = new SolarSearch(_client, _logger);
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
                        var line = reader.ReadLine()?.Trim();
                        if (string.IsNullOrWhiteSpace(line)) continue; //skip if null or empty
                        if (!line.Any(char.IsDigit)) continue; // Skip lines without digits
                        list.Add(line);
                    }
                }
                else if (file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
                    var ws = workbook.Worksheets.First();
                    foreach (var row in ws.RowsUsed())
                    {
                        var val = row.Cell(1).GetValue<string>().Trim();
                        if (string.IsNullOrWhiteSpace(val)) continue; //skip if null or empty
                        if (!val.Any(char.IsDigit)) continue; // Skip rows without digits
                        list.Add(val);
                    }
                }
            }

            return list.Distinct().ToList();
        }

        public async Task<ArticleResult> TryGetEpdLink(string eNumber, List<string> selectedSources)
        {
            if (selectedSources.Contains("E-nummersök"))
            {
                var pdfUrl = await _enummersok.TryGetEpdLink(eNumber);
                if (await IsLinkValid(pdfUrl))
                    return new ArticleResult { ENumber = eNumber, Source = "E-nummersök", EpdLink = pdfUrl };
            }
            if (selectedSources.Contains("Ahlsell"))
            {
                var pdfUrl = await _ahlsell.TryGetEpdLink(eNumber);
                if (await IsLinkValid(pdfUrl))
                    return new ArticleResult { ENumber = eNumber, Source = "Ahlsell", EpdLink = pdfUrl };
            }

            if (selectedSources.Contains("Solar"))
            {
                var pdfUrl = await _solar.TryGetEpdLink(eNumber);
                if (await IsLinkValid(pdfUrl))
                    return new ArticleResult { ENumber = eNumber, Source = "Solar", EpdLink = pdfUrl };
            }

            if (selectedSources.Contains("Onninen"))
            {
                var pdfUrl = await _onninen.TryGetEpdLink(eNumber);
                if (await IsLinkValid(pdfUrl))
                    return new ArticleResult { ENumber = eNumber, Source = "Onninen", EpdLink = pdfUrl };
            }

            throw new ArgumentException("Ej hittad");
        }

        private async Task<bool> IsLinkValid(string url)
        {
            try
            {
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
    }
}
