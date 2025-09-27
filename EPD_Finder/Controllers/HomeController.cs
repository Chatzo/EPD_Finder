using ClosedXML.Excel;
using EPD_Finder.Models;
using EPD_Finder.Services.IServices;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace EPD_Finder.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IEpdService _epdService;
        private static ConcurrentDictionary<string, List<string>> _jobs = new();
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
        public IActionResult CreateJob(IFormFile file, string eNumbers)
        {
            var list = _epdService.ParseInput(eNumbers, file);
            if (!list.Any()) return BadRequest("Inga E-nummer hittades.");

            var jobId = Guid.NewGuid().ToString();
            _jobs[jobId] = list;

            return Ok(new { jobId, eNumbers = list });
        }

        [HttpGet]
        public async Task GetResultsStream(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId) || !_jobs.TryGetValue(jobId, out var list))
            {
                Response.StatusCode = 400; // Bad Request
                await Response.WriteAsync("Invalid or missing jobId");
                return;
            }

            Response.ContentType = "text/event-stream";
            Response.Headers.Add("Cache-Control", "no-cache");

            var tasks = list.Select(async num =>
            {
                try
                {
                    var epdLink = await _epdService.TryGetEpdLink(num);
                    return new ArticleResult { ENumber = num, EpdLink = epdLink };
                }
                catch
                {
                    return new ArticleResult { ENumber = num, EpdLink = "Ej hittad" };
                }
            }).ToList();

            while (tasks.Any())
            {
                var finished = await Task.WhenAny(tasks);
                tasks.Remove(finished);

                var result = await finished;
                var json = System.Text.Json.JsonSerializer.Serialize(result);
                await Response.WriteAsync($"data: {json}\n\n");
                await Response.Body.FlushAsync();
            }

            await Response.WriteAsync("event: done\ndata: complete\n\n");
            await Response.Body.FlushAsync();

            _jobs.TryRemove(jobId, out _);
        }



        //[HttpPost]
        //public IActionResult DownloadExcel(List<ArticleResult> results)
        //{
        //    using var workbook = new XLWorkbook();
        //    var ws = workbook.Worksheets.Add("EPD Links");
        //    ws.Cell(1, 1).Value = "E-nummer";
        //    ws.Cell(1, 2).Value = "EPD-länk";

        //    for (int i = 0; i < results.Count; i++)
        //    {
        //        ws.Cell(i + 2, 1).Value = results[i].ENumber;
        //        ws.Cell(i + 2, 2).Value = results[i].EpdLink;
        //    }

        //    using var stream = new MemoryStream();
        //    workbook.SaveAs(stream);
        //    stream.Position = 0;
        //    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "epd_links.xlsx");
        //}
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
