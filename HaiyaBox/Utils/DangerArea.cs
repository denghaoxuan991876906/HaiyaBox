using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace HaiyaBox.Utils
{
    // 限制范围类型枚举（新增）
    public enum LimitRangeType
    {
        Rectangle, // 矩形范围
        Circle     // 圆形范围
    }
    public abstract class DangerArea
    {
        public abstract bool IsPointInDanger(Point point);
    }

    public class CircleDangerArea : DangerArea
    {
        public Point Center { get; set; }
        public double Radius { get; set; }

        public override bool IsPointInDanger(Point point)
        {
            double distance = Math.Sqrt(Math.Pow(point.X - Center.X, 2) + Math.Pow(point.Y - Center.Y, 2));
            return distance <= Radius;
        }
    }

    public class RectangleDangerArea : DangerArea
    {
        public double MinX { get; set; }
        public double MaxX { get; set; }
        public double MinY { get; set; }
        public double MaxY { get; set; }

        public override bool IsPointInDanger(Point point)
        {
            return point.X >= MinX && point.X <= MaxX && point.Y >= MinY && point.Y <= MaxY;
        }
    }

    public class Point
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double GetDistanceTo(Point other)
        {
            return Math.Sqrt(Math.Pow(X - other.X, 2) + Math.Pow(Y - other.Y, 2));
        }

        public override string ToString()
        {
            return $"({X:F1}, {Y:F1})";
        }

        // Vector3 坐标转换方法
        public static Point FromVector3(Vector3 v3)
        {
            return new Point(v3.X, v3.Z);
        }

        public static Vector3 ToVector3(Point point)
        {
            return new Vector3((float)point.X, 0, (float)point.Y);
        }
    }

    public class SafePointCalculator
    {
        /// <summary>
        /// 查找安全点（支持矩形/圆形限制范围）
        /// </summary>
        /// <param name="limitType">限制范围类型（矩形/圆形）</param>
        /// <param name="rectLimitParams">矩形范围参数（minX, maxX, minY, maxY）</param>
        /// <param name="circleLimitParams">圆形范围参数（圆心, 半径）</param>
        /// <param name="dangerAreas">危险区域列表</param>
        /// <param name="referencePoint">参考点</param>
        /// <param name="minSafePointDistance">安全点之间最小间距</param>
        /// <param name="closeToRefCount">需要贴近参考点的数量</param>
        /// <param name="maxFarDistance">自由分布组的最大远离距离</param>
        /// <param name="sampleStep">采样步长</param>
        /// <param name="totalSafePointCount">总安全点数量</param>
        /// <returns>分组合并后的安全点列表</returns>
        public List<Point> FindSafePoints(
            LimitRangeType limitType,
            Tuple<Point, double, double> rectLimitParams = null, // 矩形参数：(中心, 长度, 宽度)
            Tuple<Point, double> circleLimitParams = null, // 圆形参数：(圆心, 半径)
            List<DangerArea> dangerAreas = null,
            Point referencePoint = null,
            double minSafePointDistance = 3.0,
            int closeToRefCount = 3,
            double maxFarDistance = 25.0,
            double sampleStep = 0.5,
            int totalSafePointCount = 8)
        {
            // 校验参数合法性
            if (limitType == LimitRangeType.Rectangle && rectLimitParams == null)
                throw new ArgumentNullException(nameof(rectLimitParams), "矩形范围需传入rectLimitParams");
            if (limitType == LimitRangeType.Circle && circleLimitParams == null)
                throw new ArgumentNullException(nameof(circleLimitParams), "圆形范围需传入circleLimitParams");
            dangerAreas ??= new List<DangerArea>();
            referencePoint ??= new Point(0, 0);

            // 1. 网格采样（根据限制范围动态计算采样边界，避免无效采样）
            (double sampleMinX, double sampleMaxX, double sampleMinY, double sampleMaxY) =
                GetSampleBounds(limitType, rectLimitParams, circleLimitParams);
            List<Point> allSamplePoints = new List<Point>();
            for (double x = sampleMinX; x <= sampleMaxX; x += sampleStep)
            {
                for (double y = sampleMinY; y <= sampleMaxY; y += sampleStep)
                {
                    allSamplePoints.Add(new Point(x, y));
                }
            }

            // 2. 筛选基础安全点：① 在限制范围内 ② 不在任何危险区
            List<Point> baseSafePoints = allSamplePoints
                .Where(p => IsPointInLimitRange(p, limitType, rectLimitParams, circleLimitParams)) // 新增：限制范围判定
                .Where(p => !dangerAreas.Any(area => area.IsPointInDanger(p)))
                .ToList();

            if (baseSafePoints.Count == 0)
                throw new Exception("无可用安全点，请调整参数（如扩大范围、缩小危险区、减小采样步长）");

            // 3. 第一组：优先贴近参考点的安全点
            List<Point> closeToRefPoints = new List<Point>();
            var sortedByRefDistance = baseSafePoints.OrderBy(p => p.GetDistanceTo(referencePoint)).ToList();
            foreach (var point in sortedByRefDistance)
            {
                if (closeToRefPoints.All(p => p.GetDistanceTo(point) >= minSafePointDistance))
                {
                    closeToRefPoints.Add(point);
                    if (closeToRefPoints.Count == closeToRefCount)
                        break;
                }
            }

            // 4. 第二组：自由分布安全点
            List<Point> freePoints = new List<Point>();
            var candidateFreePoints = baseSafePoints
                .Where(p => p.GetDistanceTo(referencePoint) <= maxFarDistance)
                .Except(closeToRefPoints)
                .OrderBy(_ => Guid.NewGuid()) // 随机排序，自由分布
                .ToList();

            foreach (var point in candidateFreePoints)
            {
                bool isFarEnough = closeToRefPoints.All(p => p.GetDistanceTo(point) >= minSafePointDistance)
                                   && freePoints.All(p => p.GetDistanceTo(point) >= minSafePointDistance);
                if (isFarEnough)
                {
                    freePoints.Add(point);
                    if (closeToRefPoints.Count + freePoints.Count == totalSafePointCount)
                        break;
                }
            }

            // 5. 合并结果
            var result = closeToRefPoints.Concat(freePoints).ToList();
            if (result.Count < totalSafePointCount)
                Console.WriteLine($"警告：仅找到{result.Count}个符合要求的安全点（需{totalSafePointCount}个），可缩小最小间距或调整maxFarDistance");

            return result;
        }

        /// <summary>
        /// 辅助方法：判断点是否在限制范围内（矩形/圆形）
        /// </summary>
        private bool IsPointInLimitRange(
            Point point,
            LimitRangeType limitType,
            Tuple<Point, double, double> rectParams,
            Tuple<Point, double> circleParams)
        {
            switch (limitType)
            {
                case LimitRangeType.Rectangle:
                    {
                        var center = rectParams.Item1;
                        var length = rectParams.Item2; // 长度 (X方向)
                        var width = rectParams.Item3; // 宽度 (Y方向)

                        var minX = center.X - length / 2.0;
                        var maxX = center.X + length / 2.0;
                        var minY = center.Y - width / 2.0;
                        var maxY = center.Y + width / 2.0;

                        return point.X >= minX && point.X <= maxX && point.Y >= minY && point.Y <= maxY;
                    }
                case LimitRangeType.Circle:
                    return point.GetDistanceTo(circleParams.Item1) <= circleParams.Item2;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 辅助方法：计算采样边界（避免超出限制范围的无效采样）
        /// </summary>
        private (double minX, double maxX, double minY, double maxY) GetSampleBounds(
            LimitRangeType limitType,
            Tuple<Point, double, double> rectParams,
            Tuple<Point, double> circleParams)
        {
            switch (limitType)
            {
                case LimitRangeType.Rectangle:
                    {
                        var center = rectParams.Item1;
                        var length = rectParams.Item2;
                        var width = rectParams.Item3;

                        var minX = center.X - length / 2.0;
                        var maxX = center.X + length / 2.0;
                        var minY = center.Y - width / 2.0;
                        var maxY = center.Y + width / 2.0;

                        return (minX, maxX, minY, maxY);
                    }
                case LimitRangeType.Circle:
                    return (circleParams.Item1.X - circleParams.Item2, circleParams.Item1.X + circleParams.Item2,
                        circleParams.Item1.Y - circleParams.Item2, circleParams.Item1.Y + circleParams.Item2);
                default:
                    return (0, 0, 0, 0);
            }
        }
    }

/*// 示例调用（圆形限制范围+35×35等效场景）
    class Program
    {
        static void Main(string[] args)
        {
            // 1. 配置参数（圆形限制范围示例：圆心(17.5,17.5)，半径17.5 → 等效35×35正方形的外接圆）
            LimitRangeType limitType = LimitRangeType.Circle;
            var circleLimit = new Tuple<Point, double>(new Point(17.5, 17.5), 17.5); // 圆形范围：圆心+半径
            Point referencePoint = new Point(18, 18); // 参考点

            // 2. 核心参数
            int totalSafePointCount = 8;
            int closeToRefCount = 3;
            double minSafePointDistance = 3.0;
            double maxFarDistance = 25.0;
            double sampleStep = 0.5;

            // 3. 危险区域（示例）
            List<DangerArea> dangerAreas = new List<DangerArea>
            {
                new CircleDangerArea { Center = new Point(10, 10), Radius = 2.0 },
                new RectangleDangerArea { MinX = 20, MaxX = 25, MinY = 15, MaxY = 20 },
                new CircleDangerArea { Center = new Point(30, 30), Radius = 1.5 }
            };

            // 4. 计算安全点
            SafePointCalculator calculator = new SafePointCalculator();
            List<Point> safePoints = calculator.FindSafePoints(
                limitType: limitType,
                circleLimitParams: circleLimit,
                dangerAreas: dangerAreas,
                referencePoint: referencePoint,
                minSafePointDistance: minSafePointDistance,
                closeToRefCount: closeToRefCount,
                maxFarDistance: maxFarDistance,
                sampleStep: sampleStep,
                totalSafePointCount: totalSafePointCount);

            // 5. 输出结果
            Console.WriteLine($"限制范围：圆形（圆心{circleLimit.Item1}，半径{circleLimit.Item2:F1}）");
            Console.WriteLine($"参考点：{referencePoint}");
            Console.WriteLine($"配置：{closeToRefCount}个贴近点 + {totalSafePointCount - closeToRefCount}个自由分布点（距参考点≤{maxFarDistance}单位）");
            Console.WriteLine($"安全点列表（前{closeToRefCount}个为贴近参考点）：");
            for (int i = 0; i < safePoints.Count; i++)
            {
                double distToRef = safePoints[i].GetDistanceTo(referencePoint);
                string group = i < closeToRefCount ? "[贴近组]" : "[自由组]";
                Console.WriteLine($"{i+1}. {group} {safePoints[i]} （距参考点：{distToRef:F2}单位）");
            }

            Console.ReadKey();
        }
    }*/
}
