using System.Numerics;
using AEAssist;
using AEAssist.CombatRoutine.Module;
using AEAssist.Extension;
using AEAssist.Helper;
using AEAssist.MemoryApi;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace HaiyaBox.Plugin;

public class MoveManage : IDisposable
{
    public static MoveManage Instance { get; } = new();

    public List<MoveInfoDelay> MoveInfoDelays = new List<MoveInfoDelay>();
    public List<MoveInfoTimeout> MoveInfoTimeouts = new List<MoveInfoTimeout>();
    
    private bool initialized;
    private List<Job> jobs =
        [Job.BLM, Job.BLU, Job.PCT, Job.WHM, Job.RDM, Job.ACN, Job.CNJ, Job.SCH, Job.SGE, Job.ACN, Job.SMN, Job.THM];
    private const int DefaultMeleeBufferMs = 100;
    private const int DefaultCasterSafeMs = 400;
    private const int DefaultOtherBufferMs = 50;
    private const int DefaultAssembleDelayMs = 200;
    private const int DefaultGcdMs = 2500;

    public bool TryInitialize()
    {
        if (initialized)
            return true;
        Svc.Commands.AddHandler("/moveinfo", new CommandInfo(OnCommand)
        {
            HelpMessage = ""
        });
        MoveInfoDelays.Clear();
        MoveInfoTimeouts.Clear();
        return true;
    }
    public void Dispose()
    {
        if (!initialized)
            return;
        MoveInfoDelays.Clear();
        MoveInfoTimeouts.Clear();
    }
    public void Update()
    {
        if (!initialized)
            return;
        var 当前战斗时间 = AI.Instance.BattleData.CurrBattleTimeInMs;
        if (MoveInfoTimeouts.Count > 0)
        {
            var moveInfo = MoveInfoTimeouts[0];
            if(SetPosTimeout(moveInfo, 当前战斗时间))
            {
                Core.Me.SetPos(moveInfo.Position);
                MoveInfoTimeouts.RemoveAt(0);
            }
            
        }
        if (MoveInfoDelays.Count > 0)
        {
            var moveInfo = MoveInfoDelays[0];
            if(SetPosDelay(moveInfo, 当前战斗时间))
            {
                Core.Me.SetPos(moveInfo.Position);
                MoveInfoDelays.RemoveAt(0);
            }
        }
    }

    private void OnCommand(string command, string args)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            return;
        }

        var data = args.Split(" ", 3);
        if (data.Length < 3)
            LogHelper.Print("命令少于3个参数");
        if (data[0] == "delay")
        {
            var targetTime = long.Parse(data[1]);
            var pos = data[2].Split(",", 3);
            var position = new Vector3(float.Parse(pos[0]), float.Parse(pos[1]), float.Parse(pos[2]));
            var moveInfo = new MoveInfoDelay
            {
                Position = position,
                BattleTimeInMs = targetTime
            };
            MoveInfoDelays.Add(moveInfo);
        }
        else if (data[0] == "timeout")
        {
            var targetTime = long.Parse(data[1]);
            var pos = data[2].Split(",", 3);
            var position = new Vector3(float.Parse(pos[0]), float.Parse(pos[1]), float.Parse(pos[2]));
            var moveInfo = new MoveInfoTimeout
            {
                Position = position,
                BattleTimeInMs = targetTime
            };
            MoveInfoTimeouts.Add(moveInfo);
        }
    }
    private bool SetPosDelay(MoveInfoDelay moveInfoDelay, long battleTimeMs)
    {
        if (moveInfoDelay.BattleTimeInMs <= battleTimeMs)
            return true;
        var (gcdTotalMs, gcdCurrentMs) = TryGetGcdInfo();

        var (isCasting, castTotalMs, castCurrentMs) = TryGetCastInfo();
        var job = Core.Me.ClassJob.Value.GetJob();
        var currentGcdEndMs = battleTimeMs + GCDHelper.GetGCDCooldown();
        if (isCasting)
        {
            if (jobs.Contains(job))
            {
                var nextSlideStart =
                    CalculateNextGcdSlideWindowStartMs(battleTimeMs, gcdTotalMs, gcdCurrentMs, castTotalMs);
                if (moveInfoDelay.BattleTimeInMs > nextSlideStart)
                    return false;
                var currentSlideStart = CalculateSlideWindowStartMs(battleTimeMs, castTotalMs, castCurrentMs);
                return battleTimeMs >= currentSlideStart;
            }
            else
            {
                var slideStart = CalculateSlideWindowStartMs(battleTimeMs, castTotalMs, castCurrentMs);
                if (moveInfoDelay.BattleTimeInMs >= slideStart)
                    return moveInfoDelay.BattleTimeInMs - battleTimeMs < 300;
                return true;
            }
        }
        else
        {
            if (!jobs.Contains(job))
                return moveInfoDelay.BattleTimeInMs - battleTimeMs < 300;
            else
            {
                var nextSlideStart =
                    CalculateNextGcdSlideWindowStartMs(battleTimeMs, gcdTotalMs, gcdCurrentMs, 2000);
                return moveInfoDelay.BattleTimeInMs <= nextSlideStart;
            }
        }
    }

    private bool SetPosTimeout(MoveInfoTimeout moveInfoTimeout, long battleTimeMs)
    {
        if (battleTimeMs >= moveInfoTimeout.BattleTimeInMs)
            return true;
        var (gcdTotalMs, gcdCurrentMs) = TryGetGcdInfo();
        var (isCasting, castTotalMs, castCurrentMs) = TryGetCastInfo();
        var job = Core.Me.ClassJob.Value.GetJob();
        if (jobs.Contains(job))
        {
            if (Core.Me.IsCasting)
            {
                var slideStart = CalculateSlideWindowStartMs(battleTimeMs, castTotalMs, castCurrentMs);
                return  battleTimeMs >= slideStart;
            }
        }
        return battleTimeMs >= moveInfoTimeout.BattleTimeInMs;
    }
    /// <summary>
    /// 计算下一GCD滑步窗口起点（预测读条）。
    /// </summary>
    public static long CalculateNextGcdSlideWindowStartMs(
        long currentBattleTimeMs,
        int? gcdMs,
        int? gcdRemainingMs,
        int? castTotalMs)
    {
        // 获取当前GCD结束时间
        var currentGcdEndMs = currentBattleTimeMs + GCDHelper.GetGCDCooldown();
        var safeCastTotalMs = castTotalMs.HasValue && castTotalMs.Value > 0
            ? castTotalMs.Value
            : DefaultGcdMs;
        var start = currentGcdEndMs + safeCastTotalMs - DefaultCasterSafeMs;
        return Math.Max(0, start);
    }
    /// <summary>
    /// 计算滑步窗口起点（读条安全起点）。
    /// </summary>
    public static long CalculateSlideWindowStartMs(
        long currentBattleTimeMs,
        int? castTotalMs,
        int? castCurrentMs)
    {
        var safeCastTotalMs = castTotalMs.HasValue && castTotalMs.Value > 0
            ? castTotalMs.Value
            : DefaultGcdMs;
        var safeCastCurrentMs = castCurrentMs.HasValue && castCurrentMs.Value >= 0
            ? castCurrentMs.Value
            : 0;
        safeCastCurrentMs = Math.Clamp(safeCastCurrentMs, 0, safeCastTotalMs);
        var start = currentBattleTimeMs - safeCastCurrentMs + safeCastTotalMs - DefaultCasterSafeMs;
        return Math.Max(0, start);
    }
    private bool IsCastor(IBattleChara? battleChara)
    {
        return battleChara != null && jobs.Contains(battleChara.ClassJob.Value.GetJob());
    }
    private static (int? TotalMs, int? RemainingMs) TryGetGcdInfo()
    {
        unsafe
        {
            var actionManager = ActionManager.Instance();
            if (actionManager == null)
                return (null, null);

            var gcdDetail = actionManager->GetRecastGroupDetail(57);
            if (gcdDetail == null || gcdDetail->Total <= 0)
                return (null, null);

            var totalSeconds = gcdDetail->Total;
            var elapsedSeconds = Math.Clamp(gcdDetail->Elapsed, 0f, totalSeconds);
            var remainingSeconds = Math.Max(0f, totalSeconds - elapsedSeconds);

            var totalMs = (int)Math.Round(totalSeconds * 1000f);
            var remainingMs = (int)Math.Round(remainingSeconds * 1000f);
            return (totalMs, remainingMs);
        }
    }
    private (bool IsCasting, int? CastTotalMs, int? CastCurrentMs) TryGetCastInfo()
    {
        var player = Core.Me;
        if (player == null)
            return (false, null, null);

        var isCasting = player.IsCasting;
        if (!isCasting)
            return (false, null, null);

        var total = player.TotalCastTime;
        var current = player.CurrentCastTime;
        if (total <= 0 || current < 0)
            return (false, null, null);

        var totalMs = (int)Math.Round(total * 1000f);
        var currentMs = (int)Math.Round(current * 1000f);
        return (true, totalMs, currentMs);
    }
}
public class MoveInfoDelay
{
    public long BattleTimeInMs { get; set; }
    public Vector3 Position { get; set; }
}
public class MoveInfoTimeout
{
    public long BattleTimeInMs { get; set; }
    public Vector3 Position { get; set; }
}
