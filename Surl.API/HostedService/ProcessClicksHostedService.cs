using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Surl.API.RequestResponse.Dto;
using Surl.API.Services.UrlShortener;
using System.Text;
using System.Text.Json;

namespace Surl.API.HostedService
{
    public class ProcessClicksHostedService(ILogger<ProcessClicksHostedService> logger, IServiceProvider serviceProvider) : IHostedService
    {
        private readonly ConnectionFactory factory = new() { HostName = "localhost" };
        private readonly ILogger _logger = logger;
        private IConnection _connection = null!;
        private IChannel _channel = null!;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await _channel.QueueDeclareAsync(queue: "url_access_processing", durable: true, exclusive: false, autoDelete: false,
                arguments: null, cancellationToken: cancellationToken);

            _logger.LogInformation("### Proccess starting ###");

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                _logger.LogInformation($" [x] Received {message}");
                return ProcessMessage(message);
            };

            await _channel.BasicConsumeAsync("url_access_processing", autoAck: true, consumer: consumer, cancellationToken: cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _channel.CloseAsync(cancellationToken);
            await _connection.CloseAsync(cancellationToken);
            _logger.LogInformation("### Proccess stoping - {0} ###", DateTime.Now);
        }
    
        private async Task ProcessMessage(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                _logger.LogWarning("Received an empty message.");
                return;
            }

            using var scope = serviceProvider.CreateScope();
            var urlShortenerService = scope.ServiceProvider.GetRequiredService<IUrlShortenerService>();

            try
            {
                _logger.LogInformation("Processing message: {Message}", message);
                await urlShortenerService.ProcessClicksAsync(JsonSerializer.Deserialize<UrlAccessProcessingMessage>(message)!);
                _logger.LogInformation("Message processed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message: {Message}", message);
            }
        }
    }
}
