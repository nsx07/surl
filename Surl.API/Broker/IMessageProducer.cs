namespace Surl.API.Broker
{
    public interface IMessageProducer
    {
        Task Produce<T>(string topic, T message) where T : class;
    }
}
