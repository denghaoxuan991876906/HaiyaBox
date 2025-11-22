using System.Runtime.Loader;
using AEAssist.AEPlugin;
using AEAssist.CombatRoutine.Trigger;
using AEAssist.Helper;
using AEAssist.Verify;
using Dalamud.Bindings.ImGui;
using HaiyaBox.Hooks;
using HaiyaBox.Triggers.TriggerAction;
using HaiyaBox.Triggers.TriggerCondition;
using HaiyaBox.UI;

namespace HaiyaBox.Plugin
{
    public class AutoRaidHelper : IAEPlugin
    {
        private readonly GeometryTab _geometryTab = new();
        private readonly AutomationTab _automationTab = new();
        private readonly FaGeneralSettingTab _faGeneralSettingTab = new();
        private readonly EventRecordTab _eventRecordTab = new();
        private readonly BlackListTab _blackListTab = new();
        #region IAEPlugin Implementation


        private ActorControlHook? actorControlHook;

        public PluginSetting BuildPlugin()
        {
            /*if (ECHelper.ClientState.LocalContentId != 18014449510753729)
                return null;*/
            TriggerMgr.Instance.Add("嗨呀AE工具", new 指定职能tp指定位置().GetType());
            TriggerMgr.Instance.Add("嗨呀AE工具", new 检测目标位置().GetType());
            TriggerMgr.Instance.Add("嗨呀AE工具", new 启动bmr().GetType());
            actorControlHook = new ActorControlHook();
            return new PluginSetting
            {
                Name = "嗨呀AE工具",
                LimitLevel = VIPLevel.Normal,
            };
        }

        public void OnLoad(AssemblyLoadContext loadContext)
        {
            _automationTab.OnLoad(loadContext);
            _eventRecordTab.OnLoad(loadContext);
        }

        public void Dispose()
        {
            _automationTab.Dispose();
            _eventRecordTab.Dispose();
            actorControlHook?.Dispose();
        }

        public void Update()
        {
            _geometryTab.Update();
            _automationTab.Update();
            _blackListTab.Update();
        }

        public void OnPluginUI()
        {
            if (ImGui.BeginTabBar("MainTabBar"))
            {
                if (ImGui.BeginTabItem("几何计算"))
                {
                    _geometryTab.Draw();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("自动化"))
                {
                    _automationTab.Draw();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("FA全局设置"))
                {
                    _faGeneralSettingTab.Draw();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("事件记录"))
                {
                    _eventRecordTab.Draw();
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("黑名单管理"))
                {
                    _blackListTab.Draw();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }

        #endregion


    }
}