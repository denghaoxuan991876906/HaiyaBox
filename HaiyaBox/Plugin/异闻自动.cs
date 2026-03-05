using System.Numerics;
using System.Runtime.Loader;
using AEAssist;
using AEAssist.CombatRoutine.Module;
using AEAssist.CombatRoutine.Module.Target;
using AEAssist.CombatRoutine.Trigger;
using AEAssist.Extension;
using AEAssist.Helper;
using AEAssist.MemoryApi;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using HaiyaBox.Utils;

namespace HaiyaBox.Plugin;

public class 异闻自动
{
    public static 异闻自动 Instance { get; } = new();
    private Vector3 老一 = new Vector3(375.3f, -29.5f, 534.9f);
    private Vector3 老二 = new Vector3(170.1f, -16.0f, -818.7f);
    private Vector3 老三 = new Vector3(-759, -54, -800);


    private bool 无敌挂机 = false;
    private int 进度 = 0;
    private bool 流程结束 = false;
    private DateTime 等待开始时间;
    private bool 正在等待 = false;
    private bool 敌人已选中 = false;
    private bool 老一换位f = false;
    private bool 老二换位f = false;
    private bool 副本结束tp = false;
    
    private DateTime 跟随时间 = DateTime.Now;
    private bool 开始头标 = false;
    private bool 事件启动 = false;
    private bool 无敌已开启 = false;
    private bool 无敌已关闭_战斗结束 = false;
    private bool 传送指令已发送 = false;

    private readonly uint[] 敌人ID列表 = { 19097, 19226, 19056 };
    /// <summary>
    /// 在模块加载时调用
    /// </summary>
    /// <param name="loadContext">当前插件的加载上下文</param>
    public void OnLoad(AssemblyLoadContext loadContext)
    {
        // 订阅条件参数创建事件回调
        TriggerlineData.OnCondParamsCreate += OnCondParamsCreateEvent;
    }

    /// <summary>
    /// 当插件卸载或者模块释放时调用
    /// </summary>
    public void Dispose()
    {
        // 取消条件参数创建事件回调的注册
        TriggerlineData.OnCondParamsCreate -= OnCondParamsCreateEvent;
    }

    /// <summary>
    /// 绘制事件记录Tab的UI界面
    /// </summary>
    public void Draw()
    {
        ImGui.Text("异闻自动设置");

        var 无敌 = 无敌挂机;
        if (ImGui.Checkbox("无敌人", ref 无敌))
        {
            无敌挂机 = 无敌;
        }
        
        ImGui.Text("DEBUG");
        
        ImGui.Text($"进度:{进度}");
        ImGui.Text($"战斗时间:{AI.Instance.BattleData.CurrBattleTimeInMs}");
        ImGui.Text($"老一可选中:{TargetMgr.Instance.EnemysIn20.Values.Any(e => e.BaseId == 19097 && e.IsTargetable)}");
        ImGui.Text($"老二可选中:{TargetMgr.Instance.EnemysIn20.Values.Any(e => e.BaseId == 19226 && e.IsTargetable)}");
        ImGui.Text($"老三可选中:{TargetMgr.Instance.EnemysIn20.Values.Any(e => e.BaseId == 19056 && e.IsTargetable)}");
    }
    public void Update()
    {
        if (Core.Resolve<MemApiMap>().GetCurrTerrId() != 1317)
            return;

        if (流程结束 && !副本结束tp)
        {
            Core.Me.SetPos(new Vector3(-760.0f, -54.0f, -811.3f));
            事件启动 = false;
            开始头标 = false;
            副本结束tp = true;
            return;
        }

        if (Svc.Condition[ConditionFlag.Unconscious] || Svc.Condition[ConditionFlag.BetweenAreas])
            return;

        if (Core.Me.IsInCombat())
        {
            更新战斗中进度();
            return;
        }

        执行非战斗流程();
    }

    private void 更新战斗中进度()
    {
        int 新进度 = 进度;
        if (TargetMgr.Instance.EnemysIn20.Values.Any(e => e.BaseId == 19097 && e.IsTargetable))
        {
            新进度 = 1;
        }
        if (TargetMgr.Instance.EnemysIn20.Values.Any(e => e.BaseId == 19226 && e.IsTargetable))
        {
            新进度 = 2;
        }
        if (TargetMgr.Instance.EnemysIn20.Values.Any(e => e.BaseId == 19056 && e.IsTargetable))
        {
            新进度 = 3;
        }
        
        if (新进度 != 进度)
        {
            进度 = 新进度;
            无敌已开启 = false;
            传送指令已发送 = false;
            老一换位f = false;
            老二换位f = false;
        }
        
        if (无敌挂机 )
        {
            if (Core.Me.IsTargetable && AI.Instance.BattleData.CurrBattleTimeInMs > 15 * 1000 && !无敌已开启)
            {
                无敌已开启 = true;
                LogHelper.Print("/xsz-invuln on");
            }
            return;
        }
        if (进度 == 1)
        {
            if (AI.Instance.BattleData.CurrBattleTimeInMs > 200 * 1000 && !老一换位f)
            {
                老一换位f = true;
                LogHelper.Print($"/xsz-respawn set 370.3 -29.5 530.4");
            }
        }

        if (进度 == 2)
        {
            if (AI.Instance.BattleData.CurrBattleTimeInMs > 200 * 1000 && !老二换位f)
            {
                老二换位f = true;
                LogHelper.Print($"/xsz-respawn set 170.1 -16.0 -809.7");
            }
        }

        if (进度 == 3)
        {
            if (AI.Instance.BattleData.CurrBattleTimeInMs < 45 * 1000)
            {
                var 自己 = Core.Me;
                var 目标 = 自己.GetCurrTarget();
                if (目标  == null)
                    return;
                if (目标.DistanceToPlayer() > 6)
                {
                    if ((DateTime.Now - 跟随时间).TotalMilliseconds < 500)
                        return;
                    跟随时间 = DateTime.Now;
                    var 坐标 = GeometryUtilsXZ.ExtendPoint(老三, 目标.Position, 5);
                    Core.Me.SetPos(坐标);
                }
            }

            if (!开始头标 && AI.Instance.BattleData.CurrBattleTimeInMs > 45 * 1000)
            {
                事件启动 = true;
                开始头标 = true;
            }
        }
    }

    private void 执行非战斗流程()
    {
        if (进度 >= 3 && !敌人可选中(敌人ID列表[2]))
        {
            流程结束 = true;
            if (!Core.Me.IsTargetable && 无敌挂机 && !无敌已关闭_战斗结束)
            {
                无敌已关闭_战斗结束 = true;
                LogHelper.Print("/xsz-invuln off");
            }
            return;
        }

        for (int i = 0; i < 敌人ID列表.Length; i++)
        {
            if (敌人可选中(敌人ID列表[i]))
            {
                进度 = i + 1;
                敌人已选中 = true;
                正在等待 = false;
                传送指令已发送 = false;
                return;
            }
        }

        if (敌人已选中)
        {
            敌人已选中 = false;
            正在等待 = false;
            return;
        }

        if (!正在等待)
        {
            开始等待传送();
        }
        else
        {
            检查等待结果();
        }
    }

    private void 开始等待传送()
    {
        正在等待 = true;
        等待开始时间 = DateTime.Now;
        
        Vector3 目标位置 = 进度 switch
        {
            0 => 老一,
            1 => 老二,
            2 => 老三,
            _ => 老一
        };
        
        if (!传送指令已发送)
        {
            传送指令已发送 = true;
            Core.Me.SetPos(目标位置);
            if (!Core.Me.IsTargetable && 无敌挂机)
                LogHelper.Print("/xsz-invuln off");
            LogHelper.Print($"/xsz-respawn set {目标位置.X} {目标位置.Y} {目标位置.Z}");
        }
    }

    private void 检查等待结果()
    {
        if (Svc.Condition[ConditionFlag.Unconscious] || Svc.Condition[ConditionFlag.BetweenAreas])
        {
            正在等待 = false;
            return;
        }

        var 等待时间 = (DateTime.Now - 等待开始时间).TotalSeconds;
        
        for (int i = 0; i < 敌人ID列表.Length; i++)
        {
            if (敌人可选中(敌人ID列表[i]))
            {
                进度 = i + 1;
                敌人已选中 = true;
                正在等待 = false;
                return;
            }
        }
        
        if (等待时间 < 5)
            return;


        正在等待 = false;
        传送指令已发送 = false;
    }

    private bool 敌人可选中(uint 敌人ID)
    {
        return TargetMgr.Instance.EnemysIn20.Values.Any(e => e.BaseId == 敌人ID && e.IsTargetable);
    }
    /// <summary>
    /// 事件回调：处理条件参数创建事件（这里主要用于同步事件记录器）
    /// </summary>
    /// <param name="condParams">触发条件参数对象</param>
    private void OnCondParamsCreateEvent(ITriggerCondParams condParams)
    {
        if (!事件启动)
            return;
        if (condParams is TargetIconEffectTestCondParams iconEffect )
        {
            if (iconEffect.Target == Core.Me)
            {
                Core.Me.SetPos(new Vector3(374.3f, -29.6f, 558.9f));
            }
        }

    }
}