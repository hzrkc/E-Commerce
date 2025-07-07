namespace ECommerce.Core.Interfaces;

public interface IMessageBroker
{
    Task PublishAsync<T>(string queueName, T message) where T : class;
    Task<T?> ConsumeAsync<T>(string queueName) where T : class;
    void Subscribe<T>(string queueName, Func<T, Task> handler) where T : class;
}