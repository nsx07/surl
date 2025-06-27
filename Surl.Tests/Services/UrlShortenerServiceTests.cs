using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Surl.API.Data;
using Surl.API.Model;
using Surl.API.RequestResponse.ViewModel;
using Surl.API.Services.UrlShortener;
using Xunit;

namespace Surl.UnitTests.Services
{
    public class UrlShortenerServiceTests
    {
        private readonly IConfiguration _config;

        public UrlShortenerServiceTests()
        {
            var inMemorySettings = new Dictionary<string, string>
            {
                { "BaseUrl", "http://short.url" }
            };

            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        [Fact]
        public async Task DeleteShortenUrlAsync_ShouldDeleteUrl_WhenCodeMatches()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Isolation
                .Options;

            await using var context = new AppDbContext(options);

            var entity = UrlShorten.CreateOne("http://original.com", "http://short.url/r/test", "test");
            context.UrlShorten.Add(entity);
            await context.SaveChangesAsync();

            var service = new UrlShortenerService(_config, context);

            // Act
            await service.DeleteShortenUrlAsync("test");

            // Assert
            var result = await context.UrlShorten.FindAsync(entity.Id);
            result.Should().BeNull();
        }

        [Fact]
        public async Task DeleteShortenUrlAsync_ShouldThrowArgumentException_WhenCodeDoesNotMatch()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Isolation
                .Options;
            await using var context = new AppDbContext(options);
            var entity = UrlShorten.CreateOne("http://original.com", "http://short.url/r/test", "test");
            context.UrlShorten.Add(entity);
            await context.SaveChangesAsync();
            var service = new UrlShortenerService(_config, context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteShortenUrlAsync("nonexistent"));
        }

        [Fact]
        public async Task ShortenUrlAsync_ShouldReturnShortenedUrl_WhenValidUrlProvided()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Isolation
                .Options;
            await using var context = new AppDbContext(options);
            var service = new UrlShortenerService(_config, context);
            var request = new ShortenUrlViewModel { Url = "http://original.com" };
            // Act
            var result = await service.ShortenUrlAsync(request);
            // Assert
            result.Should().NotBeNull();
            result.OriginalUrl.Should().Be("http://original.com");
            result.Url.Should().StartWith("http://short.url/r/");
        }

        [Fact]
        public async Task ShortenUrlAsync_ShouldThrowArgumentException_WhenInvalidUrlProvided()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Isolation
                .Options;
            await using var context = new AppDbContext(options);
            var service = new UrlShortenerService(_config, context);
            var request = new ShortenUrlViewModel { Url = "invalid-url" };
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.ShortenUrlAsync(request));
        }

        [Fact]
        public async Task ShortenUrlAsync_ShouldReturnExistingShortenedUrl_WhenUrlAlreadyExists()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Isolation
                .Options;
            await using var context = new AppDbContext(options);
            var existingEntity = UrlShorten.CreateOne("http://original.com", "http://short.url/r/test", "test");
            context.UrlShorten.Add(existingEntity);
            await context.SaveChangesAsync();
            var service = new UrlShortenerService(_config, context);
            var request = new ShortenUrlViewModel { Url = "http://original.com" };
            // Act
            var result = await service.ShortenUrlAsync(request);
            // Assert
            result.Should().NotBeNull();
            result.OriginalUrl.Should().Be("http://original.com");
            result.Url.Should().Be("http://short.url/r/test");
        }
    }
}
