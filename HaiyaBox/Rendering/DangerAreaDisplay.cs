using System;
using System.Collections.Generic;
using System.Numerics;
using HaiyaBox.Settings;
using HaiyaBox.Utils;

namespace HaiyaBox.Rendering;

public abstract class DisplayObject
{
}

public sealed class DisplayObjectCircle : DisplayObject
{
    public Vector3 Center { get; }
    public float Radius { get; }
    public uint Color { get; }
    public float Thickness { get; }
    public bool Filled { get; }

    public DisplayObjectCircle(Vector3 center, float radius, uint color, float thickness, bool filled)
    {
        Center = center;
        Radius = radius;
        Color = color;
        Thickness = thickness;
        Filled = filled;
    }
}

public sealed class DisplayObjectLine : DisplayObject
{
    public Vector3 Start { get; }
    public Vector3 End { get; }
    public uint Color { get; }
    public float Thickness { get; }

    public DisplayObjectLine(Vector3 start, Vector3 end, uint color, float thickness)
    {
        Start = start;
        End = end;
        Color = color;
        Thickness = thickness;
    }
}

public sealed class DisplayObjectPolygon : DisplayObject
{
    public Vector3[] Vertices { get; }
    public uint Color { get; }
    public float Thickness { get; }

    public DisplayObjectPolygon(Vector3[] vertices, uint color, float thickness)
    {
        Vertices = vertices;
        Color = color;
        Thickness = thickness;
    }
}

public sealed class DisplayObjectDot : DisplayObject
{
    public Vector3 Position { get; }
    public float Radius { get; }
    public uint Color { get; }

    public DisplayObjectDot(Vector3 position, float radius, uint color)
    {
        Position = position;
        Radius = radius;
        Color = color;
    }
}

public sealed class DisplayObjectText : DisplayObject
{
    public Vector3 Position { get; }
    public string Text { get; }
    public uint BackgroundColor { get; }
    public uint ForegroundColor { get; }
    public float Scale { get; }

    public DisplayObjectText(Vector3 position, string text, uint backgroundColor, uint foregroundColor, float scale)
    {
        Position = position;
        Text = text;
        BackgroundColor = backgroundColor;
        ForegroundColor = foregroundColor;
        Scale = scale;
    }
}

public sealed class DangerAreaRenderConfig
{
    public static DangerAreaRenderConfig Default => new();

    public uint CircleColor { get; init; } = PackColor(1f, 0.2f, 0.2f, 0.85f);
    public uint RectangleColor { get; init; } = PackColor(1f, 0.72f, 0.2f, 0.85f);
    public uint SafePointPrimaryColor { get; init; } = PackColor(0.2f, 0.8f, 0.2f, 0.95f);
    public uint SafePointSecondaryColor { get; init; } = PackColor(0.2f, 0.6f, 1f, 0.95f);
    public uint ReferencePointColor { get; init; } = PackColor(1f, 1f, 0.2f, 1f);
    public uint LabelBackgroundColor { get; init; } = PackColor(0.05f, 0.05f, 0.05f, 0.8f);
    public uint LabelTextColor { get; init; } = PackColor(1f, 1f, 1f, 1f);
    public string ReferenceLabel { get; init; } = "REF";
    public float OutlineThickness { get; init; } = 3f;
    public float SafePointRadius { get; init; } = 6f;
    public float ReferencePointRadius { get; init; } = 10f;
    public float LabelHeightOffset { get; init; } = 0.5f;
    public float LabelScale { get; init; } = 1f;
    public bool ShowSafePointLabels { get; init; } = true;

    private static uint PackColor(float r, float g, float b, float a)
    {
        var cr = (uint)(r * 255f + 0.5f);
        var cg = (uint)(g * 255f + 0.5f);
        var cb = (uint)(b * 255f + 0.5f);
        var ca = (uint)(a * 255f + 0.5f);
        return (ca << 24) | (cb << 16) | (cg << 8) | cr;
    }
}

public static class DangerAreaDisplayBuilder
{
    public static List<DisplayObject> Build(BattleData battleData, DangerAreaRenderConfig config)
    {
        var result = new List<DisplayObject>();
        if(battleData == null || config == null)
        {
            return result;
        }

        var height = battleData.ReferencePoint?.Y ?? 0f;
        var heightOffset = new Vector3(0f, height, 0f);

        foreach(var area in battleData.TempDangerAreas)
        {
            switch(area)
            {
                case CircleDangerArea circle:
                    var center = Point.ToVector3(circle.Center) + heightOffset;
                    result.Add(new DisplayObjectCircle(center, (float)circle.Radius, config.CircleColor, config.OutlineThickness, false));
                    break;
                case RectangleDangerArea rectangle:
                    result.Add(BuildRectangle(rectangle, config, height));
                    break;
            }
        }

        if(battleData.ReferencePoint.HasValue)
        {
            var reference = battleData.ReferencePoint.Value;
            result.Add(new DisplayObjectDot(reference, config.ReferencePointRadius, config.ReferencePointColor));
            result.Add(new DisplayObjectText(reference + new Vector3(0f, config.LabelHeightOffset, 0f), config.ReferenceLabel, config.LabelBackgroundColor, config.LabelTextColor, config.LabelScale));
        }

        var closeCount = Math.Clamp(battleData.CloseToRefCount, 0, battleData.SafePoints.Count);
        for(var i = 0; i < battleData.SafePoints.Count; i++)
        {
            var point = Point.ToVector3(battleData.SafePoints[i]) + heightOffset;
            var color = i < closeCount ? config.SafePointPrimaryColor : config.SafePointSecondaryColor;
            result.Add(new DisplayObjectDot(point, config.SafePointRadius, color));
            if(config.ShowSafePointLabels)
            {
                var labelPos = point + new Vector3(0f, config.LabelHeightOffset, 0f);
                result.Add(new DisplayObjectText(labelPos, (i + 1).ToString(), config.LabelBackgroundColor, config.LabelTextColor, config.LabelScale));
            }
        }

        return result;
    }

    private static DisplayObjectPolygon BuildRectangle(RectangleDangerArea rectangle, DangerAreaRenderConfig config, float height)
    {
        var center = Point.ToVector3(rectangle.Center);
        center.Y = height;

        var halfWidth = (float)(rectangle.Width / 2.0);
        var halfHeight = (float)(rectangle.Height / 2.0);

        var a = new Vector3(center.X - halfWidth, height, center.Z - halfHeight);
        var b = new Vector3(center.X + halfWidth, height, center.Z - halfHeight);
        var c = new Vector3(center.X + halfWidth, height, center.Z + halfHeight);
        var d = new Vector3(center.X - halfWidth, height, center.Z + halfHeight);

        if (Math.Abs(rectangle.Rotation) > 0.001)
        {
            a = GeometryUtilsXZ.RotateAroundPoint(a, center, (float)rectangle.Rotation);
            b = GeometryUtilsXZ.RotateAroundPoint(b, center, (float)rectangle.Rotation);
            c = GeometryUtilsXZ.RotateAroundPoint(c, center, (float)rectangle.Rotation);
            d = GeometryUtilsXZ.RotateAroundPoint(d, center, (float)rectangle.Rotation);
        }

        return new DisplayObjectPolygon([a, b, c, d], config.RectangleColor, config.OutlineThickness);
    }

    private static Vector3 CreateWorldPosition(double x, double z, float height) => new((float)x, height, (float)z);
}
