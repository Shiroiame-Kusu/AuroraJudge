using System.Text;
using System.Text.Json;
using AuroraJudge.Judger.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AuroraJudge.Judger;

/// <summary>
/// 判题机后台服务
/// </summary>
public class JudgerWorker : BackgroundService
{
    private readonly ILogger<JudgerWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly JudgeService _judgeService;
    private IConnection? _connection;
    private IChannel? _channel;
    
    private readonly Guid _judgerId;
    private readonly string _judgerName;
    private readonly int _maxConcurrentTasks;
    private int _currentTasks;

    private static int GetInt(IConfiguration configuration, string key, int defaultValue)
    {
        var raw = configuration[key];
        return int.TryParse(raw, out var value) ? value : defaultValue;
    }
    
    public JudgerWorker(ILogger<JudgerWorker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _judgerId = Guid.NewGuid();
        _judgerName = _configuration["Judger:Name"] ?? $"judger-{Environment.MachineName}";
        _maxConcurrentTasks = GetInt(_configuration, "Judger:MaxConcurrentTasks", 4);
        
        var workDir = _configuration["Judger:WorkDir"] ?? "/tmp/aurora-judge";
        _judgeService = new JudgeService(logger, workDir);
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("判题机 {Name} 启动中...", _judgerName);
        
        await ConnectToRabbitMQAsync(stoppingToken);
        
        // 启动心跳任务
        _ = SendHeartbeatAsync(stoppingToken);
        
        // 开始消费判题任务
        await ConsumeTasksAsync(stoppingToken);
    }
    
    private async Task ConnectToRabbitMQAsync(CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("RabbitMQ") ?? "amqp://guest:guest@localhost:5672";
        
        var factory = new ConnectionFactory
        {
            Uri = new Uri(connectionString),
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };
        
        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
        
        // 声明队列
        await _channel.QueueDeclareAsync(
            queue: "judge.task",
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);
        
        await _channel.QueueDeclareAsync(
            queue: "judge.result",
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);
        
        await _channel.QueueDeclareAsync(
            queue: "judge.heartbeat",
            durable: false,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);
        
        // 设置预取数量
        await _channel.BasicQosAsync(0, (ushort)_maxConcurrentTasks, false, cancellationToken);
        
        _logger.LogInformation("已连接到 RabbitMQ");
    }
    
    private async Task ConsumeTasksAsync(CancellationToken cancellationToken)
    {
        if (_channel == null) return;
        
        var consumer = new AsyncEventingBasicConsumer(_channel);
        
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                Interlocked.Increment(ref _currentTasks);
                
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var task = JsonSerializer.Deserialize(json, JudgerJsonContext.Default.JudgeTask);
                
                if (task != null)
                {
                    _logger.LogInformation("收到判题任务: {SubmissionId}", task.SubmissionId);
                    
                    // 执行判题
                    var result = await _judgeService.JudgeAsync(task, cancellationToken);
                    
                    // 发送结果
                    await PublishResultAsync(result, cancellationToken);
                    
                    _logger.LogInformation("判题完成: {SubmissionId} - {Status}", task.SubmissionId, result.Status);
                }
                
                await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理判题任务失败");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true, cancellationToken);
            }
            finally
            {
                Interlocked.Decrement(ref _currentTasks);
            }
        };
        
        await _channel.BasicConsumeAsync("judge.task", false, consumer, cancellationToken);
        
        _logger.LogInformation("开始监听判题任务队列");
        
        // 保持运行
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken);
        }
    }
    
    private async Task PublishResultAsync(JudgeResponse result, CancellationToken cancellationToken)
    {
        if (_channel == null) return;
        
        var json = JsonSerializer.Serialize(result, JudgerJsonContext.Default.JudgeResponse);
        var body = Encoding.UTF8.GetBytes(json);
        
        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json"
        };
        
        await _channel.BasicPublishAsync(
            exchange: "",
            routingKey: "judge.result",
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }
    
    private async Task SendHeartbeatAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_channel != null)
                {
                    var heartbeat = new JudgerHeartbeat
                    {
                        JudgerId = _judgerId,
                        Name = _judgerName,
                        HostName = Environment.MachineName,
                        Version = "1.0.0",
                        IsOnline = true,
                        CpuUsage = GetCpuUsage(),
                        MemoryUsage = GetMemoryUsage(),
                        CurrentTasks = _currentTasks,
                        MaxConcurrentTasks = _maxConcurrentTasks,
                        Timestamp = DateTime.UtcNow
                    };
                    
                    var json = JsonSerializer.Serialize(heartbeat, JudgerJsonContext.Default.JudgerHeartbeat);
                    var body = Encoding.UTF8.GetBytes(json);
                    
                    await _channel.BasicPublishAsync(
                        exchange: "",
                        routingKey: "judge.heartbeat",
                        body: body,
                        cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "发送心跳失败");
            }
            
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
        }
    }
    
    private static double GetCpuUsage()
    {
        // 简化实现，实际应使用 System.Diagnostics.Process 或 /proc/stat
        return 0;
    }
    
    private static double GetMemoryUsage()
    {
        var process = System.Diagnostics.Process.GetCurrentProcess();
        return process.WorkingSet64 / (1024.0 * 1024.0 * 1024.0); // GB
    }
    
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("判题机 {Name} 正在关闭...", _judgerName);
        
        if (_channel != null)
        {
            await _channel.CloseAsync(cancellationToken);
            _channel.Dispose();
        }
        
        if (_connection != null)
        {
            await _connection.CloseAsync(cancellationToken);
            _connection.Dispose();
        }
        
        await base.StopAsync(cancellationToken);
    }
}
