using System.Numerics;
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
            ImGui.SameLine();
            DrawEventTypeCheckbox("连线事件", "Tether", Settings.RecordTether, Settings.UpdateRecordTether);
            ImGui.SameLine();
            DrawEventTypeCheckbox("点名头标", "TargetIconEffect", Settings.RecordTargetIconEffect, Settings.UpdateRecordTargetIconEffect);
            ImGui.SameLine();
            DrawEventTypeCheckbox("创建单位", "UnitCreate", Settings.RecordUnitCreate, Settings.UpdateRecordUnitCreate);

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
        /// 跟踪事件类型折叠状态的字典
        /// </summary>
        private readonly Dictionary<string, bool> _collapsedStates = new();

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
            ImGui.Spacing();

            // 创建滚动区域
            var totalRecords = 0;
            var enabledEventTypes = GetEnabledEventTypes();
            foreach (var eventType in enabledEventTypes)
            {
                totalRecords += _recordManager.GetTimedRecords(eventType).Count;
            }

            // 显示统计信息
            var statsText = $"共记录 {totalRecords} 条事件（显示最近15条/类型）";
            ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 1), statsText);
            ImGui.Spacing();

            if (ImGui.BeginChild("EventRecordsScrollArea", new System.Numerics.Vector2(0, 0), true))
            {
                if (enabledEventTypes.Count == 0)
                {
                    ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 1), "没有开启任何事件记录");
                }
                else
                {
                    // 为每个已开启的事件类型创建下拉列表
                    foreach (var eventType in enabledEventTypes)
                    {
                        DrawEventTypeRecords(eventType);
                    }
                }
            }
            ImGui.EndChild();
        }

        /// <summary>
        /// 获取所有已开启记录的事件类型
        /// </summary>
        /// <returns>已开启的事件类型列表</returns>
        private List<string> GetEnabledEventTypes()
        {
            var enabledTypes = new List<string>();

            // 精简后的4种核心事件类型
            if (Settings.RecordEnemyCastSpell) enabledTypes.Add("EnemyCastSpell");
            if (Settings.RecordTether) enabledTypes.Add("Tether");
            if (Settings.RecordTargetIconEffect) enabledTypes.Add("TargetIconEffect");
            if (Settings.RecordUnitCreate) enabledTypes.Add("UnitCreate");

            return enabledTypes;
        }

        /// <summary>
        /// 绘制单个事件类型的记录列表
        /// </summary>
        /// <param name="eventType">事件类型</param>
        private void DrawEventTypeRecords(string eventType)
        {
            var records = _recordManager.GetTimedRecords(eventType);
            var color = GetEventTypeColor(eventType);
            var typeName = GetEventTypeDisplayName(eventType);

            // 初始化折叠状态（如果不存在）
            if (!_collapsedStates.ContainsKey(eventType))
            {
                _collapsedStates[eventType] = false; // 默认展开状态
            }

            // 创建折叠标题，显示事件类型名称和记录数量
            var headerText = $"{typeName} ({records.Count}/15)";

            ImGui.PushStyleColor(ImGuiCol.Text, color);

            // 使用 SetNextItemOpen 来保持状态
            if (!_collapsedStates[eventType])
            {
                ImGui.SetNextItemOpen(true);
            }

            // 使用 CollapsingHeader，它有内置的状态保持功能
            if (ImGui.CollapsingHeader(headerText))
            {
                // 当 CollapsingHeader 展开时，更新状态为展开
                _collapsedStates[eventType] = false;

                ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1, 1, 1, 1));

                // 在记录列表头部添加单独清空按钮
                if (records.Count > 0)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.8f, 0.3f, 0.3f, 1));
                    if (ImGui.Button($"清空{typeName}记录", new System.Numerics.Vector2(120, 0)))
                    {
                        _recordManager.ClearRecords(eventType);
                    }
                    ImGui.PopStyleColor();
                    ImGui.Spacing();
                }

                if (records.Count == 0)
                {
                    ImGui.Indent();
                    ImGui.TextColored(new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 1), "暂无记录");
                    ImGui.Unindent();
                }
                else
                {
                    // 显示该事件类型的所有记录
                    for (int i = 0; i < records.Count; i++)
                    {
                        var timedRecord = records[i];
                        DrawSingleRecord(timedRecord, i, color);
                    }
                }

                ImGui.PopStyleColor();
            }
            else
            {
                // 当 CollapsingHeader 折叠时，更新状态为折叠
                _collapsedStates[eventType] = true;
            }

            ImGui.PopStyleColor();
        }

        /// <summary>
        /// 绘制单个事件记录
        /// </summary>
        /// <param name="timedRecord">带时间戳的事件记录</param>
        /// <param name="index">记录索引</param>
        /// <param name="color">显示颜色</param>
        private void DrawSingleRecord(TimedRecord timedRecord, int index, System.Numerics.Vector4 color)
        {
            ImGui.Indent();

            // 记录标题（真实时间戳）
            var timeText = timedRecord.Timestamp.ToString("HH:mm:ss.fff");
            var headerText = $"[{timeText}] 记录 #{index + 1}";

            ImGui.PushStyleColor(ImGuiCol.Text, color);
            ImGui.BulletText(headerText);
            ImGui.PopStyleColor();

            // 尝试显示事件对象的关键属性
            DrawRecordProperties(timedRecord.Record);

            ImGui.Spacing();
            ImGui.Unindent();
        }

        /// <summary>
        /// 绘制事件记录的属性信息
        /// </summary>
        /// <param name="record">事件记录对象</param>
        private void DrawRecordProperties(ITriggerCondParams record)
        {
            // 根据不同的事件类型使用专门的显示方法
            if (record is EnemyCastSpellCondParams spell)
            {
                DrawEnemyCastSpellRecord(spell);
            }
            else if (record is TetherCondParams tether)
            {
                DrawTetherRecord(tether);
            }
            else if (record is TargetIconEffectTestCondParams iconEffect)
            {
                DrawTargetIconEffectRecord(iconEffect);
            }
            else if (record is UnitCreateCondParams unitCreate)
            {
                DrawUnitCreateRecord(unitCreate);
            }
            else
            {
                // 通用显示方法（向后兼容）
                DrawGenericRecord(record);
            }
        }

        /// <summary>
        /// 绘制咏唱事件记录 - 重点显示技能名称、施法时间、目标信息
        /// </summary>
        private void DrawEnemyCastSpellRecord(EnemyCastSpellCondParams spell)
        {
            ImGui.Indent();

            // 技能名称（突出显示）
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1, 0.8f, 0.4f, 1));
            ImGui.BulletText("技能信息");
            ImGui.PopStyleColor();

            // 获取技能名称
            var spellName = spell.SpellName;
            var spellId = spell.SpellId;
            var castTime = spell.TotalCastTimeInSec;

            ImGui.Text($"  技能: {spellName} (ID: {spellId})");
            ImGui.SameLine();
            ImGui.Text($"  咏唱时间: {castTime}s");

            // 目标信息
            ImGui.Spacing();
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.4f, 1f, 1f, 1));
            ImGui.BulletText("目标信息");
            ImGui.PopStyleColor();

            var spellCastPos = FormatPosition(spell.CastPos);
            var spellCastRot = spell.CastRot;

            ImGui.Text($"  释放信息: {spellCastPos} (rot: {spellCastRot})");


            ImGui.Unindent();
        }

        /// <summary>
        /// 绘制连线事件记录 - 重点显示连线类型、连接双方、状态
        /// </summary>
        private void DrawTetherRecord(TetherCondParams tether)
        {
            ImGui.Indent();

            // 连线信息（紫色主题）
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1, 0.6f, 1f, 1));
            ImGui.BulletText("连线信息");
            ImGui.PopStyleColor();

            var tetherId = tether.Args0;
            ImGui.Text($"  连线ID: {tetherId}");

            // 连接对象
            ImGui.Spacing();
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.6f, 1f, 0.6f, 1));
            ImGui.BulletText("连接对象");
            ImGui.PopStyleColor();

            var sourceName = tether.Left.Name;
            var sourceId = tether.LeftId;
            var targetName = tether.Right.Name;
            var targetId = tether.RightId;

            ImGui.Text($"  起始: {sourceName} (ID: {sourceId})");
            ImGui.Text($"  目标: {targetName} (ID: {targetId})");

            ImGui.Unindent();
        }

        /// <summary>
        /// 绘制点名头标事件记录 - 重点显示头标类型、被点名者、时效
        /// </summary>
        private void DrawTargetIconEffectRecord(TargetIconEffectTestCondParams iconEffect)
        {
            ImGui.Indent();

            // 头标信息（红色主题）
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1, 0.4f, 0.4f, 1));
            ImGui.BulletText("头标信息");
            ImGui.PopStyleColor();

            var iconId = iconEffect.IconId;

            ImGui.Text($"  头标ID: {iconId}");


            // 被点名者信息
            ImGui.Spacing();
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1f, 0.8f, 0.4f, 1));
            ImGui.BulletText("被点名者");
            ImGui.PopStyleColor();

            var targetName = iconEffect.Target.Name;
            var targetId = iconEffect.Target.DataId;
            //var targetIndex = GetPropertyValue(iconEffect, "TargetIndex") ?? "N/A";

            ImGui.Text($"  角色: {targetName} (ID: {targetId})");
            //ImGui.Text($"  目标序号: {targetIndex}");

            ImGui.Unindent();
        }

        /// <summary>
        /// 绘制创建单位事件记录 - 重点显示单位信息、位置、属性
        /// </summary>
        private void DrawUnitCreateRecord(UnitCreateCondParams unitCreate)
        {
            ImGui.Indent();

            // 单位信息（绿色主题）
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.4f, 1f, 0.6f, 1));
            ImGui.BulletText("单位信息");
            ImGui.PopStyleColor();

            var name = unitCreate.BattleChara.Name;
            var dataId = unitCreate.BattleChara.DataId;
            var unitId = unitCreate.BattleChara.EntityId;
            var position = FormatPosition(unitCreate.BattleChara.Position);

            ImGui.Text($"  名称: {name} (DataID: {dataId})");
            ImGui.SameLine();
            ImGui.Text($"  单位ID: {unitId}");
            ImGui.SameLine();
            ImGui.Text($"  位置: {position}");

            ImGui.Unindent();
        }

        /// <summary>
        /// 通用属性显示方法（用于未特别处理的事件类型）
        /// </summary>
        private void DrawGenericRecord(ITriggerCondParams record)
        {
            ImGui.Indent();

            try
            {
                // 使用反射获取对象属性
                var properties = record.GetType().GetProperties();
                int displayedCount = 0;

                foreach (var prop in properties)
                {
                    try
                    {
                        var value = prop.GetValue(record);
                        if (value != null)
                        {
                            var valueStr = FormatPropertyValue(value);
                            var propText = $"{prop.Name}: {valueStr}";

                            // 限制显示的属性数量，避免界面过于冗长
                            if (displayedCount < 8)
                            {
                                ImGui.TextColored(new System.Numerics.Vector4(0.8f, 0.8f, 0.8f, 1), propText);
                                displayedCount++;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // 忽略属性读取错误
                        continue;
                    }
                }

                // 如果有更多属性未显示，显示省略号
                if (displayedCount >= 8)
                {
                    ImGui.TextColored(new System.Numerics.Vector4(0.6f, 0.6f, 0.6f, 1), "...");
                }
            }
            catch (Exception)
            {
                ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), $"解析记录时出错");
            }

            ImGui.Unindent();
        }

        private Vector3 FormatPosition(Vector3 position)
        {
            // 对每个分量四舍五入保留2位小数，转换为 float 类型（匹配 Vector3 分量类型）
            float x = (float)Math.Round(position.X, 2);
            float y = (float)Math.Round(position.Y, 2);
            float z = (float)Math.Round(position.Z, 2);
    
            return new Vector3(x, y, z);
        }
        /// <summary>
        /// 格式化属性值用于显示
        /// </summary>
        /// <param name="value">属性值</param>
        /// <returns>格式化后的字符串</returns>
        private string FormatPropertyValue(object value)
        {
            if (value == null) return "null";

            // 处理数组类型
            if (value.GetType().IsArray)
            {
                var array = (Array)value;
                if (array.Length == 0) return "[]";

                var items = array.Cast<object>()
                    .Take(3) // 最多显示3个元素
                    .Select(item => item?.ToString() ?? "null");
                var result = string.Join(", ", items);

                if (array.Length > 3)
                    result += $", ... (共{array.Length}个)";

                return $"[{result}]";
            }

            // 处理集合类型
            if (value is System.Collections.IEnumerable enumerable && !(value is string))
            {
                var list = enumerable.Cast<object>().Take(3).ToList();
                if (list.Count == 0) return "[]";

                var result = string.Join(", ", list.Select(item => item?.ToString() ?? "null"));
                return list.Count >= 3 ? $"{result}, ..." : $"[{result}]";
            }

            // 处理日期时间
            if (value is DateTime dateTime)
            {
                return dateTime.ToString("HH:mm:ss.fff");
            }

            // 截断过长的字符串
            var str = value.ToString() ?? "null";
            if (str.Length > 50)
            {
                return str.Substring(0, 47) + "...";
            }

            return str;
        }

        /// <summary>
        /// 获取事件类型的显示名称
        /// </summary>
        /// <param name="eventType">事件类型</param>
        /// <returns>显示名称</returns>
        private string GetEventTypeDisplayName(string eventType)
        {
            return eventType switch
            {
                "EnemyCastSpell" => "咏唱事件",
                "Tether" => "连线事件",
                "TargetIconEffect" => "点名头标",
                "UnitCreate" => "创建单位",
                _ => eventType
            };
        }

        /// <summary>
        /// 获取事件类型对应的显示颜色
        /// </summary>
        private System.Numerics.Vector4 GetEventTypeColor(string eventType)
        {
            return eventType switch
            {
                "EnemyCastSpell" => new System.Numerics.Vector4(1, 0.5f, 0, 1),        // 橙色
                "Tether" => new System.Numerics.Vector4(1, 0, 1, 1),                   // 紫色
                "TargetIconEffect" => new System.Numerics.Vector4(1, 0, 0, 1),         // 红色
                "UnitCreate" => new System.Numerics.Vector4(0, 1, 0, 1),               // 绿色
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
            // 精简后的4种核心事件类型

            if (condParams is EnemyCastSpellCondParams spell && Settings.RecordEnemyCastSpell)
            {
                RecordEvent(spell, "EnemyCastSpell");
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
        }

        private void RecordEvent(ITriggerCondParams condParams, string eventType)
        {
            EventRecordManager.Instance.AddRecord(eventType, condParams);
        }
    }
}
