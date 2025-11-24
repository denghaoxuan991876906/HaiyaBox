using System.Collections.Generic;
using System.Numerics;

namespace HaiyaBox.TimeLine.Editor.Data;

/// <summary>
/// 时间轴节点工厂类 - 提供创建各种类型节点的便捷方法
/// </summary>
public static class TimelineNodeFactory
{
    /// <summary>
    /// 创建并行节点
    /// </summary>
    public static TimelineNode CreateParallel(string displayName = "并行", bool anyReturn = false)
    {
        return new TimelineNode
        {
            Type = NodeType.Parallel,
            DisplayName = displayName,
            Parameters = new Dictionary<string, object>
            {
                [ParallelNodeParams.AnyReturn] = anyReturn
            }
        };
    }

    /// <summary>
    /// 创建序列节点
    /// </summary>
    public static TimelineNode CreateSequence(string displayName = "序列", bool ignoreFailure = false)
    {
        return new TimelineNode
        {
            Type = NodeType.Sequence,
            DisplayName = displayName,
            Parameters = new Dictionary<string, object>
            {
                [SequenceNodeParams.IgnoreFailure] = ignoreFailure
            }
        };
    }

    /// <summary>
    /// 创建循环节点
    /// </summary>
    public static TimelineNode CreateLoop(string displayName, int loopCount = 1)
    {
        return new TimelineNode
        {
            Type = NodeType.Loop,
            DisplayName = $"{displayName} [循环{loopCount}次]",
            Parameters = new Dictionary<string, object>
            {
                [LoopNodeParams.LoopCount] = loopCount,
                [LoopNodeParams.CurrentIndex] = 0
            }
        };
    }

    /// <summary>
    /// 创建延迟节点
    /// </summary>
    public static TimelineNode CreateDelay(float delaySeconds)
    {
        return new TimelineNode
        {
            Type = NodeType.Delay,
            DisplayName = $"延迟 [{delaySeconds:F2}秒]",
            Parameters = new Dictionary<string, object>
            {
                [DelayNodeParams.DelaySeconds] = delaySeconds
            }
        };
    }

    /// <summary>
    /// 创建脚本节点
    /// </summary>
    public static TimelineNode CreateScript(string displayName, string scriptCode = "", string scriptTypeName = "")
    {
        return new TimelineNode
        {
            Type = NodeType.Script,
            DisplayName = displayName,
            Parameters = new Dictionary<string, object>
            {
                [ScriptNodeParams.ScriptCode] = scriptCode,
                [ScriptNodeParams.ScriptTypeName] = scriptTypeName
            }
        };
    }

    /// <summary>
    /// 创建条件节点 - 技能释放
    /// </summary>
    public static TimelineNode CreateCondition_SpellCast(string displayName, uint spellId, float timeoutSeconds = 0)
    {
        return new TimelineNode
        {
            Type = NodeType.Condition,
            DisplayName = displayName,
            Parameters = new Dictionary<string, object>
            {
                [ConditionNodeParams.ConditionType] = TriggerConditionType.EnemyCastSpell,
                [ConditionNodeParams.SpellId] = spellId,
                [ConditionNodeParams.TimeoutSeconds] = timeoutSeconds
            }
        };
    }

    /// <summary>
    /// 创建条件节点 - 单位生成
    /// </summary>
    public static TimelineNode CreateCondition_UnitCreate(string displayName, uint unitDataId, float timeoutSeconds = 0)
    {
        return new TimelineNode
        {
            Type = NodeType.Condition,
            DisplayName = displayName,
            Parameters = new Dictionary<string, object>
            {
                [ConditionNodeParams.ConditionType] = TriggerConditionType.UnitCreate,
                [ConditionNodeParams.UnitDataId] = unitDataId,
                [ConditionNodeParams.TimeoutSeconds] = timeoutSeconds
            }
        };
    }

    /// <summary>
    /// 创建条件节点 - 游戏时间
    /// </summary>
    public static TimelineNode CreateCondition_GameTime(string displayName, float timeSeconds)
    {
        return new TimelineNode
        {
            Type = NodeType.Condition,
            DisplayName = displayName,
            Parameters = new Dictionary<string, object>
            {
                [ConditionNodeParams.ConditionType] = TriggerConditionType.GameTime,
                [ConditionNodeParams.TimeoutSeconds] = timeSeconds
            }
        };
    }

    /// <summary>
    /// 创建动作节点 - 设置位置
    /// </summary>
    public static TimelineNode CreateAction_SetPosition(string targetRole, Vector3 position)
    {
        return new TimelineNode
        {
            Type = NodeType.Action,
            DisplayName = $"设置位置 ({targetRole})",
            Parameters = new Dictionary<string, object>
            {
                [ActionNodeParams.ActionType] = TimelineActionType.SetPosition,
                [ActionNodeParams.TargetRole] = targetRole,
                [ActionNodeParams.Position] = $"{position.X},{position.Y},{position.Z}"
            }
        };
    }

    /// <summary>
    /// 创建动作节点 - 发送命令
    /// </summary>
    public static TimelineNode CreateAction_Command(string command, string displayName = "")
    {
        if (string.IsNullOrEmpty(displayName))
            displayName = $"执行命令: {command}";

        return new TimelineNode
        {
            Type = NodeType.Action,
            DisplayName = displayName,
            Parameters = new Dictionary<string, object>
            {
                [ActionNodeParams.ActionType] = TimelineActionType.SendCommand,
                [ActionNodeParams.Command] = command
            }
        };
    }

    /// <summary>
    /// 创建动作节点 - 启用AI
    /// </summary>
    public static TimelineNode CreateAction_EnableAI()
    {
        return new TimelineNode
        {
            Type = NodeType.Action,
            DisplayName = "启用 AI",
            Parameters = new Dictionary<string, object>
            {
                [ActionNodeParams.ActionType] = TimelineActionType.ToggleAI,
                [ActionNodeParams.Command] = "/bmrai on"
            }
        };
    }

    /// <summary>
    /// 创建动作节点 - 禁用AI
    /// </summary>
    public static TimelineNode CreateAction_DisableAI()
    {
        return new TimelineNode
        {
            Type = NodeType.Action,
            DisplayName = "禁用 AI",
            Parameters = new Dictionary<string, object>
            {
                [ActionNodeParams.ActionType] = TimelineActionType.ToggleAI,
                [ActionNodeParams.Command] = "/bmrai off"
            }
        };
    }

    /// <summary>
    /// 获取节点类型的默认图标
    /// </summary>
    public static string GetNodeTypeIcon(NodeType type)
    {
        return type switch
        {
            NodeType.Parallel => "⫴",      // 并行符号
            NodeType.Sequence => "→",      // 箭头
            NodeType.Loop => "↻",          // 循环
            NodeType.Delay => "⏱",         // 计时器
            NodeType.Script => "📝",        // 脚本
            NodeType.Condition => "❓",     // 问号
            NodeType.Action => "⚡",        // 闪电
            _ => "●"
        };
    }

    /// <summary>
    /// 获取节点类型的颜色（RGBA格式）
    /// </summary>
    public static uint GetNodeTypeColor(NodeType type)
    {
        return type switch
        {
            NodeType.Parallel => 0xFF4488FF,     // 蓝色
            NodeType.Sequence => 0xFF44FF44,     // 绿色
            NodeType.Loop => 0xFFFF8844,         // 橙色
            NodeType.Delay => 0xFFFFFF44,        // 黄色
            NodeType.Script => 0xFFFF44FF,       // 紫色
            NodeType.Condition => 0xFF44FFFF,    // 青色
            NodeType.Action => 0xFFFF4444,       // 红色
            _ => 0xFFCCCCCC
        };
    }
}
