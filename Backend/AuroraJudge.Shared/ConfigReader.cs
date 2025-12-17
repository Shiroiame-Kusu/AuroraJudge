using System.Text.RegularExpressions;

namespace AuroraJudge.Shared;

/// <summary>
/// 配置文件读取器，支持 INI 风格的 .conf 文件格式
/// 
/// 格式示例:
/// [database]
/// host = localhost
/// port = 5432
/// 
/// [jwt]
/// secret = your_secret_key
/// </summary>
public class ConfigReader
{
    private readonly Dictionary<string, Dictionary<string, string>> _sections = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _globalValues = new(StringComparer.OrdinalIgnoreCase);
    
    /// <summary>
    /// 配置文件路径
    /// </summary>
    public string? FilePath { get; private set; }
    
    /// <summary>
    /// 是否成功加载了配置文件
    /// </summary>
    public bool IsLoaded { get; private set; }

    /// <summary>
    /// 从文件加载配置
    /// </summary>
    /// <param name="filePath">配置文件路径</param>
    /// <returns>是否成功加载</returns>
    public bool Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        try
        {
            FilePath = Path.GetFullPath(filePath);
            ParseFile(File.ReadAllLines(filePath));
            IsLoaded = true;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 从多个可能的路径中加载配置文件
    /// </summary>
    /// <param name="possiblePaths">可能的配置文件路径列表</param>
    /// <returns>是否成功加载</returns>
    public bool LoadFromPaths(params string[] possiblePaths)
    {
        foreach (var path in possiblePaths)
        {
            if (Load(path))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 获取配置值
    /// </summary>
    /// <param name="section">节名</param>
    /// <param name="key">键名</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>配置值</returns>
    public string Get(string section, string key, string defaultValue = "")
    {
        if (_sections.TryGetValue(section, out var sectionDict))
        {
            if (sectionDict.TryGetValue(key, out var value))
            {
                return value;
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// 获取全局配置值（不在任何 section 下的配置）
    /// </summary>
    /// <param name="key">键名</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>配置值</returns>
    public string GetGlobal(string key, string defaultValue = "")
    {
        return _globalValues.TryGetValue(key, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// 获取整数配置值
    /// </summary>
    public int GetInt(string section, string key, int defaultValue = 0)
    {
        var value = Get(section, key);
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// 获取布尔配置值
    /// </summary>
    public bool GetBool(string section, string key, bool defaultValue = false)
    {
        var value = Get(section, key).ToLower();
        if (string.IsNullOrEmpty(value)) return defaultValue;
        return value is "true" or "1" or "yes" or "on";
    }

    /// <summary>
    /// 获取整个 section 的所有配置
    /// </summary>
    public Dictionary<string, string>? GetSection(string section)
    {
        return _sections.TryGetValue(section, out var sectionDict) 
            ? new Dictionary<string, string>(sectionDict, StringComparer.OrdinalIgnoreCase) 
            : null;
    }

    /// <summary>
    /// 检查是否存在某个 section
    /// </summary>
    public bool HasSection(string section)
    {
        return _sections.ContainsKey(section);
    }

    /// <summary>
    /// 检查是否存在某个配置项
    /// </summary>
    public bool Has(string section, string key)
    {
        return _sections.TryGetValue(section, out var sectionDict) && sectionDict.ContainsKey(key);
    }

    /// <summary>
    /// 将配置转换为扁平化的字典，可用于添加到 IConfiguration
    /// 键格式为 Section:Key
    /// </summary>
    public Dictionary<string, string?> ToConfigurationDictionary()
    {
        var configData = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        // 添加全局配置
        foreach (var kvp in _globalValues)
        {
            configData[kvp.Key] = kvp.Value;
        }

        // 添加分节配置，使用 : 作为层级分隔符
        foreach (var section in _sections)
        {
            foreach (var kvp in section.Value)
            {
                configData[$"{section.Key}:{kvp.Key}"] = kvp.Value;
            }
        }

        return configData;
    }

    /// <summary>
    /// 将配置应用到 IConfigurationBuilder
    /// </summary>
    public void ApplyTo(Microsoft.Extensions.Configuration.IConfigurationBuilder configBuilder)
    {
        var configData = ToConfigurationDictionary();
        configBuilder.Add(new MemoryConfigurationSource(configData));
    }

    private void ParseFile(string[] lines)
    {
        string currentSection = "";
        var commentPattern = new Regex(@"^\s*[#;]");
        var sectionPattern = new Regex(@"^\s*\[([^\]]+)\]\s*$");
        var keyValuePattern = new Regex(@"^\s*([^=]+?)\s*=\s*(.*)$");

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            // 跳过空行和注释
            if (string.IsNullOrEmpty(line) || commentPattern.IsMatch(line))
            {
                continue;
            }

            // 检查是否是 section 定义（不处理行尾注释，section 行不应有注释）
            var sectionMatch = sectionPattern.Match(line);
            if (sectionMatch.Success)
            {
                currentSection = sectionMatch.Groups[1].Value.Trim();
                if (!_sections.ContainsKey(currentSection))
                {
                    _sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                continue;
            }

            // 检查是否是键值对
            var kvMatch = keyValuePattern.Match(line);
            if (kvMatch.Success)
            {
                var key = kvMatch.Groups[1].Value.Trim();
                var value = kvMatch.Groups[2].Value.Trim();
                
                // 处理引号包裹的值（引号内不处理注释）
                if ((value.StartsWith('"') && value.EndsWith('"')) || 
                    (value.StartsWith('\'') && value.EndsWith('\'')))
                {
                    value = value[1..^1];
                }
                else
                {
                    // 非引号包裹的值，处理行尾注释
                    // 查找空格后跟 # 的位置作为注释起始（避免把值中的 # 当注释）
                    var spaceHashIndex = value.IndexOf(" #", StringComparison.Ordinal);
                    if (spaceHashIndex > 0)
                    {
                        value = value[..spaceHashIndex].Trim();
                    }
                }

                // 处理转义字符
                value = value.Replace("\\#", "#").Replace("\\n", "\n").Replace("\\t", "\t");

                if (string.IsNullOrEmpty(currentSection))
                {
                    _globalValues[key] = value;
                }
                else
                {
                    _sections[currentSection][key] = value;
                }
            }
        }
    }

    /// <summary>
    /// 创建一个预配置的 ConfigReader 实例，自动搜索常见路径
    /// </summary>
    public static ConfigReader CreateDefault(string configName)
    {
        var reader = new ConfigReader();
        var searchPaths = new[]
        {
            configName,                                              // 当前目录
            $"../{configName}",                                      // 上一级目录
            $"../../{configName}",                                   // 上两级目录
            $"../../../{configName}",                                // 上三级目录
            Path.Combine(AppContext.BaseDirectory, configName),
            Path.Combine(AppContext.BaseDirectory, "..", configName),
            Path.Combine(AppContext.BaseDirectory, "..", "..", configName),
        };

        reader.LoadFromPaths(searchPaths);
        return reader;
    }
}

/// <summary>
/// 内存配置源，用于将字典数据添加到 IConfigurationBuilder
/// </summary>
internal class MemoryConfigurationSource : Microsoft.Extensions.Configuration.IConfigurationSource
{
    private readonly Dictionary<string, string?> _data;

    public MemoryConfigurationSource(Dictionary<string, string?> data)
    {
        _data = data;
    }

    public Microsoft.Extensions.Configuration.IConfigurationProvider Build(Microsoft.Extensions.Configuration.IConfigurationBuilder builder)
    {
        return new MemoryConfigurationProvider(_data);
    }
}

/// <summary>
/// 内存配置提供程序
/// </summary>
internal class MemoryConfigurationProvider : Microsoft.Extensions.Configuration.ConfigurationProvider
{
    public MemoryConfigurationProvider(Dictionary<string, string?> data)
    {
        foreach (var kvp in data)
        {
            Data[kvp.Key] = kvp.Value;
        }
    }
}
