using EPD_Finder.Models;

namespace EPD_Finder.Services.IServices
{
    public interface IEpdService
    {
        List<string> ParseInput(string eNumbers, IFormFile file);
        Task<ArticleResult> TryGetEpdLink(string eNumber, List<string> selectedSources);

    }
}
