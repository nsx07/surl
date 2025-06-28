using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Surl.API.Broker
{
    public class MessageProducerRabbitMQ : IMessageProducer
    {
        public async Task Produce<T>(string topic, T message) where T : class
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: topic, durable: true, exclusive: false, autoDelete: false,
                arguments: null);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            await channel.BasicPublishAsync(exchange: string.Empty, routingKey: topic, body: body);
        }
    }
}
