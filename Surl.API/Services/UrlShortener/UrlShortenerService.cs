using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RabbitMQ.Client;
using Surl.API.Broker;
using Surl.API.Data;
using Surl.API.Model;
using Surl.API.RequestResponse.Dto;
using Surl.API.RequestResponse.ViewModel;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Surl.API.Services.UrlShortener
{
    public class UrlShortenerService(IConfiguration config, AppDbContext context, IMessageProducer messageProducer, IMemoryCache cache) : IUrlShortenerService
    {
        private const string ALPHABET = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private readonly IConfiguration _configuration = config;
        private const string ROUTE = "r";

        public async Task<UrlShortenedDto> ShortenUrlAsync(ShortenUrlViewModel request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Url) || !Uri.TryCreate(request.Url, UriKind.Absolute, out var uri))
            {
                throw new ArgumentException("Invalid URL provided.", nameof(request));
            }

            var existingUrl = await cache.GetOrCreateAsync(request.Url, async (key) => await context.UrlShorten
                    .FirstOrDefaultAsync(u => u.OriginalUrl == request.Url));

            if (existingUrl != null)
            {
                return new UrlShortenedDto
                {
                    OriginalUrl = existingUrl.OriginalUrl,
                    Url = FormatUrlShortened(existingUrl.Code)
                };
            }

            string originalUrl = request.Url;
            string shortenedUrl = GenerateShortenedUrl(originalUrl);
            UrlShorten shorten = UrlShorten.CreateOne(originalUrl, shortenedUrl, code: shortenedUrl.Split($"/{ROUTE}/").LastOrDefault()!);
            context.UrlShorten.Add(shorten);

            if (context.Database.IsRelational())
            {
                using var transaction = await context.Database.BeginTransactionAsync();
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            else
            {
                await context.SaveChangesAsync();
            }

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

            var url = await cache.GetOrCreateAsync(code, async (key) => await context.UrlShorten.FirstOrDefaultAsync(u => u.Code == code));

            if (url == null)
            {
                throw new KeyNotFoundException("URL not found.");
            }

            var message = new UrlAccessProcessingMessage
            {
                Code = code,
                UrlId = url.Id,
                IpAddress = ipAddress,
                AccessedAt = DateTime.UtcNow,
                Headers = JsonSerializer.Serialize(headers)
            };

            await messageProducer.Produce("url_access_processing", message);

            return url.OriginalUrl;
        }

        public async Task ProcessClicksAsync(UrlAccessProcessingMessage message)
        {
            UrlShorten url = await context.UrlShorten.FirstAsync(u => u.Id == message.UrlId);

            var access = new UrlShortenAccess
            {
                UrlShorten = url,
                UrlShortenId = url.Id,
                HeadersRaw = message.Headers,
                IpAddress = message.IpAddress,
                AccessedAt = message.AccessedAt,
            };

            url.LastAccessedAt = access.AccessedAt;
            url.ClickCount++;

            context.Update(url);
            context.UrlShortenAccess.Add(access);

            if (context.Database.IsRelational())
            {
                using var transaction = await context.Database.BeginTransactionAsync();
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            else
            {
                await context.SaveChangesAsync();
            }
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
            using var crypto = RandomNumberGenerator.Create();
            var seedBytes = Encoding.UTF8.GetBytes(url);
            crypto.GetBytes(seedBytes);
            int seed = BitConverter.ToInt32(seedBytes, 0);
            var random = new Random(seed);

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
