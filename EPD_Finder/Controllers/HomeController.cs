using ClosedXML.Excel;
using EPD_Finder.Models;
using EPD_Finder.Services.IServices;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace EPD_Finder.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IEpdService _epdService;
        public HomeController(ILogger<HomeController> logger, IEpdService epdService)
        {
            _logger = logger;
            _epdService = epdService;
        }

        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Results(string eNumbers, IFormFile file)
        {
            var list = _epdService.ParseInput(eNumbers, file);

            if (!list.Any())
                return PartialView("_Results", new List<ArticleResult>());

            List<ArticleResult> results = new List<ArticleResult>();
            foreach (string num in list)
            {
                ArticleResult res = await _epdService.GetEpdLinkByEnumberAsync(num);
                if (res != null)
                    results.Add(res);
            }
            //foreach (var en in list)
            //{
            //    if (_cache.ContainsKey(en))
            //    {
            //        results.Add(_cache[en]);
            //        continue;
            //    }

            //    var result = await _epdService.ScrapeEnumber(en);
            //    results.Add(result);
            //    _cache[en] = result;

            //    await Task.Delay(700); // rate-limit
            //}

            return PartialView("_Results", results);
        }
        [HttpPost]
        public IActionResult DownloadExcel(List<ArticleResult> results)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("EPD Links");
            ws.Cell(1, 1).Value = "E-nummer";
            ws.Cell(1, 2).Value = "EPD-länk";

            for (int i = 0; i < results.Count; i++)
            {
                ws.Cell(i + 2, 1).Value = results[i].ENumber;
                ws.Cell(i + 2, 2).Value = results[i].EpdLink;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "epd_links.xlsx");
        }
        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult License()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
