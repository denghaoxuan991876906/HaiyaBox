using System.Numerics;
using System.Linq;
using AEAssist;
using AEAssist.CombatRoutine.Module;
using AEAssist.CombatRoutine.Trigger;
using AEAssist.Helper;
using Dalamud.Bindings.ImGui;
using AEAssist.CombatRoutine.Module.Target;
using AEAssist.GUI;
using AOESafetyCalculator.Core;
using AOESafetyCalculator.DistanceField;
using AOESafetyCalculator.SafetyZone;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using HaiyaBox.Rendering;
using HaiyaBox.Settings;
using HaiyaBox.Utils;

namespace HaiyaBox.UI
{
    /// <summary>
    /// GeometryTab 用于处理几何计算及相关UI交互，记录鼠标点击点、计算距离与角度，并提供调试点添加及清理功能。
    /// </summary>
    public class GeometryTab
    {
        #region Properties and Fields

        /// <summary>
        /// 获取全局的 GeometrySettings 配置单例，存储场地中心、朝向点及计算参数等配置。
        /// </summary>
        public GeometrySettings Settings => FullAutoSettings.Instance.GeometrySettings;

        /// <summary>
        /// 卫月字体大小。
        /// </summary>
        public static float Scale => ImGui.GetFontSize() / 13.0f;

        // Point Recording
        public Vector3? Point1World { get; private set; }
        public Vector3? Point2World { get; private set; }
        public float TwoPointDistanceXZ { get; private set; }
        public string ChordResultLabel { get; private set; } = "";
        
        public bool DrawDebugPoints { get; private set; } = false;

        // Distribution Configuration
        private int _distributionMode = 0;
        private float _distributionRadius = 19f;
        private float _distributionFirstOffset = 0f;
        private int _distributionCount = 8;
        private bool _distributionClockwise = true;
        private float _distributionSpacing = 3;
        private float _fixedAngle = 45f;
        private float _distributionTotalAngle = 90f;
        private List<Vector3> _distributionPositions = new List<Vector3>();
        private bool _addDistributionToDebugPoints = true;
        private bool _copyCoordinatesWithF = false;

        // Fixed Position Data
        private readonly string[] _centerLabels = ["旧(0,0,0)", "新(100,0,100)"];
        private readonly Vector3[] _centerPositions =
        [
            new(0, 0, 0),
            new(100, 0, 100)
        ];

        private readonly string[] _directionLabels = ["东(101,0,100)", "西(99,0,100)", "南(100,0,101)", "北(100,0,99)"];
        private readonly Vector3[] _directionPositions =
        [
            new(101, 0, 100),
            new(99, 0, 100),
            new(100, 0, 101),
            new(100, 0, 99)
        ];

        // Event Recording
        private readonly EventRecordManager _recordManager = EventRecordManager.Instance;
        private readonly HashSet<ITriggerCondParams> _triggerCondParamsList = new();
        private readonly List<Vector3> _pointList = new();
        private int _spellListIndex = 0;

        // Calculation Data
        private int _calculationMode = 0;
        private List<string> _rotationReferencePoints = new();
        private List<float> _rotationAngles = new();
        private List<string> _rotationCenters = new();
        private List<Vector3> _rotationResults = new();
        private List<string> _extensionPoints = new();
        private List<string> _extensionDirections = new();
        private List<float> _extensionDistances = new();
        private List<Vector3> _extensionResults = new();
        private List<string> _forwardCenters = new();
        private List<float> _forwardAngles = new();
        private List<Vector3> _forwardResults = new();
        private List<float> _forwardDistances = new();
        private string _inputDebug1;
        private string _inputDebug2;
        private float _inputDebugDistance;

        // 3坐标输入
        private float _inputX = 0f;
        private float _inputY = 0f;
        private float _inputZ = 0f;

        #endregion

        #region Public Methods

        /// <summary>
        /// 绘制与更新 GeometryTab 的各项UI组件，展示实时鼠标位置、Debug点操作、距离、角度计算等信息。
        /// </summary>
        /// <summary>
        /// 在每一帧调用，主要用于更新鼠标点击记录（点1、点2、点3）。
        /// </summary>
        public void Update() => CheckPointRecording();

        /// <summary>
        /// 绘制与更新 GeometryTab 的各项UI组件，展示实时鼠标位置、Debug点操作、距离、角度计算等信息。
        /// </summary>
        public void Draw()
        {
            DrawHeaderSection();
            DrawMousePositionSection();
            DrawDebugPointsSection();
            DrawVisualizationSection();
            DrawPointRecordingSection();
            DrawInputDebug();
            DrawThreeCoordInput();
            DrawEnemyListSection();
            DrawEventRecordingSection();
            DrawCalculationSection();
            DrawDistributionSection();
        }

        #endregion

        #region UI Drawing Methods

        private void DrawVisualizationSection()
        {
            if (ImGui.CollapsingHeader("DebugPoint可视化", ImGuiTreeNodeFlags.DefaultOpen))
            {
                bool enabled = DangerAreaTab.OverlayEnabled;
                if (ImGui.Checkbox("绘制图形", ref enabled))
                {
                    DangerAreaTab.ToggleOverlayStatic(enabled);
                }

                bool debugPointEnabled = DangerAreaTab.DebugPointEnabled;
                if (ImGui.Checkbox("绘制Debug点", ref enabled))
                {
                    DangerAreaTab.ToggleDebugPointStatic(enabled);
                }
                
                ImGui.SameLine();


                var statusColor = enabled ? new Vector4(0.4f, 0.85f, 0.4f, 1f) : new Vector4(0.85f, 0.4f, 0.4f, 1f);
                ImGui.TextColored(statusColor, enabled ? "绘制已开启" : "绘制已关闭");
                ImGui.Spacing();
            }

            if (ImGui.CollapsingHeader("SafeZone 自动绘制", ImGuiTreeNodeFlags.DefaultOpen))
            {
                var settings = FullAutoSettings.Instance.FaGeneralSetting;
                bool enabled = settings.SafeZoneAutoDrawEnabled;
                if (ImGui.Checkbox("自动绘制 SafeZone (DistanceField)", ref enabled))
                {
                    settings.UpdateSafeZoneAutoDrawEnabled(enabled);
                    if (enabled && !DangerAreaTab.OverlayEnabled)
                    {
                        DangerAreaTab.ToggleOverlayStatic(true);
                    }
                }

                var stats = SafeZoneAutoDraw.GetStats();
                ImGui.Text($"场地: {stats.ArenaCount}  危险区: {stats.ActiveZoneCount}  安全点: {stats.SafePointCount}");

                if (ImGui.Button("清空自动绘制##Geometry"))
                {
                    SafeZoneAutoDraw.ClearAll();
                }

                ImGui.Spacing();
            }
        }
        private void DrawTimeDot()
        {
            if (ImGui.Button("打断点##"))
            {
                var time = AI.Instance.BattleData.CurrBattleTimeInSec;
                LogHelper.Print(time);
            }
        }
        private void DrawHeaderSection()
        {
            ImGui.TextColored(new Vector4(1f, 0.85f, 0.4f, 1f), "提示: Ctrl 记录点1, Shift 记录点2");
            ImGui.Separator();
            ImGui.Spacing();
        }

        private void DrawMousePositionSection()
        {
            var mousePos = ImGui.GetMousePos();
            if (ScreenToWorld(mousePos, out var wPos3D))
            {
                ImGui.Text($"鼠标屏幕: <{mousePos.X:F2}, {mousePos.Y:F2}>\n鼠标世界: <{wPos3D.X:F2}, {wPos3D.Z:F2}>");

                float distMouseCenter = GeometryUtilsXZ.DistanceXZ(wPos3D, _centerPositions[Settings.SelectedCenterIndex]);
                float angleMouseCenter = GeometryUtilsXZ.AngleXZ(_directionPositions[Settings.SelectedDirectionIndex], wPos3D, _centerPositions[Settings.SelectedCenterIndex]);
                ImGui.TextColored(new Vector4(0.2f, 1f, 0.2f, 1f), $"鼠标 -> 场地中心: 距离 {distMouseCenter:F2}, 角度 {angleMouseCenter:F2}°");
            }
            else
            {
                ImGui.Text("鼠标不在游戏窗口内");
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
        }

        private void DrawDebugPointsSection()
        {
            bool addDebug = Settings.AddDebugPoints;
            if (ImGui.Checkbox("添加Debug点", ref addDebug))
            {
                Settings.UpdateAddDebugPoints(addDebug);
            }
            ImGui.SameLine();
            if (ImGui.Button("清理Debug点"))
            {
                ClearDebugPoints();
            }
        }

        private void DrawPointRecordingSection()
        {
            ImGui.Spacing();
            ImGui.Text($"点1: {FormatPointXZ(Point1World)}");
            ImGui.SameLine();
            if (ImGui.Button("复制##point1"))
            {
                if (Point1World != null)
                    ImGui.SetClipboardText($"{Point1World.Value.X:F2},{Point1World.Value.Y:F2},{Point1World.Value.Z:F2}");
            }

            ImGui.Text($"点2: {FormatPointXZ(Point2World)}");
            ImGui.SameLine();
            if (ImGui.Button("复制##point2"))
            {
                if (Point2World != null)
                    ImGui.SetClipboardText($"{Point2World.Value.X:F2},{Point2World.Value.Y:F2},{Point2World.Value.Z:F2}");
            }

            if (Point1World.HasValue && Point2World.HasValue)
            {
                ImGui.TextColored(new Vector4(0.2f, 1f, 0.2f, 1f), $"点1 -> 点2: 距离 {TwoPointDistanceXZ:F2}");
            }
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
        }

        private void DrawInputDebug()
        {
            ImGui.Spacing();
            ImGui.Text("输入俩点坐标，计算距离");
            ImGui.InputText("输入A点", ref _inputDebug1);
            ImGui.InputText("输入B点", ref _inputDebug2);
            if (ImGui.Button("计算距离##input"))
            {
                var posA = StringToVector3(_inputDebug1);
                var posB = StringToVector3(_inputDebug2);
                _inputDebugDistance = GeometryUtilsXZ.DistanceXZ(posA, posB);
            }
            if (_inputDebugDistance != 0)
                ImGui.Text($"距离: {_inputDebugDistance}");
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
        }

        private void DrawThreeCoordInput()
        {
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(0.8f, 0.8f, 1f, 1f), "输入坐标参数 (X, Y, Z)");
            ImGuiHelper.LeftInputFloat("X坐标", ref _inputX, 0, 200);
            ImGuiHelper.LeftInputFloat("Y坐标", ref _inputY, 0, 200);
            ImGuiHelper.LeftInputFloat("Z坐标", ref _inputZ, 0, 200);

            if (ImGui.Button("添加到TrustDebug##xyz"))
            {
                var pos = new Vector3(_inputX, _inputY, _inputZ);
                DebugPoint.Add(pos);
            }
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
        }
        private void DrawEnemyListSection()
        {
            ImGui.Text("根据记录的读条事件计算：");
            ImGui.SameLine();
            if (ImGui.Button("打断点##"))
            {
                var time = AI.Instance.BattleData.CurrBattleTimeInSec;
                LogHelper.Print(time);
            }
            ImGui.Text("当前可选中敌人");

            var enemyList = TargetMgr.Instance.Enemys.Values.ToList();
            if (enemyList.Count > 0)
            {
                foreach (var enemy in enemyList)
                {
                    DrawEnemyInfo(enemy);
                }
            }
            else
            {
                ImGui.Text("当前没有可选中敌人");
            }
            ImGui.Spacing();
        }

        private void DrawEnemyInfo(IBattleChara enemy)
        {
            ImGui.Text($"Name：{enemy.Name} ({enemy.DataId})");
            ImGui.SameLine();

            var pos = $"{enemy.Position.X:F2},{enemy.Position.Y:F2},{enemy.Position.Z:F2}";
            ImGui.Text("位置：" + pos);
            if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                ImGui.SetClipboardText(pos);
            }

            ImGui.SameLine();
            var rot = $"{enemy.Rotation * 180 / float.Pi:F2}";
            ImGui.Text($"方向：{rot}");
            if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                ImGui.SetClipboardText(rot);
            }
        }

        private void DrawEventRecordingSection()
        {
            ImGui.Text("选择读条事件：");
            var spellList = _recordManager.GetRecords("EnemyCastSpell")
                .Where(p => p is EnemyCastSpellCondParams spellCondParams)
                .Select(p => $"{(p as EnemyCastSpellCondParams)!.SpellName}:{(p as EnemyCastSpellCondParams)!.SpellId}")
                .ToList();

            if (spellList.Count > 0)
            {
                DrawSpellCombo(spellList);
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "暂无读条事件记录");
            }

            DrawSelectedSpellRecords();
        }

        private void DrawSpellCombo(List<string> spellList)
        {
            if (_spellListIndex >= spellList.Count)
                _spellListIndex = 0;

            string currentSelection = spellList[_spellListIndex] + $"#{_spellListIndex + 1}";
            if (ImGui.BeginCombo("##SpellList", currentSelection))
            {
                for (int i = 0; i < spellList.Count; i++)
                {
                    bool isSelected = (_spellListIndex == i);
                    if (ImGui.Selectable(spellList[i] + $"#{i + 1}", isSelected))
                    {
                        _spellListIndex = i;
                        _triggerCondParamsList.Add(_recordManager.GetRecords("EnemyCastSpell")[i]);
                    }
                }
                ImGui.EndCombo();
            }
        }

        private void DrawSelectedSpellRecords()
        {
            ImGui.Spacing();
            var records = _triggerCondParamsList.ToList();

            for (int i = 0; i < records.Count; i++)
            {
                if (records[i] is EnemyCastSpellCondParams spellCondParams)
                {
                    DrawSpellRecord(spellCondParams, i);
                }
            }
        }

        private void DrawSpellRecord(EnemyCastSpellCondParams spellCondParams, int index)
        {
            ImGui.Text($"Name:{spellCondParams.SpellName} {spellCondParams.SpellId}");
            ImGui.SameLine();

            var castPos = FormatPosition(spellCondParams.CastPos);
            ImGui.Text($"Pos:{castPos}");
            if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                ImGui.SetClipboardText($"{castPos.X},{castPos.Y},{castPos.Z}");
            }

            ImGui.SameLine();
            var castRot = (float)Math.Round(spellCondParams.CastRot * 180 / float.Pi, 2);
            ImGui.Text($"Rot:{castRot}");
            if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                ImGui.SetClipboardText($"{castRot}");
            }

            ImGui.SameLine();
            ImGui.Text($"可选中:{spellCondParams.Object.IsTargetable}");
            ImGui.SameLine();

            if (ImGui.Button($"删除##{index}"))
            {
                try
                {
                    _triggerCondParamsList.Remove(spellCondParams);
                    // 重要：删除后索引回退，避免跳过下一个元素
                    index--;
                }
                catch (Exception e)
                {
                    LogHelper.Print("删除失败");
                    LogHelper.Print(e.Message);
                }
            }
        }

        private void DrawCalculationSection()
        {
            ImGui.Spacing();
            DrawCalculationModeSelection();
            DrawRotationCalculations();
            DrawExtensionCalculations();
            DrawForwardCalculations();
            
        }

        private void DrawCalculationModeSelection()
        {
            string algorithmName = _calculationMode switch
            {
                0 => "旋转",
                1 => "延伸",
                2 => "面向",
                _ => "未知"
            };

            ImGui.Text("选择计算方式：");
            if (ImGui.BeginCombo("算法选择##", algorithmName))
            {
                if (ImGui.Selectable("旋转", _calculationMode == 0))
                    _calculationMode = 0;
                if (ImGui.Selectable("延伸", _calculationMode == 1))
                    _calculationMode = 1;
                if (ImGui.Selectable("面向", _calculationMode == 2))
                    _calculationMode = 2;
                ImGui.EndCombo();
            }

            if (ImGui.Button("添加计算"))
            {
                if (_calculationMode == 0)
                {
                    _rotationCenters.Add(string.Empty);
                    _rotationAngles.Add(0);
                    _rotationReferencePoints.Add(string.Empty);
                    _rotationResults.Add(Vector3.Zero);
                }
                else if (_calculationMode == 1)
                {
                    _extensionDirections.Add(string.Empty);
                    _extensionDistances.Add(0);
                    _extensionPoints.Add(string.Empty);
                    _extensionResults.Add(Vector3.Zero);
                }
                else if (_calculationMode == 2)
                {
                    _forwardResults.Add(Vector3.Zero);
                    _forwardAngles.Add(0);
                    _forwardCenters.Add(string.Empty);
                    _forwardDistances.Add(0);
                }
            }
        }

        private void DrawForwardCalculations()
        {
            for (int i = 0; i < _forwardResults.Count; i++)
            {
                var buf = _forwardCenters[i];
                if (ImGui.InputText($"前向中心##{i}", ref buf))
                    _forwardCenters[i] = buf;
                var buf2 = _forwardAngles[i];
                if (ImGui.InputFloat($"前向弧度##{i}", ref buf2))
                    _forwardAngles[i] = buf2;
                var buf3 = _forwardDistances[i];
                if (ImGui.InputFloat($"前向距离##{i}", ref buf3))
                    _forwardDistances[i] = buf3;

                if (ImGui.Button($"计算结果##前向{i}"))
                {
                    var result = GeometryUtilsXZ.Forward(StringToVector3(_forwardCenters[i]), _forwardAngles[i]*Single.Pi/180,
                        _forwardDistances[i]);
                    _forwardResults[i] = result;
                    DebugPoint.Add(result);
                }
                
                ImGui.SameLine();
                if (ImGui.Button($"删除##前向计算{i}"))
                {
                    RemoveFormardCalculation(i);
                    i--;
                }
                if (_forwardResults[i] != Vector3.Zero && i < _forwardResults.Count)
                {
                    DrawCalculationResult(_forwardResults[i], $"前向计算{i}");
                }
            }
        }
        private void DrawRotationCalculations()
        {
            for (int i = 0; i < _rotationResults.Count; i++)
            {
                var buf = _rotationReferencePoints[i];
                if (ImGui.InputText($"旋转参考点##{i}", ref buf))
                    _rotationReferencePoints[i] = buf;

                var buf2 = _rotationCenters[i];
                if (ImGui.InputText($"旋转中心##{i}", ref buf2))
                    _rotationCenters[i] = buf2;

                var buf3 = _rotationAngles[i];
                if (ImGui.InputFloat($"旋转角度##{i}", ref buf3))
                    _rotationAngles[i] = buf3;

                if (ImGui.Button($"计算结果##旋转{i}"))
                {
                    var result = GeometryUtilsXZ.RotateAroundPoint(StringToVector3(_rotationReferencePoints[i]), StringToVector3(_rotationCenters[i]), _rotationAngles[i]);
                    _rotationResults[i] = result;
                    DebugPoint.Add(result);
                }

                ImGui.SameLine();
                if (ImGui.Button($"删除##旋转计算{i}"))
                {
                    RemoveRotationCalculation(i);
                    i--;
                }

                if (_rotationResults[i] != Vector3.Zero && i < _rotationResults.Count)
                {
                    DrawCalculationResult(_rotationResults[i], $"旋转计算{i}");
                }
            }
        }

        private void DrawExtensionCalculations()
        {
            for (int i = 0; i < _extensionResults.Count; i++)
            {
                var buf4 = _extensionPoints[i];
                if (ImGui.InputText($"延伸点##{i}", ref buf4))
                    _extensionPoints[i] = buf4;

                var buf6 = _extensionDirections[i];
                if (ImGui.InputText($"延伸方向##{i}", ref buf6))
                    _extensionDirections[i] = buf6;

                var buf5 = _extensionDistances[i];
                if (ImGui.InputFloat($"延伸距离##{i}", ref buf5))
                    _extensionDistances[i] = buf5;

                if (ImGui.Button($"计算结果##延伸{i}"))
                {
                    var result = GeometryUtilsXZ.ExtendPoint(StringToVector3(_extensionPoints[i]), StringToVector3(_extensionDirections[i]), _extensionDistances[i]);
                    _extensionResults[i] = result;
                    DebugPoint.Add(result);
                }

                ImGui.SameLine();
                if (ImGui.Button($"删除##延伸计算{i}"))
                {
                    RemoveExtensionCalculation(i);
                    i--;
                }

                if (_extensionResults[i] != Vector3.Zero && i < _extensionResults.Count)
                {
                    DrawCalculationResult(_extensionResults[i], $"延伸计算{i}");
                }
            }
        }

        private void DrawCalculationResult(Vector3 result, string id)
        {
            ImGui.Text($"计算结果：{FormatPosition(result)}");
            if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                ImGui.SetClipboardText($"{FormatPosition(result).X},{FormatPosition(result).Y},{FormatPosition(result).Z}");
            }
        }

        private void DrawDistributionSection()
        {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.TextColored(new Vector4(0.8f, 0.8f, 1f, 1f), "扇形分散");
            DrawDistributionControls();
            DrawDistributionResults();
        }

        private void DrawDistributionControls()
        {
            string comboLabel = _distributionMode switch
            {
                0 => "全圆均匀分布",
                1 => "直线间距分布",
                2 => "固定角度分布",
                3 => "总计角度分布",
                _ => "未知模式"
            };

            if (ImGui.BeginCombo("##DistributionMode", comboLabel))
            {
                if (ImGui.Selectable("全圆均匀分布", _distributionMode == 0))
                    _distributionMode = 0;
                if (ImGui.Selectable("直线间距分布", _distributionMode == 1))
                    _distributionMode = 1;
                if (ImGui.Selectable("固定角度分布", _distributionMode == 2))
                    _distributionMode = 2;
                if (ImGui.Selectable("总计角度分布", _distributionMode == 3))
                    _distributionMode = 3;
                ImGui.EndCombo();
            }

            ImGui.InputFloat("半径", ref _distributionRadius, 1f, 5f, "%.2f");
            ImGui.InputFloat("第一人偏移角度", ref _distributionFirstOffset, 1f, 5f, "%.2f");
            ImGui.InputInt("人数", ref _distributionCount);
            ImGui.Checkbox("顺时针", ref _distributionClockwise);

            if (_distributionMode == 1)
            {
                ImGui.InputFloat("直线间距", ref _distributionSpacing, 1f, 5f, "%.2f");
            }
            if (_distributionMode == 2)
            {
                ImGui.InputFloat("固定角度", ref _fixedAngle, 1f, 5f, "%.2f");
            }
            if (_distributionMode == 3)
            {
                ImGui.InputFloat("总计角度", ref _distributionTotalAngle, 1f, 5f, "%.2f");
            }

            if (ImGui.Button("计算分布"))
            {
                CalculateDistribution();
            }
            ImGui.SameLine();
            ImGui.Checkbox("添加计算结果到Debug点", ref _addDistributionToDebugPoints);
        }

        private void DrawDistributionResults()
        {
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(1f, 1f, 0.6f, 1f), "计算结果：");
            ImGui.SameLine();
            ImGui.Checkbox("复制时附加f和括号", ref _copyCoordinatesWithF);

            for (int i = 0; i < _distributionPositions.Count; i++)
            {
                var pos = _distributionPositions[i];
                string line = _copyCoordinatesWithF
                    ? $"({pos.X:F2}f, {pos.Y:F2}f, {pos.Z:F2}f)"
                    : $"{pos.X:F2}, {pos.Y:F2}, {pos.Z:F2}";

                ImGui.Text(line);
                ImGui.SameLine();
                if (ImGui.Button("复制##" + i))
                {
                    ImGui.SetClipboardText(line);
                }
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 通过监听键盘按键（Ctrl、Shift、Alt）记录鼠标在世界坐标中的位置，
        /// 同时在记录 Debug 点时更新点1/点2/点3的值，并计算点1与点2之间的距离。
        /// </summary>
        public void CheckPointRecording()
        {
            bool ctrl = ImGui.IsKeyPressed(ImGuiKey.LeftCtrl) || ImGui.IsKeyPressed(ImGuiKey.RightCtrl);
            bool shift = ImGui.IsKeyPressed(ImGuiKey.LeftShift) || ImGui.IsKeyPressed(ImGuiKey.RightShift);

            var mousePos = ImGui.GetMousePos();
            if (ScreenToWorld(mousePos, out var wPos3D))
            {
                var pointXZ = new Vector3(wPos3D.X, 0, wPos3D.Z);
                if (ctrl)
                    Point1World = pointXZ;
                else if (shift)
                    Point2World = pointXZ;

                if (Settings.AddDebugPoints && (ctrl || shift))
                    AddDebugPoint(pointXZ);
            }

            if (Point1World.HasValue && Point2World.HasValue)
            {
                TwoPointDistanceXZ = GeometryUtilsXZ.DistanceXZ(Point1World.Value, Point2World.Value);
            }
        }

        private Vector3 FormatPosition(Vector3 position)
        {
            float x = (float)Math.Round(position.X, 2);
            float y = (float)Math.Round(position.Y, 2);
            float z = (float)Math.Round(position.Z, 2);
            return new Vector3(x, y, z);
        }

        private string FormatPointXZ(Vector3? p) =>
            p.HasValue ? $"<{p.Value.X:F2}, 0, {p.Value.Z:F2}>" : "未记录";

        private bool ScreenToWorld(Vector2 screenPos, out Vector3 worldPos)
        {
            Svc.GameGui.ScreenToWorld(screenPos, out worldPos);
            return true;
        }

        private Vector3 StringToVector3(string str)
        {
            var v = str.Split(",");
            return new Vector3(float.Parse(v[0]), float.Parse(v[1]), float.Parse(v[2]));
        }

        private void CalculateDistribution()
        {
            var center = _centerPositions[Settings.SelectedCenterIndex];

            _distributionPositions = _distributionMode switch
            {
                0 => GeometryUtilsXZ.ComputeFullCirclePositions(center, _distributionRadius, _distributionFirstOffset, _distributionCount, _distributionClockwise),
                1 => GeometryUtilsXZ.ComputeArcPositionsByChordSpacing(center, _distributionRadius, _distributionFirstOffset, _distributionCount, _distributionClockwise, _distributionSpacing),
                2 => GeometryUtilsXZ.ComputePositionsByFixedAngle(center, _distributionRadius, _distributionFirstOffset, _distributionCount, _distributionClockwise, _fixedAngle),
                3 => GeometryUtilsXZ.ComputeArcPositionsByTotalAngle(center, _distributionRadius, _distributionFirstOffset, _distributionCount, _distributionClockwise, _distributionTotalAngle),
                _ => _distributionPositions
            };

            if (_addDistributionToDebugPoints)
            {
                foreach (var pos in _distributionPositions)
                {
                    AddDebugPoint(pos);
                }
            }
        }

        private void RemoveFormardCalculation(int index)
        {
            _forwardCenters.RemoveAt(index);
            _forwardDistances.RemoveAt(index);
            _forwardAngles.RemoveAt(index);
            _forwardResults.RemoveAt(index);
        }
        private void RemoveRotationCalculation(int index)
        {
            _rotationResults.RemoveAt(index);
            _rotationReferencePoints.RemoveAt(index);
            _rotationCenters.RemoveAt(index);
            _rotationAngles.RemoveAt(index);
        }

        private void RemoveExtensionCalculation(int index)
        {
            _extensionResults.RemoveAt(index);
            _extensionPoints.RemoveAt(index);
            _extensionDirections.RemoveAt(index);
            _extensionDistances.RemoveAt(index);
        }

        private void AddDebugPoint(Vector3 point)
        {
            LogHelper.Print($"添加Debug点: {point}");
            DebugPoint.Add(point);
        }

        private void ClearDebugPoints()
        {
            LogHelper.Print("清理Debug点");
            DebugPoint.Clear();
        }

        #endregion
    }
}
