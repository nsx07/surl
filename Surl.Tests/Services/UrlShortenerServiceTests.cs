using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Surl.API.Broker;
using Surl.API.Data;
using Surl.API.Model;
using Surl.API.RequestResponse.Dto;
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
            IEnumerable<KeyValuePair<string, string?>> inMemorySettings = new Dictionary<string, string?>
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
            IMessageProducer producer = new MessageProducerRabbitMQ();
            IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());

            var service = new UrlShortenerService(_config, context, producer, cache);

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
            IMessageProducer producer = new MessageProducerRabbitMQ();
            IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());

            var service = new UrlShortenerService(_config, context, producer, cache);
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
            IMessageProducer producer = new MessageProducerRabbitMQ();
            IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());

            var service = new UrlShortenerService(_config, context, producer, cache);
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
            IMessageProducer producer = new MessageProducerRabbitMQ();
            IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());

            var service = new UrlShortenerService(_config, context, producer, cache);
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
            IMessageProducer producer = new MessageProducerRabbitMQ();
            IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());

            var service = new UrlShortenerService(_config, context, producer, cache);
            var request = new ShortenUrlViewModel { Url = "http://original.com" };
            // Act
            var result = await service.ShortenUrlAsync(request);
            // Assert
            result.Should().NotBeNull();
            result.OriginalUrl.Should().Be("http://original.com");
            result.Url.Should().Be("http://short.url/r/test");
        }

        [Fact]
        public async Task ProcessClicksAsync_ShouldDoesNotThrowException_WithValidMessage()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Isolation
                .Options;

            await using var context = new AppDbContext(options);
            var existingEntity = UrlShorten.CreateOne("http://original.com", "http://short.url/r/test", "test");
            context.UrlShorten.Add(existingEntity);
            await context.SaveChangesAsync();

            IMessageProducer producer = new MessageProducerRabbitMQ();
            IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());
            var service = new UrlShortenerService(_config, context, producer, cache);
            var request = new ShortenUrlViewModel { Url = "http://original.com" };

            // Act
            UrlAccessProcessingMessage message = new()
            {
                AccessedAt = DateTime.UtcNow,
                UrlId = existingEntity.Id,
                Code = "test",
            };

            await service.ProcessClicksAsync(message);

            // Assert
            var item = context.UrlShorten
                .Include(x => x.UrlShortenAccesses)
                .FirstOrDefault(u => u.Id == existingEntity.Id);

            item.Should().NotBeNull();
            item!.UrlShortenAccesses.Should().NotBeEmpty();
            item.UrlShortenAccesses.Should().HaveCount(1);
            item.UrlShortenAccesses.First().AccessedAt.Should().BeCloseTo(message.AccessedAt, TimeSpan.FromSeconds(1));
            item.ClickCount.Should().Be(1);
        }

        [Fact]
        public async Task ProcessClicksAsync_ShouldThrowException_WhenMessageIsNull()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Isolation
                .Options;
            await using var context = new AppDbContext(options);
            IMessageProducer producer = new MessageProducerRabbitMQ();
            IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());
            var service = new UrlShortenerService(_config, context, producer, cache);
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.ProcessClicksAsync(null!));
        }

        [Fact]
        public async Task ProcessClicksAsync_ShouldThrowException_WhenUrlIdDoesNotExist()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Isolation
                .Options;
            await using var context = new AppDbContext(options);
            IMessageProducer producer = new MessageProducerRabbitMQ();
            IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());
            var service = new UrlShortenerService(_config, context, producer, cache);
            // Act & Assert
            UrlAccessProcessingMessage message = new()
            {
                AccessedAt = DateTime.UtcNow,
                UrlId = Guid.NewGuid(), // Non-existent ID
                Code = "test",
            };
            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ProcessClicksAsync(message));
        }
    }
}
