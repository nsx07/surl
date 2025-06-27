using Microsoft.EntityFrameworkCore;
using Surl.API.Data;
using Surl.API.Model;
using Surl.API.RequestResponse.Dto;
using Surl.API.RequestResponse.ViewModel;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Surl.API.Services.UrlShortener
{
    public class UrlShortenerService(IConfiguration config, AppDbContext context) : IUrlShortenerService
    {
        private const string ALPHABET = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private readonly IConfiguration _configuration = config;
        private const string ROUTE = "r";

        public async Task<UrlShortenedDto> ShortenUrlAsync(ShortenUrlViewModel request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Url) || Uri.TryCreate(request.Url, UriKind.Absolute, out _))
            {
                throw new ArgumentException("Invalid URL provided.", nameof(request));
            }

            var httpsFragment = new Regex(@"^https?://", RegexOptions.IgnoreCase);
            if (!httpsFragment.IsMatch(request.Url))
            {
                request.Url = "http://" + request.Url;
            }

            var existingUrl = await context.UrlShorten
                .FirstOrDefaultAsync(u => u.OriginalUrl == request.Url);

            if (existingUrl != null)
            {
                return new UrlShortenedDto
                {
                    OriginalUrl = existingUrl.OriginalUrl,
                    Url = FormatUrlShortened(existingUrl.Code)
                };
            }

            using var transaction = await context.Database.BeginTransactionAsync();

            string originalUrl = request.Url;
            string shortenedUrl = GenerateShortenedUrl(originalUrl);
            UrlShorten shorten = UrlShorten.CreateOne(originalUrl, shortenedUrl, code: shortenedUrl.Split($"/{ROUTE}/").LastOrDefault()!);
            context.UrlShorten.Add(shorten);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new UrlShortenedDto()
            {
                Url = shortenedUrl,
                Code = shorten.Code,
                OriginalUrl = shorten.OriginalUrl,
            };
        }

        public async Task DeleteShortenUrlAsync(string urlCode)
        {
            if (string.IsNullOrEmpty(urlCode))
            {
                throw new ArgumentException("Please inform a URL or code", nameof(urlCode));
            }

            var existent = await context.UrlShorten
                .Where(x => urlCode.Equals(x.OriginalUrl) || urlCode.Equals(x.Code))
                .FirstOrDefaultAsync();

            if (existent == null)
            {
                throw new KeyNotFoundException("URL not found.");
            }

            context.UrlShorten.Remove(existent);
            await context.SaveChangesAsync();
        }

        public async Task<string> GetLinkAsync(string code, IHeaderDictionary headers, string? ipAddress)
        {
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentException("Code cannot be null or empty.", nameof(code));
            }

            var url = await context.UrlShorten.FirstOrDefaultAsync(u => u.Code == code);

            if (url == null)
            {
                throw new KeyNotFoundException("URL not found.");
            }

            var access = new UrlShortenAccess
            {
                UrlShorten = url,
                IpAddress = ipAddress,
                UrlShortenId = url.Id,
                AccessedAt = DateTime.UtcNow,
                HeadersRaw = JsonSerializer.Serialize(headers),
            };

            url.LastAccessedAt = access.AccessedAt;
            url.ClickCount++;

            context.Update(url);
            context.Add(access);
            await context.SaveChangesAsync();

            return url.OriginalUrl;
        }

        private string FormatUrlShortened(string code)
        {
            return $"{GetBaseUrl()}/{ROUTE}/{code}";
        }

        private string GenerateShortenedUrl(string originalUrl)
        {
            return FormatUrlShortened(GenerateUniqueCode(originalUrl));
        }

        private static string GenerateUniqueCode(string url)
        {
            Random random = new(url.Length);
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
    }
}
