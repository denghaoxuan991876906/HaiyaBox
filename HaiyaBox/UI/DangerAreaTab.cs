using System;
using System.Collections.Generic;
using System.Numerics;
using AOESafetyCalculator.Core;
using AOESafetyCalculator.SafetyZone;
using AOESafetyCalculator.Shapes;
using Dalamud.Bindings.ImGui;
using ECommons.DalamudServices;
using HaiyaBox.Settings;
using HaiyaBox.Rendering;
using HaiyaBox.Utils;

namespace HaiyaBox.UI
{
    /// <summary>
    /// DangerAreaTab 使用 AOESafetyCalculator 管理 AOE 形状，并提供绘制与配置 UI。
    /// </summary>
    public class DangerAreaTab
    {
        private sealed class AoeEntry
        {
            public AOEShape Shape { get; init; } = null!;
            public Vector3 Origin { get; init; }
            public Angle Rotation { get; init; }
            public bool Enabled { get; set; } = true;
            public uint Color { get; init; }
        }

        private static readonly string[] ShapeLabels =
        [
            "圆形",
            "环形",
            "扇形",
            "扇形环",
            "矩形",
            "十字",
            "三角扇形",
            "胶囊",
            "弧形胶囊"
        ];

        private static readonly uint[] Palette =
        [
            PackColor(0.95f, 0.25f, 0.25f, 0.85f),
            PackColor(0.25f, 0.9f, 0.35f, 0.85f),
            PackColor(0.2f, 0.6f, 1f, 0.85f),
            PackColor(0.95f, 0.85f, 0.2f, 0.85f),
            PackColor(0.85f, 0.35f, 0.95f, 0.85f)
        ];

        private readonly List<AoeEntry> _entries = new();
        private readonly DangerAreaRenderer _dangerAreaRenderer = new();
        private SafeZoneCalculator _safeZoneCalculator = new();
        private readonly List<WPos> _safePoints = new();

        private static readonly uint SafePointPrimaryColor = PackColor(0.2f, 0.8f, 0.2f, 0.95f);
        private static readonly uint SafePointSecondaryColor = PackColor(0.2f, 0.6f, 1f, 0.95f);
        private static readonly uint SafePointLabelBackgroundColor = PackColor(0.05f, 0.05f, 0.05f, 0.8f);
        private static readonly uint SafePointLabelTextColor = PackColor(1f, 1f, 1f, 1f);
        private static readonly uint ArenaBoundsColor = PackColor(1f, 0f, 0f, 0.5f);
        private static readonly uint ReferencePointColor = PackColor(1f, 1f, 0.2f, 1f);
        private const float ArenaOutlineThickness = 2f;
        private const float ArenaHeightOffset = 1f;
        private const float SafePointRenderScale = 1f;
        private const string ReferencePointLabel = "参考点";

        private int _shapeTypeIndex;
        private string _originInput = "100,0,100";
        private Vector3 _origin = new(100, 0, 100);
        private float _rotationDeg;
        private float _directionOffsetDeg;
        private bool _invertForbiddenZone;

        private float _circleRadius = 5f;
        private float _donutInnerRadius = 5f;
        private float _donutOuterRadius = 10f;
        private float _coneRadius = 20f;
        private float _coneHalfAngleDeg = 45f;
        private float _donutSectorInnerRadius = 5f;
        private float _donutSectorOuterRadius = 15f;
        private float _donutSectorHalfAngleDeg = 45f;
        private float _rectLengthFront = 10f;
        private float _rectHalfWidth = 5f;
        private float _rectLengthBack;
        private float _crossLength = 12f;
        private float _crossHalfWidth = 3f;
        private float _triConeSideLength = 12f;
        private float _triConeHalfAngleDeg = 45f;
        private float _capsuleRadius = 3f;
        private float _capsuleLength = 10f;
        private float _arcCapsuleRadius = 3f;
        private float _arcCapsuleAngularLengthDeg = 90f;
        private string _orbitCenterInput = "100,0,100";
        private Vector3 _orbitCenter = new(100, 0, 100);

        private bool _arenaEnabled = true;
        private int _arenaTypeIndex;
        private string _arenaCenterInput = "100,0,100";
        private Vector3 _arenaCenter = new(100, 0, 100);
        private float _arenaRadius = 40f;
        private float _arenaHalfWidth = 20f;
        private float _arenaHalfLength = 20f;
        private float _arenaRotationDeg;

        private int _safePointCount = 8;
        private string _searchCenterInput = "100,0,100";
        private Vector3 _searchCenter = new(100, 0, 100);
        private float _searchRadius = 40f;
        private float _minDistanceBetween = 2f;
        private float _minAngleBetweenDeg;
        private bool _useReferencePoint;
        private bool _orderByReference = true;
        private int _closeToReferenceCount = 3;
        private bool _limitByMaxDistance;
        private float _maxDistanceFromReference = 20f;
        private string _referencePointInput = "100,0,100";
        private Vector3 _referencePoint = new(100, 0, 100);
        private bool _showSafePoints = true;
        private bool _showSafePointLabels = true;
        private float _safePointRadius = 6f;
        private float _safePointLabelHeight = 0.5f;

        private bool _overlayDirty = true;
        private int _colorIndex;
        private string _lastError = string.Empty;
        private string _safePointError = string.Empty;
        private static DangerAreaTab? _instance;

        public static bool OverlayEnabled;
        public static bool DebugPointEnabled;

        /// <summary>
        /// 获取 DangerAreaRenderer 实例，供外部模块使用。
        /// </summary>
        public DangerAreaRenderer Renderer => _dangerAreaRenderer;

        public DangerAreaTab()
        {
            _instance = this;
        }

        /// <summary>
        /// 静态方法：切换可视化状态，供 GeometryTab 等外部模块调用。
        /// </summary>
        public static void ToggleOverlayStatic(bool enabled)
        {
            if (_instance == null) return;
            _instance.ToggleOverlay(enabled);
        }

        public static void ToggleDebugPointStatic(bool enabled)
        {
            if (_instance == null) return;
            _instance.ToggleDebug(enabled);
        }

        public void Update()
        {
            SyncOverlayIfNeeded();
        }

        public void Draw()
        {
            DrawHeaderSection();
            DrawOverlaySection();
            DrawArenaSection();
            DrawSafePointParameterSection();
            DrawShapeConfigSection();
            DrawShapeListSection();
            DrawSafePointResultSection();
            SyncOverlayIfNeeded();
        }

        public void Dispose()
        {
            _dangerAreaRenderer.Dispose();
            _entries.Clear();
        }

        private void DrawHeaderSection()
        {
            ImGui.Text("AOE 安全区与危险区配置");
            ImGui.Separator();
            ImGui.Spacing();
        }

        private void DrawOverlaySection()
        {
            if (ImGui.CollapsingHeader("绘制开关", ImGuiTreeNodeFlags.DefaultOpen))
            {
                bool enabled = OverlayEnabled;
                if (ImGui.Checkbox("在战斗画面中绘制 AOEShape", ref enabled))
                {
                    ToggleOverlay(enabled);
                }

                var statusColor = enabled ? new Vector4(0.4f, 0.85f, 0.4f, 1f) : new Vector4(0.85f, 0.4f, 0.4f, 1f);
                ImGui.TextColored(statusColor, enabled ? "绘制已开启" : "绘制已关闭");
                ImGui.Spacing();
            }

            DrawSafeZoneAutoDrawSection();
        }

        private void DrawSafeZoneAutoDrawSection()
        {
            if (!ImGui.CollapsingHeader("SafeZone 自动绘制", ImGuiTreeNodeFlags.DefaultOpen))
            {
                return;
            }

            var settings = FullAutoSettings.Instance.FaGeneralSetting;
            bool enabled = settings.SafeZoneAutoDrawEnabled;
            if (ImGui.Checkbox("自动绘制 SafeZone (DistanceField)", ref enabled))
            {
                settings.UpdateSafeZoneAutoDrawEnabled(enabled);
                if (enabled && !OverlayEnabled)
                {
                    ToggleOverlay(true);
                }
            }

            var stats = SafeZoneAutoDraw.GetStats();
            ImGui.Text($"场地: {stats.ArenaCount}  危险区: {stats.ActiveZoneCount}  安全点: {stats.SafePointCount}");

            if (ImGui.Button("清空自动绘制"))
            {
                SafeZoneAutoDraw.ClearAll();
            }

            ImGui.Spacing();
        }

        private void DrawArenaSection()
        {
            if (!ImGui.CollapsingHeader("场地参数设置", ImGuiTreeNodeFlags.DefaultOpen))
            {
                return;
            }

            if (ImGui.Checkbox("启用场地限制##场地", ref _arenaEnabled)) MarkOverlayDirty();

            var arenaLabel = _arenaTypeIndex == 0 ? "圆形" : "矩形";
            if (ImGui.BeginCombo("场地类型##场地", arenaLabel))
            {
                if (ImGui.Selectable("圆形##场地", _arenaTypeIndex == 0))
                {
                    _arenaTypeIndex = 0;
                    MarkOverlayDirty();
                }
                if (ImGui.Selectable("矩形##场地", _arenaTypeIndex == 1))
                {
                    _arenaTypeIndex = 1;
                    MarkOverlayDirty();
                }
                ImGui.EndCombo();
            }

            ImGui.Text("场地中心 (x,y,z):");
            ImGui.InputText("场地中心##ArenaCenter", ref _arenaCenterInput, 64);

            if (ImGui.Button("设置场地中心##ArenaCenter"))
            {
                if (TryParseVector3(_arenaCenterInput, out var pos))
                {
                    _arenaCenter = pos;
                    _safePointError = string.Empty;
                    MarkOverlayDirty();
                }
                else
                {
                    _safePointError = "场地中心格式错误，示例: 100,0,100";
                }
            }

            ImGui.SameLine();
            if (ImGui.Button("使用玩家位置##ArenaCenter"))
            {
                var player = Svc.ClientState.LocalPlayer;
                if (player != null)
                {
                    _arenaCenter = player.Position;
                    _arenaCenterInput = $"{player.Position.X:F1},{player.Position.Y:F1},{player.Position.Z:F1}";
                    _safePointError = string.Empty;
                    MarkOverlayDirty();
                }
            }

            ImGui.Text($"当前场地中心: {FormatVector3(_arenaCenter)}");

            if (_arenaTypeIndex == 0)
            {
                if (ImGui.InputFloat("场地半径##场地", ref _arenaRadius, 1f, 5f)) MarkOverlayDirty();
            }
            else
            {
                if (ImGui.InputFloat("半宽##场地", ref _arenaHalfWidth, 0.5f, 1f)) MarkOverlayDirty();
                if (ImGui.InputFloat("半长##场地", ref _arenaHalfLength, 0.5f, 1f)) MarkOverlayDirty();
                if (ImGui.InputFloat("朝向角度(度)##场地", ref _arenaRotationDeg, 1f, 5f)) MarkOverlayDirty();
            }

            ImGui.Spacing();
        }

        private void DrawSafePointParameterSection()
        {
            if (!ImGui.CollapsingHeader("安全点计算参数设置", ImGuiTreeNodeFlags.DefaultOpen))
            {
                return;
            }

            ImGui.InputInt("安全点数量", ref _safePointCount, 1, 1);

            ImGui.Text("搜索中心 (x,y,z):");
            ImGui.InputText("搜索中心##SafeCenter", ref _searchCenterInput, 64);
            if (ImGui.Button("设置搜索中心##SafeCenter"))
            {
                if (TryParseVector3(_searchCenterInput, out var pos))
                {
                    _searchCenter = pos;
                    _safePointError = string.Empty;
                }
                else
                {
                    _safePointError = "搜索中心格式错误，示例: 100,0,100";
                }
            }
            ImGui.SameLine();
            if (ImGui.Button("使用场地中心##SafeCenter"))
            {
                _searchCenter = _arenaCenter;
                _searchCenterInput = $"{_arenaCenter.X:F1},{_arenaCenter.Y:F1},{_arenaCenter.Z:F1}";
                _safePointError = string.Empty;
            }
            ImGui.Text($"当前搜索中心: {FormatVector3(_searchCenter)}");

            ImGui.InputFloat("搜索半径##安全点", ref _searchRadius, 1f, 5f);
            ImGui.InputFloat("最小间距##安全点", ref _minDistanceBetween, 0.1f, 0.5f);
            ImGui.InputFloat("最小角度间隔(度)##安全点", ref _minAngleBetweenDeg, 1f, 5f);

            ImGui.Separator();

            if (ImGui.Checkbox("使用参考点", ref _useReferencePoint)) MarkOverlayDirty();
            if (_useReferencePoint)
            {
                ImGui.Text("参考点 (x,y,z):");
                ImGui.InputText("参考点##ReferencePoint", ref _referencePointInput, 64);
                if (ImGui.Button("设置参考点##ReferencePoint"))
                {
                    if (TryParseVector3(_referencePointInput, out var pos))
                    {
                        _referencePoint = pos;
                        _safePointError = string.Empty;
                        MarkOverlayDirty();
                    }
                    else
                    {
                        _safePointError = "参考点格式错误，示例: 100,0,100";
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button("使用玩家位置##ReferencePoint"))
                {
                    var player = Svc.ClientState.LocalPlayer;
                    if (player != null)
                    {
                        _referencePoint = player.Position;
                        _referencePointInput = $"{player.Position.X:F1},{player.Position.Y:F1},{player.Position.Z:F1}";
                        _safePointError = string.Empty;
                        MarkOverlayDirty();
                    }
                }
                ImGui.Text($"当前参考点: {FormatVector3(_referencePoint)}");

                ImGui.Checkbox("按参考点排序##安全点", ref _orderByReference);
                ImGui.InputInt("贴近参考点数量##安全点", ref _closeToReferenceCount, 1, 1);
                ImGui.Checkbox("限制与参考点最大距离##安全点", ref _limitByMaxDistance);
                if (_limitByMaxDistance)
                {
                    ImGui.InputFloat("参考点最大距离##安全点", ref _maxDistanceFromReference, 0.5f, 1f);
                }
            }

            ImGui.Separator();

            var showSafePoints = _showSafePoints;
            if (ImGui.Checkbox("在 Overlay 显示安全点##安全点", ref showSafePoints))
            {
                _showSafePoints = showSafePoints;
                MarkOverlayDirty();
            }
            ImGui.SameLine();
            var showLabels = _showSafePointLabels;
            if (ImGui.Checkbox("显示安全点编号##安全点", ref showLabels))
            {
                _showSafePointLabels = showLabels;
                MarkOverlayDirty();
            }

            if (ImGui.InputFloat("安全点半径##安全点", ref _safePointRadius, 0.5f, 1f)) MarkOverlayDirty();
            ImGui.InputFloat("编号高度偏移##安全点", ref _safePointLabelHeight, 0.1f, 0.2f);

            if (ImGui.Button("计算安全点##安全点"))
            {
                ComputeSafePoints();
            }

            ImGui.SameLine();
            if (ImGui.Button("清空安全点##安全点"))
            {
                _safePoints.Clear();
                _safePointError = string.Empty;
                MarkOverlayDirty();
            }

            if (!string.IsNullOrWhiteSpace(_safePointError))
            {
                ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), _safePointError);
            }

            ImGui.Spacing();
        }

        private void DrawSafePointResultSection()
        {
            if (!ImGui.CollapsingHeader("安全点结果##安全点", ImGuiTreeNodeFlags.DefaultOpen))
            {
                return;
            }

            ImGui.Text($"当前安全点数量: {_safePoints.Count}");
            if (_safePoints.Count == 0)
            {
                ImGui.Text("暂无安全点数据");
                ImGui.Spacing();
                return;
            }

            for (int i = 0; i < _safePoints.Count; i++)
            {
                var point = _safePoints[i];
                ImGui.Text($"{i + 1}. ({point.X:F1}, {point.Z:F1})");
            }

            ImGui.Spacing();
        }

        private void DrawShapeConfigSection()
        {
            if (!ImGui.CollapsingHeader("AOE 形状参数设置##aoeshape", ImGuiTreeNodeFlags.DefaultOpen))
            {
                return;
            }

            DrawShapeTypeSelector();
            DrawOriginInputs();
            DrawCommonShapeInputs();
            DrawShapeSpecificInputs();

            if (ImGui.Button("添加 AOEShape##aoeshape"))
            {
                AddShape();
            }

            ImGui.SameLine();
            if (ImGui.Button("清除输入错误##aoeshape"))
            {
                _lastError = string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(_lastError))
            {
                ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), _lastError);
            }

            ImGui.Spacing();
        }

        private void DrawShapeTypeSelector()
        {
            var label = ShapeLabels[Math.Clamp(_shapeTypeIndex, 0, ShapeLabels.Length - 1)];
            if (ImGui.BeginCombo("形状类型##aoeshape", label))
            {
                for (int i = 0; i < ShapeLabels.Length; i++)
                {
                    if (ImGui.Selectable(ShapeLabels[i], _shapeTypeIndex == i))
                    {
                        _shapeTypeIndex = i;
                    }
                }
                ImGui.EndCombo();
            }
        }

        private void DrawOriginInputs()
        {
            ImGui.Text("AOE 位置 (x,y,z):");
            ImGui.InputText("坐标##AOEOrigin", ref _originInput, 64);

            if (ImGui.Button("设置位置##AOEOrigin"))
            {
                if (TryParseVector3(_originInput, out var pos))
                {
                    _origin = pos;
                    _lastError = string.Empty;
                }
                else
                {
                    _lastError = "AOE 位置格式错误，示例: 100,0,100";
                }
            }

            ImGui.SameLine();
            if (ImGui.Button("获取玩家位置##AOEOrigin"))
            {
                var player = Svc.ClientState.LocalPlayer;
                if (player != null)
                {
                    _origin = player.Position;
                    _originInput = $"{player.Position.X:F1},{player.Position.Y:F1},{player.Position.Z:F1}";
                    _lastError = string.Empty;
                }
            }

            ImGui.Text($"当前位置: {FormatVector3(_origin)}");
        }

        private void DrawCommonShapeInputs()
        {
            ImGui.InputFloat("旋转角度(度)##aoeshape", ref _rotationDeg, 1f, 5f);
            ImGui.InputFloat("方向偏移(度)##aoeshape", ref _directionOffsetDeg, 1f, 5f);
            ImGui.Checkbox("反转禁止区域##aoeshape", ref _invertForbiddenZone);
        }

        private void DrawShapeSpecificInputs()
        {
            switch (_shapeTypeIndex)
            {
                case 0: // Circle
                    ImGui.InputFloat("半径##aoeshape", ref _circleRadius, 0.5f, 1f);
                    break;
                case 1: // Donut
                    ImGui.InputFloat("内圈半径##aoeshape", ref _donutInnerRadius, 0.5f, 1f);
                    ImGui.InputFloat("外圈半径##aoeshape", ref _donutOuterRadius, 0.5f, 1f);
                    break;
                case 2: // Cone
                    ImGui.InputFloat("半径##aoeshape", ref _coneRadius, 0.5f, 1f);
                    ImGui.InputFloat("半角(度)##aoeshape", ref _coneHalfAngleDeg, 1f, 5f);
                    break;
                case 3: // DonutSector
                    ImGui.InputFloat("内圈半径##aoeshape", ref _donutSectorInnerRadius, 0.5f, 1f);
                    ImGui.InputFloat("外圈半径##aoeshape", ref _donutSectorOuterRadius, 0.5f, 1f);
                    ImGui.InputFloat("半角(度)##aoeshape", ref _donutSectorHalfAngleDeg, 1f, 5f);
                    break;
                case 4: // Rect
                    ImGui.InputFloat("前方长度##aoeshape", ref _rectLengthFront, 0.5f, 1f);
                    ImGui.InputFloat("后方长度##aoeshape", ref _rectLengthBack, 0.5f, 1f);
                    ImGui.InputFloat("半宽##aoeshape", ref _rectHalfWidth, 0.5f, 1f);
                    break;
                case 5: // Cross
                    ImGui.InputFloat("臂长##aoeshape", ref _crossLength, 0.5f, 1f);
                    ImGui.InputFloat("半宽##aoeshape", ref _crossHalfWidth, 0.5f, 1f);
                    break;
                case 6: // TriCone
                    ImGui.InputFloat("边长##aoeshape", ref _triConeSideLength, 0.5f, 1f);
                    ImGui.InputFloat("半角(度)##aoeshape", ref _triConeHalfAngleDeg, 1f, 5f);
                    break;
                case 7: // Capsule
                    ImGui.InputFloat("半径##aoeshape", ref _capsuleRadius, 0.5f, 1f);
                    ImGui.InputFloat("长度##aoeshape", ref _capsuleLength, 0.5f, 1f);
                    break;
                case 8: // ArcCapsule
                    ImGui.InputFloat("半径##aoeshape", ref _arcCapsuleRadius, 0.5f, 1f);
                    ImGui.InputFloat("弧长角度(度)##aoeshape", ref _arcCapsuleAngularLengthDeg, 1f, 5f);
                    ImGui.InputText("轨道中心(x,y,z)##aoeshape", ref _orbitCenterInput, 64);
                    if (ImGui.Button("设置轨道中心##Orbit##aoeshape"))
                    {
                        if (TryParseVector3(_orbitCenterInput, out var pos))
                        {
                            _orbitCenter = pos;
                            _lastError = string.Empty;
                        }
                        else
                        {
                            _lastError = "轨道中心格式错误，示例: 100,0,100";
                        }
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("获取玩家位置##Orbit"))
                    {
                        var player = Svc.ClientState.LocalPlayer;
                        if (player != null)
                        {
                            _orbitCenter = player.Position;
                            _orbitCenterInput = $"{player.Position.X:F1},{player.Position.Y:F1},{player.Position.Z:F1}";
                            _lastError = string.Empty;
                        }
                    }
                    ImGui.Text($"轨道中心: {FormatVector3(_orbitCenter)}");
                    break;
            }
        }

        private void DrawShapeListSection()
        {
            if (!ImGui.CollapsingHeader("当前 AOEShape 数据", ImGuiTreeNodeFlags.DefaultOpen))
            {
                return;
            }

            ImGui.Text($"当前数量: {_entries.Count}");
            if (ImGui.Button("清空 AOEShape 列表"))
            {
                _entries.Clear();
                MarkOverlayDirty();
            }

            if (_entries.Count == 0)
            {
                ImGui.Text("暂无 AOEShape 数据");
                ImGui.Spacing();
                return;
            }

            ImGui.Separator();

            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                bool enabled = entry.Enabled;
                if (ImGui.Checkbox($"##aoe-enabled-{i}", ref enabled))
                {
                    entry.Enabled = enabled;
                    MarkOverlayDirty();
                }
                ImGui.SameLine();
                ImGui.Text($"{i + 1}. {entry.Shape}");
                ImGui.SameLine();
                if (ImGui.SmallButton($"删除##aoe-remove-{i}"))
                {
                    _entries.RemoveAt(i);
                    MarkOverlayDirty();
                    break;
                }
                ImGui.Text($"  位置: {FormatVector3(entry.Origin)}");
                ImGui.Text($"  旋转: {entry.Rotation.Deg:F1}°");
                ImGui.Spacing();
            }
        }

        private void AddShape()
        {
            _lastError = string.Empty;

            if (!TryParseVector3(_originInput, out var origin))
            {
                _lastError = "AOE 位置格式错误，示例: 100,0,100";
                return;
            }

            _origin = origin;
            var rotation = _rotationDeg.Degrees();
            var offset = _directionOffsetDeg.Degrees();

            AOEShape shape = _shapeTypeIndex switch
            {
                0 => new AOEShapeCircle(_circleRadius, _invertForbiddenZone),
                1 => new AOEShapeDonut(_donutInnerRadius, _donutOuterRadius, _invertForbiddenZone),
                2 => new AOEShapeCone(_coneRadius, _coneHalfAngleDeg.Degrees(), offset, _invertForbiddenZone),
                3 => new AOEShapeDonutSector(_donutSectorInnerRadius, _donutSectorOuterRadius, _donutSectorHalfAngleDeg.Degrees(), offset, _invertForbiddenZone),
                4 => new AOEShapeRect(_rectLengthFront, _rectHalfWidth, _rectLengthBack, offset, _invertForbiddenZone),
                5 => new AOEShapeCross(_crossLength, _crossHalfWidth, offset, _invertForbiddenZone),
                6 => new AOEShapeTriCone(_triConeSideLength, _triConeHalfAngleDeg.Degrees(), offset, _invertForbiddenZone),
                7 => new AOEShapeCapsule(_capsuleRadius, _capsuleLength, offset, _invertForbiddenZone),
                8 => CreateArcCapsuleShape(),
                _ => new AOEShapeCircle(_circleRadius, _invertForbiddenZone)
            };

            _entries.Add(new AoeEntry
            {
                Shape = shape,
                Origin = origin,
                Rotation = rotation,
                Enabled = true,
                Color = NextColor()
            });

            MarkOverlayDirty();
        }

        private AOEShape CreateArcCapsuleShape()
        {
            if (!TryParseVector3(_orbitCenterInput, out var orbitCenter))
            {
                _lastError = "轨道中心格式错误，示例: 100,0,100";
                orbitCenter = _orbitCenter;
            }
            else
            {
                _orbitCenter = orbitCenter;
            }

            return new AOEShapeArcCapsule(_arcCapsuleRadius, _arcCapsuleAngularLengthDeg.Degrees(), WPos.FromVec3(orbitCenter), _invertForbiddenZone);
        }

        private void ComputeSafePoints()
        {
            _safePointError = string.Empty;
            _safePoints.Clear();

            if (_safePointCount <= 0)
            {
                _safePointError = "安全点数量必须大于 0";
                return;
            }

            if (_searchRadius <= 0f)
            {
                _safePointError = "搜索半径必须大于 0";
                return;
            }

            if (_minDistanceBetween <= 0f)
            {
                _safePointError = "最小间距必须大于 0";
                return;
            }

            if (!TryParseVector3(_searchCenterInput, out var searchCenter))
            {
                _safePointError = "搜索中心格式错误，示例: 100,0,100";
                return;
            }

            _searchCenter = searchCenter;

            var arenaCenter = _arenaCenter;
            var referencePoint = _referencePoint;

            if (_useReferencePoint && !TryParseVector3(_referencePointInput, out referencePoint))
            {
                _safePointError = "参考点格式错误，示例: 100,0,100";
                return;
            }

            if (_arenaEnabled && !TryParseVector3(_arenaCenterInput, out arenaCenter))
            {
                _safePointError = "场地中心格式错误，示例: 100,0,100";
                return;
            }

            if (_arenaEnabled)
            {
                _arenaCenter = arenaCenter;
            }

            if (_useReferencePoint)
            {
                _referencePoint = referencePoint;
            }

            try
            {
                _safeZoneCalculator = new SafeZoneCalculator();

                if (_arenaEnabled)
                {
                    var arenaWPos = WPos.FromVec3(_arenaCenter);
                    if (_arenaTypeIndex == 0)
                    {
                        _safeZoneCalculator.SetArenaBounds(new CircleArenaBounds(arenaWPos, _arenaRadius));
                    }
                    else
                    {
                        var direction = _arenaRotationDeg.Degrees().ToDirection();
                        _safeZoneCalculator.SetArenaBounds(new RectArenaBounds(arenaWPos, direction, _arenaHalfWidth, _arenaHalfLength));
                    }
                }

                foreach (var zone in BuildForbiddenZones())
                {
                    _safeZoneCalculator.AddForbiddenZone(zone);
                }

                var query = _safeZoneCalculator.FindSafePositions(
                    _safePointCount,
                    WPos.FromVec3(_searchCenter),
                    _searchRadius,
                    DateTime.Now)
                    .MinDistanceBetween(_minDistanceBetween);

                if (_useReferencePoint)
                {
                    var reference = WPos.FromVec3(_referencePoint);
                    if (_limitByMaxDistance && _maxDistanceFromReference > 0f)
                    {
                        query = query.NearTarget(reference, _maxDistanceFromReference);
                    }
                    else
                    {
                        query = query.NearTarget(reference);
                    }

                    if (_orderByReference)
                    {
                        query = query.OrderByDistanceTo(reference);
                    }
                }

                if (_minAngleBetweenDeg > 0f)
                {
                    query = query.WithMinAngle(WPos.FromVec3(_searchCenter), _minAngleBetweenDeg.Degrees());
                }

                _safePoints.AddRange(query.Execute());
                MarkOverlayDirty();
            }
            catch (Exception ex)
            {
                _safePointError = $"安全点计算失败: {ex.Message}";
            }
        }

        private List<ForbiddenZone> BuildForbiddenZones()
        {
            var zones = new List<ForbiddenZone>();
            foreach (var entry in _entries)
            {
                if (!entry.Enabled) continue;
                var origin = WPos.FromVec3(entry.Origin);
                var distance = entry.Shape.InvertForbiddenZone
                    ? entry.Shape.InvertedDistance(origin, entry.Rotation)
                    : entry.Shape.Distance(origin, entry.Rotation);
                zones.Add(new ForbiddenZone { Shape = distance });
            }
            return zones;
        }

        private void ToggleOverlay(bool enabled)
        {
            if (OverlayEnabled == enabled) return;
            OverlayEnabled = enabled;
            _dangerAreaRenderer.Enabled = enabled;
            if (enabled)
            {
                MarkOverlayDirty();
                SyncOverlayIfNeeded();
            }
        }

        private void ToggleDebug(bool enabled)
        {
            if (DebugPointEnabled == enabled) return;
            DebugPointEnabled = enabled;
        }

        private void MarkOverlayDirty()
        {
            _overlayDirty = true;
        }

        private void SyncOverlayIfNeeded()
        {
            if (!OverlayEnabled || !_overlayDirty) return;

            var payload = new List<DisplayObject>();
            foreach (var entry in _entries)
            {
                if (!entry.Enabled) continue;
                var origin = entry.Origin;
                var objects = AOEShapeDebug.BuildDisplayObjectsFor(entry.Shape, WPos.FromVec3(origin), entry.Rotation, origin.Y, entry.Color);
                if (objects != null)
                {
                    payload.AddRange(objects);
                }
            }

            if (_arenaEnabled)
            {
                var arenaCenter = _arenaCenter;
                arenaCenter.Y += ArenaHeightOffset;
                if (_arenaTypeIndex == 0)
                {
                    payload.Add(new DisplayObjectCircle(arenaCenter, _arenaRadius, ArenaBoundsColor, ArenaOutlineThickness, false));
                }
                else
                {
                    var halfWidth = _arenaHalfWidth;
                    var halfLength = _arenaHalfLength;
                    var a = new Vector3(arenaCenter.X - halfWidth, arenaCenter.Y, arenaCenter.Z - halfLength);
                    var b = new Vector3(arenaCenter.X + halfWidth, arenaCenter.Y, arenaCenter.Z - halfLength);
                    var c = new Vector3(arenaCenter.X + halfWidth, arenaCenter.Y, arenaCenter.Z + halfLength);
                    var d = new Vector3(arenaCenter.X - halfWidth, arenaCenter.Y, arenaCenter.Z + halfLength);
                    if (Math.Abs(_arenaRotationDeg) > 0.001f)
                    {
                        a = GeometryUtilsXZ.RotateAroundPoint(a, arenaCenter, _arenaRotationDeg);
                        b = GeometryUtilsXZ.RotateAroundPoint(b, arenaCenter, _arenaRotationDeg);
                        c = GeometryUtilsXZ.RotateAroundPoint(c, arenaCenter, _arenaRotationDeg);
                        d = GeometryUtilsXZ.RotateAroundPoint(d, arenaCenter, _arenaRotationDeg);
                    }
                    payload.Add(new DisplayObjectLine(a, b, ArenaBoundsColor, ArenaOutlineThickness));
                    payload.Add(new DisplayObjectLine(b, c, ArenaBoundsColor, ArenaOutlineThickness));
                    payload.Add(new DisplayObjectLine(c, d, ArenaBoundsColor, ArenaOutlineThickness));
                    payload.Add(new DisplayObjectLine(d, a, ArenaBoundsColor, ArenaOutlineThickness));
                }
            }

            var renderSafePointRadius = _safePointRadius * SafePointRenderScale;
            if (_useReferencePoint)
            {
                var referencePos = new Vector3(_referencePoint.X, _searchCenter.Y, _referencePoint.Z);
                payload.Add(new DisplayObjectDot(referencePos, renderSafePointRadius, ReferencePointColor));
                payload.Add(new DisplayObjectText(referencePos + new Vector3(0f, _safePointLabelHeight, 0f), ReferencePointLabel, SafePointLabelBackgroundColor, SafePointLabelTextColor, 1f));
            }

            if (_showSafePoints && _safePoints.Count > 0)
            {
                var closeCount = _useReferencePoint ? Math.Clamp(_closeToReferenceCount, 0, _safePoints.Count) : 0;
                for (int i = 0; i < _safePoints.Count; i++)
                {
                    var point = _safePoints[i];
                    var color = i < closeCount ? SafePointPrimaryColor : SafePointSecondaryColor;
                    var pos = new Vector3(point.X, _searchCenter.Y, point.Z);
                    payload.Add(new DisplayObjectDot(pos, renderSafePointRadius, color));
                    if (_showSafePointLabels)
                    {
                        var labelPos = pos + new Vector3(0f, _safePointLabelHeight, 0f);
                        payload.Add(new DisplayObjectText(labelPos, (i + 1).ToString(), SafePointLabelBackgroundColor, SafePointLabelTextColor, 1f));
                    }
                }
            }

            _dangerAreaRenderer.UpdateObjects(payload);
            _overlayDirty = false;
        }

        private static bool TryParseVector3(string input, out Vector3 result)
        {
            result = Vector3.Zero;
            try
            {
                var parts = input.Split(',');
                if (parts.Length == 3)
                {
                    float x = float.Parse(parts[0].Trim());
                    float y = float.Parse(parts[1].Trim());
                    float z = float.Parse(parts[2].Trim());
                    result = new Vector3(x, y, z);
                    return true;
                }
            }
            catch
            {
                // Ignore parse errors
            }
            return false;
        }

        private static string FormatVector3(Vector3 value) => $"({value.X:F1}, {value.Y:F1}, {value.Z:F1})";

        private uint NextColor()
        {
            var color = Palette[_colorIndex % Palette.Length];
            _colorIndex++;
            return color;
        }

        private static uint PackColor(float r, float g, float b, float a)
        {
            var cr = (uint)(r * 255f + 0.5f);
            var cg = (uint)(g * 255f + 0.5f);
            var cb = (uint)(b * 255f + 0.5f);
            var ca = (uint)(a * 255f + 0.5f);
            return (ca << 24) | (cb << 16) | (cg << 8) | cr;
        }
    }
}
