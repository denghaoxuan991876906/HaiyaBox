using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using AEAssist;
using AEAssist.CombatRoutine.Module;
using AEAssist.CombatRoutine.Module.Target;
using AEAssist.CombatRoutine.Trigger;
using AEAssist.Extension;
using AEAssist.Helper;
using AEAssist.MemoryApi;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaiyaBox.Settings;
using HaiyaBox.Utils;

namespace HaiyaBox.Plugin;

public class 异闻自动
{
    public static 异闻自动 Instance { get; } = new();
    private const uint 商客异变地图ID = 商客异变.地图ID;
    private const string 路线命令名 = "/vvdroute";
    private AutomationSettings Settings => FullAutoSettings.Instance.AutomationSettings;

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
        Svc.Commands.AddHandler(路线命令名, new CommandInfo(OnRouteCommand)
        {
            HelpMessage = "异闻路线选择。用法: /vvdroute [关键字或正则]；不填则按当前阶段默认路线投票"
        });
    }

    /// <summary>
    /// 当插件卸载或者模块释放时调用
    /// </summary>
    public void Dispose()
    {
        // 取消条件参数创建事件回调的注册
        TriggerlineData.OnCondParamsCreate -= OnCondParamsCreateEvent;
        Svc.Commands.RemoveHandler(路线命令名);
    }

    private string _addonName = "VVDVoteRoute";
    /// <summary>
    /// 绘制事件记录Tab的UI界面
    /// </summary>
    public void Draw()
    {
        var 默认重生点 = Settings.GetOccultRespawnPoint();
        var 当前角色坐标 = Core.Me.Position;
        var 当前目标 = Svc.Targets.Target;
        var 老一坐标 = Settings.GetOccultBossPoint(0);
        var 老二坐标 = Settings.GetOccultBossPoint(1);
        var 老三坐标 = Settings.GetOccultBossPoint(2);
        var 老一Id = Settings.GetOccultBossId(0);
        var 老二Id = Settings.GetOccultBossId(1);
        var 老三Id = Settings.GetOccultBossId(2);

        ImGui.Text("异闻自动设置");
        ImGui.Checkbox("启动功能", ref 功能启动);
        ImGui.Checkbox("遥控位", ref 是否遥控位);
        var 无敌 = 无敌挂机;
        if (ImGui.Checkbox("无敌人", ref 无敌))
        {
            无敌挂机 = 无敌;
        }

        ImGui.Text($"当前角色坐标: X={当前角色坐标.X:F1}, Y={当前角色坐标.Y:F1}, Z={当前角色坐标.Z:F1}");
        ImGui.Text($"默认重生点: X={默认重生点.X:F1}, Y={默认重生点.Y:F1}, Z={默认重生点.Z:F1}");
        if (ImGui.Button("记录当前角色坐标为重生点"))
        {
            Settings.UpdateOccultRespawnPoint(当前角色坐标);
            商客异变.设置默认重生点(当前角色坐标);
            LogHelper.Print($"[商客异闻] 默认重生点已更新为 {当前角色坐标}");
        }

        if (当前目标 != null)
        {
            ImGui.Text($"当前目标: {当前目标.Name.TextValue} BaseId={当前目标.BaseId} 坐标=({当前目标.Position.X:F1}, {当前目标.Position.Y:F1}, {当前目标.Position.Z:F1})");
        }
        else
        {
            ImGui.Text("当前目标: 无");
        }

        DrawBossConfig("老一", 0, 老一Id, 老一坐标, 当前目标);
        DrawBossConfig("老二", 1, 老二Id, 老二坐标, 当前目标);
        DrawBossConfig("老三", 2, 老三Id, 老三坐标, 当前目标);
        
        ImGui.Text("DEBUG");
        
        ImGui.Text($"进度:{进度}");
        ImGui.Text($"商客异变阶段:{商客异变.进度}");
        ImGui.Text($"当前BossId:{商客异变.当前bossId}");
        ImGui.Text($"交互完成:{商客异变.交互完成}");
        ImGui.Text($"路线完成:{商客异变.路线选择完成}");
        ImGui.Text($"战斗中:{商客异变.战斗中}");
        ImGui.Text($"Boss已死亡:{商客异变.boss已死亡}");
        ImGui.Text($"重生点:{商客异变.重生点}");
        ImGui.Text($"交互对象名:{商客异变.交互对象名称}");
        ImGui.Text($"交互对象坐标:{商客异变.交互位置}");
        ImGui.Text($"交互对象可交互:{商客异变.可交互}");
        ImGui.Text($"交互对象候选数:{商客异变.交互对象候选数量}");
        ImGui.Text($"路线窗口文本:{商客异变.路线窗口文本}");
        ImGui.Text($"战斗时间:{AI.Instance.BattleData.CurrBattleTimeInSec}");
        ImGui.Text($"老一可选中:{敌人可选中(老一Id)}");
        ImGui.Text($"老二可选中:{敌人可选中(老二Id)}");
        ImGui.Text($"老三可选中:{敌人可选中(老三Id)}");

        var 路线选择ready = Core.Resolve<MemApiAddon>().IsAddonAndNodesReady(_addonName);
        if (路线选择ready)
        {
            ImGui.Text("异变路线选择中：");
            ImGui.Text($"GetNodeText{Core.Resolve<MemApiAddon>().GetAddonValue(_addonName, 31001).String}");
        }
    }


    public void Update()
    {
        try
        {
            商客异闻fa();
        }
        catch (Exception e)
        {
            LogHelper.Error($"[商客异闻] Update异常: {e}");
        }
        商客异变.更新交互对象();
    }

    private void DrawBossConfig(string 名称, int index, uint bossId, Vector3 坐标, IGameObject? 当前目标)
    {
        ImGui.Text($"{名称}配置: BaseId={bossId} 坐标=({坐标.X:F1}, {坐标.Y:F1}, {坐标.Z:F1})");
        if (ImGui.Button($"记录当前目标为{名称}"))
        {
            if (当前目标 == null)
            {
                LogHelper.Print($"[商客异闻] 记录{名称}失败，当前没有目标");
                return;
            }

            Settings.UpdateOccultBossSlot(index, 当前目标.BaseId, 当前目标.Position);
            LogHelper.Print($"[商客异闻] {名称}配置已更新，BaseId={当前目标.BaseId}，坐标={当前目标.Position}");
        }
    }

    private void 商客异闻fa()
    {
        处理商客异变地图状态();
        if (!功能启动)
            return;

        if (Core.Resolve<MemApiMap>().GetCurrTerrId() == 商客异变地图ID)
        {
            商客异变.Update();
            进度 = 商客异变.进度;
        }
    }

    private void 处理商客异变地图状态()
    {
        var 当前地图ID = Core.Resolve<MemApiMap>().GetCurrTerrId();
        
        if (当前地图ID != 商客异变地图ID)
        {
            if (上次地图ID == 商客异变地图ID)
            {
                LogHelper.Print("[商客异闻] 已离开商客异变地图，重置流程状态");
                商客异变.Reset();
                正在等待进入 = false;
                进度 = 0;
            }

            上次地图ID = 当前地图ID;
            return;
        }

        if (上次地图ID != 商客异变地图ID || !正在等待进入)
        {
            进入地图时间 = DateTime.Now;
            正在等待进入 = true;
            LogHelper.Print("[商客异闻] 检测到进入商客异变地图，开始初始化");
            商客异变.Initialize(
                GetBossPoints(),
                GetBossIds(),
                Settings.GetOccultRespawnPoint());
        }

        上次地图ID = 当前地图ID;
    }
    

    private bool 敌人可选中(uint 敌人ID)
    {
        return TargetMgr.Instance.EnemysIn20.Values.Any(e => e.BaseId == 敌人ID && e.IsTargetable);
    }

    private void OnRouteCommand(string command, string args)
    {
        var pattern = string.IsNullOrWhiteSpace(args) ? null : args.Trim();
        if (!商客异变.手动提交路线投票(pattern))
        {
            LogHelper.Print($"[商客异闻] 路线命令执行失败。命令: {路线命令名} 参数: {(pattern ?? "<当前阶段默认>")}");
        }
    }

    private List<Vector3> GetBossPoints()
    {
        return
        [
            Settings.GetOccultBossPoint(0),
            Settings.GetOccultBossPoint(1),
            Settings.GetOccultBossPoint(2)
        ];
    }

    private List<uint> GetBossIds()
    {
        return
        [
            Settings.GetOccultBossId(0),
            Settings.GetOccultBossId(1),
            Settings.GetOccultBossId(2)
        ];
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
                RemoteControl.SetPos("D1|D2|D3",new Vector3(374.3f, -29.6f, 558.9f));
            }
        }
    }
}

public class 商客异变
{
    public const uint 地图ID = 1316;
    public static int 进度;
    public static Vector3 重生点;
    public static Vector3 默认重生点;
    public static Vector3 boss1检测点;
    public static Vector3 boss2检测点;
    public static Vector3 boss3检测点;
    public static Vector3 交互位置;
    public static string 交互对象名称 = "无";
    public static int 交互对象候选数量;
    public static string 路线窗口文本 = "";
    public static bool 可交互;
    public static bool 交互完成;
    public static bool 交互流程中;
    public static bool 路线选择完成;
    public static uint 当前bossId;
    public static bool 战斗中;
    public static float boss血量;
    public static bool boss已死亡;
    private static readonly TimeSpan 退本等待时间 = TimeSpan.FromSeconds(10);
    private static readonly float 检测点触发距离 = 3f;
    private static readonly float 检敌距离 = 30f;
    private static readonly string[] 阶段名称 =
    [
        "初始化",
        "交互流程1",
        "战斗流程1",
        "交互流程2",
        "战斗流程2",
        "交互流程3",
        "战斗流程3",
        "等待退本",
        "流程结束"
    ];
    private static readonly List<uint> BossId列表 = [];
    private static readonly List<Vector3> Boss检测点列表 = [];
    private static readonly Dictionary<string, (DateTime Time, string Message)> 限频日志状态 = [];
    private static DateTime 战斗结束等待开始 = DateTime.MinValue;
    private static bool 已发送退本指令;
    private static bool 已初始化;
    private static bool 上一帧在战斗中;
    private static bool 战斗刚结束;
    private static bool 等待路线窗口;
    private static bool 路线窗口已出现;
    private static int? 上次路线选择索引;
    private static DateTime 上次路线选择时间 = DateTime.MinValue;
    private static DateTime 移动等待结束时间 = DateTime.MinValue;
    private static DateTime 下次路线命令时间 = DateTime.MinValue;
    private const int 路线选择冷却毫秒 = 2000;

    public static void Reset()
    {
        LogHelper.Print("[商客异变] Reset，清空当前流程状态");
        可交互 = false;
        交互完成 = false;
        战斗中 = false;
        boss已死亡 = false;
        路线选择完成 = false;
        交互流程中 = false;
        进度 = 0;
        当前bossId = 0;
        boss血量 = 0;
        战斗结束等待开始 = DateTime.MinValue;
        已发送退本指令 = false;
        上一帧在战斗中 = false;
        战斗刚结束 = false;
        已初始化 = false;
        等待路线窗口 = false;
        路线窗口已出现 = false;
        上次路线选择索引 = null;
        上次路线选择时间 = DateTime.MinValue;
        移动等待结束时间 = DateTime.MinValue;
        下次路线命令时间 = DateTime.MinValue;
        限频日志状态.Clear();
        重生点 = 默认重生点 != default ? 默认重生点 : boss1检测点;
    }

    public static void Initialize(List<Vector3> bossPoints, List<uint> bossIds, Vector3 默认重生点坐标)
    {
        LogHelper.Print("[商客异闻] 功能启动，开始执行商客异变流程");
        LogHelper.Print($"[商客异变] Initialize，BossID列表: {string.Join(", ", bossIds)}");
        if (bossPoints.Count > 0) boss1检测点 = bossPoints[0];
        if (bossPoints.Count > 1) boss2检测点 = bossPoints[1];
        if (bossPoints.Count > 2) boss3检测点 = bossPoints[2];
        LogHelper.Print($"[商客异变] Initialize，Boss检测点: {boss1检测点} / {boss2检测点} / {boss3检测点}");
        默认重生点 = 默认重生点坐标;

        Boss检测点列表.Clear();
        Boss检测点列表.AddRange(bossPoints);

        BossId列表.Clear();
        BossId列表.AddRange(bossIds);

        Reset();
        已初始化 = true;
        ChatHelper.SendMessage("/xsz-respawn on");
        ChatHelper.SendMessage("/xsz-respawn mode fixed");
        LogHelper.Print($"[商客异变] Initialize完成，地图ID={地图ID}，已初始化={已初始化}");
    }

    public static void 设置默认重生点(Vector3 point)
    {
        默认重生点 = point;
        if (!战斗中)
        {
            重生点 = point;
        }
    }

    public static bool 手动提交路线投票(string? pattern)
    {
        if (!VVDVoteRouteHelper.IsAddonOpen())
        {
            LogHelper.PrintError("[商客异变] 路线窗口未打开，无法提交路线投票");
            return false;
        }

        if (!Core.Resolve<MemApiAddon>().IsAddonAndNodesReady(VVDVoteRouteHelper.AddonName))
        {
            LogHelper.PrintError("[商客异变] 路线窗口节点未Ready，稍后再试");
            return false;
        }

        var 路线文本 = string.IsNullOrWhiteSpace(pattern) ? 获取当前阶段路线文本() : pattern;

        if (string.IsNullOrWhiteSpace(路线文本))
        {
            LogHelper.PrintError("[商客异变] 当前阶段未配置默认路线，请传入关键字或正则");
            return false;
        }
        var 目标索引 = 按正则匹配路线索引(路线文本);
        if (!目标索引.HasValue)
            return false;

        if (执行路线选择(目标索引.Value))
        {
            LogHelper.Print($"[商客异变] 手动路线投票已提交: {路线文本} -> 索引 {目标索引.Value}");
            return true;
        }


        LogHelper.PrintError($"[商客异变] 手动路线投票失败，未命中目标: {string.Join(", ", 路线文本)}");
        return false;
    }

    public static void Update()
    {
        var mapid = Core.Resolve<MemApiMap>().GetCurrTerrId();
        if (mapid != 地图ID || !已初始化) return;
        if (Svc.Condition[ConditionFlag.BetweenAreas])return;
        BattleUpdate();
        进度更新();
    }

    private static void 进度更新()
    {
        switch (进度)
        {
            case 0:
                重生点 = 默认重生点 != default ? 默认重生点 : boss1检测点;
                LogHelper.Print($"[商客异变] 阶段0 初始化，重生点设置为 {重生点}，本阶段不主动传送");
                进入下一阶段();
                break;
            case 1:
                if (执行交互流程())
                {
                    进入下一阶段();
                }
                break;
            case 2:
                if (执行战斗流程(0))
                {
                    进入下一阶段();
                }
                break;
            case 3:
                if (执行交互流程())
                {
                    进入下一阶段();
                }
                break;
            case 4:
                if (执行战斗流程(1))
                {
                    进入下一阶段();
                }
                break;
            case 5:
                if (执行交互流程())
                {
                    进入下一阶段();
                }
                break;
            case 6:
                if (执行战斗流程(2))
                {
                    战斗结束等待开始 = DateTime.Now;
                    进入下一阶段();
                }
                break;
            case 7:
                if (!已发送退本指令 && 战斗结束等待开始 != DateTime.MinValue &&
                    DateTime.Now - 战斗结束等待开始 >= 退本等待时间)
                {
                    发送退本指令();
                    已发送退本指令 = true;
                    进入下一阶段();
                }
                break;
        }
    }

    private static void BattleUpdate()
    {
        var 玩家在战斗中 = Core.Me.InCombat();
        var 当前Boss = 获取当前Boss对象();
        战斗刚结束 = false;

        战斗中 = 玩家在战斗中;
        boss血量 = 当前Boss?.CurrentHp ?? 0;

        if (玩家在战斗中 && !上一帧在战斗中)
        {
            重生点 = 获取当前检测点();
            RemoteControl.Cmd("",$"/xsz-respawn set {重生点.X:f2} {重生点.Y:f2} {重生点.Z:f2}");
            LogHelper.Print($"[商客异变] 进入战斗，更新重生点为 {重生点}，当前BossId={当前bossId}");
        }

        if (上一帧在战斗中 && !玩家在战斗中)
        {
            战斗刚结束 = true;
            boss已死亡 = 当前Boss == null || !当前Boss.IsTargetable || boss血量 <= 1;
            LogHelper.Print($"[商客异变] 战斗结束，BossId={当前bossId}，死亡判定={boss已死亡}");
        }
        else if (当前Boss != null)
        {
            boss已死亡 = 当前Boss.CurrentHp <= 1;
        }

        上一帧在战斗中 = 玩家在战斗中;
    }

    private static bool 执行交互流程()
    {
        更新交互对象();
        var 路线窗口打开 = VVDVoteRouteHelper.IsAddonOpen();
        var 当前阶段路线文本 = 获取当前阶段路线文本();

        if (路线窗口打开)
        {
            路线窗口已出现 = true;
            var 路线窗口Ready = Core.Resolve<MemApiAddon>().IsAddonAndNodesReady(VVDVoteRouteHelper.AddonName);
            路线窗口文本 = 路线窗口Ready ? 获取路线窗口文本() : "<未Ready>";

            if (!路线窗口Ready)
            {
                限频日志("等待路线窗口ready", "[商客异变] 路线窗口已打开，但节点尚未Ready，继续等待");
                if (!string.IsNullOrWhiteSpace(当前阶段路线文本) && DateTime.Now >= 下次路线命令时间)
                {
                    RemoteControl.Cmd("", $"/vvdroute {当前阶段路线文本}");
                    下次路线命令时间 = DateTime.Now.AddSeconds(2);
                }
            }
            else if (当前路线文本已匹配目标(当前阶段路线文本))
            {
                路线选择完成 = true;
                限频日志("路线已匹配目标", $"[商客异变] 路线窗口当前文本已匹配目标: {路线窗口文本}");
            }
            else if (!交互完成)
            {
                限频日志("等待交互完成后选路线", "[商客异变] 路线窗口已出现，等待交互流程结束后再执行路线选择");
            }
            else if (!路线选择完成)
            {
                限频日志("路线选择窗口", "[商客异变] 检测到路线选择窗口，开始尝试选路线");
                尝试选择路线(当前阶段路线文本);
            }
        }

        var 交互对象 = 获取交互对象();
        if (交互对象 != null && !交互完成 && !交互流程中)
        {
            if (移动后等待中())
            {
                限频日志("交互等待移动完成", "[商客异变] 已执行前往位置，等待500ms后继续交互流程");
                return false;
            }

            if (!可交互)
            {
                限频日志("交互等待可交互", $"[商客异变] 交互对象已识别，但当前不可交互，继续等待。位置={交互对象.Position}");
                return false;
            }

            var 距离 = Vector3.Distance(Core.Me.Position, 交互对象.Position);
            if (距离 > 2.5f)
            {
                限频日志("交互靠近目标", $"[商客异变] 交互对象已可交互，开始贴近交互点。位置={交互对象.Position}，距离={距离:F1}");
                前往位置(交互对象.Position);
                return false;
            }

            LogHelper.Print($"[商客异变] 检测到可交互对象，位置={交互对象.Position}");
            交互(交互对象);
        }

        if (交互完成)
        {
            if (等待路线窗口 && !路线窗口已出现)
            {
                限频日志("等待路线窗口出现", "[商客异变] 交互完成，等待路线选择窗口出现");
                return false;
            }

            if (路线窗口已出现 && 路线窗口打开)
            {
                限频日志("等待路线窗口关闭", "[商客异变] 路线选择窗口已出现，等待选择完成并关闭");
                return false;
            }

            if (路线窗口已出现 && !路线窗口打开)
            {
                路线选择完成 = true;
                路线窗口文本 = "";
                LogHelper.Print("[商客异变] 交互流程完成，路线窗口已关闭，进入下一阶段");
                return true;
            }
        }

        return 交互完成 && 路线选择完成;
    }

    private static bool 执行战斗流程(int 序号)
    {
        if (序号 < 0 || 序号 >= Boss检测点列表.Count || 序号 >= BossId列表.Count)
            return false;

        当前bossId = BossId列表[序号];
        var 检测点 = Boss检测点列表[序号];

        if (!战斗中)
        {
            if (移动后等待中())
            {
                限频日志($"战斗等待移动完成{序号}", $"[商客异变] 已执行前往位置，等待500ms后继续战斗流程{序号 + 1}");
                return false;
            }

            限频日志($"战斗靠近检测点{序号}", $"[商客异变] 战斗流程{序号 + 1}，前往检测点 {检测点}，目标BossId={当前bossId}");
            前往位置(检测点);

            if (Vector3.Distance(Core.Me.Position, 检测点) > 检测点触发距离)
                return false;

            var boss = 获取Boss对象(当前bossId);
            if (boss == null || !boss.IsTargetable)
            {
                限频日志($"战斗等待Boss{序号}", $"[商客异变] 战斗流程{序号 + 1}，Boss未出现或不可选中，继续等待。BossId={当前bossId}");
                return false;
            }

            重生点 = 检测点;
            boss已死亡 = false;
            LogHelper.Print($"[商客异变] 战斗流程{序号 + 1}，Boss已可选中，等待进入战斗。重生点更新为 {重生点}");
            return false;
        }

        if (boss已死亡 || 战斗刚结束)
        {
            LogHelper.Print($"[商客异变] 战斗流程{序号 + 1} 完成，boss已死亡={boss已死亡}，战斗刚结束={战斗刚结束}");
        }
        return boss已死亡 || 战斗刚结束;
    }

    private static void 进入下一阶段()
    {
        if (进度 < 阶段名称.Length - 1)
        {
            进度++;
            交互完成 = false;
            交互流程中 = false;
            可交互 = false;
            路线选择完成 = 获取当前阶段路线文本().Length == 0;
            等待路线窗口 = false;
            路线窗口已出现 = false;
            路线窗口文本 = "";
            上次路线选择索引 = null;
            上次路线选择时间 = DateTime.MinValue;
            下次路线命令时间 = DateTime.MinValue;
            当前bossId = 0;
            LogHelper.Print($"[商客异变] 进入阶段: {阶段名称[进度]}");
        }
    }

    public static void 更新交互对象()
    {
        var 交互对象 = 获取交互对象();
        if (交互对象 == null)
        {
            交互对象名称 = "无";
            交互位置 = default;
            交互对象候选数量 = 0;
            可交互 = false;
            return;
        }

        交互对象候选数量 = 获取交互对象候选列表().Count;
        交互对象名称 = string.IsNullOrWhiteSpace(交互对象.Name.TextValue) ? "<空名对象>" : 交互对象.Name.TextValue;
        交互位置 = 交互对象.Position;
        可交互 = 交互对象.IsTargetable;
        /*if (!可交互)return;
        LogHelper.Print($"[商客异变] 当前交互对象位置={交互位置}，可交互={可交互}");*/
    }

    private static void 限频日志(string key, string message, double intervalSeconds = 1.5)
    {
        var now = DateTime.Now;
        if (限频日志状态.TryGetValue(key, out var state))
        {
            if (state.Message == message && (now - state.Time).TotalSeconds < intervalSeconds)
                return;
        }

        限频日志状态[key] = (now, message);
        LogHelper.Print(message);
    }

    private static IGameObject? 获取交互对象()
    {
        return 获取交互对象候选列表().FirstOrDefault();
    }

    private static List<IGameObject> 获取交互对象候选列表()
    {
        return Svc.Objects
            .Where(e => e.Name.TextValue == "选择目的地" && e.DistanceToPlayer() < 30)
            .OrderByDescending(e => e.IsTargetable)
            .ThenBy(e => e.DistanceToPlayer())
            .ToList();
    }

    private static string 获取当前阶段路线文本()
    {
        return 进度 switch
        {
            1 => "人鱼",
            3 => "刚剑",
            5 => "睡火",
            _ => ""
        };
    }

    private static void 尝试选择路线(string 路线文本)
    {
        if (路线文本.Length == 0)
        {
            LogHelper.Print("[商客异变] 未配置路线文本，默认视为路线已完成");
            路线选择完成 = true;
            return;
        }

        {
            var 目标索引 = 按正则匹配路线索引(路线文本);
            if (!目标索引.HasValue)
            {
                return;
            }

            if (应跳过本次路线选择(目标索引.Value))
                return;

            if (执行路线选择(目标索引.Value))
            {
                路线选择完成 = true;
                上次路线选择索引 = 目标索引.Value;
                上次路线选择时间 = DateTime.Now;
                LogHelper.Print($"[商客异变] 本地路线投票已提交: {路线文本} -> 索引 {目标索引.Value}");
                return;
            }
        }

        LogHelper.Print("[商客异变] 路线选择失败，当前没有命中任何路线文本");
    }

    private static int? 按正则匹配路线索引(string pattern)
    {
        try
        {
            var regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var entries = VVDVoteRouteHelper.GetEntries();
            for (var i = 0; i < entries.Count; i++)
            {
                if (regex.IsMatch(entries[i].Text))
                {
                    return entries[i].Index;
                }
            }

            LogHelper.PrintError($"[商客异变] 未找到正则匹配路线: {pattern}");
            return null;
        }
        catch (Exception ex)
        {
            LogHelper.PrintError($"[商客异变] 路线正则无效: {pattern}，错误: {ex.Message}");
            return null;
        }
    }

    private static bool 应跳过本次路线选择(int targetIndex)
    {
        return 上次路线选择索引 == targetIndex &&
               (DateTime.Now - 上次路线选择时间).TotalMilliseconds < 路线选择冷却毫秒;
    }

    private static bool 执行路线选择(int index)
    {
        return VVDVoteRouteHelper.SelectByIndex(index);
    }

    private static string 获取路线窗口文本()
    {
        try
        {
            return Core.Resolve<MemApiAddon>().GetAddonValue(VVDVoteRouteHelper.AddonName, 31001).String ?? "";
        }
        catch
        {
            return "";
        }
    }

    private static bool 当前路线文本已匹配目标(string patterns)
    {
        if (patterns==""|| string.IsNullOrWhiteSpace(路线窗口文本))
            return false;
        try
        {
            if (Regex.IsMatch(路线窗口文本, patterns, RegexOptions.IgnoreCase))
                return true;
        }
        catch
        {
        }


        return false;
    }

    private static IBattleChara? 获取当前Boss对象()
    {
        if (当前bossId == 0)
            return null;

        return 获取Boss对象(当前bossId);
    }

    private static IBattleChara? 获取Boss对象(uint bossId)
    {
        return TargetMgr.Instance.EnemysIn20.Values
            .FirstOrDefault(e => e.BaseId == bossId && e.DistanceToPlayer() <= 检敌距离);
    }

    private static Vector3 获取当前检测点()
    {
        var bossIndex = BossId列表.IndexOf(当前bossId);
        if (bossIndex >= 0 && bossIndex < Boss检测点列表.Count)
            return Boss检测点列表[bossIndex];

        return 重生点;
    }

    private static void 前往位置(Vector3 位置)
    {
        if (Vector3.Distance(Core.Me.Position, 位置) <= 0.5f)
            return;

        LogHelper.Print($"[商客异变] 传送到位置 {位置}");
        RemoteControl.Cmd("",$"/xsz-smarttp {位置.X} {位置.Y} {位置.Z}");
        移动等待结束时间 = DateTime.Now.AddMilliseconds(500);
    }

    private static void 前往重生点()
    {
        if (重生点 != default)
        {
            前往位置(重生点);
        }
    }

    private static bool 移动后等待中()
    {
        return DateTime.Now < 移动等待结束时间;
    }

    private static void 发送退本指令()
    {
        LogHelper.Print("[商客异变] 最终Boss完成，等待10秒后发送退本指令");
        if (FullAutoSettings.Instance.AutomationSettings.DRCmdEnabled)
            RemoteControl.Cmd("", "/pdr leaveduty");
        else
            RemoteControl.Cmd("", "/xsz-leaveduty");
        ChatHelper.SendMessage("/xsz-respawn off");
    }

    private static async void 交互(IGameObject 交互对象)
    {
        try
        {
            交互流程中 = true;
            LogHelper.Print($"[商客异变] 开始交互，目标位置={交互对象.Position}");
            Core.Me.SetPos(交互对象.Position);
            await Task.Delay(500);
            交互对象.TargetInteract();
            await Task.Delay(500);
            交互完成 = true;
            交互流程中 = false;
            等待路线窗口 = true;
            路线窗口已出现 = false;
            路线窗口文本 = "";
            上次路线选择索引 = null;
            上次路线选择时间 = DateTime.MinValue;
            LogHelper.Print("[商客异变] 交互完成");
        }
        catch (Exception e)
        {
            LogHelper.Error(e.ToString());
        }
    }
}
