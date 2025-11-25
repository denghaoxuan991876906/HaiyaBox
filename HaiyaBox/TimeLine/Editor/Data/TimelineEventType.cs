using System;
using AEAssist.CombatRoutine.Trigger;

namespace HaiyaBox.TimeLine.Editor.Data;

/// <summary>
/// 触发条件类型
/// </summary>
public enum TriggerConditionType
{
    /// <summary>无条件（始终触发）</summary>
    None,

    /// <summary>敌对咏唱技能</summary>
    EnemyCastSpell,

    /// <summary>创建单位</summary>
    UnitCreate,

    /// <summary>连线/系绳</summary>
    Tether,

    /// <summary>目标标记</summary>
    TargetIcon,

    /// <summary>倒计时（相对时间）</summary>
    GameTime
}

/// <summary>
/// 动作类型
/// </summary>
public enum TimelineActionType
{
    /// <summary>设置位置</summary>
    SetPosition,

    /// <summary>显示危险区域</summary>
    ShowDangerArea,

    /// <summary>发送游戏命令</summary>
    SendCommand,

    /// <summary>执行脚本</summary>
    ExecuteScript,

    /// <summary>启用/禁用AI</summary>
    ToggleAI,

    /// <summary>存储变量到环境</summary>
    StoreVariable,

    /// <summary>清除变量</summary>
    ClearVariable,

    /// <summary>显示通知</summary>
    ShowNotification,

    /// <summary>播放音效</summary>
    PlaySound
}

/// <summary>
/// 节点执行结果
/// </summary>
public enum NodeExecutionResult
{
    /// <summary>继续等待（节点返回false）</summary>
    Waiting,

    /// <summary>执行成功（节点返回true）</summary>
    Success,

    /// <summary>执行失败</summary>
    Failure,

    /// <summary>跳过（节点被禁用）</summary>
    Skipped
}

/// <summary>
/// 条件节点订阅信息
/// </summary>
public class ConditionSubscription
{
    /// <summary>节点ID</summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>条件类型（技能释放/单位生成等）</summary>
    public TriggerConditionType ConditionType { get; set; }

    /// <summary>目标ID（技能ID或单位DataID等）</summary>
    public uint TargetId { get; set; }

    /// <summary>事件匹配时的回调</summary>
    public Action<ITriggerCondParams> OnEventMatched { get; set; } = null!;
}

/// <summary>
/// 条件节点运行时状态
/// </summary>
public class ConditionNodeState
{
    /// <summary>事件是否已匹配</summary>
    public bool EventMatched { get; set; }

    /// <summary>匹配的事件数据（可选，用于调试）</summary>
    public ITriggerCondParams? MatchedEvent { get; set; }
}
