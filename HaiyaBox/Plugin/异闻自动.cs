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
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaiyaBox.Utils;

namespace HaiyaBox.Plugin;

public class 异闻自动
{
    public static 异闻自动 Instance { get; } = new();
    private Vector3 老一 = new Vector3(375.3f, -29.5f, 534.9f);
    private Vector3 老二 = new Vector3(170.1f, -16.0f, -818.7f);
    private Vector3 老三 = new Vector3(-759, -54, -800);

    private bool 功能启动 = false;
    private bool 无敌挂机 = false;
    private int 进度 = 0;
    private bool 流程结束 = false;
    private DateTime 等待开始时间;
    private bool 正在等待 = false;
    private bool 敌人已选中 = false;
    private bool 老一换位f = false;
    private bool 老二换位f = false;
    private bool 副本结束tp = false;
    private DateTime 流程结束时间 = DateTime.Now;
    private bool 结束指令已发送 = false;
    
    private DateTime 跟随时间 = DateTime.Now;
    private bool 是否遥控位 = false;
    private bool 开始头标 = false;
    private bool 事件启动 = false;
    private bool 无敌已开启 = false;
    private bool 无敌已关闭_战斗结束 = false;
    private bool 传送指令已发送 = false;
    private bool 复活位置指令 = false;
    private float 当前目标血量;

    private uint 上次地图ID = 0;
    private DateTime 进入地图时间 = DateTime.Now;
    private bool 正在等待进入 = false;

    private DateTime 切换Boss时间 = DateTime.Now;
    private bool 正在等待切换Boss = false;
    private bool 已完成一轮检测 = false;

    private DateTime 战斗开始时间 = DateTime.Now;
    private int 战斗进度 = 0;
    private bool 上一帧在战斗中 = false;

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

    private string _addonName = "VVDVoteRoute";
    /// <summary>
    /// 绘制事件记录Tab的UI界面
    /// </summary>
    public void Draw()
    {
        ImGui.Text("异闻自动设置");
        ImGui.Checkbox("启动功能", ref 功能启动);
        ImGui.Checkbox("遥控位", ref 是否遥控位);
        var 无敌 = 无敌挂机;
        if (ImGui.Checkbox("无敌人", ref 无敌))
        {
            无敌挂机 = 无敌;
        }
        
        ImGui.Text("DEBUG");
        
        ImGui.Text($"进度:{进度}");
        ImGui.Text($"战斗时间:{(int)(DateTime.Now - 战斗开始时间).TotalSeconds}");
        ImGui.Text($"老一可选中:{TargetMgr.Instance.EnemysIn20.Values.Any(e => e.BaseId == 19097 && e.IsTargetable)}");
        ImGui.Text($"老二可选中:{TargetMgr.Instance.EnemysIn20.Values.Any(e => e.BaseId == 19226 && e.IsTargetable)}");
        ImGui.Text($"老三可选中:{TargetMgr.Instance.EnemysIn20.Values.Any(e => e.BaseId == 19056 && e.IsTargetable)}");

        var 路线选择ready = Core.Resolve<MemApiAddon>().IsAddonAndNodesReady(_addonName);
        if (路线选择ready)
        {
            ImGui.Text("异变路线选择中：");
            ImGui.Text($"GetNodeText{Core.Resolve<MemApiAddon>().GetAddonValue(_addonName, 31001).String}");
        }
    }


    public void Update()
    {
        商客异闻fa();
    }
    private void 商客异闻fa()
    {
       
    }

    private void 商客异变fa()
    {
        var 当前地图ID = Core.Resolve<MemApiMap>().GetCurrTerrId();
        
        if (当前地图ID != 1317)
        {
            上次地图ID = 当前地图ID;
            return;
        }
        
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
            if (iconEffect.Target == Core.Me && (iconEffect.IconId == 499 || iconEffect.IconId == 185) )
            {
                Core.Me.SetPos(new Vector3(374.3f, -29.6f, 558.9f));
            }
        }
    }
}

public class 商客异变
{
    public static int 进度;
    public static Vector3 重生点;
    public static Vector3 boss1检测点;
    public static Vector3 boss2检测点;
    public static Vector3 boss3检测点;
    public static Vector3 交互位置;
    public static bool 可交互;
    public static bool 交互完成;
    public static bool 交互流程中;
    public static bool 路线选择完成;
    public static uint 当前bossId;
    public static bool 战斗中;
    public static float boss血量;
    public static bool boss已死亡;
    private static List<string> 路线文本 = [];

    public static void Reset()
    {
        可交互 = false;
        交互完成 = false;
        战斗中 = false;
        boss已死亡 = false;
        路线选择完成 = false;
        交互流程中 = false;
        进度 = 0;
        重生点 = boss1检测点;
    }

    public static void Update()
    {
        var mapid = Core.Resolve<MemApiMap>().GetCurrTerrId();
        if (mapid != 1317) return;
        if (Svc.Condition[ConditionFlag.BetweenAreas])return;
        var 交互对象 = Svc.Objects.FirstOrDefault(e => e.Name.TextValue == "" && e.DistanceToPlayer() < 20);
        if (交互对象 != null)
        {
            交互位置 = 交互对象.Position;
            可交互 = 交互对象.IsTargetable;
        }

        if (可交互 && !交互完成 && !交互流程中)
        {
            交互(交互对象);
            return;
        }

        if (VVDVoteRouteHelper.IsAddonOpen() && !路线选择完成)
        {
            List<bool> 完成选择路线 = [];
            完成选择路线.AddRange(路线文本.Select(路线 => VVDVoteRouteHelper.SelectByText(路线)));
            if (完成选择路线.Any())
            {
                路线选择完成 = true;
            }
        }
    }

    private static void 进度更新()
    {
        
    }

    private static void BattleUpdate()
    {
        
    }

    private static async void 交互(IGameObject 交互对象)
    {
        try
        {
            交互流程中 = true;
            Core.Me.SetPos(交互对象.Position);
            await Task.Delay(500);
            交互对象.TargetInteract();
            await Task.Delay(500);
            交互完成 = true;
            交互流程中 = false;
        }
        catch (Exception e)
        {
            LogHelper.Error(e.ToString());
        }
    }
}

