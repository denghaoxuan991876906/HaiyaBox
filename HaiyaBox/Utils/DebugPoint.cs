using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using HaiyaBox.Rendering;
using HaiyaBox.Settings;

namespace HaiyaBox.Utils;

public static class DebugPoint
{
    private const float Radius = 4f;
    private const float LineThickness = 2f;
    private const float LabelOffset = 4f;

    public static List<Vector3> Point = new();
    public static Dictionary<string, Vector3> DebugPointWithText = new();

    private static DangerAreaRenderer? _renderer;
    private static Func<List<DisplayObject>>? _callback;

    public static void Initialize(DangerAreaRenderer renderer)
    {
        _renderer = renderer;
        _callback = GetDisplayObjects;
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
    }

    public static void Add(Vector3 pos) => Point.Add(pos);

    public static void Clear()
    {
        Point.Clear();
        DebugPointWithText.Clear();
    }

    private static List<DisplayObject> GetDisplayObjects()
    {
        var result = new List<DisplayObject>();

        if (!FullAutoSettings.Instance.FaGeneralSetting.PrintDebugInfo)
        {
            return result;
        }

        uint red = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0f, 0f, 1f));
        uint yellow = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 0f, 1f));
        uint green = ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 1f, 0.2f, 1f));

        // Draw lines between points
        if (Point.Count > 1)
        {
            for (int i = 0; i < Point.Count - 1; i++)
            {
                result.Add(new DisplayObjectLine(Point[i], Point[i + 1], red, LineThickness));
            }
        }

        // Draw points with indices
        for (int i = 0; i < Point.Count; i++)
        {
            var pos = Point[i];
            result.Add(new DisplayObjectDot(pos, Radius, red));
            var labelPos = pos + new Vector3(0f, 0.5f, 0f);
            result.Add(new DisplayObjectText(labelPos, $"[{i + 1}]", 0x80000000, yellow, 1f));
        }

        // Draw labeled points
        foreach (var entry in DebugPointWithText)
        {
            string label = entry.Key is null ? string.Empty : entry.Key.ToString();
            if (string.IsNullOrWhiteSpace(label))
            {
                continue;
            }

            result.Add(new DisplayObjectDot(entry.Value, Radius, green));
            var labelPos = entry.Value + new Vector3(0f, 0.5f, 0f);
            result.Add(new DisplayObjectText(labelPos, label, 0x80000000, yellow, 1f));
        }

        return result;
    }
}
