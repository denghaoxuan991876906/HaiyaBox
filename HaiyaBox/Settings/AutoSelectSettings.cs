using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace HaiyaBox.Settings;

public enum AutoSelectMode
{
    Index,
    TextMatch,
    RegexMatch
}

public class AutoSelectEntry
{
    public bool Enabled { get; set; } = true;
    public string AddonName { get; set; } = "";
    public AutoSelectMode SelectMode { get; set; } = AutoSelectMode.TextMatch;
    public int SelectIndex { get; set; } = 0;
    public string MatchText { get; set; } = "";
    public bool UseRegex { get; set; } = false;
    public int DelayMs { get; set; } = 0;
    
    [JsonIgnore]
    public string Name => SelectMode switch
    {
        AutoSelectMode.Index => $"[{AddonName}] 索引:{SelectIndex}",
        AutoSelectMode.TextMatch => $"[{AddonName}] 文本:{MatchText}",
        AutoSelectMode.RegexMatch => $"[{AddonName}] 正则:{MatchText}",
        _ => $"[{AddonName}]"
    };
    
    [JsonIgnore]
    public Regex? MatchRegex
    {
        get
        {
            if (!UseRegex || string.IsNullOrEmpty(MatchText)) return null;
            try
            {
                return new Regex(MatchText, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }
            catch
            {
                return null;
            }
        }
    }
}

public class AutoSelectSettings
{
    public bool AutoSelectEnabled { get; set; } = false;
    public System.Collections.Generic.List<AutoSelectEntry> Entries { get; set; } = new();
    
    public void UpdateAutoSelectEnabled(bool enabled)
    {
        AutoSelectEnabled = enabled;
        FullAutoSettings.Instance.Save();
    }
    
    public void AddEntry(AutoSelectEntry entry)
    {
        Entries.Add(entry);
        FullAutoSettings.Instance.Save();
    }
    
    public void RemoveEntry(int index)
    {
        if (index >= 0 && index < Entries.Count)
        {
            Entries.RemoveAt(index);
            FullAutoSettings.Instance.Save();
        }
    }
    
    public void ClearEntries()
    {
        Entries.Clear();
        FullAutoSettings.Instance.Save();
    }
}
