using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using AEAssist.Helper;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaiyaBox.Utils;

public static unsafe class VVDVoteRouteHelper
{
    public const string AddonName = "VVDVoteRoute";
    
    public static bool IsAddonOpen()
    {
        var addon = Svc.GameGui.GetAddonByName(AddonName);
        return addon != nint.Zero;
    }
    
    public static AtkUnitBase* GetAddon()
    {
        var addon = Svc.GameGui.GetAddonByName(AddonName);
        if (addon == nint.Zero) return null;
        return (AtkUnitBase*)addon.Address;
    }
    
    public static List<(int Index, string Text)> GetEntries()
    {
        var entries = new List<(int Index, string Text)>();
        var atk = GetAddon();
        if (atk == null || atk->UldManager.NodeList == null) return entries;
        
        for (var i = 0; i < atk->UldManager.NodeListCount; i++)
        {
            var node = atk->UldManager.NodeList[i];
            if (node != null && node->Type == (NodeType)1008)
            {
                var componentList = node->GetAsAtkComponentList();
                if (componentList != null)
                {
                    var listLength = componentList->ListLength;
                    for (var j = 0; j < listLength; j++)
                    {
                        var labelPtr = componentList->GetItemLabel(j);
                        var text = labelPtr.HasValue ? labelPtr.ToString() : "";
                        if (!string.IsNullOrEmpty(text))
                        {
                            entries.Add((j, text));
                        }
                    }
                }
                break;
            }
        }
        
        return entries;
    }
    
    public static int? FindIndexByText(string matchText, bool ignoreCase = true)
    {
        var entries = GetEntries();
        for (var i = 0; i < entries.Count; i++)
        {
            if (ignoreCase)
            {
                if (entries[i].Text.Contains(matchText, System.StringComparison.OrdinalIgnoreCase))
                    return entries[i].Index;
            }
            else
            {
                if (entries[i].Text.Contains(matchText))
                    return entries[i].Index;
            }
        }
        return null;
    }
    
    public static int? FindExactIndexByText(string matchText, bool ignoreCase = true)
    {
        var entries = GetEntries();
        for (var i = 0; i < entries.Count; i++)
        {
            if (ignoreCase)
            {
                if (string.Equals(entries[i].Text, matchText, System.StringComparison.OrdinalIgnoreCase))
                    return entries[i].Index;
            }
            else
            {
                if (entries[i].Text == matchText)
                    return entries[i].Index;
            }
        }
        return null;
    }
    
    public static bool SelectByIndex(int index)
    {
        var atk = GetAddon();
        if (atk == null)
        {
            LogHelper.PrintError($"{AddonName} 未打开");
            return false;
        }
        
        var entries = GetEntries();
        var targetEntry = entries.FirstOrDefault(e => e.Index == index);
        if (targetEntry == default)
        {
            LogHelper.PrintError($"未找到 {AddonName} 回调索引 {index}");
            return false;
        }
        
        try
        {
            var atkValue = new AtkValue();
            atkValue.SetInt(1);
            LogHelper.Print($"[{AddonName}] 当前条目: {BuildEntriesSummary(entries)}");
            LogHelper.Print($"[{AddonName}] 准备选择回调索引 {index}: {targetEntry.Text}");
            atk->FireCallback((uint)index, &atkValue);
            LogHelper.Print($"已选择 {AddonName} 索引 {index}: {targetEntry.Text}");
            return true;
        }
        catch (System.Exception ex)
        {
            LogHelper.PrintError($"选择 {AddonName} 失败: {ex.Message}");
            return false;
        }
    }
    
    public static bool SelectByText(string matchText, bool ignoreCase = true)
    {
        var index = FindIndexByText(matchText, ignoreCase);
        if (index == null)
        {
            LogHelper.PrintError($"未找到匹配文本: {matchText}");
            return false;
        }
        return SelectByIndex(index.Value);
    }
    
    public static bool SelectExactByText(string matchText, bool ignoreCase = true)
    {
        var index = FindExactIndexByText(matchText, ignoreCase);
        if (index == null)
        {
            LogHelper.PrintError($"未找到完全匹配文本: {matchText}");
            return false;
        }
        return SelectByIndex(index.Value);
    }
    
    public static void PrintEntries()
    {
        var entries = GetEntries();
        if (entries.Count == 0)
        {
            LogHelper.Print($"{AddonName} 没有选项");
            return;
        }
        
        LogHelper.Print($"{AddonName} 选项列表:");
        foreach (var entry in entries)
        {
            LogHelper.Print($"  [{entry.Index}] {entry.Text}");
        }
    }

    private static string BuildEntriesSummary(List<(int Index, string Text)> entries)
    {
        if (entries.Count == 0)
            return "<无条目>";

        return string.Join(" | ", entries.Select(e => $"[{e.Index}] {e.Text}"));
    }
}
