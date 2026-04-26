using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AEAssist.Helper;
using Dalamud.Bindings.ImGui;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaiyaBox.Settings;
using HaiyaBox.UI;
using HaiyaBox.Utils;

namespace HaiyaBox.Plugin;

public class AutoSelectAddonService
{
    private static AutoSelectAddonService? _instance;
    public static AutoSelectAddonService Instance => _instance ??= new AutoSelectAddonService();
    
    private readonly AddonDebugTab _debugTab;
    private bool _isEnabled = false;
    private readonly Dictionary<string, int> _lastSelectedIndex = new();
    private readonly Dictionary<string, DateTime> _lastSelectTime = new();
    private const int SelectCooldownMs = 2000;
    
    public AutoSelectAddonService()
    {
        _debugTab = new AddonDebugTab();
    }
    
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;
            FullAutoSettings.Instance.AutoSelectSettings.AutoSelectEnabled = value;
            FullAutoSettings.Instance.Save();
            LogHelper.Print($"自动选择Addon功能: {(value ? "已启用" : "已禁用")}");
        }
    }
    
    public void Update()
    {
        if (!_isEnabled) return;
        
        var entries = FullAutoSettings.Instance.AutoSelectSettings.Entries;
        foreach (var entry in entries.Where(e => e.Enabled))
        {
            TryAutoSelect(entry);
        }
    }
    
    private void TryAutoSelect(AutoSelectEntry entry)
    {
        var addonEntries = _debugTab.GetAddonEntries(entry.AddonName);
        if (addonEntries == null || addonEntries.Count == 0) return;
        
        int? targetIndex = null;
        
        switch (entry.SelectMode)
        {
            case AutoSelectMode.Index:
                if (entry.SelectIndex >= 0 && entry.SelectIndex < addonEntries.Count)
                {
                    targetIndex = entry.SelectIndex;
                }
                break;
                
            case AutoSelectMode.TextMatch:
                for (var i = 0; i < addonEntries.Count; i++)
                {
                    if (addonEntries[i].Text.Contains(entry.MatchText, System.StringComparison.OrdinalIgnoreCase))
                    {
                        targetIndex = addonEntries[i].Index;
                        break;
                    }
                }
                break;
                
            case AutoSelectMode.RegexMatch:
                if (entry.MatchRegex != null)
                {
                    for (var i = 0; i < addonEntries.Count; i++)
                    {
                        if (entry.MatchRegex.IsMatch(addonEntries[i].Text))
                        {
                            targetIndex = addonEntries[i].Index;
                            break;
                        }
                    }
                }
                break;
        }
        
        if (targetIndex.HasValue)
        {
            if (ShouldSkipSelect(entry.AddonName, targetIndex.Value))
                return;
            
            ExecuteSelect(entry.AddonName, targetIndex.Value, entry.DelayMs);
        }
    }
    
    private bool ShouldSkipSelect(string addonName, int targetIndex)
    {
        if (_lastSelectedIndex.TryGetValue(addonName, out var lastIndex) && 
            _lastSelectTime.TryGetValue(addonName, out var lastTime))
        {
            if (lastIndex == targetIndex && (DateTime.Now - lastTime).TotalMilliseconds < SelectCooldownMs)
            {
                return true;
            }
        }
        return false;
    }
    
    private async void ExecuteSelect(string addonName, int index, int delayMs)
    {
        try
        {
            _lastSelectedIndex[addonName] = index;
            _lastSelectTime[addonName] = DateTime.Now;
            
            if (delayMs > 0)
            {
                await System.Threading.Tasks.Task.Delay(delayMs);
            }
            
            var addon = Svc.GameGui.GetAddonByName(addonName);
            if (addon == nint.Zero) return;
            
            unsafe
            {
                var atk = (FFXIVClientStructs.FFXIV.Component.GUI.AtkUnitBase*)addon.Address;
                
                switch (addonName)
                {
                    case "SelectString":
                    case "SelectIconString":
                        var selectString = new ECommons.UIHelpers.AddonMasterImplementations.AddonMaster.SelectString(atk);
                        if (index >= 0 && index < selectString.Entries.Length)
                        {
                            selectString.Entries[index].Select();
                            LogHelper.Print($"自动选择 {addonName} 索引 {index}");
                        }
                        break;
                        
                    case "VVDVoteRoute":
                        VVDVoteRouteHelper.SelectByIndex(index);
                        break;
                        
                    case "SelectYesNo":
                        var selectYesNo = new ECommons.UIHelpers.AddonMasterImplementations.AddonMaster.SelectYesno(atk);
                        if (index == 0)
                        {
                            selectYesNo.Yes();
                            LogHelper.Print($"自动选择 {addonName} Yes");
                        }
                        else
                        {
                            selectYesNo.No();
                            LogHelper.Print($"自动选择 {addonName} No");
                        }
                        break;
                        
                    case "Talk":
                        var talk = new ECommons.UIHelpers.AddonMasterImplementations.AddonMaster.Talk(atk);
                        talk.Click();
                        LogHelper.Print($"自动点击 {addonName}");
                        break;
                }
            }
        }
        catch (System.Exception ex)
        {
            LogHelper.PrintError($"自动选择失败: {ex.Message}");
        }
    }
    
    public void DrawDebugTab()
    {
        _debugTab.Draw();
    }
    
    public void DrawConfigTab()
    {
        ImGui.Text("自动选择 Addon 配置");
        ImGui.Separator();
        
        var enabled = _isEnabled;
        if (ImGui.Checkbox("启用自动选择功能", ref enabled))
        {
            IsEnabled = enabled;
        }
        
        ImGui.Separator();
        
        if (ImGui.Button("添加新规则"))
        {
            FullAutoSettings.Instance.AutoSelectSettings.Entries.Add(new AutoSelectEntry());
            FullAutoSettings.Instance.Save();
        }
        
        ImGui.SameLine();
        if (ImGui.Button("清空所有规则"))
        {
            FullAutoSettings.Instance.AutoSelectSettings.Entries.Clear();
            FullAutoSettings.Instance.Save();
        }
        
        ImGui.Separator();
        
        var entries = FullAutoSettings.Instance.AutoSelectSettings.Entries;
        for (var i = entries.Count - 1; i >= 0; i--)
        {
            var entry = entries[i];
            ImGui.PushID(i);
            
            var entryEnabled = entry.Enabled;
            if (ImGui.Checkbox("启用", ref entryEnabled))
            {
                entry.Enabled = entryEnabled;
                FullAutoSettings.Instance.Save();
            }
            ImGui.SameLine();
            
            if (ImGui.Button("删除"))
            {
                entries.RemoveAt(i);
                FullAutoSettings.Instance.Save();
                ImGui.PopID();
                continue;
            }
            
            ImGui.Text($"规则: {entry.Name}");
            ImGui.Indent();
            
            ImGui.SetNextItemWidth(150);
            var addonName = entry.AddonName;
            if (ImGui.InputText("Addon名称", ref addonName, 64))
            {
                entry.AddonName = addonName;
                FullAutoSettings.Instance.Save();
            }
            
            var mode = (int)entry.SelectMode;
            if (ImGui.Combo("选择模式", ref mode, "索引选择\0文本匹配\0正则匹配\0"))
            {
                entry.SelectMode = (AutoSelectMode)mode;
                FullAutoSettings.Instance.Save();
            }
            
            switch (entry.SelectMode)
            {
                case AutoSelectMode.Index:
                    var idx = entry.SelectIndex;
                    if (ImGui.InputInt("选择索引", ref idx))
                    {
                        entry.SelectIndex = idx;
                        FullAutoSettings.Instance.Save();
                    }
                    break;
                    
                case AutoSelectMode.TextMatch:
                case AutoSelectMode.RegexMatch:
                    var text = entry.MatchText;
                    if (ImGui.InputText("匹配文本", ref text, 256))
                    {
                        entry.MatchText = text;
                        FullAutoSettings.Instance.Save();
                    }
                    
                    if (entry.SelectMode == AutoSelectMode.RegexMatch)
                    {
                        var useRegex = entry.UseRegex;
                        if (ImGui.Checkbox("使用正则", ref useRegex))
                        {
                            entry.UseRegex = useRegex;
                            FullAutoSettings.Instance.Save();
                        }
                        
                        if (entry.UseRegex && entry.MatchRegex == null && !string.IsNullOrEmpty(entry.MatchText))
                        {
                            ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "正则表达式无效");
                        }
                    }
                    break;
            }
            
            var delay = entry.DelayMs;
            if (ImGui.InputInt("延迟(ms)", ref delay))
            {
                entry.DelayMs = delay;
                FullAutoSettings.Instance.Save();
            }
            
            ImGui.Unindent();
            ImGui.Separator();
            ImGui.PopID();
        }
    }
    
    public void ClearLastSelect(string addonName)
    {
        _lastSelectedIndex.Remove(addonName);
        _lastSelectTime.Remove(addonName);
    }
    
    public void Dispose()
    {
        _debugTab.Dispose();
    }
}
