using System;
using System.Collections.Generic;
using System.Numerics;
using AEAssist;
using AEAssist.Helper;
using Dalamud.Bindings.ImGui;
using ECommons.DalamudServices;
using HaiyaBox.Settings;

namespace HaiyaBox.Utils;

public static class DebugPoint
{
    private const float Radius = 4f;
    private const float LineThickness = 2f;
    private const float LabelOffset = 4f;
    private static bool _subscribed;

    public static void Initialize()
    {
        if (_subscribed)
        {
            return;
        }

        Svc.PluginInterface.UiBuilder.Draw += Render;
        _subscribed = true;
    }

    public static void Dispose()
    {
        if (!_subscribed)
        {
            return;
        }

        Svc.PluginInterface.UiBuilder.Draw -= Render;
        _subscribed = false;
    }

    public static void Add(Vector3 pos) => Share.TrustDebugPoint.Add(pos);

    public static void Clear() => Share.TrustDebugPoint.Clear();

    private static void Render()
    {
        if (!FullAutoSettings.Instance.FaGeneralSetting.PrintDebugInfo)
        {
            return;
        }

        try
        {
            var points = Share.TrustDebugPoint;
            var labeledPoints = Share.DebugPointWithText;

            var hasPoints = points != null && points.Count > 0;
            var hasLabels = labeledPoints != null && labeledPoints.Count > 0;

            if (!hasPoints && !hasLabels)
            {
                return;
            }

            var drawList = ImGui.GetForegroundDrawList();
            uint red = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0f, 0f, 1f));
            uint yellow = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 0f, 1f));
            uint green = ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 1f, 0.2f, 1f));

            List<Vector3>? snapshot = null;
            if (hasPoints)
            {
                snapshot = new List<Vector3>(points);
            }

            if (snapshot != null && snapshot.Count > 1)
            {
                for (int i = 0; i < snapshot.Count - 1; i++)
                {
                    if (TryProject(snapshot[i], out var p1) && TryProject(snapshot[i + 1], out var p2))
                    {
                        drawList.AddLine(p1, p2, red, LineThickness);
                    }
                }
            }

            if (snapshot != null)
            {
                for (int i = 0; i < snapshot.Count; i++)
                {
                    if (!TryProject(snapshot[i], out var screenPos))
                    {
                        continue;
                    }

                    drawList.AddCircleFilled(screenPos, Radius, red);
                    var textPos = new Vector2(screenPos.X + Radius + LabelOffset, screenPos.Y - Radius / 2f);
                    drawList.AddText(textPos, yellow, $"[{i + 1}]");
                }
            }

            if (hasLabels)
            {
                var labelSnapshot = new List<(string Label, Vector3 Position)>();
                foreach (var entry in labeledPoints)
                {
                    string label = entry.Key is null ? string.Empty : entry.Key.ToString();
                    if (string.IsNullOrWhiteSpace(label))
                    {
                        continue;
                    }

                    labelSnapshot.Add((label, entry.Value));
                }

                foreach (var item in labelSnapshot)
                {
                    if (!TryProject(item.Position, out var screenPos))
                    {
                        continue;
                    }

                    drawList.AddCircleFilled(screenPos, Radius, green);
                    var textPos = new Vector2(screenPos.X + Radius + LabelOffset, screenPos.Y - Radius / 2f);
                    drawList.AddText(textPos, yellow, item.Label);
                }
            }
        }
        catch (Exception ex)
        {
            LogHelper.PrintError($"DebugPoint.Render failed: {ex.Message}");
        }
    }

    private static bool TryProject(Vector3 world, out Vector2 screen)
    {
        screen = Vector2.Zero;
        return Svc.GameGui != null && Svc.GameGui.WorldToScreen(world, out screen);
    }
}
