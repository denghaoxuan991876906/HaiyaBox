using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using ECommons.DalamudServices;

namespace HaiyaBox.Rendering;

public sealed class DangerAreaRenderer : IDisposable
{
    private const int CircleSegments = 64;
    private readonly object _sync = new();
    private readonly List<DisplayObject> _objects = new();
    private readonly List<DisplayObject> _tempObjects = new();
    private readonly List<Func<List<DisplayObject>>> _tempObjectCallbacks = new();
    private bool _enabled;
    private bool _subscribed;

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if(_enabled == value) return;
            _enabled = value;
            UpdateSubscription();
        }
    }

    public void UpdateObjects(IEnumerable<DisplayObject> objects)
    {
        lock(_sync)
        {
            _objects.Clear();
            if(objects == null) return;
            _objects.AddRange(objects);
        }
    }

    public void AddTempObjects(IEnumerable<DisplayObject> objects)
    {
        if(objects == null) return;
        lock(_sync)
        {
            _tempObjects.AddRange(objects);
        }
    }

    public void ClearTempObjects()
    {
        lock(_sync)
        {
            _tempObjects.Clear();
        }
    }

    public void RegisterTempObjectCallback(Func<List<DisplayObject>> callback)
    {
        if(callback == null) return;
        lock(_sync)
        {
            _tempObjectCallbacks.Add(callback);
        }
    }

    public void UnregisterTempObjectCallback(Func<List<DisplayObject>> callback)
    {
        if(callback == null) return;
        lock(_sync)
        {
            _tempObjectCallbacks.Remove(callback);
        }
    }

    private void UpdateSubscription()
    {
        if(_enabled && !_subscribed)
        {
            Svc.PluginInterface.UiBuilder.Draw += Draw;
            _subscribed = true;
        }
        else if(!_enabled && _subscribed)
        {
            Svc.PluginInterface.UiBuilder.Draw -= Draw;
            _subscribed = false;
        }
    }

    private void Draw()
    {
        if(!_enabled) return;

        List<DisplayObject> snapshot;
        List<DisplayObject> tempSnapshot;
        List<Func<List<DisplayObject>>> callbacks;
        lock(_sync)
        {
            if(_objects.Count == 0 && _tempObjects.Count == 0 && _tempObjectCallbacks.Count == 0) return;
            snapshot = new List<DisplayObject>(_objects);
            tempSnapshot = new List<DisplayObject>(_tempObjects);
            callbacks = new List<Func<List<DisplayObject>>>(_tempObjectCallbacks);
            _tempObjects.Clear();
        }

        ImGuiHelpers.ForceNextWindowMainViewport();
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGuiHelpers.SetNextWindowPosRelativeMainViewport(Vector2.Zero);
        ImGui.SetNextWindowSize(ImGuiHelpers.MainViewport.Size);
        ImGui.Begin("HaiyaBoxDangerAreaOverlay", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar);
        var drawList = ImGui.GetWindowDrawList();

        foreach(var obj in snapshot)
        {
            RenderObject(drawList, obj);
        }

        foreach(var obj in tempSnapshot)
        {
            RenderObject(drawList, obj);
        }

        foreach(var callback in callbacks)
        {
            try
            {
                var objects = callback();
                if(objects != null)
                {
                    foreach(var obj in objects)
                    {
                        RenderObject(drawList, obj);
                    }
                }
            }
            catch
            {
                // Ignore callback errors
            }
        }

        ImGui.End();
        ImGui.PopStyleVar();
    }

    private static void RenderObject(ImDrawListPtr drawList, DisplayObject obj)
    {
        switch(obj)
        {
            case DisplayObjectCircle circle:
                DrawCircle(drawList, circle);
                break;
            case DisplayObjectLine line:
                DrawLine(drawList, line);
                break;
            case DisplayObjectDot dot:
                DrawDot(drawList, dot);
                break;
            case DisplayObjectText text:
                DrawText(drawList, text);
                break;
        }
    }

    private static void DrawLine(ImDrawListPtr drawList, DisplayObjectLine line)
    {
        if(!TryProject(line.Start, out var start) || !TryProject(line.End, out var end)) return;
        drawList.AddLine(start, end, line.Color, line.Thickness);
    }

    private static void DrawDot(ImDrawListPtr drawList, DisplayObjectDot dot)
    {
        if(!TryProject(dot.Position, out var center)) return;
        drawList.AddCircleFilled(center, dot.Radius, dot.Color, 32);
    }

    private static void DrawText(ImDrawListPtr drawList, DisplayObjectText text)
    {
        if(string.IsNullOrWhiteSpace(text.Text)) return;
        if(!TryProject(text.Position, out var center)) return;

        var measured = ImGui.CalcTextSize(text.Text) * text.Scale;
        var padding = new Vector2(6f, 4f);
        var min = center - measured / 2f - padding;
        var max = center + measured / 2f + padding;
        drawList.AddRectFilled(min, max, text.BackgroundColor, 6f);
        drawList.AddText(ImGui.GetFont(), ImGui.GetFontSize() * text.Scale, center - measured / 2f, text.ForegroundColor, text.Text);
    }

    private static void DrawCircle(ImDrawListPtr drawList, DisplayObjectCircle circle)
    {
        Span<Vector2> buffer = stackalloc Vector2[CircleSegments];
        var count = 0;
        for(var i = 0; i < CircleSegments; i++)
        {
            var angle = (float)(i / (double)CircleSegments * Math.PI * 2.0);
            var world = new Vector3(
                circle.Center.X + circle.Radius * MathF.Cos(angle),
                circle.Center.Y,
                circle.Center.Z + circle.Radius * MathF.Sin(angle));
            if(TryProject(world, out var point))
            {
                buffer[count++] = point;
            }
        }

        if(count < 3) return;

        drawList.PathClear();
        for(var i = 0; i < count; i++)
        {
            drawList.PathLineTo(buffer[i]);
        }

        if(circle.Filled)
        {
            drawList.PathFillConvex(circle.Color);
        }
        else
        {
            drawList.PathStroke(circle.Color, ImDrawFlags.Closed, circle.Thickness);
        }
    }

    private static bool TryProject(Vector3 world, out Vector2 screen)
    {
        screen = Vector2.Zero;
        return Svc.GameGui != null && Svc.GameGui.WorldToScreen(world, out screen);
    }

    public void Dispose()
    {
        Enabled = false;
        lock(_sync)
        {
            _objects.Clear();
        }
    }
}
