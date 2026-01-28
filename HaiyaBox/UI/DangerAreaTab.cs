

using System.Numerics;
using System.Linq;
using System.Collections.Generic;
using System;
using AEAssist;
using AEAssist.CombatRoutine.Module.Target;
using AEAssist.Extension;
using AEAssist.Helper;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using ECommons.DalamudServices;
using HaiyaBox.Settings;
using HaiyaBox.Rendering;
using HaiyaBox.Utils;

namespace HaiyaBox.UI
{
    /// <summary>
    /// DangerAreaTab 用于危险区域检测和安全点计算的UI界面，提供危险区域配置、参考点设置和安全点计算功能。
    /// </summary>
    public class DangerAreaTab
    {
        #region Properties and Fields

        /// <summary>
        /// 获取全局的 BattleData 实例，用于存储临时参数和计算结果。
        /// </summary>
        public BattleData BattleDataInstance => Settings.BattleData.Instance;

        /// <summary>
        /// 卫月字体大小。
        /// </summary>
        public static float Scale => ImGui.GetFontSize() / 13.0f;

        // 危险区域配置
        private int _dangerAreaType = 0; // 0: 圆形, 1: 矩形
        private float _circleRadius = 5.0f;
        private float _rectWidth = 10.0f;
        private float _rectHeight = 10.0f;
        private float _rectRotation = 0.0f; // 矩形旋转角度（度数）
        private Vector3 _tempDangerAreaPos = Vector3.Zero;
        private string _tempDangerAreaPosInput = "100,0,100"; // 危险区位置文本输入

        // 参考点配置
        private Vector3 _referencePoint = Vector3.Zero;
        private string _referencePointInput = "100,0,100";

        // 计算参数
        private int _closeToRefCount = 3;
        private double _maxFarDistance = 25.0;
        private double _minSafePointDistance = 3.0;
        private double _sampleStep = 0.5;

        // 限制范围配置
        private int _limitRangeType = 0; // 0: 矩形, 1: 圆形

        // 矩形范围参数
        private Vector3 _limitRectCenter = new Vector3(100,0,100);
        private double _limitRectLength = 35.0; // 长度 (X方向)
        private double _limitRectWidth = 35.0;   // 宽度 (Y方向)
        private string _limitRectCenterInput = "100,0,100";

        // 圆形范围参数
        private Vector3 _limitCircleCenter = new Vector3(100,0,100);
        private double _limitCircleRadius = 17.5;
        private string _limitCircleCenterInput = "100,0,100";

        // 可视化相关
        private readonly DangerAreaRenderer _dangerAreaRenderer = new();
        private readonly DangerAreaRenderConfig _renderConfig = DangerAreaRenderConfig.Default;
        public static bool OverlayEnabled;
        private bool _overlayDirty = true;
        private static DangerAreaTab _instance;

        /// <summary>
        /// 获取 DangerAreaRenderer 实例，供外部模块使用。
        /// </summary>
        public DangerAreaRenderer Renderer => _dangerAreaRenderer;

        #endregion

        #region Public Methods

        /// <summary>
        /// 构造函数，设置静态实例引用。
        /// </summary>
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

        /// <summary>
        /// 更新可视化状态。
        /// </summary>
        public void Update()
        {
            SyncOverlayIfNeeded();
        }

        /// <summary>
        /// 绘制 DangerAreaTab 的UI界面。
        /// </summary>
        public void Draw()
        {
            DrawHeaderSection();
            DrawDangerAreaConfigSection();
            DrawReferencePointSection();
            DrawCalculationParamsSection();
            DrawCalculationSection();
            DrawResultsSection();
            DrawVisualizationSection();
        }

        public void Dispose()
        {
            _dangerAreaRenderer.Dispose();
        }

        #endregion

        #region Private Drawing Methods

        /// <summary>
        /// 绘制标题头部区域。
        /// </summary>
        private void DrawHeaderSection()
        {
            ImGui.Text("危险区域检测与安全点计算");
            ImGui.Separator();
            ImGui.Spacing();
        }

        /// <summary>
        /// 绘制危险区域配置区域。
        /// </summary>
        private void DrawDangerAreaConfigSection()
        {
            if (ImGui.CollapsingHeader("危险区域配置", ImGuiTreeNodeFlags.DefaultOpen))
            {
                // 危险区域类型选择
                ImGui.Text("危险区域类型:");
                ImGui.RadioButton("圆形", ref _dangerAreaType, 0);
                ImGui.SameLine();
                ImGui.RadioButton("矩形", ref _dangerAreaType, 1);

                if (_dangerAreaType == 0)
                {
                    // 圆形配置
                    ImGui.InputFloat("半径##圆形", ref _circleRadius, 0.5f, 1.0f);
                }
                else
                {
                    // 矩形配置
                    ImGui.InputFloat("宽度##矩形", ref _rectWidth, 0.5f, 1.0f);
                    ImGui.InputFloat("高度##矩形", ref _rectHeight, 0.5f, 1.0f);
                    ImGui.InputFloat("旋转角度（度）##矩形", ref _rectRotation, 1.0f, 5.0f);
                    ImGui.SameLine();
                    if (ImGui.SmallButton("归零##ResetRotation"))
                    {
                        _rectRotation = 0.0f;
                    }
                }

                // 位置设置
                ImGui.Text("危险区域位置:");
                ImGui.InputText("坐标 (x,y,z)##矩形中心", ref _tempDangerAreaPosInput, 50);

                if (ImGui.Button("设置位置##矩形中心"))
                {
                    if (TryParseVector3(_tempDangerAreaPosInput, out Vector3 pos))
                    {
                        _tempDangerAreaPos = pos;
                    }
                }

                ImGui.SameLine();
                if (ImGui.Button("获取玩家位置##DangerArea"))
                {
                    var player = Svc.ClientState.LocalPlayer;
                    if (player != null)
                    {
                        _tempDangerAreaPos = player.Position;
                        _tempDangerAreaPosInput = $"{player.Position.X:F1},{player.Position.Y:F1},{player.Position.Z:F1}";
                    }
                }

                ImGui.Text($"当前位置: ({_tempDangerAreaPos.X:F1}, {_tempDangerAreaPos.Y:F1}, {_tempDangerAreaPos.Z:F1})");

                // 添加危险区域按钮
                if (ImGui.Button("添加危险区域"))
                {
                    AddDangerArea();
                }

                ImGui.SameLine();
                if (ImGui.Button("清除所有危险区域"))
                {
                    BattleDataInstance.TempDangerAreas.Clear();
                    BattleDataInstance.IsCalculated = false;
                    MarkOverlayDirty();
                }

                // 显示当前危险区域数量
                ImGui.Text($"当前危险区域数量: {BattleDataInstance.TempDangerAreas.Count}");

                // 显示危险区域列表
                if (BattleDataInstance.TempDangerAreas.Count > 0)
                {
                    ImGui.Separator();
                    ImGui.Text("危险区域列表:");
                    for (int i = 0; i < BattleDataInstance.TempDangerAreas.Count; i++)
                    {
                        var area = BattleDataInstance.TempDangerAreas[i];
                        if (area is CircleDangerArea circle)
                        {
                            ImGui.Text($"{i+1}. [圆形] 中心: ({circle.Center.X:F1}, {circle.Center.Y:F1}), 半径: {circle.Radius:F1}");
                        }
                        else if (area is RectangleDangerArea rect)
                        {
                            ImGui.Text($"{i+1}. [矩形] 中心: ({rect.Center.X:F1}, {rect.Center.Y:F1}), 宽: {rect.Width:F1}, 高: {rect.Height:F1}, 旋转: {rect.Rotation:F1}°");
                        }

                        // 添加删除按钮
                        ImGui.SameLine();
                        if (ImGui.SmallButton($"删除##{i}"))
                        {
                            BattleDataInstance.TempDangerAreas.RemoveAt(i);
                            BattleDataInstance.IsCalculated = false;
                            MarkOverlayDirty();
                            break; // 删除后退出循环，避免索引问题
                        }
                    }
                }

                ImGui.Spacing();
            }
        }

        /// <summary>
        /// 绘制参考点设置区域。
        /// </summary>
        private void DrawReferencePointSection()
        {
            if (ImGui.CollapsingHeader("参考点设置##参考点", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Text("参考点坐标:##参考点");
                ImGui.InputText("坐标 (x,y,z)##参考点", ref _referencePointInput, 50);

                if (ImGui.Button("设置参考点##参考点"))
                {
                    if (TryParseVector3(_referencePointInput, out Vector3 point))
                    {
                        _referencePoint = point;
                        BattleDataInstance.ReferencePoint = point;
                        MarkOverlayDirty();
                    }
                }

                ImGui.SameLine();
                if (ImGui.Button("获取BOSS位置"))
                {
                    var player = TargetMgr.Instance.Enemys.Values.FirstOrDefault(e => e.IsBoss() && e.IsTargetable);
                    if (player != null)
                    {
                        _referencePoint = player.Position;
                        BattleDataInstance.ReferencePoint = player.Position;
                        _referencePointInput = $"{player.Position.X:F1},{player.Position.Y:F1},{player.Position.Z:F1}";
                        MarkOverlayDirty();
                    }
                }

                if (BattleDataInstance.ReferencePoint.HasValue)
                {
                    ImGui.Text($"当前参考点: {BattleDataInstance.ReferencePoint.Value.X:F1}, {BattleDataInstance.ReferencePoint.Value.Y:F1}, {BattleDataInstance.ReferencePoint.Value.Z:F1}");
                }

                ImGui.Spacing();
            }
        }

        /// <summary>
        /// 绘制计算参数设置区域。
        /// </summary>
        private void DrawCalculationParamsSection()
        {
            if (ImGui.CollapsingHeader("计算参数设置", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.InputInt("贴近参考点数量", ref _closeToRefCount, 1, 1);
                _closeToRefCount = Math.Max(1, Math.Min(8, _closeToRefCount));

                ImGui.InputDouble("最大远离距离", ref _maxFarDistance, 1.0, 5.0);
                ImGui.InputDouble("最小安全点间距", ref _minSafePointDistance, 0.5, 1.0);
                ImGui.InputDouble("采样步长", ref _sampleStep, 0.1, 0.5);

                ImGui.Separator();
                ImGui.Text("限制范围设置:");

                // 限制范围类型选择
                ImGui.Text("限制范围类型:");
                ImGui.RadioButton("矩形范围", ref _limitRangeType, 0);
                ImGui.SameLine();
                ImGui.RadioButton("圆形范围", ref _limitRangeType, 1);

                if (_limitRangeType == 0)
                {
                    // 矩形范围配置
                    ImGui.Text("矩形范围参数:");
                    ImGui.InputText("中心坐标 (x,y,z)", ref _limitRectCenterInput, 50);
                    ImGui.SameLine();
                    if (ImGui.Button("获取玩家位置##limitRect"))
                    {
                        var player = Svc.ClientState.LocalPlayer;
                        if (player != null)
                        {
                            _limitRectCenter = player.Position;
                            _limitRectCenterInput = $"{player.Position.X:F1},{player.Position.Y:F1},{player.Position.Z:F1}";
                        }
                    }
                    ImGui.InputDouble("长度 (X方向)", ref _limitRectLength, 1.0, 5.0);
                    ImGui.SameLine();
                    ImGui.InputDouble("宽度 (Y方向)", ref _limitRectWidth, 1.0, 5.0);
                }
                else
                {
                    // 圆形范围配置
                    ImGui.Text("圆形范围参数:");
                    ImGui.InputText("中心坐标 (x,y,z)", ref _limitCircleCenterInput, 50);
                    ImGui.SameLine();
                    if (ImGui.Button("获取玩家位置##limitCircle"))
                    {
                        var player = Svc.ClientState.LocalPlayer;
                        if (player != null)
                        {
                            _limitCircleCenter = player.Position;
                            _limitCircleCenterInput = $"{player.Position.X:F1},{player.Position.Y:F1},{player.Position.Z:F1}";
                        }
                    }
                    ImGui.InputDouble("半径", ref _limitCircleRadius, 1.0, 5.0);
                }

                ImGui.Spacing();
            }
        }

        /// <summary>
        /// 绘制计算控制区域。
        /// </summary>
        private void DrawCalculationSection()
        {
            if (ImGui.CollapsingHeader("安全点计算", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.Button("计算安全点"))
                {
                    CalculateSafePoints();
                }

                ImGui.SameLine();
                if (ImGui.Button("清除计算结果"))
                {
                    BattleDataInstance.SafePoints.Clear();
                    BattleDataInstance.IsCalculated = false;
                    MarkOverlayDirty();
                }

                ImGui.Spacing();
            }
        }

        /// <summary>
        /// 绘制结果显示区域。
        /// </summary>
        private void DrawResultsSection()
        {
            if (ImGui.CollapsingHeader("计算结果", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (BattleDataInstance.IsCalculated && BattleDataInstance.SafePoints.Count > 0)
                {
                    ImGui.Text($"找到 {BattleDataInstance.SafePoints.Count} 个安全点:");

                    for (int i = 0; i < BattleDataInstance.SafePoints.Count; i++)
                    {
                        var point = BattleDataInstance.SafePoints[i];
                        var worldPos = Point.ToVector3(point);
                        string group = i < _closeToRefCount ? "[贴近组]" : "[自由组]";

                        ImGui.Text($"{i+1}. {group} ({worldPos.X:F1}, {worldPos.Z:F1})");
                    }
                }
                else
                {
                    ImGui.Text("尚未计算安全点或计算无结果");
                }
            }
        }

        private void DrawVisualizationSection()
        {
            if (ImGui.CollapsingHeader("危险区域可视化", ImGuiTreeNodeFlags.DefaultOpen))
            {
                bool enabled = OverlayEnabled;
                if (ImGui.Checkbox("在战斗画面中绘制危险区域", ref enabled))
                {
                    ToggleOverlay(enabled);
                }

                ImGui.SameLine();
                if (ImGui.Button("刷新绘制##DangerAreaOverlay"))
                {
                    MarkOverlayDirty();
                }

                var statusColor = OverlayEnabled ? new Vector4(0.4f, 0.85f, 0.4f, 1f) : new Vector4(0.85f, 0.4f, 0.4f, 1f);
                ImGui.TextColored(statusColor, OverlayEnabled ? "绘制已开启" : "绘制已关闭");
                ImGui.Spacing();
            }

            SyncOverlayIfNeeded();
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// 添加危险区域到列表中。
        /// </summary>
        private void AddDangerArea()
        {
            var center2D = Point.FromVector3(_tempDangerAreaPos);

            if (_dangerAreaType == 0)
            {
                // 添加圆形危险区域
                BattleDataInstance.TempDangerAreas.Add(new CircleDangerArea
                {
                    Center = center2D,
                    Radius = _circleRadius
                });
            }
            else
            {
                // 添加矩形危险区域（使用中心点+宽高+旋转角度方式）
                BattleDataInstance.TempDangerAreas.Add(new RectangleDangerArea
                {
                    Center = center2D,
                    Width = _rectWidth,
                    Height = _rectHeight,
                    Rotation = _rectRotation
                });
            }

            BattleDataInstance.IsCalculated = false;
            MarkOverlayDirty();
        }

        /// <summary>
        /// 计算安全点。
        /// </summary>
        private void CalculateSafePoints()
        {
            if (!BattleDataInstance.ReferencePoint.HasValue)
            {
                ImGui.Text("请先设置参考点");
                return;
            }

            if (BattleDataInstance.TempDangerAreas.Count == 0)
            {
                ImGui.Text("请先添加危险区域");
                return;
            }

            try
            {
                var calculator = new SafePointCalculator();
                var referencePoint2D = Point.FromVector3(BattleDataInstance.ReferencePoint.Value);

                // 更新 BattleData 中的参数
                BattleDataInstance.CloseToRefCount = _closeToRefCount;
                BattleDataInstance.MaxFarDistance = _maxFarDistance;
                BattleDataInstance.MinSafePointDistance = _minSafePointDistance;

                // 根据限制范围类型调用不同的计算方法
                LimitRangeType limitType = _limitRangeType == 0 ? LimitRangeType.Rectangle : LimitRangeType.Circle;

                if (limitType == LimitRangeType.Rectangle)
                {
                    // 解析矩形中心坐标
                    if (!TryParseVector3(_limitRectCenterInput, out Vector3 rectCenterWorld))
                    {
                        ImGui.Text("矩形限制范围的中心坐标格式错误");
                        return;
                    }
                    var rectCenter2D = Point.FromVector3(rectCenterWorld);
                    var rectParams = new Tuple<Point, double, double>(rectCenter2D, _limitRectLength, _limitRectWidth);

                    BattleDataInstance.SafePoints = calculator.FindSafePoints(
                        limitType: limitType,
                        rectLimitParams: rectParams,
                        dangerAreas: BattleDataInstance.TempDangerAreas,
                        referencePoint: referencePoint2D,
                        minSafePointDistance: _minSafePointDistance,
                        closeToRefCount: _closeToRefCount,
                        maxFarDistance: _maxFarDistance,
                        sampleStep: _sampleStep,
                        totalSafePointCount: 8);
                }
                else
                {
                    // 解析圆形中心坐标
                    if (!TryParseVector3(_limitCircleCenterInput, out Vector3 centerWorld))
                    {
                        ImGui.Text("圆形限制范围的中心坐标格式错误");
                        return;
                    }
                    var center2D = Point.FromVector3(centerWorld);
                    var circleParams = new Tuple<Point, double>(center2D, _limitCircleRadius);

                    BattleDataInstance.SafePoints = calculator.FindSafePoints(
                        limitType: limitType,
                        circleLimitParams: circleParams,
                        dangerAreas: BattleDataInstance.TempDangerAreas,
                        referencePoint: referencePoint2D,
                        minSafePointDistance: _minSafePointDistance,
                        closeToRefCount: _closeToRefCount,
                        maxFarDistance: _maxFarDistance,
                        sampleStep: _sampleStep,
                        totalSafePointCount: 8);
                }

                BattleDataInstance.IsCalculated = true;
                MarkOverlayDirty();
            }
            catch (Exception ex)
            {
                ImGui.Text($"计算失败: {ex.Message}");
            }
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
            var payload = DangerAreaDisplayBuilder.Build(BattleDataInstance, _renderConfig);
            _dangerAreaRenderer.UpdateObjects(payload);
            _overlayDirty = false;
        }

        /// <summary>
        /// 尝试解析Vector3坐标字符串。
        /// </summary>
        private bool TryParseVector3(string input, out Vector3 result)
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
                // 解析失败，返回false
            }
            return false;
        }

        #endregion
    }
}
