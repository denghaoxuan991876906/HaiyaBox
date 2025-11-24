using System;
using System.Collections.Generic;

namespace HaiyaBox.TimeLine.Editor.Runtime;

/// <summary>
/// 脚本执行环境 - 提供节点间共享数据的KV存储
/// 类似于 AEAssist 的 ScriptEnv
/// </summary>
public class ScriptEnvironment
{
    /// <summary>键值对存储 - 用于节点间共享数据</summary>
    public Dictionary<string, object> KV { get; } = new();

    /// <summary>时间轴开始时间</summary>
    public DateTime StartTime { get; private set; }

    /// <summary>获取运行时长（秒）</summary>
    public double ElapsedSeconds => (DateTime.Now - StartTime).TotalSeconds;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ScriptEnvironment()
    {
        StartTime = DateTime.Now;
    }

    /// <summary>
    /// 重置环境（开始新的时间轴）
    /// </summary>
    public void Reset()
    {
        KV.Clear();
        StartTime = DateTime.Now;
    }

    /// <summary>
    /// 尝试获取值
    /// </summary>
    public bool TryGetValue<T>(string key, out T? value)
    {
        if (KV.TryGetValue(key, out var obj) && obj is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// 获取值（如果不存在返回默认值）
    /// </summary>
    public T? GetValueOrDefault<T>(string key, T? defaultValue = default)
    {
        return TryGetValue<T>(key, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// 设置值
    /// </summary>
    public void SetValue(string key, object value)
    {
        KV[key] = value;
    }

    /// <summary>
    /// 移除值
    /// </summary>
    public bool RemoveValue(string key)
    {
        return KV.Remove(key);
    }

    /// <summary>
    /// 检查是否存在
    /// </summary>
    public bool ContainsKey(string key)
    {
        return KV.ContainsKey(key);
    }

    /// <summary>
    /// 清空所有数据
    /// </summary>
    public void Clear()
    {
        KV.Clear();
    }

    /// <summary>
    /// 获取所有键
    /// </summary>
    public IEnumerable<string> GetAllKeys()
    {
        return KV.Keys;
    }
}
