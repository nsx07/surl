using Surl.API.RequestResponse.Dto;
using Surl.API.RequestResponse.ViewModel;

namespace Surl.API.Services.UrlShortener
{
    public interface IUrlShortenerService
    {
        Task<UrlShortenedDto> ShortenUrlAsync(ShortenUrlViewModel shortenUrlRequest);
        Task DeleteShortenUrlAsync(string urlCode);
        Task<string> GetLinkAsync(string code, IHeaderDictionary headers, string? ipAddress);
        Task ProcessClicksAsync(UrlAccessProcessingMessage message);
    }
}
