using System.Runtime.Loader;
using AEAssist.CombatRoutine.Module;
using AEAssist.CombatRoutine.Trigger;
using Dalamud.Bindings.ImGui;
using HaiyaBox.Settings;

namespace HaiyaBox.UI;

public class DutyBattleTab
{
    /// <summary>
    /// 从全局配置中获取 DebugPrintSettings 配置
    /// </summary>
    public DutyBattleSettings Settings => FullAutoSettings.Instance.DutyBattleSettings;
    /// <summary>
    /// 在模块加载时调用
    /// </summary>
    /// <param name="loadContext">当前插件的加载上下文</param>
    public void OnLoad(AssemblyLoadContext loadContext)
    {
        // 订阅条件参数创建事件回调
        TriggerlineData.OnCondParamsCreate += OnCondParamsCreateEvent;
    }

    /// <summary>
    /// 当插件卸载或者模块释放时调用
    /// </summary>
    public void Dispose()
    {
        // 取消条件参数创建事件回调的注册
        TriggerlineData.OnCondParamsCreate -= OnCondParamsCreateEvent;
    }

    public void Draw()
    {
        // 特殊副本功能总开关
        bool dutyBattleEnabled = Settings.DutyBattleEnabled;
        if (ImGui.Checkbox("特殊副本功能", ref dutyBattleEnabled))
        {
            Settings.UpdateDutyBattleEnabled(dutyBattleEnabled);
        }

        // 如果总开关未启用，则不继续绘制下列详细选项
        if (!dutyBattleEnabled)
        {
            ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "特殊副本功能已关闭");
            return;
        }
    }

    private readonly List<uint> iconId = [404, 405, 406, 407, 408, 409, 410, 411];
    private void OnCondParamsCreateEvent(ITriggerCondParams condParams)
    {
        if (condParams is EnemyCastSpellCondParams spellCondParams)
        {
            if (spellCondParams.SpellId is (43957 or 43960))
            {
                
            }

            if (spellCondParams.SpellId is  (43955 or 43958))
            {
                
            }
        }

        if (condParams is TargetIconEffectTestCondParams targetIcon)
        {
            if (iconId.Contains(targetIcon.IconId))
            {
                
            }
        }
    }
}