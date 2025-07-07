using ECommerce.Core.Interfaces;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace ECommerce.Infrastructure.Services;

public class RabbitMQService : IMessageBroker, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQService> _logger;

    public RabbitMQService(ILogger<RabbitMQService> logger)
    {
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "guest",
            Password = "guest"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    public async Task PublishAsync<T>(string queueName, T message) where T : class
    {
        try
        {
            // Queue'yu declare et
            _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);

            // Message'ı serialize et
            var messageJson = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(messageJson);

            // Message properties
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;

            // Publish message
            _channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: properties, body: body);

            _logger.LogInformation("Message published to queue {QueueName}: {Message}", queueName, messageJson);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message to queue {QueueName}", queueName);
            throw;
        }
    }

    public async Task<T?> ConsumeAsync<T>(string queueName) where T : class
    {
        try
        {
            _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);

            var result = _channel.BasicGet(queueName, autoAck: true);
            if (result == null)
                return null;

            var messageJson = Encoding.UTF8.GetString(result.Body.ToArray());
            var message = JsonSerializer.Deserialize<T>(messageJson);

            _logger.LogInformation("Message consumed from queue {QueueName}: {Message}", queueName, messageJson);

            return await Task.FromResult(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consuming message from queue {QueueName}", queueName);
            throw;
        }
    }

    public void Subscribe<T>(string queueName, Func<T, Task> handler) where T : class
    {
        try
        {
            _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var messageJson = Encoding.UTF8.GetString(body);
                    var message = JsonSerializer.Deserialize<T>(messageJson);

                    if (message != null)
                    {
                        await handler(message);
                        _logger.LogInformation("Message processed from queue {QueueName}: {Message}", queueName, messageJson);
                    }

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from queue {QueueName}", queueName);
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

            _logger.LogInformation("Subscribed to queue {QueueName}", queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to queue {QueueName}", queueName);
            throw;
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        _channel?.Dispose();
        _connection?.Dispose();
    }
}