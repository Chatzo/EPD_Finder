using EPD_Finder.Models;

namespace EPD_Finder.Services.IServices
{
    public interface IEpdService
    {
        List<string> ParseInput(string eNumbers, IFormFile file);
        Task<string> TryGetEpdLink(string eNumber);

    }
}
