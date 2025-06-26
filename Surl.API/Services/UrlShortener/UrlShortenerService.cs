using Surl.API.RequestResponse.Dto;
using Surl.API.RequestResponse.ViewModel;

namespace Surl.API.Services.UrlShortener
{
    public class UrlShortenerService(IConfiguration config) : IUrlShortenerService
    {
        private const string ALPHABET = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private readonly IConfiguration _configuration = config;
        private const string ROUTE = "r";

        public Task<UrlShortenedDto> ShortenUrlAsync(ShortenUrlViewModel shortenUrlRequest)
        {
            string originalUrl = shortenUrlRequest.Url;
            string shortenedUrl = GenerateShortenedUrl(originalUrl);

            return Task.FromResult(new UrlShortenedDto
            {
                Url = shortenedUrl,
                OriginalUrl = originalUrl,
                Code = shortenedUrl.Split($"/{ROUTE}/").LastOrDefault()!
            });
        }

        private string GenerateShortenedUrl(string originalUrl)
        {
            string code = GenerateUniqueCode(originalUrl);
            string shortenedUrl = $"{GetBaseUrl()}/{ROUTE}/{code}";
            return shortenedUrl;
        }

        private static string GenerateUniqueCode(string url)
        {
            Random random = new();
            char[] codeChars = new char[6];
            for (int i = 0; i < codeChars.Length; i++)
            {
                codeChars[i] = ALPHABET[random.Next(ALPHABET.Length)];
            }
            return new string(codeChars);
        }

        private string GetBaseUrl()
        {
            return _configuration["BaseUrl"] ?? "http://localhost:5148";
        }

        public string FormatUrlShortened(string code)
        {
            return $"{GetBaseUrl()}/{ROUTE}/{code}";
        }
    }
}
