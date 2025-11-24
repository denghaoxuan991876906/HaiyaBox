using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using AEAssist;
using AEAssist.Helper;

namespace HaiyaBox.TimeLine.Editor.Data;

/// <summary>
/// 时间轴管理器 - 负责时间轴的加载、保存、列表管理
/// </summary>
public sealed class TimelineManager
{
    private static TimelineManager? _instance;
    private static readonly object _lock = new();

    /// <summary>获取单例实例</summary>
    public static TimelineManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new TimelineManager();
                }
            }
            return _instance;
        }
    }

    /// <summary>时间轴文件存储目录</summary>
    private static string TimelineDirectory => Path.Combine(
        Share.CurrentDirectory,
        @"..\..\Timelines\HaiyaBox"
    );

    /// <summary>JSON 序列化选项</summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,  // 支持中文
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter()  // 枚举序列化为字符串
        }
    };

    /// <summary>已加载的时间轴缓存</summary>
    private readonly Dictionary<string, Timeline> _loadedTimelines = new();

    private TimelineManager()
    {
        EnsureDirectoryExists();
    }

    /// <summary>
    /// 确保时间轴目录存在
    /// </summary>
    private void EnsureDirectoryExists()
    {
        try
        {
            if (!Directory.Exists(TimelineDirectory))
            {
                Directory.CreateDirectory(TimelineDirectory);
                LogHelper.Print($"[时间轴管理器] 创建时间轴目录: {TimelineDirectory}");
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[时间轴管理器] 创建目录失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取所有时间轴文件列表
    /// </summary>
    public List<TimelineFileInfo> GetTimelineList()
    {
        var list = new List<TimelineFileInfo>();

        try
        {
            EnsureDirectoryExists();

            var files = Directory.GetFiles(TimelineDirectory, "*.json", SearchOption.AllDirectories);

            foreach (var filePath in files)
            {
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    var relativePath = Path.GetRelativePath(TimelineDirectory, filePath);

                    // 尝试读取文件获取基本信息
                    var json = File.ReadAllText(filePath);
                    var timeline = JsonSerializer.Deserialize<Timeline>(json, JsonOptions);

                    if (timeline != null)
                    {
                        list.Add(new TimelineFileInfo
                        {
                            FilePath = filePath,
                            RelativePath = relativePath,
                            FileName = fileInfo.Name,
                            TimelineName = timeline.Name,
                            DutyName = timeline.DutyName,
                            Author = timeline.Author,
                            Version = timeline.Version,
                            ModifiedAt = fileInfo.LastWriteTime
                        });
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"[时间轴管理器] 读取文件失败 {filePath}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[时间轴管理器] 获取时间轴列表失败: {ex.Message}");
        }

        return list.OrderByDescending(t => t.ModifiedAt).ToList();
    }

    /// <summary>
    /// 加载时间轴
    /// </summary>
    public Timeline? LoadTimeline(string filePath)
    {
        try
        {
            // 检查缓存
            if (_loadedTimelines.TryGetValue(filePath, out var cached))
            {
                return cached;
            }

            if (!File.Exists(filePath))
            {
                LogHelper.Error($"[时间轴管理器] 文件不存在: {filePath}");
                return null;
            }

            var json = File.ReadAllText(filePath);
            var timeline = JsonSerializer.Deserialize<Timeline>(json, JsonOptions);

            if (timeline != null)
            {
                // 验证数据
                var errors = timeline.Validate();
                if (errors.Count > 0)
                {
                    LogHelper.Print($"[时间轴管理器] 时间轴存在 {errors.Count} 个警告:");
                    foreach (var error in errors)
                    {
                        LogHelper.Print($"  - {error}");
                    }
                }

                // 缓存
                _loadedTimelines[filePath] = timeline;

                LogHelper.Print($"[时间轴管理器] 成功加载时间轴: {timeline.Name}");
                return timeline;
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[时间轴管理器] 加载时间轴失败 {filePath}: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// 保存时间轴
    /// </summary>
    public bool SaveTimeline(Timeline timeline, string? filePath = null)
    {
        try
        {
            // 如果没有指定路径，使用时间轴名称作为文件名
            if (string.IsNullOrEmpty(filePath))
            {
                // 清理文件名中的非法字符
                var fileName = GetSafeFileName(timeline.Name) + ".json";
                filePath = Path.Combine(TimelineDirectory, fileName);
            }

            // 确保目录存在
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 更新修改时间
            timeline.ModifiedAt = DateTime.Now;

            // 序列化
            var json = JsonSerializer.Serialize(timeline, JsonOptions);

            // 保存文件
            File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);

            // 更新缓存
            _loadedTimelines[filePath] = timeline;

            LogHelper.Print($"[时间轴管理器] 成功保存时间轴: {timeline.Name} -> {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[时间轴管理器] 保存时间轴失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 删除时间轴文件
    /// </summary>
    public bool DeleteTimeline(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);

                // 从缓存中移除
                _loadedTimelines.Remove(filePath);

                LogHelper.Print($"[时间轴管理器] 成功删除时间轴: {filePath}");
                return true;
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[时间轴管理器] 删除时间轴失败 {filePath}: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// 创建新时间轴
    /// </summary>
    public Timeline CreateNewTimeline(string name, uint dutyId = 0, string dutyName = "")
    {
        var timeline = new Timeline
        {
            Name = name,
            DutyId = dutyId,
            DutyName = dutyName,
            Author = "HaiyaBox",
            CreatedAt = DateTime.Now,
            ModifiedAt = DateTime.Now
        };

        return timeline;
    }

    /// <summary>
    /// 导出时间轴到指定路径
    /// </summary>
    public bool ExportTimeline(Timeline timeline, string exportPath)
    {
        return SaveTimeline(timeline, exportPath);
    }

    /// <summary>
    /// 导入时间轴
    /// </summary>
    public Timeline? ImportTimeline(string importPath)
    {
        try
        {
            if (!File.Exists(importPath))
            {
                LogHelper.Error($"[时间轴管理器] 导入文件不存在: {importPath}");
                return null;
            }

            var json = File.ReadAllText(importPath);
            var timeline = JsonSerializer.Deserialize<Timeline>(json, JsonOptions);

            if (timeline != null)
            {
                // 生成新ID
                timeline.Id = Guid.NewGuid().ToString();

                // 复制到时间轴目录
                var fileName = GetSafeFileName(timeline.Name) + ".json";
                var targetPath = Path.Combine(TimelineDirectory, fileName);

                // 如果文件已存在，添加后缀
                int counter = 1;
                while (File.Exists(targetPath))
                {
                    fileName = GetSafeFileName(timeline.Name) + $"_{counter}.json";
                    targetPath = Path.Combine(TimelineDirectory, fileName);
                    counter++;
                }

                SaveTimeline(timeline, targetPath);

                LogHelper.Print($"[时间轴管理器] 成功导入时间轴: {timeline.Name}");
                return timeline;
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[时间轴管理器] 导入时间轴失败: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// 清除缓存
    /// </summary>
    public void ClearCache()
    {
        _loadedTimelines.Clear();
        LogHelper.Print("[时间轴管理器] 已清除缓存");
    }

    /// <summary>
    /// 获取安全的文件名（移除非法字符）
    /// </summary>
    private string GetSafeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var safeName = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(safeName) ? "timeline" : safeName;
    }
}

/// <summary>
/// 时间轴文件信息
/// </summary>
public class TimelineFileInfo
{
    /// <summary>文件完整路径</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>相对路径</summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>文件名</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>时间轴名称</summary>
    public string TimelineName { get; set; } = string.Empty;

    /// <summary>副本名称</summary>
    public string DutyName { get; set; } = string.Empty;

    /// <summary>作者</summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>版本</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>修改时间</summary>
    public DateTime ModifiedAt { get; set; }
}
