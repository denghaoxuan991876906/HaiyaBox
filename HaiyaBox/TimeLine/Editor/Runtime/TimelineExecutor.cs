using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AEAssist;
using AEAssist.CombatRoutine.Trigger;
using AEAssist.Helper;
using HaiyaBox.TimeLine.Editor.Data;

namespace HaiyaBox.TimeLine.Editor.Runtime;

/// <summary>
/// 时间轴状态机执行引擎
/// </summary>
public sealed class TimelineExecutor : IDisposable
{
    /// <summary>当前执行的时间轴</summary>
    public Timeline? CurrentTimeline { get; private set; }

    /// <summary>是否正在运行</summary>
    public bool IsRunning { get; private set; }

    /// <summary>脚本执行环境</summary>
    public ScriptEnvironment ScriptEnv { get; private set; } = new();

    /// <summary>事件分发器</summary>
    private readonly EventDispatcher _eventDispatcher = new();

    /// <summary>当前活动节点（正在执行的节点）</summary>
    private readonly HashSet<string> _activeNodes = new();

    /// <summary>节点开始时间记录（用于延迟节点）</summary>
    private readonly Dictionary<string, DateTime> _nodeStartTimes = new();

    /// <summary>最近的游戏事件缓存</summary>
    private EnemyCastSpellCondParams? _lastSpellCast;
    private UnitCreateCondParams? _lastUnitCreate;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TimelineExecutor()
    {
        // 订阅事件
        _eventDispatcher.OnEnemyCastSpell += spell => _lastSpellCast = spell;
        _eventDispatcher.OnUnitCreate += unit => _lastUnitCreate = unit;
    }

    /// <summary>
    /// 加载并启动时间轴
    /// </summary>
    public bool Start(Timeline timeline)
    {
        if (IsRunning)
        {
            LogHelper.Print("[时间轴执行器] 已有时间轴正在运行，请先停止");
            return false;
        }

        CurrentTimeline = timeline;
        ScriptEnv.Reset();
        _activeNodes.Clear();
        _nodeStartTimes.Clear();
        _lastSpellCast = null;
        _lastUnitCreate = null;

        // 重置所有节点状态
        ResetNodeStatus(timeline.RootNode);

        // 订阅事件
        _eventDispatcher.Subscribe();

        IsRunning = true;
        LogHelper.Print($"[时间轴执行器] 启动时间轴: {timeline.Name}");

        return true;
    }

    /// <summary>
    /// 停止时间轴
    /// </summary>
    public void Stop()
    {
        if (!IsRunning)
            return;

        IsRunning = false;
        _eventDispatcher.Unsubscribe();
        _activeNodes.Clear();
        _nodeStartTimes.Clear();
        ScriptEnv.Clear();

        LogHelper.Print($"[时间轴执行器] 停止时间轴");
    }

    /// <summary>
    /// 每帧更新 - 在 AutoRaidHelper.Update() 中调用
    /// </summary>
    public void Update()
    {
        if (!IsRunning || CurrentTimeline == null)
            return;

        try
        {
            // 从根节点开始执行
            ExecuteNode(CurrentTimeline.RootNode);
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[时间轴执行器] 更新失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 执行节点
    /// </summary>
    private NodeExecutionResult ExecuteNode(TimelineNode node)
    {
        // 跳过已禁用的节点
        if (!node.Enabled)
        {
            node.Status = NodeStatus.Success;
            return NodeExecutionResult.Skipped;
        }

        // 如果节点已完成，直接返回成功
        if (node.Status == NodeStatus.Success)
            return NodeExecutionResult.Success;

        // 根据节点类型执行不同逻辑
        var result = node.Type switch
        {
            NodeType.Parallel => ExecuteParallel(node),
            NodeType.Sequence => ExecuteSequence(node),
            NodeType.Loop => ExecuteLoop(node),
            NodeType.Delay => ExecuteDelay(node),
            NodeType.Script => ExecuteScript(node),
            NodeType.Condition => ExecuteCondition(node),
            NodeType.Action => ExecuteAction(node),
            _ => NodeExecutionResult.Failure
        };

        // 更新节点状态
        node.Status = result switch
        {
            NodeExecutionResult.Success => NodeStatus.Success,
            NodeExecutionResult.Failure => NodeStatus.Failure,
            NodeExecutionResult.Waiting => NodeStatus.Running,
            _ => node.Status
        };

        return result;
    }

    /// <summary>
    /// 执行并行节点 - 同时执行所有子节点
    /// </summary>
    private NodeExecutionResult ExecuteParallel(TimelineNode node)
    {
        if (node.Children.Count == 0)
            return NodeExecutionResult.Success;

        bool anyReturn = node.Parameters.GetValueOrDefault(ParallelNodeParams.AnyReturn, false) is bool b && b;
        int successCount = 0;
        int totalCount = node.Children.Count;

        foreach (var child in node.Children)
        {
            var result = ExecuteNode(child);

            if (result == NodeExecutionResult.Success)
            {
                successCount++;

                // 如果配置为任意成功则返回，立即返回成功
                if (anyReturn)
                    return NodeExecutionResult.Success;
            }
        }

        // 所有子节点都成功才返回成功
        return successCount == totalCount ? NodeExecutionResult.Success : NodeExecutionResult.Waiting;
    }

    /// <summary>
    /// 执行序列节点 - 按顺序执行子节点
    /// </summary>
    private NodeExecutionResult ExecuteSequence(TimelineNode node)
    {
        if (node.Children.Count == 0)
            return NodeExecutionResult.Success;

        bool ignoreFailure = node.Parameters.GetValueOrDefault(SequenceNodeParams.IgnoreFailure, false) is bool b && b;

        foreach (var child in node.Children)
        {
            var result = ExecuteNode(child);

            // 等待当前节点完成
            if (result == NodeExecutionResult.Waiting)
                return NodeExecutionResult.Waiting;

            // 如果节点失败且不忽略失败，返回失败
            if (result == NodeExecutionResult.Failure && !ignoreFailure)
                return NodeExecutionResult.Failure;
        }

        // 所有节点执行完成
        return NodeExecutionResult.Success;
    }

    /// <summary>
    /// 执行循环节点 - 重复执行子节点N次
    /// </summary>
    private NodeExecutionResult ExecuteLoop(TimelineNode node)
    {
        if (node.Children.Count == 0)
            return NodeExecutionResult.Success;

        // 获取循环次数
        int loopCount = node.Parameters.GetValueOrDefault(LoopNodeParams.LoopCount, 1) is int lc ? lc : 1;
        int currentIndex = node.Parameters.GetValueOrDefault(LoopNodeParams.CurrentIndex, 0) is int ci ? ci : 0;

        if (currentIndex >= loopCount)
            return NodeExecutionResult.Success;

        // 执行所有子节点
        foreach (var child in node.Children)
        {
            var result = ExecuteNode(child);

            if (result == NodeExecutionResult.Waiting)
                return NodeExecutionResult.Waiting;

            if (result == NodeExecutionResult.Failure)
                return NodeExecutionResult.Failure;
        }

        // 一轮完成，重置子节点状态，递增索引
        currentIndex++;
        node.Parameters[LoopNodeParams.CurrentIndex] = currentIndex;

        // 重置所有子节点状态，准备下一轮
        foreach (var child in node.Children)
        {
            ResetNodeStatus(child);
        }

        // 如果还没完成所有循环，继续等待
        return currentIndex >= loopCount ? NodeExecutionResult.Success : NodeExecutionResult.Waiting;
    }

    /// <summary>
    /// 执行延迟节点 - 等待指定时间
    /// </summary>
    private NodeExecutionResult ExecuteDelay(TimelineNode node)
    {
        float delaySeconds = node.Parameters.GetValueOrDefault(DelayNodeParams.DelaySeconds, 0f) is float d ? d : 0f;

        // 记录开始时间
        if (!_nodeStartTimes.ContainsKey(node.Id))
        {
            _nodeStartTimes[node.Id] = DateTime.Now;
        }

        var elapsed = (DateTime.Now - _nodeStartTimes[node.Id]).TotalSeconds;

        if (elapsed >= delaySeconds)
        {
            _nodeStartTimes.Remove(node.Id);
            return NodeExecutionResult.Success;
        }

        return NodeExecutionResult.Waiting;
    }

    /// <summary>
    /// 执行脚本节点 - 执行自定义脚本
    /// </summary>
    private NodeExecutionResult ExecuteScript(TimelineNode node)
    {
        // TODO: 实现脚本编译和执行
        // 目前简单返回成功
        LogHelper.Print($"[时间轴执行器] 执行脚本节点: {node.DisplayName} (暂未实现)");
        return NodeExecutionResult.Success;
    }

    /// <summary>
    /// 执行条件节点 - 检查条件是否满足
    /// </summary>
    private NodeExecutionResult ExecuteCondition(TimelineNode node)
    {
        if (!node.Parameters.TryGetValue(ConditionNodeParams.ConditionType, out var condTypeObj) ||
            condTypeObj is not TriggerConditionType condType)
        {
            return NodeExecutionResult.Failure;
        }

        switch (condType)
        {
            case TriggerConditionType.EnemyCastSpell:
                return CheckSpellCondition(node);

            case TriggerConditionType.UnitCreate:
                return CheckUnitCreateCondition(node);

            case TriggerConditionType.GameTime:
                return CheckGameTimeCondition(node);

            default:
                return NodeExecutionResult.Waiting;
        }
    }

    /// <summary>
    /// 检查技能条件
    /// </summary>
    private NodeExecutionResult CheckSpellCondition(TimelineNode node)
    {
        if (_lastSpellCast == null)
            return NodeExecutionResult.Waiting;

        uint targetSpellId = node.Parameters.GetValueOrDefault(ConditionNodeParams.SpellId, 0u) is uint sid ? sid : 0u;

        if (_lastSpellCast.SpellId == targetSpellId)
        {
            // 清除缓存，避免重复触发
            _lastSpellCast = null;
            return NodeExecutionResult.Success;
        }

        return NodeExecutionResult.Waiting;
    }

    /// <summary>
    /// 检查单位生成条件
    /// </summary>
    private NodeExecutionResult CheckUnitCreateCondition(TimelineNode node)
    {
        if (_lastUnitCreate == null)
            return NodeExecutionResult.Waiting;

        uint targetDataId = node.Parameters.GetValueOrDefault(ConditionNodeParams.UnitDataId, 0u) is uint did ? did : 0u;

        if (_lastUnitCreate.BattleChara.DataId == targetDataId)
        {
            _lastUnitCreate = null;
            return NodeExecutionResult.Success;
        }

        return NodeExecutionResult.Waiting;
    }

    /// <summary>
    /// 检查游戏时间条件
    /// </summary>
    private NodeExecutionResult CheckGameTimeCondition(TimelineNode node)
    {
        float targetTime = node.Parameters.GetValueOrDefault(ConditionNodeParams.TimeoutSeconds, 0f) is float t ? t : 0f;

        return ScriptEnv.ElapsedSeconds >= targetTime ? NodeExecutionResult.Success : NodeExecutionResult.Waiting;
    }

    /// <summary>
    /// 执行动作节点 - 执行具体动作
    /// </summary>
    private NodeExecutionResult ExecuteAction(TimelineNode node)
    {
        if (!node.Parameters.TryGetValue(ActionNodeParams.ActionType, out var actionTypeObj) ||
            actionTypeObj is not TimelineActionType actionType)
        {
            return NodeExecutionResult.Failure;
        }

        try
        {
            switch (actionType)
            {
                case TimelineActionType.SetPosition:
                    return ExecuteSetPosition(node);

                case TimelineActionType.SendCommand:
                    return ExecuteSendCommand(node);

                case TimelineActionType.ToggleAI:
                    return ExecuteToggleAI(node);

                default:
                    LogHelper.Print($"[时间轴执行器] 未实现的动作类型: {actionType}");
                    return NodeExecutionResult.Success;
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[时间轴执行器] 执行动作失败 {node.DisplayName}: {ex.Message}");
            return NodeExecutionResult.Failure;
        }
    }

    /// <summary>
    /// 执行设置位置动作
    /// </summary>
    private NodeExecutionResult ExecuteSetPosition(TimelineNode node)
    {
        string role = node.Parameters.GetValueOrDefault(ActionNodeParams.TargetRole, "") as string ?? "";
        string posStr = node.Parameters.GetValueOrDefault(ActionNodeParams.Position, "") as string ?? "";

        if (string.IsNullOrEmpty(posStr))
            return NodeExecutionResult.Failure;

        // 解析坐标字符串 "x,y,z"
        var parts = posStr.Split(',');
        if (parts.Length != 3 ||
            !float.TryParse(parts[0], out float x) ||
            !float.TryParse(parts[1], out float y) ||
            !float.TryParse(parts[2], out float z))
        {
            return NodeExecutionResult.Failure;
        }

        var position = new Vector3(x, y, z);
        RemoteControlHelper.SetPos(role, position);

        LogHelper.Print($"[时间轴执行器] 设置位置: {role} -> ({x:F1}, {y:F1}, {z:F1})");
        return NodeExecutionResult.Success;
    }

    /// <summary>
    /// 执行发送命令动作
    /// </summary>
    private NodeExecutionResult ExecuteSendCommand(TimelineNode node)
    {
        string command = node.Parameters.GetValueOrDefault(ActionNodeParams.Command, "") as string ?? "";

        if (string.IsNullOrEmpty(command))
            return NodeExecutionResult.Failure;

        RemoteControlHelper.Cmd("", command);
        LogHelper.Print($"[时间轴执行器] 执行命令: {command}");

        return NodeExecutionResult.Success;
    }

    /// <summary>
    /// 执行切换AI动作
    /// </summary>
    private NodeExecutionResult ExecuteToggleAI(TimelineNode node)
    {
        string command = node.Parameters.GetValueOrDefault(ActionNodeParams.Command, "") as string ?? "";

        RemoteControlHelper.Cmd("", command);
        LogHelper.Print($"[时间轴执行器] AI 切换: {command}");

        return NodeExecutionResult.Success;
    }

    /// <summary>
    /// 重置节点状态（递归）
    /// </summary>
    private void ResetNodeStatus(TimelineNode node)
    {
        node.Status = NodeStatus.Pending;

        // 重置循环计数器
        if (node.Type == NodeType.Loop)
        {
            node.Parameters[LoopNodeParams.CurrentIndex] = 0;
        }

        foreach (var child in node.Children)
        {
            ResetNodeStatus(child);
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Stop();
        _eventDispatcher.Dispose();
    }
}

/// <summary>
/// Dictionary 扩展方法
/// </summary>
internal static class DictionaryExtensions
{
    public static TValue? GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, object> dict, TKey key, TValue? defaultValue = default) where TKey : notnull
    {
        if (dict.TryGetValue(key, out var value) && value is TValue typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }
}
