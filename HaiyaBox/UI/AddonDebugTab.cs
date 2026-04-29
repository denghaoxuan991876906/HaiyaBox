using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using AEAssist;
using AEAssist.Extension;
using AEAssist.Helper;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaiyaBox.Utils;

namespace HaiyaBox.UI;

public class AddonDebugTab
{
    private readonly Dictionary<string, List<(int Index, string Text)>> _addonEntries = new();
    private readonly Dictionary<string, string> _addonTexts = new();
    private readonly HashSet<string> _watchedAddons = new() { "SelectString", "SelectIconString", "SelectYesNo", "Talk", "VVDVoteRoute" };
    private bool _autoRefresh = true;
    private string _selectedAddon = "";
    private int _selectIndexInput = 0;
    private string _matchTextInput = "";

    public AddonDebugTab()
    {
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectString", OnAddonSetup);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectIconString", OnAddonSetup);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectYesNo", OnAddonSetup);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "Talk", OnAddonSetup);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "VVDVoteRoute", OnAddonSetup);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "SelectString", OnAddonFinalize);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "SelectIconString", OnAddonFinalize);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "SelectYesNo", OnAddonFinalize);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "Talk", OnAddonFinalize);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "VVDVoteRoute", OnAddonFinalize);
    }

    public void Dispose()
    {
        Svc.AddonLifecycle.UnregisterListener(OnAddonSetup);
        Svc.AddonLifecycle.UnregisterListener(OnAddonFinalize);
    }

    private unsafe void OnAddonSetup(AddonEvent eventType, AddonArgs addonInfo)
    {
        var addonName = addonInfo.AddonName;
        var atk = (AtkUnitBase*)addonInfo.Addon.Address;
        
        if (atk == null) return;
        
        UpdateAddonData(addonName, atk);
        LogHelper.Print($"[Addon调试] 打开 {addonName}");
        if (addonName == VVDVoteRouteHelper.AddonName)
        {
            VVDVoteRouteHelper.PrintEntries();
        }
    }

    private void OnAddonFinalize(AddonEvent eventType, AddonArgs addonInfo)
    {
        var addonName = addonInfo.AddonName;
        LogHelper.Print($"[Addon调试] 关闭 {addonName}");
        _addonEntries.Remove(addonName);
        _addonTexts.Remove(addonName);
        Plugin.AutoSelectAddonService.Instance.ClearLastSelect(addonName);
    }

    private unsafe void UpdateAddonData(string addonName, AtkUnitBase* atk)
    {
        try
        {
            var entries = new List<(int Index, string Text)>();
            var textBuilder = new StringBuilder();
            if (addonName == "VVDVoteRoute")
            {
                entries = VVDVoteRouteHelper.GetEntries();
                foreach (var entry in entries)
                {
                    textBuilder.AppendLine($"[{entry.Index}] {entry.Text}");
                }
            }

            
            _addonEntries[addonName] = entries;
            _addonTexts[addonName] = textBuilder.ToString();
        }
        catch (System.Exception ex)
        {
            LogHelper.PrintError($"更新Addon数据失败: {ex.Message}");
        }
    }

    private unsafe string GetAddonTextNode(AtkUnitBase* atk, uint nodeId)
    {
        var node = atk->GetNodeById(nodeId);
        if (node == null) return "";
        
        var textNode = node->GetAsAtkTextNode();
        if (textNode == null) return "";
        
        return textNode->NodeText.ToString();
    }

    private unsafe uint GetEventId(IGameObject gameObject)
    {
        var gameObjecetStruct = gameObject.Struct();
        return gameObjecetStruct->EventId;
    }

    private uint eventId = 984302;
    public void Draw()
    {
        ImGui.Text("Addon 调试界面");
        ImGui.Separator();

        var 目的地s = Svc.Objects.Where(e => e.Name.TextValue == "选择目的地" && e.IsTargetable);
        foreach (var gameObject in 目的地s)
        {
            ImGui.Text($"坐标：{gameObject.Position}");
        }
        var 目的地id = Svc.Objects.FirstOrDefault(e => e.Name.TextValue == "选择目的地" && e.IsTargetable);
        if (目的地id != null)
        {
            if (ImGui.Button("交互"))
            {
                目的地id.TargetInteract();
            }
        }
        ImGui.Checkbox("自动刷新", ref _autoRefresh);
        ImGui.SameLine();
        if (ImGui.Button("手动刷新"))
        {
            RefreshAllAddons();
        }
        
        ImGui.Separator();
        
        ImGui.Text("监控的 Addon:");
        foreach (var addon in _watchedAddons)
        {
            ImGui.BulletText($"{addon}: {(_addonEntries.ContainsKey(addon) ? "已打开" : "未打开")}");
        }
        
        ImGui.Separator();
        
        ImGui.Text("当前 Addon 内容:");
        
        foreach (var kvp in _addonEntries)
        {
            var addonName = kvp.Key;
            var entries = kvp.Value;
            
            if (ImGui.CollapsingHeader($"{addonName} ({entries.Count} 项)###{addonName}", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Indent();
                
                if (ImGui.BeginTable($"{addonName}_table", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
                {
                    ImGui.TableSetupColumn("Index");
                    ImGui.TableSetupColumn("Text");
                    ImGui.TableHeadersRow();
                    
                    foreach (var entry in entries)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.Text($"{entry.Index}");
                        ImGui.TableSetColumnIndex(1);
                        ImGui.TextWrapped(entry.Text);
                    }
                    
                    ImGui.EndTable();
                }
                
                ImGui.Unindent();
            }
        }
        
        if (_addonEntries.Count == 0)
        {
            ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 1f), "当前没有打开的 Addon");
        }
        
        ImGui.Separator();
        ImGui.Text("手动选择测试:");
        
        if (ImGui.BeginCombo("选择 Addon", _selectedAddon))
        {
            foreach (var addon in _watchedAddons)
            {
                if (ImGui.Selectable(addon, _selectedAddon == addon))
                {
                    _selectedAddon = addon;
                }
            }
            ImGui.EndCombo();
        }
        
        ImGui.InputInt("选择索引", ref _selectIndexInput);
        ImGui.SameLine();
        if (ImGui.Button("执行索引选择"))
        {
            ExecuteSelectByIndex(_selectedAddon, _selectIndexInput);
        }
        
        ImGui.InputText("匹配文本", ref _matchTextInput, 256);
        ImGui.SameLine();
        if (ImGui.Button("执行文本匹配选择"))
        {
            ExecuteSelectByText(_selectedAddon, _matchTextInput);
        }
    }

    private unsafe void RefreshAllAddons()
    {
        foreach (var addonName in _watchedAddons)
        {
            var addon = Svc.GameGui.GetAddonByName(addonName);
            if (addon != nint.Zero)
            {
                UpdateAddonData(addonName, (AtkUnitBase*)addon.Address);
            }
        }
    }

    private unsafe void ExecuteSelectByIndex(string addonName, int index)
    {
        try
        {
            var addon = Svc.GameGui.GetAddonByName(addonName);
            if (addon == nint.Zero)
            {
                LogHelper.Print($"{addonName} 未打开");
                return;
            }
            
            var atk = (AtkUnitBase*)addon.Address;
            
            switch (addonName)
            {
                case "SelectString":
                case "SelectIconString":
                    break;
                    
                case "VVDVoteRoute":
                    VVDVoteRouteHelper.SelectByIndex(index);
                    break;
                    
                default:
                    LogHelper.PrintError($"不支持的 Addon 类型: {addonName}");
                    break;
            }
        }
        catch (System.Exception ex)
        {
            LogHelper.PrintError($"执行选择失败: {ex.Message}");
        }
    }

    private unsafe void ExecuteSelectByText(string addonName, string matchText)
    {
        try
        {
            if (!_addonEntries.TryGetValue(addonName, out var entries))
            {
                LogHelper.PrintError($"{addonName} 未打开或没有数据");
                return;
            }
            
            var matchedIndex = -1;
            for (var i = 0; i < entries.Count; i++)
            {
                if (entries[i].Text.Contains(matchText, System.StringComparison.OrdinalIgnoreCase))
                {
                    matchedIndex = entries[i].Index;
                    break;
                }
            }
            
            if (matchedIndex >= 0)
            {
                ExecuteSelectByIndex(addonName, matchedIndex);
            }
            else
            {
                LogHelper.PrintError($"未找到匹配文本: {matchText}");
            }
        }
        catch (System.Exception ex)
        {
            LogHelper.PrintError($"执行文本匹配选择失败: {ex.Message}");
        }
    }

    public List<(int Index, string Text)>? GetAddonEntries(string addonName)
    {
        return _addonEntries.TryGetValue(addonName, out var entries) ? entries : null;
    }

    public string? GetAddonText(string addonName)
    {
        return _addonTexts.TryGetValue(addonName, out var text) ? text : null;
    }
}
