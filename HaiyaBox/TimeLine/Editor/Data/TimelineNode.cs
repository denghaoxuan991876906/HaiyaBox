using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HaiyaBox.TimeLine.Editor.Data;

/// <summary>
/// 节点类型枚举
/// </summary>
public enum NodeType
{
    /// <summary>并行节点 - 同时执行所有子节点</summary>
    Parallel,

    /// <summary>序列节点 - 按顺序执行子节点</summary>
    Sequence,

    /// <summary>循环节点 - 重复执行子节点N次</summary>
    Loop,

    /// <summary>延迟节点 - 等待指定时间</summary>
    Delay,

    /// <summary>脚本节点 - 执行自定义脚本逻辑</summary>
    Script,

    /// <summary>条件节点 - 检查条件是否满足</summary>
    Condition,

    /// <summary>动作节点 - 执行特定动作</summary>
    Action
}

/// <summary>
/// 节点状态枚举
/// </summary>
public enum NodeStatus
{
    /// <summary>等待执行</summary>
    Pending,

    /// <summary>正在执行</summary>
    Running,

    /// <summary>执行成功</summary>
    Success,

    /// <summary>执行失败</summary>
    Failure
}

/// <summary>
/// 时间轴节点基类
/// </summary>
public class TimelineNode
{
    /// <summary>节点唯一标识符</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>节点类型</summary>
    public NodeType Type { get; set; }

    /// <summary>显示名称</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>备注说明</summary>
    public string Remark { get; set; } = string.Empty;

    /// <summary>父节点ID</summary>
    [JsonIgnore]
    public string? ParentId { get; set; }

    /// <summary>子节点列表</summary>
    public List<TimelineNode> Children { get; set; } = new();

    /// <summary>节点参数（用于存储各类型节点的特定配置）</summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>是否启用</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 运行时状态（不序列化）
    /// </summary>
    [JsonIgnore]
    public NodeStatus Status { get; set; } = NodeStatus.Pending;

    /// <summary>
    /// 深度克隆节点
    /// </summary>
    public TimelineNode Clone()
    {
        var clone = new TimelineNode
        {
            Id = Guid.NewGuid().ToString(), // 新的ID
            Type = Type,
            DisplayName = DisplayName + " (副本)",
            Remark = Remark,
            Parameters = new Dictionary<string, object>(Parameters),
            Enabled = Enabled
        };

        foreach (var child in Children)
        {
            var childClone = child.Clone();
            childClone.ParentId = clone.Id;
            clone.Children.Add(childClone);
        }

        return clone;
    }

    /// <summary>
    /// 获取节点层级深度
    /// </summary>
    public int GetDepth()
    {
        int depth = 0;
        var current = this;
        while (current.ParentId != null)
        {
            depth++;
            // 需要从树中查找父节点，这里暂时返回0
            break;
        }
        return depth;
    }

    /// <summary>
    /// 添加子节点
    /// </summary>
    public void AddChild(TimelineNode child)
    {
        child.ParentId = Id;
        Children.Add(child);
    }

    /// <summary>
    /// 移除子节点
    /// </summary>
    public bool RemoveChild(TimelineNode child)
    {
        child.ParentId = null;
        return Children.Remove(child);
    }

    /// <summary>
    /// 移除指定ID的子节点
    /// </summary>
    public bool RemoveChild(string childId)
    {
        var child = Children.Find(c => c.Id == childId);
        if (child != null)
        {
            return RemoveChild(child);
        }
        return false;
    }
}

/// <summary>
/// 并行节点配置
/// </summary>
public static class ParallelNodeParams
{
    /// <summary>是否任意子节点成功就返回成功（默认false，需要所有子节点成功）</summary>
    public const string AnyReturn = "AnyReturn";
}

/// <summary>
/// 序列节点配置
/// </summary>
public static class SequenceNodeParams
{
    /// <summary>是否忽略子节点失败继续执行（默认false）</summary>
    public const string IgnoreFailure = "IgnoreFailure";
}

/// <summary>
/// 循环节点配置
/// </summary>
public static class LoopNodeParams
{
    /// <summary>循环次数</summary>
    public const string LoopCount = "LoopCount";

    /// <summary>当前循环索引（运行时）</summary>
    public const string CurrentIndex = "CurrentIndex";
}

/// <summary>
/// 延迟节点配置
/// </summary>
public static class DelayNodeParams
{
    /// <summary>延迟时间（秒）</summary>
    public const string DelaySeconds = "DelaySeconds";

    /// <summary>开始时间（运行时）</summary>
    public const string StartTime = "StartTime";
}

/// <summary>
/// 脚本节点配置
/// </summary>
public static class ScriptNodeParams
{
    /// <summary>脚本代码内容</summary>
    public const string ScriptCode = "ScriptCode";

    /// <summary>脚本类型名称</summary>
    public const string ScriptTypeName = "ScriptTypeName";
}

/// <summary>
/// 条件节点配置
/// </summary>
public static class ConditionNodeParams
{
    /// <summary>条件类型（技能释放、单位生成等）</summary>
    public const string ConditionType = "ConditionType";

    /// <summary>技能ID（用于技能释放条件）</summary>
    public const string SpellId = "SpellId";

    /// <summary>单位DataID（用于单位生成条件）</summary>
    public const string UnitDataId = "UnitDataId";

    /// <summary>超时时间（秒，0表示永不超时）</summary>
    public const string TimeoutSeconds = "TimeoutSeconds";

    /// <summary>目标ID（用于Tether/TargetIcon等条件）</summary>
    public const string TargetId = "TargetId";
}

/// <summary>
/// 动作节点配置
/// </summary>
public static class ActionNodeParams
{
    /// <summary>动作类型（设置位置、显示危险区域、发送命令等）</summary>
    public const string ActionType = "ActionType";

    /// <summary>目标职能（如"MT", "ST", "H1"等）</summary>
    public const string TargetRole = "TargetRole";

    /// <summary>位置坐标（Vector3序列化为字符串 "x,y,z"）</summary>
    public const string Position = "Position";

    /// <summary>命令文本</summary>
    public const string Command = "Command";
}
