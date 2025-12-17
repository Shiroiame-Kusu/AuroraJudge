using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AuroraJudge.Judger.Contracts;
using Microsoft.Extensions.Logging;

namespace AuroraJudge.Judger;

/// <summary>
/// 判题核心服务
/// </summary>
public class JudgeService
{
    private readonly ILogger _logger;
    private readonly SandboxRunner _sandbox;
    private readonly Dictionary<string, LanguageConfig> _languages;
    private readonly string _workDir;
    
    public JudgeService(ILogger logger, string workDir)
    {
        _logger = logger;
        _workDir = workDir;
        _sandbox = new SandboxRunner(logger);
        _languages = new Dictionary<string, LanguageConfig>
        {
            ["cpp"] = new() 
            { 
                Name = "cpp", 
                Extension = ".cpp", 
                CompileCommand = "g++ -std=c++17 -O2 -o {output} {source}", 
                RunCommand = "./{executable}" 
            },
            ["c"] = new() 
            { 
                Name = "c", 
                Extension = ".c", 
                CompileCommand = "gcc -std=c11 -O2 -o {output} {source}", 
                RunCommand = "./{executable}" 
            },
            ["python"] = new() 
            { 
                Name = "python", 
                Extension = ".py", 
                CompileCommand = null, 
                RunCommand = "python3 {source}",
                TimeMultiplier = 3.0,
                MemoryMultiplier = 2.0
            },
            ["java"] = new() 
            { 
                Name = "java", 
                Extension = ".java", 
                CompileCommand = "javac {source}", 
                RunCommand = "java -Xmx{memory}m Main",
                TimeMultiplier = 2.0,
                MemoryMultiplier = 2.0
            }
        };
    }
    
    /// <summary>
    /// 执行判题
    /// </summary>
    public async Task<JudgeResponse> JudgeAsync(JudgeTask task, CancellationToken cancellationToken = default)
    {
        var response = new JudgeResponse
        {
            SubmissionId = task.SubmissionId,
            Status = JudgeStatus.Judging,
            Results = new List<TestCaseResult>()
        };
        
        var workDir = Path.Combine(_workDir, task.SubmissionId.ToString());
        
        try
        {
            Directory.CreateDirectory(workDir);
            
            if (!_languages.TryGetValue(task.Language, out var langConfig))
            {
                response.Status = JudgeStatus.SystemError;
                response.CompileInfo = $"不支持的语言: {task.Language}";
                return response;
            }
            
            // 写入源代码
            var sourceFile = Path.Combine(workDir, $"Main{langConfig.Extension}");
            await File.WriteAllTextAsync(sourceFile, task.Code, cancellationToken);
            
            // 编译
            if (!string.IsNullOrEmpty(langConfig.CompileCommand))
            {
                var compileResult = await CompileAsync(workDir, sourceFile, langConfig, cancellationToken);
                if (!compileResult.Success)
                {
                    response.Status = JudgeStatus.CompileError;
                    response.CompileInfo = compileResult.Output;
                    return response;
                }
            }
            
            // 计算实际时间和内存限制
            var realTimeLimit = (int)(task.TimeLimit * langConfig.TimeMultiplier);
            var realMemoryLimit = (int)(task.MemoryLimit * langConfig.MemoryMultiplier);
            
            // 运行测试用例
            var results = new List<TestCaseResult>();
            var totalScore = 0;
            var maxTime = 0;
            var maxMemory = 0;
            var overallStatus = JudgeStatus.Accepted;
            
            foreach (var testCase in task.TestCases.OrderBy(t => t.Order))
            {
                var result = await RunTestCaseAsync(
                    workDir, langConfig, testCase, 
                    realTimeLimit, realMemoryLimit, 
                    task.JudgeMode, task.SpecialJudgeCode,
                    cancellationToken);
                
                results.Add(result);
                totalScore += result.Score;
                maxTime = Math.Max(maxTime, result.Time);
                maxMemory = Math.Max(maxMemory, result.Memory);
                
                if (result.Status != JudgeStatus.Accepted && overallStatus == JudgeStatus.Accepted)
                {
                    overallStatus = result.Status;
                }
            }
            
            // 设置最终结果
            response.Results = results;
            response.Time = maxTime;
            response.Memory = maxMemory;
            response.Score = totalScore;
            
            // 如果有部分正确
            if (overallStatus != JudgeStatus.Accepted && totalScore > 0)
            {
                response.Status = JudgeStatus.PartiallyAccepted;
            }
            else
            {
                response.Status = overallStatus;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "判题异常: {SubmissionId}", task.SubmissionId);
            response.Status = JudgeStatus.SystemError;
            response.CompileInfo = ex.Message;
        }
        finally
        {
            // 清理工作目录
            try
            {
                if (Directory.Exists(workDir))
                {
                    Directory.Delete(workDir, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "清理工作目录失败: {WorkDir}", workDir);
            }
        }
        
        return response;
    }
    
    private async Task<(bool Success, string Output)> CompileAsync(
        string workDir, string sourceFile, LanguageConfig config, CancellationToken cancellationToken)
    {
        var outputFile = Path.Combine(workDir, "Main");
        var command = config.CompileCommand!
            .Replace("{source}", sourceFile)
            .Replace("{output}", outputFile);
        
        var result = await _sandbox.RunAsync(new SandboxOptions
        {
            Command = command,
            WorkDir = workDir,
            TimeLimit = 30000, // 30 秒编译超时
            MemoryLimit = 512 * 1024, // 512MB
            OutputLimit = 10 * 1024 * 1024 // 10MB
        }, cancellationToken);
        
        return (result.ExitCode == 0, result.Stderr ?? result.Stdout ?? "");
    }
    
    private async Task<TestCaseResult> RunTestCaseAsync(
        string workDir, LanguageConfig config, TestCaseData testCase,
        int timeLimit, int memoryLimit, JudgeMode judgeMode, string? spjCode,
        CancellationToken cancellationToken)
    {
        var result = new TestCaseResult
        {
            Order = testCase.Order,
            Status = JudgeStatus.SystemError
        };
        
        try
        {
            // 读取输入
            var inputData = await File.ReadAllTextAsync(testCase.InputPath, cancellationToken);
            
            // 构建运行命令
            var command = config.RunCommand
                .Replace("{source}", Path.Combine(workDir, $"Main{config.Extension}"))
                .Replace("{executable}", Path.Combine(workDir, "Main"))
                .Replace("{memory}", memoryLimit.ToString());
            
            // 运行程序
            var runResult = await _sandbox.RunAsync(new SandboxOptions
            {
                Command = command,
                WorkDir = workDir,
                Input = inputData,
                TimeLimit = timeLimit,
                MemoryLimit = memoryLimit * 1024, // 转换为 KB
                OutputLimit = 64 * 1024 * 1024 // 64MB
            }, cancellationToken);
            
            result.Time = runResult.Time;
            result.Memory = runResult.Memory;
            result.ExitCode = runResult.ExitCode.ToString();
            
            // 检查运行状态
            if (runResult.TimeLimitExceeded)
            {
                result.Status = JudgeStatus.TimeLimitExceeded;
                result.Score = 0;
                return result;
            }
            
            if (runResult.MemoryLimitExceeded)
            {
                result.Status = JudgeStatus.MemoryLimitExceeded;
                result.Score = 0;
                return result;
            }
            
            if (runResult.ExitCode != 0)
            {
                result.Status = JudgeStatus.RuntimeError;
                result.Message = runResult.Stderr;
                result.Score = 0;
                return result;
            }
            
            // 比较输出
            var expectedOutput = await File.ReadAllTextAsync(testCase.OutputPath, cancellationToken);
            var actualOutput = runResult.Stdout ?? "";
            
            bool isCorrect;
            if (judgeMode == JudgeMode.SpecialJudge && !string.IsNullOrEmpty(spjCode))
            {
                isCorrect = await RunSpecialJudgeAsync(
                    workDir, spjCode, inputData, expectedOutput, actualOutput, cancellationToken);
            }
            else
            {
                isCorrect = CompareOutput(expectedOutput, actualOutput);
            }
            
            if (isCorrect)
            {
                result.Status = JudgeStatus.Accepted;
                result.Score = testCase.Score;
            }
            else
            {
                result.Status = JudgeStatus.WrongAnswer;
                result.Score = 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "运行测试点异常: {Order}", testCase.Order);
            result.Status = JudgeStatus.SystemError;
            result.Message = ex.Message;
        }
        
        return result;
    }
    
    private static bool CompareOutput(string expected, string actual)
    {
        // 标准化输出（去除行末空格和多余空行）
        var expectedLines = expected.Split('\n').Select(l => l.TrimEnd()).Where(l => !string.IsNullOrEmpty(l)).ToArray();
        var actualLines = actual.Split('\n').Select(l => l.TrimEnd()).Where(l => !string.IsNullOrEmpty(l)).ToArray();
        
        if (expectedLines.Length != actualLines.Length)
        {
            return false;
        }
        
        for (var i = 0; i < expectedLines.Length; i++)
        {
            if (expectedLines[i] != actualLines[i])
            {
                return false;
            }
        }
        
        return true;
    }
    
    private async Task<bool> RunSpecialJudgeAsync(
        string workDir, string spjCode, string input, string expected, string actual,
        CancellationToken cancellationToken)
    {
        // 写入 SPJ 源码并编译运行
        var spjSource = Path.Combine(workDir, "spj.cpp");
        var spjOutput = Path.Combine(workDir, "spj");
        
        await File.WriteAllTextAsync(spjSource, spjCode, cancellationToken);
        
        // 编译 SPJ
        var compileResult = await _sandbox.RunAsync(new SandboxOptions
        {
            Command = $"g++ -std=c++17 -O2 -o {spjOutput} {spjSource}",
            WorkDir = workDir,
            TimeLimit = 30000,
            MemoryLimit = 512 * 1024
        }, cancellationToken);
        
        if (compileResult.ExitCode != 0)
        {
            return false;
        }
        
        // 写入文件
        await File.WriteAllTextAsync(Path.Combine(workDir, "input.txt"), input, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(workDir, "expected.txt"), expected, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(workDir, "actual.txt"), actual, cancellationToken);
        
        // 运行 SPJ
        var runResult = await _sandbox.RunAsync(new SandboxOptions
        {
            Command = $"{spjOutput} input.txt expected.txt actual.txt",
            WorkDir = workDir,
            TimeLimit = 10000,
            MemoryLimit = 256 * 1024
        }, cancellationToken);
        
        return runResult.ExitCode == 0;
    }
}
