using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace AuroraJudge.Judger;

/// <summary>
/// 沙盒运行选项
/// </summary>
public class SandboxOptions
{
    public string Command { get; set; } = string.Empty;
    public string WorkDir { get; set; } = string.Empty;
    public string? Input { get; set; }
    public int TimeLimit { get; set; } = 1000; // 毫秒
    public int MemoryLimit { get; set; } = 256 * 1024; // KB
    public int OutputLimit { get; set; } = 64 * 1024 * 1024; // 字节
}

/// <summary>
/// 沙盒运行结果
/// </summary>
public class SandboxResult
{
    public int ExitCode { get; set; }
    public string? Stdout { get; set; }
    public string? Stderr { get; set; }
    public int Time { get; set; } // 毫秒
    public int Memory { get; set; } // KB
    public bool TimeLimitExceeded { get; set; }
    public bool MemoryLimitExceeded { get; set; }
    public bool OutputLimitExceeded { get; set; }
}

/// <summary>
/// 沙盒运行器
/// 在生产环境中应使用 nsjail 或 isolate 进行隔离
/// </summary>
public class SandboxRunner
{
    private readonly ILogger _logger;
    private readonly bool _useIsolate;
    
    public SandboxRunner(ILogger logger)
    {
        _logger = logger;
        // 检测是否有 isolate 可用
        _useIsolate = File.Exists("/usr/bin/isolate") || File.Exists("/usr/local/bin/isolate");
    }
    
    public async Task<SandboxResult> RunAsync(SandboxOptions options, CancellationToken cancellationToken = default)
    {
        if (_useIsolate)
        {
            return await RunWithIsolateAsync(options, cancellationToken);
        }
        else
        {
            return await RunDirectAsync(options, cancellationToken);
        }
    }
    
    /// <summary>
    /// 使用 isolate 沙盒运行
    /// </summary>
    private async Task<SandboxResult> RunWithIsolateAsync(SandboxOptions options, CancellationToken cancellationToken)
    {
        var result = new SandboxResult();
        var boxId = Random.Shared.Next(0, 1000);
        
        try
        {
            // 初始化沙盒
            var initResult = await ExecuteCommandAsync("isolate", $"--box-id={boxId} --init", null, 10000, cancellationToken);
            if (initResult.ExitCode != 0)
            {
                result.ExitCode = -1;
                result.Stderr = "初始化沙盒失败";
                return result;
            }
            
            var boxPath = initResult.Stdout?.Trim() ?? $"/var/local/lib/isolate/{boxId}/box";
            
            // 复制工作目录内容
            if (Directory.Exists(options.WorkDir))
            {
                foreach (var file in Directory.GetFiles(options.WorkDir))
                {
                    File.Copy(file, Path.Combine(boxPath, Path.GetFileName(file)), true);
                }
            }
            
            // 构建 isolate 命令
            var isolateArgs = new StringBuilder();
            isolateArgs.Append($"--box-id={boxId} ");
            isolateArgs.Append($"--time={options.TimeLimit / 1000.0:F1} ");
            isolateArgs.Append($"--wall-time={options.TimeLimit / 1000.0 * 2:F1} ");
            isolateArgs.Append($"--mem={options.MemoryLimit} ");
            isolateArgs.Append($"--fsize={options.OutputLimit / 1024} ");
            isolateArgs.Append("--processes=10 ");
            isolateArgs.Append("--env=PATH=/usr/bin:/bin ");
            isolateArgs.Append("--run -- ");
            isolateArgs.Append(options.Command);
            
            // 运行
            var runResult = await ExecuteCommandAsync("isolate", isolateArgs.ToString(), options.Input, options.TimeLimit * 2, cancellationToken);
            
            result.ExitCode = runResult.ExitCode;
            result.Stdout = runResult.Stdout;
            result.Stderr = runResult.Stderr;
            result.Time = runResult.Time;
            result.Memory = runResult.Memory;
            result.TimeLimitExceeded = runResult.TimeLimitExceeded;
            result.MemoryLimitExceeded = runResult.MemoryLimitExceeded;
        }
        finally
        {
            // 清理沙盒
            await ExecuteCommandAsync("isolate", $"--box-id={boxId} --cleanup", null, 10000, cancellationToken);
        }
        
        return result;
    }
    
    /// <summary>
    /// 直接运行（开发环境使用，不安全）
    /// </summary>
    private async Task<SandboxResult> RunDirectAsync(SandboxOptions options, CancellationToken cancellationToken)
    {
        var result = new SandboxResult();
        
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{options.Command.Replace("\"", "\\\"")}\"",
                WorkingDirectory = options.WorkDir,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            var outputLimitExceeded = false;
            
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null && stdout.Length < options.OutputLimit)
                {
                    stdout.AppendLine(e.Data);
                    if (stdout.Length >= options.OutputLimit)
                    {
                        outputLimitExceeded = true;
                    }
                }
            };
            
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null && stderr.Length < 64 * 1024)
                {
                    stderr.AppendLine(e.Data);
                }
            };
            
            var sw = Stopwatch.StartNew();
            
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            // 写入输入
            if (!string.IsNullOrEmpty(options.Input))
            {
                await process.StandardInput.WriteAsync(options.Input);
                await process.StandardInput.FlushAsync(cancellationToken);
            }
            process.StandardInput.Close();
            
            // 等待完成或超时
            var completed = await Task.Run(() => process.WaitForExit(options.TimeLimit + 500), cancellationToken);
            
            sw.Stop();
            
            if (!completed)
            {
                try { process.Kill(true); } catch { }
                result.TimeLimitExceeded = true;
            }
            
            result.ExitCode = completed ? process.ExitCode : -1;
            result.Stdout = stdout.ToString();
            result.Stderr = stderr.ToString();
            result.Time = (int)sw.ElapsedMilliseconds;
            result.Memory = (int)(process.PeakWorkingSet64 / 1024);
            result.OutputLimitExceeded = outputLimitExceeded;
            
            // 检查内存限制
            if (result.Memory > options.MemoryLimit)
            {
                result.MemoryLimitExceeded = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行命令失败: {Command}", options.Command);
            result.ExitCode = -1;
            result.Stderr = ex.Message;
        }
        
        return result;
    }
    
    private async Task<SandboxResult> ExecuteCommandAsync(
        string fileName, string arguments, string? input, int timeout, CancellationToken cancellationToken)
    {
        var result = new SandboxResult();
        
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardInput = !string.IsNullOrEmpty(input),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            var sw = Stopwatch.StartNew();
            
            process.Start();
            
            if (!string.IsNullOrEmpty(input))
            {
                await process.StandardInput.WriteAsync(input);
                await process.StandardInput.FlushAsync(cancellationToken);
                process.StandardInput.Close();
            }
            
            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            
            var completed = await Task.Run(() => process.WaitForExit(timeout), cancellationToken);
            
            sw.Stop();
            
            if (!completed)
            {
                try { process.Kill(true); } catch { }
                result.TimeLimitExceeded = true;
            }
            
            result.ExitCode = completed ? process.ExitCode : -1;
            result.Stdout = await stdoutTask;
            result.Stderr = await stderrTask;
            result.Time = (int)sw.ElapsedMilliseconds;
        }
        catch (Exception ex)
        {
            result.ExitCode = -1;
            result.Stderr = ex.Message;
        }
        
        return result;
    }
}
