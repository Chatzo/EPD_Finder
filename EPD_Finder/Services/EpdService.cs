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
        private readonly SolarSearch _solar;
        private readonly SoneparSearch _sonepar;
        private readonly RexelSearch _rexel;

        public EpdService(HttpClient client, 
            ILogger<EpdService> logger, 
            AhlsellSearch ahlsell, 
            EnummersokSearch enummersok,
            SolarSearch solar,
            SoneparSearch sonepar,
            RexelSearch rexel
            )
        {
            _client = client;
             _logger = logger;
            _ahlsell = ahlsell;
            _enummersok = enummersok;
            _solar = solar;
            _sonepar = sonepar;
            _rexel = rexel;
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
            if (selectedSources.Contains("Sonepar"))
            {
                var pdfUrl = await _sonepar.TryGetEpdLink(eNumber);
                if (await IsLinkValid(pdfUrl))
                    return new ArticleResult { ENumber = eNumber, Source = "Sonepar", EpdLink = pdfUrl };
            }
            if (selectedSources.Contains("Rexel"))
            {
                var pdfUrl = await _rexel.TryGetEpdLink(eNumber);
                if (await IsLinkValid(pdfUrl))
                    return new ArticleResult { ENumber = eNumber, Source = "Rexel", EpdLink = pdfUrl };
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
