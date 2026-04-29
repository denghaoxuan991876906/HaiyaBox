using System;
using System.Collections.Generic;
using System.Numerics;
using AOESafetyCalculator.Core;
using AOESafetyCalculator.Shapes;
using ECommons.DalamudServices;
using HaiyaBox.Rendering;

namespace HaiyaBox.Utils;

public static class AOEShapeDebug
{
    private const float LineThickness = 3f;
    private const float MinDurationSeconds = 0.1f;
    private const int CircleSegments = 64;
    private const int MinArcSegments = 6;

    private static readonly uint[] Palette =
    [
        PackColor(0.95f, 0.25f, 0.25f, 0.85f), // red
        PackColor(0.25f, 0.9f, 0.35f, 0.85f),  // green
        PackColor(0.2f, 0.6f, 1f, 0.85f),      // blue
        PackColor(0.95f, 0.85f, 0.2f, 0.85f),  // yellow
        PackColor(0.85f, 0.35f, 0.95f, 0.85f)  // magenta
    ];

    private sealed class Entry
    {
        public AOEShape Shape { get; init; } = null!;
        public WPos Origin { get; init; }
        public DateTime ExpiresAt { get; init; }
        public uint Color { get; init; }
        public float Height { get; init; }
    }

    private static readonly object Sync = new();
    private static readonly List<Entry> Entries = new();
    private static DangerAreaRenderer? _renderer;
    private static Func<List<DisplayObject>>? _callback;

    public static void Initialize(DangerAreaRenderer renderer)
    {
        _renderer = renderer;
        _callback = BuildDisplayObjects;
        _renderer.RegisterTempObjectCallback(_callback);
    }

    public static void Dispose()
    {
        if (_renderer != null && _callback != null)
        {
            _renderer.UnregisterTempObjectCallback(_callback);
        }
        _renderer = null;
        _callback = null;
        lock (Sync)
        {
            Entries.Clear();
        }
    }

    public static void Clear()
    {
        lock (Sync)
        {
            Entries.Clear();
        }
    }

    public static void Draw(AOEShape shape, WPos origin, float durationSeconds)
    {
        if (shape == null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var height = Svc.Objects?.LocalPlayer?.Position.Y ?? 0f;
        var color = Palette[GetColorBucket(shape, origin) % Palette.Length];
        var duration = MathF.Max(durationSeconds, MinDurationSeconds);

        lock (Sync)
        {
            Entries.Add(new Entry
            {
                Shape = shape,
                Origin = origin,
                ExpiresAt = now.AddSeconds(duration),
                Color = color,
                Height = height
            });
        }
    }

    internal static int GetColorBucket(AOEShape shape, WPos origin)
    {
        var hash = 2166136261u;
        hash = Fnv1A(hash, shape.GetType().Name);
        switch (shape)
        {
            case AOEShapeCircle circle:
                hash = Fnv1A(hash, circle.Radius);
                break;
            case AOEShapeCone cone:
                hash = Fnv1A(hash, cone.Radius);
                hash = Fnv1A(hash, cone.HalfAngle.Rad);
                break;
            case AOEShapeDonut donut:
                hash = Fnv1A(hash, donut.InnerRadius);
                hash = Fnv1A(hash, donut.OuterRadius);
                break;
            case AOEShapeDonutSector donutSector:
                hash = Fnv1A(hash, donutSector.InnerRadius);
                hash = Fnv1A(hash, donutSector.OuterRadius);
                hash = Fnv1A(hash, donutSector.HalfAngle.Rad);
                break;
            case AOEShapeRect rect:
                hash = Fnv1A(hash, rect.LengthFront);
                hash = Fnv1A(hash, rect.LengthBack);
                hash = Fnv1A(hash, rect.HalfWidth);
                break;
            case AOEShapeCross cross:
                hash = Fnv1A(hash, cross.Length);
                hash = Fnv1A(hash, cross.HalfWidth);
                break;
            case AOEShapeTriCone tri:
                hash = Fnv1A(hash, tri.SideLength);
                hash = Fnv1A(hash, tri.HalfAngle.Rad);
                break;
            case AOEShapeCapsule capsule:
                hash = Fnv1A(hash, capsule.Radius);
                hash = Fnv1A(hash, capsule.Length);
                break;
            case AOEShapeArcCapsule arc:
                hash = Fnv1A(hash, arc.Radius);
                hash = Fnv1A(hash, arc.AngularLength.Rad);
                hash = Fnv1A(hash, (origin - arc.OrbitCenter).Length());
                break;
        }

        return (int)hash;
    }

    internal static IReadOnlyList<DisplayObject> BuildDisplayObjectsFor(AOEShape shape, WPos origin, float height, uint color)
    {
        var result = new List<DisplayObject>();
        switch (shape)
        {
            case AOEShapeCircle circle:
                result.Add(BuildCircle(origin, height, circle.Radius, color));
                break;
            case AOEShapeDonut donut:
                result.Add(BuildCircle(origin, height, donut.OuterRadius, color));
                if (donut.InnerRadius > 0f)
                {
                    result.Add(BuildCircle(origin, height, donut.InnerRadius, color));
                }
                break;
            case AOEShapeCone cone:
                result.AddRange(BuildCone(cone, origin, height, color));
                break;
            case AOEShapeDonutSector donutSector:
                result.AddRange(BuildDonutSector(donutSector, origin, height, color));
                break;
            case AOEShapeRect rect:
                result.AddRange(BuildRect(rect, origin, height, color));
                break;
            case AOEShapeCross cross:
                result.AddRange(BuildCross(cross, origin, height, color));
                break;
            case AOEShapeTriCone tri:
                result.AddRange(BuildTri(tri, origin, height, color));
                break;
            case AOEShapeCapsule capsule:
                result.AddRange(BuildCapsule(capsule, origin, height, color));
                break;
            case AOEShapeArcCapsule arc:
                result.AddRange(BuildArcCapsule(arc, origin, height, color));
                break;
        }

        return result;
    }

    private static List<DisplayObject> BuildDisplayObjects()
    {
        var now = DateTime.UtcNow;
        List<Entry> snapshot;
        lock (Sync)
        {
            for (var i = Entries.Count - 1; i >= 0; --i)
            {
                if (Entries[i].ExpiresAt <= now)
                {
                    Entries.RemoveAt(i);
                }
            }
            snapshot = new List<Entry>(Entries);
        }

        var result = new List<DisplayObject>();
        foreach (var entry in snapshot)
        {
            result.AddRange(BuildDisplayObjectsFor(entry.Shape, entry.Origin, entry.Height, entry.Color));
        }

        return result;
    }

    private static DisplayObjectCircle BuildCircle(WPos origin, float height, float radius, uint color)
    {
        var center = new Vector3(origin.X, height, origin.Z);
        return new DisplayObjectCircle(center, radius, color, LineThickness, false);
    }

    private static IEnumerable<DisplayObject> BuildCone(AOEShapeCone cone, WPos origin, float height, uint color)
    {
        var dir = cone.Direction;
        var start = dir - cone.HalfAngle;
        var end = dir + cone.HalfAngle;
        var points = new List<Vector3>();
        points.Add(ToVector3(origin, height));
        AppendArc(points, origin, cone.Radius, start.Rad, end.Rad, height, false);
        return BuildLineLoop(points, color);
    }

    private static IEnumerable<DisplayObject> BuildDonutSector(AOEShapeDonutSector sector, WPos origin, float height, uint color)
    {
        var dir = sector.Direction;
        var start = dir - sector.HalfAngle;
        var end = dir + sector.HalfAngle;
        var points = new List<Vector3>();
        AppendArc(points, origin, sector.OuterRadius, start.Rad, end.Rad, height, false);
        var innerRadius = MathF.Max(0f, sector.InnerRadius);
        if (innerRadius > 0f)
        {
            AppendArc(points, origin, innerRadius, end.Rad, start.Rad, height, true);
        }
        else
        {
            points.Add(ToVector3(origin, height));
        }
        return BuildLineLoop(points, color);
    }

    private static IEnumerable<DisplayObject> BuildRect(AOEShapeRect rect, WPos origin, float height, uint color)
    {
        var dir = rect.Direction.ToDirection();
        var normal = dir.OrthoL();
        var front = origin + dir * rect.LengthFront;
        var back = origin - dir * rect.LengthBack;
        var left = normal * rect.HalfWidth;

        var points = new List<Vector3>
        {
            ToVector3(front + left, height),
            ToVector3(front - left, height),
            ToVector3(back - left, height),
            ToVector3(back + left, height)
        };

        return BuildLineLoop(points, color);
    }

    private static IEnumerable<DisplayObject> BuildCross(AOEShapeCross cross, WPos origin, float height, uint color)
    {
        var dir = cross.Direction.ToDirection();
        var normal = dir.OrthoL();
        var len = cross.Length;
        var hw = cross.HalfWidth;

        Vector3 World(float a, float b)
        {
            var p = origin + dir * a + normal * b;
            return ToVector3(p, height);
        }

        var points = new List<Vector3>
        {
            World(len, hw),
            World(hw, hw),
            World(hw, len),
            World(-hw, len),
            World(-hw, hw),
            World(-len, hw),
            World(-len, -hw),
            World(-hw, -hw),
            World(-hw, -len),
            World(hw, -len),
            World(hw, -hw),
            World(len, -hw)
        };

        return BuildLineLoop(points, color);
    }

    private static IEnumerable<DisplayObject> BuildTri(AOEShapeTriCone tri, WPos origin, float height, uint color)
    {
        var dir = tri.Direction;
        var left = (dir + tri.HalfAngle).ToDirection() * tri.SideLength;
        var right = (dir - tri.HalfAngle).ToDirection() * tri.SideLength;
        var points = new List<Vector3>
        {
            ToVector3(origin, height),
            ToVector3(origin + left, height),
            ToVector3(origin + right, height)
        };

        return BuildLineLoop(points, color);
    }

    private static IEnumerable<DisplayObject> BuildCapsule(AOEShapeCapsule capsule, WPos origin, float height, uint color)
    {
        var dir = capsule.Direction.ToDirection();
        var angle = capsule.Direction.Rad;
        var start = origin;
        var end = origin + dir * capsule.Length;
        var points = new List<Vector3>();

        AppendArc(points, end, capsule.Radius, angle + Angle.HalfPi, angle - Angle.HalfPi, height, false);
        AppendArc(points, start, capsule.Radius, angle - Angle.HalfPi, angle + Angle.HalfPi, height, true);

        return BuildLineLoop(points, color);
    }

    private static IEnumerable<DisplayObject> BuildArcCapsule(AOEShapeArcCapsule arc, WPos origin, float height, uint color)
    {
        var tube = arc.Radius;
        var orbit = arc.OrbitCenter;
        var r0 = origin - orbit;
        var R = r0.Length();
        if (R <= 0f)
        {
            return BuildLineLoop(BuildCircleLoop(origin, height, tube), color);
        }

        var theta0 = Angle.FromDirection(r0).Rad;
        var theta1 = theta0 + arc.AngularLength.Rad;
        var sign = arc.AngularLength.Rad >= 0f ? 1f : -1f;
        var outerR = R + tube;
        var innerR = MathF.Max(0f, R - tube);
        var endCenter = orbit + new WDir(MathF.Sin(theta1), MathF.Cos(theta1)) * R;

        var points = new List<Vector3>();
        AppendArc(points, orbit, outerR, theta0, theta1, height, false);
        AppendArc(points, endCenter, tube, theta1, theta1 + sign * MathF.PI, height, true);
        if (innerR > 0f)
        {
            AppendArc(points, orbit, innerR, theta1, theta0, height, true);
        }
        else
        {
            points.Add(ToVector3(orbit, height));
        }
        AppendArc(points, origin, tube, theta0 + sign * MathF.PI, theta0, height, true);

        return BuildLineLoop(points, color);
    }

    private static List<Vector3> BuildCircleLoop(WPos origin, float height, float radius)
    {
        var points = new List<Vector3>(CircleSegments);
        AppendArc(points, origin, radius, 0f, MathF.Tau, height, false);
        return points;
    }

    private static IEnumerable<DisplayObject> BuildLineLoop(IReadOnlyList<Vector3> points, uint color)
    {
        if (points.Count < 2)
        {
            yield break;
        }
        for (var i = 0; i < points.Count; i++)
        {
            var a = points[i];
            var b = points[(i + 1) % points.Count];
            yield return new DisplayObjectLine(a, b, color, LineThickness);
        }
    }

    private static void AppendArc(List<Vector3> points, WPos center, float radius, float start, float end, float height, bool skipFirst)
    {
        var span = end - start;
        var segments = Math.Clamp((int)MathF.Ceiling(MathF.Abs(span) / MathF.Tau * CircleSegments), MinArcSegments, CircleSegments);
        for (var i = 0; i <= segments; i++)
        {
            if (skipFirst && i == 0)
            {
                continue;
            }
            var t = i / (float)segments;
            var angle = start + span * t;
            points.Add(new Vector3(
                center.X + radius * MathF.Sin(angle),
                height,
                center.Z + radius * MathF.Cos(angle)));
        }
    }

    private static Vector3 ToVector3(WPos pos, float height) => new(pos.X, height, pos.Z);

    private static uint PackColor(float r, float g, float b, float a)
    {
        var cr = (uint)(r * 255f + 0.5f);
        var cg = (uint)(g * 255f + 0.5f);
        var cb = (uint)(b * 255f + 0.5f);
        var ca = (uint)(a * 255f + 0.5f);
        return (ca << 24) | (cb << 16) | (cg << 8) | cr;
    }

    private static uint Fnv1A(uint hash, string value)
    {
        foreach (var c in value)
        {
            hash ^= c;
            hash *= 16777619u;
        }
        return hash;
    }

    private static uint Fnv1A(uint hash, float value)
    {
        var bits = BitConverter.SingleToInt32Bits(value);
        hash ^= (uint)bits;
        hash *= 16777619u;
        return hash;
    }
}
