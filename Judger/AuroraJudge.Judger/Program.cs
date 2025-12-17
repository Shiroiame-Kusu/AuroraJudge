using AuroraJudge.Judger;
using AuroraJudge.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

static string? TryGetConfigPath(string[] args)
{
    for (var i = 0; i < args.Length; i++)
    {
        var arg = args[i];
        if (string.Equals(arg, "--config", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
        {
            return args[i + 1];
        }

        const string prefix = "--config=";
        if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return arg[prefix.Length..];
        }
    }

    return Environment.GetEnvironmentVariable("AURORAJUDGE_JUDGER_CONFIG")
        ?? Environment.GetEnvironmentVariable("JUDGER_CONFIG");
}

// 加载配置文件
var configPath = TryGetConfigPath(args) ?? "judger.conf";
ConfigReader config;
if (Path.IsPathRooted(configPath))
{
    config = new ConfigReader();
    config.Load(configPath);
}
else
{
    config = ConfigReader.CreateDefault(configPath);
}

if (config.IsLoaded)
{
    Console.WriteLine($"✓ 已加载配置文件: {config.FilePath}");
}
else
{
    Console.WriteLine($"⚠ 未找到配置文件: {configPath}，使用默认配置");
    Console.WriteLine("   提示: 可使用 --config /abs/path/to/judger.conf 或设置 AURORAJUDGE_JUDGER_CONFIG");
}

var builder = Host.CreateApplicationBuilder(args);

// 将 .conf 配置应用到 Configuration
config.ApplyTo(builder.Configuration);

// 配置日志
var logLevel = config.Get("logging", "level", "Information");
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(Enum.Parse<LogLevel>(logLevel, ignoreCase: true));

// 判断运行模式: http (默认) 或 rabbitmq
var mode = config.Get("judger", "mode", "http").ToLower();

// 将 judger 配置写入 Configuration
builder.Configuration["Judger:Mode"] = mode;
builder.Configuration["Judger:Name"] = config.Get("judger", "name", "judger-1");
builder.Configuration["Judger:WorkDir"] = config.Get("judger", "work_dir", "/tmp/aurora-judge");
builder.Configuration["Judger:MaxConcurrentTasks"] = config.Get("judger", "max_concurrent_tasks", "4");

if (mode == "http")
{
    // HTTP 模式配置
    if (config.IsLoaded && config.Has("http", "backend_url"))
        builder.Configuration["Judger:BackendUrl"] = config.Get("http", "backend_url", "http://localhost:5000");

    if (config.IsLoaded && config.Has("http", "judger_id"))
        builder.Configuration["Judger:JudgerId"] = config.Get("http", "judger_id");

    if (config.IsLoaded && config.Has("http", "secret"))
        builder.Configuration["Judger:Secret"] = config.Get("http", "secret");

    if (config.IsLoaded && config.Has("http", "poll_interval_ms"))
        builder.Configuration["Judger:PollIntervalMs"] = config.Get("http", "poll_interval_ms", "1000");
}
else
{
    // RabbitMQ 模式配置
    builder.Configuration["ConnectionStrings:RabbitMQ"] = config.Get("rabbitmq", "connection", "amqp://guest:guest@localhost:5672");
}

if (mode == "rabbitmq")
{
    // RabbitMQ 模式 - 从消息队列获取任务
    builder.Services.AddHostedService<JudgerWorker>();
}
else
{
    // HTTP 模式 - 从 Backend API 拉取任务
    builder.Services.AddHostedService<HttpJudgerWorker>();
}

var host = builder.Build();

Console.WriteLine($@"
    _                              _           _            
   / \  _   _ _ __ ___  _ __ __ _ | |_   _  __| | __ _  ___ 
  / _ \| | | | '__/ _ \| '__/ _` || | | | |/ _` |/ _` |/ _ \
 / ___ \ |_| | | | (_) | | | (_| || | |_| | (_| | (_| |  __/
/_/   \_\__,_|_|  \___/|_|  \__,_|/ |\__,_|\__,_|\__, |\___|
                                |__/            |___/       
                              JUDGER v1.0.0
                              模式: {(mode == "rabbitmq" ? "RabbitMQ" : "HTTP")}
");

await host.RunAsync();
