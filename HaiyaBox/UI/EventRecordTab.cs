using System.Runtime.Loader;
using AEAssist.CombatRoutine.Module;
using AEAssist.CombatRoutine.Trigger;
using AEAssist.Helper;
using Dalamud.Bindings.ImGui;
using HaiyaBox.Settings;
using HaiyaBox.Utils;

namespace HaiyaBox.UI
{
    /// <summary>
    /// 事件记录Tab界面
    /// 用于管理事件记录的开启/关闭，以及查看最近记录的事件信息
    /// </summary>
    public class EventRecordTab
    {
        /// <summary>
        /// 从全局配置中获取 DebugPrintSettings 配置
        /// </summary>
        public RecordSettings Settings => FullAutoSettings.Instance.RecordSettings;

        /// <summary>
        /// 事件记录管理器
        /// </summary>
        private readonly EventRecordManager _recordManager = EventRecordManager.Instance;

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

        /// <summary>
        /// 绘制事件记录Tab的UI界面
        /// </summary>
        public void Draw()
        {
            // 事件记录总开关
            bool eventRecordEnabled = Settings.EventRecordEnabled;
            if (ImGui.Checkbox("启用事件记录功能", ref eventRecordEnabled))
            {
                Settings.UpdateEventRecordEnabled(eventRecordEnabled);
            }

            // 如果总开关未启用，则不继续绘制下列详细选项
            if (!eventRecordEnabled)
            {
                ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "事件记录功能已关闭");
                return;
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Text("事件类型记录开关:");

            // 各事件类型的开关
            DrawEventTypeCheckbox("咏唱事件", "EnemyCastSpell", Settings.RecordEnemyCastSpell, Settings.UpdateRecordEnemyCastSpell);
            DrawEventTypeCheckbox("地图事件", "MapEffect", Settings.RecordMapEffect, Settings.UpdateRecordMapEffect);
            DrawEventTypeCheckbox("连线事件", "Tether", Settings.RecordTether, Settings.UpdateRecordTether);
            DrawEventTypeCheckbox("点名头标", "TargetIconEffect", Settings.RecordTargetIconEffect, Settings.UpdateRecordTargetIconEffect);
            DrawEventTypeCheckbox("创建单位", "UnitCreate", Settings.RecordUnitCreate, Settings.UpdateRecordUnitCreate);
            DrawEventTypeCheckbox("删除单位", "UnitDelete", Settings.RecordUnitDelete, Settings.UpdateRecordUnitDelete);
            DrawEventTypeCheckbox("添加Buff", "AddStatus", Settings.RecordAddStatus, Settings.UpdateRecordAddStatus);
            DrawEventTypeCheckbox("删除Buff", "RemoveStatus", Settings.RecordRemoveStatus, Settings.UpdateRecordRemoveStatus);
            DrawEventTypeCheckbox("效果事件", "ReceviceAbilityEffect", Settings.RecordReceviceAbilityEffect, Settings.UpdateRecordReceviceAbilityEffect);
            DrawEventTypeCheckbox("游戏日志", "GameLog", Settings.RecordGameLog, Settings.UpdateRecordGameLog);
            DrawEventTypeCheckbox("天气变化", "WeatherChanged", Settings.RecordWeatherChanged, Settings.UpdateRecordWeatherChanged);
            DrawEventTypeCheckbox("ActorControl", "ActorControl", Settings.RecordActorControl, Settings.UpdateRecordActorControl);
            DrawEventTypeCheckbox("PlayActionTimeline", "PlayActionTimeline", Settings.RecordPlayActionTimeline, Settings.UpdateRecordPlayActionTimeline);
            DrawEventTypeCheckbox("EnvControl", "EnvControl", Settings.RecordEnvControl, Settings.UpdateRecordEnvControl);
            DrawEventTypeCheckbox("NpcYell", "NpcYell", Settings.RecordNpcYell, Settings.UpdateRecordNpcYell);

            ImGui.Spacing();
            ImGui.Separator();

            // 清空按钮
            if (ImGui.Button("清空所有记录", new System.Numerics.Vector2(120, 0)))
            {
                _recordManager.ClearAllRecords();
            }
            ImGui.SameLine();
            ImGui.Text("（每种事件类型最多保存15条记录）");

            ImGui.Spacing();
            ImGui.Separator();

            // 事件记录显示区域
            DrawEventRecords();
        }

        /// <summary>
        /// 绘制事件类型复选框
        /// </summary>
        private void DrawEventTypeCheckbox(string label, string eventType, bool currentValue, Action<bool> updateAction)
        {
            bool value = currentValue;
            if (ImGui.Checkbox(label, ref value))
            {
                updateAction(value);
            }
            ImGui.SameLine();

            // 显示该事件类型的记录数量
            var records = _recordManager.GetRecords(eventType);
            var recordCount = records.Count;
            string countText = $"({recordCount}/15)";
            ImGui.Text(countText);

            if (recordCount > 0)
            {
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0, 1, 0, 1));
                ImGui.Text("●");
                ImGui.PopStyleColor();
            }
        }

        /// <summary>
        /// 绘制事件记录列表
        /// </summary>
        private void DrawEventRecords()
        {
            ImGui.Text("最近的事件记录:");
            

            ImGui.EndChild();
        }

        /// <summary>
        /// 获取事件类型对应的显示颜色
        /// </summary>
        private System.Numerics.Vector4 GetEventTypeColor(string eventType)
        {
            return eventType switch
            {
                "EnemyCastSpell" => new System.Numerics.Vector4(1, 0.5f, 0, 1),        // 橙色
                "MapEffect" => new System.Numerics.Vector4(0, 0.5f, 1, 1),             // 蓝色
                "Tether" => new System.Numerics.Vector4(1, 0, 1, 1),                   // 紫色
                "TargetIconEffect" => new System.Numerics.Vector4(1, 0, 0, 1),         // 红色
                "UnitCreate" => new System.Numerics.Vector4(0, 1, 0, 1),               // 绿色
                "UnitDelete" => new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 1),      // 灰色
                "AddStatus" => new System.Numerics.Vector4(0.5f, 0, 1, 1),             // 紫色偏蓝
                "RemoveStatus" => new System.Numerics.Vector4(0.5f, 0.5f, 0, 1),       // 黄色偏绿
                "ReceviceAbilityEffect" => new System.Numerics.Vector4(1, 0.5f, 0.5f, 1), // 粉红色
                "GameLog" => new System.Numerics.Vector4(0, 1, 1, 1),                  // 青色
                "WeatherChanged" => new System.Numerics.Vector4(0.5f, 1, 0.5f, 1),     // 淡绿色
                "ActorControl" => new System.Numerics.Vector4(1, 1, 0, 1),             // 黄色
                "PlayActionTimeline" => new System.Numerics.Vector4(1, 0.5f, 1, 1),    // 淡紫色
                "EnvControl" => new System.Numerics.Vector4(0.5f, 1, 1, 1),            // 淡蓝色
                "NpcYell" => new System.Numerics.Vector4(1, 1, 0.5f, 1),               // 淡黄色
                _ => System.Numerics.Vector4.UnitW
            };
        }

        /// <summary>
        /// 事件回调：处理条件参数创建事件（这里主要用于同步事件记录器）
        /// </summary>
        /// <param name="condParams">触发条件参数对象</param>
        private void OnCondParamsCreateEvent(ITriggerCondParams condParams)
        {
            // 注意：实际的记录逻辑在 DebugPrintTab 中处理
            // 这里只是一个占位方法，用于保持接口一致性
            if (!Settings.EventRecordEnabled)
                return;
            // 根据条件参数类型判断，并结合对应配置选项，选择性输出日志和记录事件

            if (condParams is EnemyCastSpellCondParams spell && Settings.RecordEnemyCastSpell)
            {
                RecordEvent(spell, "EnemyCastSpell");
            }
            if (condParams is OnMapEffectCreateEvent mapEffect && Settings.RecordMapEffect)
            {
                RecordEvent(mapEffect, "MapEffect");
            }
            if (condParams is TetherCondParams tether && Settings.RecordTether)
            {
                RecordEvent(tether, "Tether");
            }
            if (condParams is TargetIconEffectTestCondParams iconEffect && Settings.RecordTargetIconEffect)
            {
                RecordEvent(iconEffect, "TargetIconEffect");
            }
            if (condParams is UnitCreateCondParams unitCreate && Settings.RecordUnitCreate)
            {
                RecordEvent(unitCreate, "UnitCreate");
            }
            if (condParams is UnitDeleteCondParams unitDelete && Settings.RecordUnitDelete)
            {
                RecordEvent(unitDelete, "UnitDelete");
            }
            if (condParams is AddStatusCondParams addStatus && Settings.RecordAddStatus)
            {
                RecordEvent(addStatus, "AddStatus");
            }
            if (condParams is RemoveStatusCondParams removeStatus && Settings.RecordRemoveStatus)
            {
                RecordEvent(removeStatus, "RemoveStatus");
            }
            if (condParams is ReceviceAbilityEffectCondParams abilityEffect && Settings.RecordReceviceAbilityEffect)
            {
                RecordEvent(abilityEffect, "ReceviceAbilityEffect");
            }
            if (condParams is GameLogCondParams gameLog && Settings.RecordGameLog)
            {
                RecordEvent(gameLog, "GameLog");
            }
            if (condParams is WeatherChangedCondParams weatherChanged && Settings.RecordWeatherChanged)
            {
                RecordEvent(weatherChanged, "WeatherChanged");
            }
            if (condParams is ActorControlCondParams actorControl && Settings.RecordActorControl)
            {
                RecordEvent(actorControl, "ActorControl");
            }
            if (condParams is PlayActionTimelineParams playActionTimeline && Settings.RecordPlayActionTimeline)
            {
                RecordEvent(playActionTimeline, "PlayActionTimeline");
            }
            if (condParams is OnEnvControlEvent envControl && Settings.RecordEnvControl)
            {
                RecordEvent(envControl, "EnvControl");
            }
            if (condParams is NpcYellCondParams npcYell && Settings.RecordNpcYell)
            {
                RecordEvent(npcYell, "NpcYell");
            }
        }

        private void RecordEvent(ITriggerCondParams condParams, string eventType)
        {
            EventRecordManager.Instance.AddRecord(eventType, condParams);
        }
    }
}
