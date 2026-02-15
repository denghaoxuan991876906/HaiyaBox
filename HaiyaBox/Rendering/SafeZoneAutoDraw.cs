using AEAssist;
using AEAssist.Extension;
using AEAssist.MemoryApi;
using AOESafetyCalculator.Core;
using AOESafetyCalculator.SafetyZone;
using ECommons.DalamudServices;
using HaiyaBox.Settings;

namespace HaiyaBox.Rendering;

public readonly record struct SafeZoneDrawStats(int ArenaCount, int ActiveZoneCount, int SafePointCount);

public static class SafeZoneAutoDraw
{
    private const float DefaultGridStep = 0.25f;
    private const float DefaultFallbackRadius = 40f;
    private const float OutlineThickness = 2f;
    private const float SafePointRadius = 6f;
    private const float SafePointLabelHeight = 0.5f;
    private const float BattleResetThreshold = 5f;

    private static DangerAreaRenderer? _renderer;
    private static Func<List<DisplayObject>>? _callback;
    private static bool _eventsBound;
    private static bool _wasInDuty;
    private static bool _wasInCombat;
    private static DateTime? _combatStartTime;

    public static bool Enabled => FullAutoSettings.Instance.FaGeneralSetting.SafeZoneAutoDrawEnabled;

    public static void Initialize(DangerAreaRenderer renderer)
    {
        if (_renderer != null) return;
        _renderer = renderer;
        _callback = BuildDisplayObjects;
        _renderer.RegisterTempObjectCallback(_callback);
        BindEvents();
    }

    public static void Dispose()
    {
        if (_renderer != null && _callback != null)
        {
            _renderer.UnregisterTempObjectCallback(_callback);
        }
        _renderer = null;
        _callback = null;
        UnbindEvents();
    }

    public static void Update()
    {
        var inDuty = IsInDuty();
        if (_wasInDuty && !inDuty)
        {
            ClearAll();
        }
        else if (!_wasInDuty && inDuty)
        {
            ClearAll();
        }
        _wasInDuty = inDuty;

        var now = DateTime.UtcNow;
        var inCombat = IsInCombat();
        if (inDuty)
        {
            if (inCombat && !_wasInCombat)
            {
                _combatStartTime = now;
            }
            else if (!inCombat && _wasInCombat && _combatStartTime.HasValue)
            {
                var combatSeconds = (now - _combatStartTime.Value).TotalSeconds;
                if (combatSeconds >= BattleResetThreshold)
                {
                    ClearAll();
                }
                _combatStartTime = null;
            }
        }
        else
        {
            _combatStartTime = null;
        }
        _wasInCombat = inCombat;
    }

    public static void ClearAll()
    {
        SafeZoneDrawRegistry.ClearAll();
    }

    public static SafeZoneDrawStats GetStats()
    {
        var now = DateTime.Now;
        var arenaCount = 0;
        var zoneCount = 0;
        var safePointCount = 0;

        foreach (var calculator in SafeZoneDrawRegistry.GetLiveCalculators())
        {
            var arena = calculator.GetArenaBounds();
            if (arena != null)
            {
                arenaCount++;
            }

            foreach (var zone in calculator.GetZones())
            {
                if (zone.IsActive(now))
                {
                    zoneCount++;
                }
            }

            if (SafeZoneDrawRegistry.TryGetSafePoints(calculator, out var points))
            {
                safePointCount += points.Count;
            }
        }

        return new SafeZoneDrawStats(arenaCount, zoneCount, safePointCount);
    }

    private static List<DisplayObject> BuildDisplayObjects()
    {
        var result = new List<DisplayObject>();
        if (!Enabled) return result;

        var now = DateTime.Now;
        var player = Svc.ClientState.LocalPlayer;
        var height = player?.Position.Y ?? 0f;
        var fallbackCenter = player == null ? new WPos(100f, 100f) : WPos.FromVec3(player.Position);

        foreach (var calculator in SafeZoneDrawRegistry.GetLiveCalculators())
        {
            var arena = calculator.GetArenaBounds();
            var center = arena?.Center ?? fallbackCenter;
            var radius = MathF.Max(arena?.ApproximateRadius ?? DefaultFallbackRadius, 1f);

            if (arena != null)
            {
                result.AddRange(DistanceFieldContourBuilder.Build(
                    p => -arena.DistanceToBorder(p),
                    center,
                    radius,
                    DefaultGridStep,
                    height,
                    ArenaColor,
                    OutlineThickness));
            }

            foreach (var zone in calculator.GetZones())
            {
                if (!zone.IsActive(now)) continue;
                result.AddRange(DistanceFieldContourBuilder.Build(
                    p => zone.Shape.Distance(p),
                    center,
                    radius,
                    DefaultGridStep,
                    height,
                    DangerColor,
                    OutlineThickness));
            }

            if (SafeZoneDrawRegistry.TryGetSafePoints(calculator, out var points))
            {
                for (var i = 0; i < points.Count; i++)
                {
                    var p = points[i];
                    var pos = new System.Numerics.Vector3(p.X, height, p.Z);
                    result.Add(new DisplayObjectDot(pos, SafePointRadius, SafePointColor));
                    var labelPos = pos + new System.Numerics.Vector3(0f, SafePointLabelHeight, 0f);
                    result.Add(new DisplayObjectText(labelPos, (i + 1).ToString(), LabelBackgroundColor, LabelTextColor, 1f));
                }
            }
        }

        return result;
    }

    private static void BindEvents()
    {
        if (_eventsBound) return;
        _eventsBound = true;
        if (Svc.DutyState != null)
        {
            Svc.DutyState.DutyCompleted += OnDutyCompleted;
            Svc.DutyState.DutyWiped += OnDutyWiped;
        }
        if (Svc.ClientState != null)
        {
            Svc.ClientState.TerritoryChanged += OnTerritoryChanged;
        }
    }

    private static void UnbindEvents()
    {
        if (!_eventsBound) return;
        _eventsBound = false;
        if (Svc.DutyState != null)
        {
            Svc.DutyState.DutyCompleted -= OnDutyCompleted;
            Svc.DutyState.DutyWiped -= OnDutyWiped;
        }
        if (Svc.ClientState != null)
        {
            Svc.ClientState.TerritoryChanged -= OnTerritoryChanged;
        }
    }

    private static void OnDutyCompleted(object? sender, ushort e) => ClearAll();
    private static void OnDutyWiped(object? sender, ushort e) => ClearAll();
    private static void OnTerritoryChanged(ushort e) => ClearAll();

    private static bool IsInDuty()
    {
        try
        {
            return Core.Resolve<MemApiDuty>().IsBoundByDuty();
        }
        catch
        {
            return false;
        }
    }

    private static bool IsInCombat()
    {
        try
        {
            return Core.Me.InCombat();
        }
        catch
        {
            return false;
        }
    }

    private static readonly uint DangerColor = PackColor(1f, 0.2f, 0.2f, 0.85f);
    private static readonly uint ArenaColor = PackColor(1f, 0f, 0f, 0.5f);
    private static readonly uint SafePointColor = PackColor(0.2f, 0.8f, 0.2f, 0.95f);
    private static readonly uint LabelBackgroundColor = PackColor(0.05f, 0.05f, 0.05f, 0.8f);
    private static readonly uint LabelTextColor = PackColor(1f, 1f, 1f, 1f);

    private static uint PackColor(float r, float g, float b, float a)
    {
        var cr = (uint)(r * 255f + 0.5f);
        var cg = (uint)(g * 255f + 0.5f);
        var cb = (uint)(b * 255f + 0.5f);
        var ca = (uint)(a * 255f + 0.5f);
        return (ca << 24) | (cb << 16) | (cg << 8) | cr;
    }
}
