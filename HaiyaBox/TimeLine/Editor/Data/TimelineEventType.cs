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
