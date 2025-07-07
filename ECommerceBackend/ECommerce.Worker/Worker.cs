using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;
using ECommerce.Shared.Events;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private IConnection _connection;
    private IModel _channel;
    private IDatabase _redisDb;

    public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;

        InitializeRabbitMQ();
        InitializeRedis();
    }

    private void InitializeRabbitMQ()
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri("amqp://guest:guest@localhost:5672/")
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(
            queue: "order-placed",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        _logger.LogInformation("Connected to RabbitMQ and declared queue.");
    }

    private void InitializeRedis()
    {
        var redis = ConnectionMultiplexer.Connect("localhost:6379");
        _redisDb = redis.GetDatabase();

        _logger.LogInformation("Connected to Redis.");
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker running. Waiting for messages...");

        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var orderEvent = JsonSerializer.Deserialize<OrderPlacedEvent>(json);

                if (orderEvent == null)
                {
                    _logger.LogWarning("Received null or invalid message.");
                    return;
                }

                _logger.LogInformation("Received order: {OrderId}", orderEvent.OrderId);

                // Simulate processing time
                await Task.Delay(2000, stoppingToken);

                // 📌 Veritabanına ProcessedAt ve Status yaz
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var order = await dbContext.Orders.FindAsync(orderEvent.OrderId);
                if (order != null)
                {
                    order.ProcessedAt = DateTime.UtcNow;
                    order.Status = ECommerce.Core.Enums.OrderStatus.Processed;

                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Updated order in DB: {OrderId}", orderEvent.OrderId);
                }

                // Redis'e log
                var key = $"order_processed:{orderEvent.OrderId}";
                var value = $"Order processed at {DateTime.UtcNow:O}";
                await _redisDb.StringSetAsync(key, value);

                _logger.LogInformation("Processed order: {OrderId}, logged to Redis.", orderEvent.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message.");
            }
        };

        _channel.BasicConsume(
            queue: "order-placed",
            autoAck: true,
            consumer: consumer
        );

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel.Close();
        _connection.Close();
        base.Dispose();
    }
}
