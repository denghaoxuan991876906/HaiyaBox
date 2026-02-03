using System;
using System.Collections.Generic;
using System.Numerics;
using AOESafetyCalculator.Core;
using AOESafetyCalculator.Shapes;
using Dalamud.Bindings.ImGui;
using ECommons.DalamudServices;
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

        private bool _overlayDirty = true;
        private int _colorIndex;
        private string _lastError = string.Empty;
        private static DangerAreaTab? _instance;

        public static bool OverlayEnabled;

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

        public void Update()
        {
            SyncOverlayIfNeeded();
        }

        public void Draw()
        {
            DrawHeaderSection();
            DrawOverlaySection();
            DrawShapeConfigSection();
            DrawShapeListSection();
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
        }

        private void DrawShapeConfigSection()
        {
            if (!ImGui.CollapsingHeader("AOE 形状参数设置", ImGuiTreeNodeFlags.DefaultOpen))
            {
                return;
            }

            DrawShapeTypeSelector();
            DrawOriginInputs();
            DrawCommonShapeInputs();
            DrawShapeSpecificInputs();

            if (ImGui.Button("添加 AOEShape"))
            {
                AddShape();
            }

            ImGui.SameLine();
            if (ImGui.Button("清除输入错误"))
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
            if (ImGui.BeginCombo("形状类型", label))
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
            ImGui.InputFloat("旋转角度(度)", ref _rotationDeg, 1f, 5f);
            ImGui.InputFloat("方向偏移(度)", ref _directionOffsetDeg, 1f, 5f);
            ImGui.Checkbox("反转禁止区域", ref _invertForbiddenZone);
        }

        private void DrawShapeSpecificInputs()
        {
            switch (_shapeTypeIndex)
            {
                case 0: // Circle
                    ImGui.InputFloat("半径", ref _circleRadius, 0.5f, 1f);
                    break;
                case 1: // Donut
                    ImGui.InputFloat("内圈半径", ref _donutInnerRadius, 0.5f, 1f);
                    ImGui.InputFloat("外圈半径", ref _donutOuterRadius, 0.5f, 1f);
                    break;
                case 2: // Cone
                    ImGui.InputFloat("半径", ref _coneRadius, 0.5f, 1f);
                    ImGui.InputFloat("半角(度)", ref _coneHalfAngleDeg, 1f, 5f);
                    break;
                case 3: // DonutSector
                    ImGui.InputFloat("内圈半径", ref _donutSectorInnerRadius, 0.5f, 1f);
                    ImGui.InputFloat("外圈半径", ref _donutSectorOuterRadius, 0.5f, 1f);
                    ImGui.InputFloat("半角(度)", ref _donutSectorHalfAngleDeg, 1f, 5f);
                    break;
                case 4: // Rect
                    ImGui.InputFloat("前方长度", ref _rectLengthFront, 0.5f, 1f);
                    ImGui.InputFloat("后方长度", ref _rectLengthBack, 0.5f, 1f);
                    ImGui.InputFloat("半宽", ref _rectHalfWidth, 0.5f, 1f);
                    break;
                case 5: // Cross
                    ImGui.InputFloat("臂长", ref _crossLength, 0.5f, 1f);
                    ImGui.InputFloat("半宽", ref _crossHalfWidth, 0.5f, 1f);
                    break;
                case 6: // TriCone
                    ImGui.InputFloat("边长", ref _triConeSideLength, 0.5f, 1f);
                    ImGui.InputFloat("半角(度)", ref _triConeHalfAngleDeg, 1f, 5f);
                    break;
                case 7: // Capsule
                    ImGui.InputFloat("半径", ref _capsuleRadius, 0.5f, 1f);
                    ImGui.InputFloat("长度", ref _capsuleLength, 0.5f, 1f);
                    break;
                case 8: // ArcCapsule
                    ImGui.InputFloat("半径", ref _arcCapsuleRadius, 0.5f, 1f);
                    ImGui.InputFloat("弧长角度(度)", ref _arcCapsuleAngularLengthDeg, 1f, 5f);
                    ImGui.InputText("轨道中心(x,y,z)", ref _orbitCenterInput, 64);
                    if (ImGui.Button("设置轨道中心##Orbit"))
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
