using EPD_Finder.Models;

namespace EPD_Finder.Services.IServices
{
    public interface IEpdService
    {
        List<string> ParseInput(string enNumbers, IFormFile file);
        Task<ArticleResult> ScrapeEnumber(string eNumber);
    }
}
