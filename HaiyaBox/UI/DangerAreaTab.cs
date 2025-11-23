

using System.Numerics;
using System.Linq;
using System.Collections.Generic;
using System;
using AEAssist;
using AEAssist.Helper;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using ECommons.DalamudServices;
using HaiyaBox.Settings;
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
        private Vector3 _tempDangerAreaPos = Vector3.Zero;

        // 参考点配置
        private Vector3 _referencePoint = Vector3.Zero;
        private string _referencePointInput = "0,0,0";

        // 计算参数
        private int _closeToRefCount = 3;
        private double _maxFarDistance = 25.0;
        private double _minSafePointDistance = 3.0;
        private double _sampleStep = 0.5;

        // 限制范围配置
        private int _limitRangeType = 0; // 0: 矩形, 1: 圆形

        // 矩形范围参数
        private Vector3 _limitRectCenter = new Vector3(17.5f, 0, 17.5f);
        private double _limitRectLength = 35.0; // 长度 (X方向)
        private double _limitRectWidth = 35.0;   // 宽度 (Y方向)
        private string _limitRectCenterInput = "17.5,0,17.5";

        // 圆形范围参数
        private Vector3 _limitCircleCenter = new Vector3(17.5f, 0, 17.5f);
        private double _limitCircleRadius = 17.5;
        private string _limitCircleCenterInput = "17.5,0,17.5";

        #endregion

        #region Public Methods

        /// <summary>
        /// 更新可视化状态。
        /// </summary>
        public void Update()
        {
            UpdateVisualization();
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
                    ImGui.InputFloat("半径", ref _circleRadius, 0.5f, 1.0f);
                }
                else
                {
                    // 矩形配置
                    ImGui.InputFloat("宽度", ref _rectWidth, 0.5f, 1.0f);
                    ImGui.InputFloat("高度", ref _rectHeight, 0.5f, 1.0f);
                }

                // 位置设置
                ImGui.Text("危险区域位置:");
                ImGui.InputFloat3("X,Y,Z", ref _tempDangerAreaPos);

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
                }

                // 显示当前危险区域数量
                ImGui.Text($"当前危险区域数量: {BattleDataInstance.TempDangerAreas.Count}");
                ImGui.Spacing();
            }
        }

        /// <summary>
        /// 绘制参考点设置区域。
        /// </summary>
        private void DrawReferencePointSection()
        {
            if (ImGui.CollapsingHeader("参考点设置", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Text("参考点坐标:");
                ImGui.InputText("坐标 (x,y,z)", ref _referencePointInput, 50);

                if (ImGui.Button("设置参考点"))
                {
                    if (TryParseVector3(_referencePointInput, out Vector3 point))
                    {
                        _referencePoint = point;
                        BattleDataInstance.ReferencePoint = point;
                    }
                }

                ImGui.SameLine();
                if (ImGui.Button("获取玩家位置"))
                {
                    var player = Svc.ClientState.LocalPlayer;
                    if (player != null)
                    {
                        _referencePoint = player.Position;
                        BattleDataInstance.ReferencePoint = player.Position;
                        _referencePointInput = $"{player.Position.X:F1},{player.Position.Y:F1},{player.Position.Z:F1}";
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
                // 添加矩形危险区域
                var halfWidth = _rectWidth / 2.0;
                var halfHeight = _rectHeight / 2.0;
                BattleDataInstance.TempDangerAreas.Add(new RectangleDangerArea
                {
                    MinX = center2D.X - halfWidth,
                    MaxX = center2D.X + halfWidth,
                    MinY = center2D.Y - halfHeight,
                    MaxY = center2D.Y + halfHeight
                });
            }

            BattleDataInstance.IsCalculated = false;
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
            }
            catch (Exception ex)
            {
                ImGui.Text($"计算失败: {ex.Message}");
            }
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

        /// <summary>
        /// 更新可视化显示（安全点和危险区域）。
        /// </summary>
        private void UpdateVisualization()
        {
            // 清除之前的可视化
            Share.TrustDebugPoint.Clear();

            // 添加安全点到可视化
            if (BattleDataInstance.IsCalculated && BattleDataInstance.SafePoints.Count > 0)
            {
                foreach (var safePoint in BattleDataInstance.SafePoints)
                {
                    var worldPos = Point.ToVector3(safePoint);
                    Share.TrustDebugPoint.Add(worldPos);
                }
            }

            // 绘制危险区域（深红色边框）
            DrawDangerAreas();
        }

        /// <summary>
        /// 绘制危险区域的深红色边框。
        /// </summary>
        private void DrawDangerAreas()
        {
            if (BattleDataInstance.TempDangerAreas.Count == 0)
                return;

            // 获取 ImGui 绘制列表
            var drawList = ImGui.GetBackgroundDrawList();

            foreach (var dangerArea in BattleDataInstance.TempDangerAreas)
            {
                if (dangerArea is CircleDangerArea circle)
                {
                    DrawCircleDangerArea(drawList, circle);
                }
                else if (dangerArea is RectangleDangerArea rect)
                {
                    DrawRectangleDangerArea(drawList, rect);
                }
            }
        }

        /// <summary>
        /// 绘制圆形危险区域。
        /// </summary>
        private void DrawCircleDangerArea(ImDrawListPtr drawList, CircleDangerArea circle)
        {
            var centerWorld = Point.ToVector3(circle.Center);

            // 绘制圆形边框（深红色）
            const int segments = 32;
            const float thickness = 2.0f;

            for (int i = 0; i < segments; i++)
            {
                float angle1 = 2.0f * MathF.PI * i / segments;
                float angle2 = 2.0f * MathF.PI * (i + 1) / segments;

                var p1World = centerWorld + new Vector3(
                    MathF.Cos(angle1) * (float)circle.Radius,
                    0,
                    MathF.Sin(angle1) * (float)circle.Radius);

                var p2World = centerWorld + new Vector3(
                    MathF.Cos(angle2) * (float)circle.Radius,
                    0,
                    MathF.Sin(angle2) * (float)circle.Radius);

                // 使用 Svc.GameGui.WorldToScreen 转换坐标
                if (Svc.GameGui.WorldToScreen(p1World, out var p1Screen) &&
                    Svc.GameGui.WorldToScreen(p2World, out var p2Screen))
                {
                    drawList.AddLine(p1Screen, p2Screen, 0xFF0000FF, thickness); // 深红色 (Alpha=FF, Red=00, Green=00, Blue=FF in BGRA)
                }
            }
        }

        /// <summary>
        /// 绘制矩形危险区域。
        /// </summary>
        private void DrawRectangleDangerArea(ImDrawListPtr drawList, RectangleDangerArea rect)
        {
            const float thickness = 2.0f;
            const uint deepRedColor = 0xFF0000FF; // 深红色

            // 定义矩形的四个角（3D世界坐标）
            var corners = new[]
            {
                new Vector3((float)rect.MinX, 0, (float)rect.MinY),
                new Vector3((float)rect.MaxX, 0, (float)rect.MinY),
                new Vector3((float)rect.MaxX, 0, (float)rect.MaxY),
                new Vector3((float)rect.MinX, 0, (float)rect.MaxY)
            };

            // 转换为屏幕坐标并绘制边框
            var screenCorners = new Vector2[corners.Length];
            int validCorners = 0;

            for (int i = 0; i < corners.Length; i++)
            {
                if (Svc.GameGui.WorldToScreen(corners[i], out screenCorners[i]))
                {
                    validCorners++;
                }
            }

            // 只有当所有角都能转换时才绘制完整的边框
            if (validCorners == 4)
            {
                for (int i = 0; i < 4; i++)
                {
                    var next = (i + 1) % 4;
                    drawList.AddLine(screenCorners[i], screenCorners[next], deepRedColor, thickness);
                }
            }
        }

        #endregion
    }
}