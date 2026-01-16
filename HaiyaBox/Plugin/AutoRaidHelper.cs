using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Loader;
using AEAssist.AEPlugin;
using AEAssist.CombatRoutine.Trigger;
using AEAssist.Helper;
using AEAssist.Verify;
using Dalamud.Bindings.ImGui;
using HaiyaBox.Hooks;
using HaiyaBox.Settings;
using HaiyaBox.Triggers.TriggerAction;
using HaiyaBox.Triggers.TriggerCondition;
using HaiyaBox.UI;
using HaiyaBox.Utils;

namespace HaiyaBox.Plugin
{
    public class AutoRaidHelper : IAEPlugin
    {
        private readonly GeometryTab _geometryTab = new();
        private readonly AutomationTab _automationTab = new();
        private readonly FaGeneralSettingTab _faGeneralSettingTab = new();
        private readonly EventRecordTab _eventRecordTab = new();
        private readonly BlackListTab _blackListTab = new();
        private readonly DangerAreaTab _dangerAreaTab = new();
        private readonly TreasureOpenerService _treasureOpener = TreasureOpenerService.Instance;
        private ActorControlHook? actorControlHook;
        private XSZToolboxIpc? _xszToolboxIpc;

        public PluginSetting BuildPlugin()
        {
            /*if (ECHelper.ClientState.LocalContentId != 18014449510753729)
                return null;*/
            // 注册原有触发器
            TriggerMgr.Instance.Add("嗨呀AE工具", new 指定职能tp指定位置().GetType());
            TriggerMgr.Instance.Add("嗨呀AE工具", new 检测目标位置().GetType());
            TriggerMgr.Instance.Add("嗨呀AE工具", new 启动bmr().GetType());
            TriggerMgr.Instance.Add("嗨呀AE工具", new 指定职能使用技能().GetType());
            TriggerMgr.Instance.Add("嗨呀AE工具", new 使用技能动作().GetType());


            actorControlHook = new ActorControlHook();
            _treasureOpener.TryInitialize();
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
            _xszToolboxIpc = new XSZToolboxIpc();
            XszRemote.Instance = _xszToolboxIpc;
            DebugPoint.Initialize();


            
            ResetAutoSettings();
        }

        private void ResetAutoSettings()
        {
            FullAutoSettings.Instance.AutomationSettings.AutoCountdownEnabled = false;
            FullAutoSettings.Instance.AutomationSettings.AutoLeaveEnabled = false;
            FullAutoSettings.Instance.AutomationSettings.AutoQueueEnabled = false;
            FullAutoSettings.Instance.Save();
        }
        public void Dispose()
        {
            _automationTab.Dispose();
            _eventRecordTab.Dispose();
            _dangerAreaTab.Dispose();

            
            actorControlHook?.Dispose();
            _treasureOpener.Dispose();
            _xszToolboxIpc?.Dispose();
        }

        public void Update()
        {
            _geometryTab.Update();
            _automationTab.Update();
            _blackListTab.Update();
            _dangerAreaTab.Update();
            _treasureOpener.Update();
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

                if (ImGui.BeginTabItem("危险区域"))
                {
                    _dangerAreaTab.Draw();
                    ImGui.EndTabItem();
                }


                ImGui.EndTabBar();
            }
        }


    }
}
