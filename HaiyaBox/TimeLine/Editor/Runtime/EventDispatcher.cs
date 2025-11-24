using System;
using AEAssist.CombatRoutine.Module;
using AEAssist.CombatRoutine.Trigger;
using AEAssist.Helper;
using HaiyaBox.TimeLine.Editor.Data;

namespace HaiyaBox.TimeLine.Editor.Runtime;

/// <summary>
/// 事件分发器 - 监听游戏事件并分发给时间轴执行器
/// </summary>
public sealed class EventDispatcher : IDisposable
{
    private bool _subscribed;

    /// <summary>技能释放事件</summary>
    public event Action<EnemyCastSpellCondParams>? OnEnemyCastSpell;

    /// <summary>单位生成事件</summary>
    public event Action<UnitCreateCondParams>? OnUnitCreate;

    /// <summary>连线事件</summary>
    public event Action<TetherCondParams>? OnTether;

    /// <summary>目标标记事件</summary>
    public event Action<TargetIconEffectTestCondParams>? OnTargetIcon;

    /// <summary>
    /// 开始监听游戏事件
    /// </summary>
    public void Subscribe()
    {
        if (_subscribed)
            return;

        try
        {
            TriggerlineData.OnCondParamsCreate += OnCondParamsCreateEvent;
            _subscribed = true;
            LogHelper.Print("[事件分发器] 已订阅游戏事件");
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[事件分发器] 订阅事件失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 停止监听游戏事件
    /// </summary>
    public void Unsubscribe()
    {
        if (!_subscribed)
            return;

        try
        {
            TriggerlineData.OnCondParamsCreate -= OnCondParamsCreateEvent;
            _subscribed = false;
            LogHelper.Print("[事件分发器] 已取消订阅游戏事件");
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[事件分发器] 取消订阅事件失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理 AEAssist 触发器事件
    /// </summary>
    private void OnCondParamsCreateEvent(ITriggerCondParams condParams)
    {
        try
        {
            switch (condParams)
            {
                case EnemyCastSpellCondParams spell:
                    OnEnemyCastSpell?.Invoke(spell);
                    break;

                case UnitCreateCondParams unit:
                    OnUnitCreate?.Invoke(unit);
                    break;

                case TetherCondParams tether:
                    OnTether?.Invoke(tether);
                    break;

                case TargetIconEffectTestCondParams icon:
                    OnTargetIcon?.Invoke(icon);
                    break;
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[事件分发器] 处理事件失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Unsubscribe();
    }
}
