using EPD_Finder.Models;

namespace EPD_Finder.Services.IServices
{
    public interface IEpdService
    {
        List<string> ParseInput(string eNumbers, IFormFile file);
        //Task<ArticleResult> GetEpdLinkByEnumberAsync(string enumber);
        Task<string> TryGetEpdLink(string eNumber);
        //Task<List<ArticleResult>> GetEpdLinksAsync(List<string> eNumbers);
        //Task<ArticleResult> ScrapeEnumber(string eNumber);
    }
}
