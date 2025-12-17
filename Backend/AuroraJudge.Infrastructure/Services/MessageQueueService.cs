using System.Text;
using System.Text.Json;
using AuroraJudge.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace AuroraJudge.Infrastructure.Services;

public class MessageQueueService : IMessageQueueService, IAsyncDisposable
{
    private readonly IConnection? _connection;
    private readonly IChannel? _channel;
    private readonly bool _isEnabled;
    
    public bool IsEnabled => _isEnabled;
    
    public MessageQueueService(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("RabbitMQ");
        if (string.IsNullOrEmpty(connectionString))
        {
            _isEnabled = false;
            return;
        }
        
        try
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(connectionString),
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };
            
            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
            _isEnabled = true;
        }
        catch
        {
            _isEnabled = false;
        }
    }
    
    public async Task PublishAsync<T>(string queue, T message, CancellationToken cancellationToken = default)
    {
        if (!_isEnabled || _channel == null)
        {
            return;
        }
        
        await _channel.QueueDeclareAsync(
            queue: queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
        
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);
        
        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json"
        };
        
        await _channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queue,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }
    
    public async Task SubscribeAsync<T>(string queue, Func<T, Task> handler, CancellationToken cancellationToken = default)
    {
        if (!_isEnabled || _channel == null)
        {
            return;
        }
        
        await _channel.QueueDeclareAsync(
            queue: queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
        
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: cancellationToken);
        
        var consumer = new RabbitMQ.Client.Events.AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<T>(json);
                
                if (message != null)
                {
                    await handler(message);
                }
                
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken);
            }
            catch
            {
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken);
            }
        };
        
        await _channel.BasicConsumeAsync(queue: queue, autoAck: false, consumer: consumer, cancellationToken: cancellationToken);
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_channel != null)
        {
            await _channel.CloseAsync();
            _channel.Dispose();
        }
        
        if (_connection != null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }
        
        GC.SuppressFinalize(this);
    }
}
