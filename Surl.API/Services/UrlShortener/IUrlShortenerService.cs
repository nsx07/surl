using Surl.API.RequestResponse.Dto;
using Surl.API.RequestResponse.ViewModel;

namespace Surl.API.Services.UrlShortener
{
    public interface IUrlShortenerService
    {
        Task<UrlShortenedDto> ShortenUrlAsync(ShortenUrlViewModel shortenUrlRequest);
        string FormatUrlShortened(string code);
    }
}
