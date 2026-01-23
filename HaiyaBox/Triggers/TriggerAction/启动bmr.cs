using AEAssist.CombatRoutine.Trigger;
using AEAssist.Helper;
using Dalamud.Bindings.ImGui;

namespace HaiyaBox.Triggers.TriggerAction;

public class 启动bmRA : ITriggerAction
{
    public bool Draw()
    {
        ImGui.Text("启动bmr");
        ImGui.InputInt("持续时间 (毫秒)", ref 持续时间);
        return true;
    }

    public int 持续时间;
    public string DisplayName { get; } = "启动bmr";
    public string Remark { get; set; } = string.Empty; // 为 Remark 提供默认值
    public bool Handle()
    {
        _ = Aion(持续时间);
        return true;
    }

    private async Task Aion(int time)
    {
        RemoteControlHelper.Cmd("", "/bmrai on");
        await Task.Delay(time);
        RemoteControlHelper.Cmd("", "/bmrai off");
    }
}