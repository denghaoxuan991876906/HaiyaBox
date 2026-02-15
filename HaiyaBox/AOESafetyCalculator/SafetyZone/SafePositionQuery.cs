using System.Runtime.CompilerServices;
using AOESafetyCalculator.Core;

namespace AOESafetyCalculator.SafetyZone;

/// <summary>
/// 安全位置查询构建器（支持链式调用）
/// </summary>
[SkipLocalsInit]
public sealed class SafePositionQuery
{
    private const float MinDistanceFloor = 0.1f;
    private readonly SafeZoneCalculator calculator;
    private readonly int count;
    private readonly WPos searchCenter;
    private readonly float searchRadius;
    private readonly DateTime currentTime;
    private readonly ArenaBounds? arenaBounds;  // 场地边界（可选）

    // 约束条件
    private WPos? targetPosition;
    private float? maxDistanceFromTarget;
    private float minDistanceBetweenPoints = 2.0f;
    private WPos? centerPoint;
    private float? minAngleBetweenPoints;
    private WPos? orderByReference;  // 排序参考点

    internal SafePositionQuery(
        SafeZoneCalculator calculator,
        int count,
        WPos searchCenter,
        float searchRadius,
        DateTime currentTime,
        ArenaBounds? arenaBounds = null)
    {
        this.calculator = calculator;
        this.count = count;
        this.searchCenter = searchCenter;
        this.searchRadius = searchRadius;
        this.currentTime = currentTime;
        this.arenaBounds = arenaBounds;
    }

    /// <summary>
    /// 约束：安全点尽量靠近目标点
    /// </summary>
    /// <param name="target">目标点</param>
    /// <param name="maxDistance">最大距离限制（可选）</param>
    public SafePositionQuery NearTarget(WPos target, float? maxDistance = null)
    {
        targetPosition = target;
        maxDistanceFromTarget = maxDistance;
        // 默认使用目标点作为排序参考
        orderByReference ??= target;
        return this;
    }

    /// <summary>
    /// 约束：安全点之间的最小距离
    /// </summary>
    /// <param name="minDistance">最小距离（米）</param>
    public SafePositionQuery MinDistanceBetween(float minDistance)
    {
        minDistanceBetweenPoints = MathF.Max(minDistance, MinDistanceFloor);
        return this;
    }

    /// <summary>
    /// 约束：安全点相对于中心点的最小角度间隔
    /// </summary>
    /// <param name="center">中心点</param>
    /// <param name="minAngle">最小角度间隔</param>
    public SafePositionQuery WithMinAngle(WPos center, Angle minAngle)
    {
        centerPoint = center;
        minAngleBetweenPoints = minAngle.Rad;
        return this;
    }

    /// <summary>
    /// 指定排序参考点（结果按距离此点的距离排序，近的在前）
    /// </summary>
    /// <param name="reference">参考点</param>
    public SafePositionQuery OrderByDistanceTo(WPos reference)
    {
        orderByReference = reference;
        return this;
    }

    /// <summary>
    /// 执行查询，返回排序后的安全位置列表
    /// </summary>
    public List<WPos> Execute()
    {
        // 1. 生成候选点
        var candidates = GenerateCandidates();

        // 2. 过滤不安全的点
        var safePoints = FilterSafePoints(candidates);

        // 3. 按距离目标点排序（近的在前）
        if (targetPosition.HasValue)
        {
            var target = targetPosition.Value;
            safePoints.Sort((a, b) =>
            {
                var distA = (a - target).LengthSq();
                var distB = (b - target).LengthSq();
                return distA.CompareTo(distB);
            });
        }

        // 4. 选择前N个点（考虑角度约束）
        var selectedPoints = SelectBestPoints(safePoints);

        // 5. 排序（如果指定了排序参考点）
        if (orderByReference.HasValue)
        {
            var refPos = orderByReference.Value;
            selectedPoints.Sort((a, b) =>
            {
                var distA = (a - refPos).LengthSq();
                var distB = (b - refPos).LengthSq();
                return distA.CompareTo(distB);
            });
        }

        SafeZoneDrawRegistry.ReportSafePoints(calculator, selectedPoints, currentTime);
        return selectedPoints;
    }

    // 生成候选点（泊松圆盘采样）
    private List<WPos> GenerateCandidates()
    {
        var candidates = new List<WPos>();
        var gridSize = minDistanceBetweenPoints / MathF.Sqrt(2);
        var gridWidth = (int)(searchRadius * 2 / gridSize) + 1;
        var grid = new int[gridWidth, gridWidth];
        var activeList = new List<WPos>();

        // 初始化：优先从目标点附近开始采样，如果没有目标点则从搜索中心开始
        var initialPoint = targetPosition ?? searchCenter;
        
        // 确保初始点在搜索范围内
        if ((initialPoint - searchCenter).Length() > searchRadius)
        {
            // 如果目标点超出搜索范围，将初始点投影到搜索边界上
            var dir = initialPoint - searchCenter;
            if (dir.Length() > 0.001f)
            {
                initialPoint = searchCenter + dir.Normalized() * (searchRadius * 0.9f);
            }
            else
            {
                initialPoint = searchCenter;
            }
        }
        
        // 确保初始点在场地内（如果设置了场地边界）
        if (arenaBounds != null && !arenaBounds.Contains(initialPoint))
        {
            initialPoint = searchCenter;
        }
        
        candidates.Add(initialPoint);
        activeList.Add(initialPoint);
        var gridX = (int)((initialPoint.X - (searchCenter.X - searchRadius)) / gridSize);
        var gridZ = (int)((initialPoint.Z - (searchCenter.Z - searchRadius)) / gridSize);
        if (gridX >= 0 && gridX < gridWidth && gridZ >= 0 && gridZ < gridWidth)
            grid[gridX, gridZ] = candidates.Count;

        var random = new Random();
        var maxAttempts = 30;

        while (activeList.Count > 0)
        {
            var index = random.Next(activeList.Count);
            var point = activeList[index];
            var found = false;

            for (var i = 0; i < maxAttempts; i++)
            {
                var angle = random.NextSingle() * MathF.Tau;
                var radius = minDistanceBetweenPoints * (1 + random.NextSingle());
                var newPoint = point + new WDir(
                    MathF.Sin(angle) * radius,
                    MathF.Cos(angle) * radius
                );

                // 检查是否在搜索范围内
                if ((newPoint - searchCenter).Length() > searchRadius)
                    continue;

                // 检查是否在场地内（如果设置了场地边界）
                if (arenaBounds != null && !arenaBounds.Contains(newPoint))
                    continue;

                // 检查网格
                gridX = (int)((newPoint.X - (searchCenter.X - searchRadius)) / gridSize);
                gridZ = (int)((newPoint.Z - (searchCenter.Z - searchRadius)) / gridSize);

                if (gridX < 0 || gridX >= gridWidth || gridZ < 0 || gridZ >= gridWidth)
                    continue;

                // 检查周围格子是否有点
                var valid = true;
                for (var dx = -2; dx <= 2 && valid; dx++)
                {
                    for (var dz = -2; dz <= 2 && valid; dz++)
                    {
                        var checkX = gridX + dx;
                        var checkZ = gridZ + dz;
                        if (checkX >= 0 && checkX < gridWidth && checkZ >= 0 && checkZ < gridWidth)
                        {
                            var cellIndex = grid[checkX, checkZ];
                            if (cellIndex > 0)
                            {
                                var existingPoint = candidates[cellIndex - 1];
                                if ((newPoint - existingPoint).Length() < minDistanceBetweenPoints)
                                {
                                    valid = false;
                                }
                            }
                        }
                    }
                }

                if (valid)
                {
                    candidates.Add(newPoint);
                    activeList.Add(newPoint);
                    grid[gridX, gridZ] = candidates.Count;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                activeList.RemoveAt(index);
            }
        }

        return candidates;
    }

    // 过滤不安全的点
    private List<WPos> FilterSafePoints(List<WPos> candidates)
    {
        var safePoints = new List<WPos>();

        foreach (var point in candidates)
        {
            // 检查是否安全
            if (!calculator.IsSafe(point, currentTime))
                continue;

            // 检查最大距离约束
            if (maxDistanceFromTarget.HasValue && targetPosition.HasValue)
            {
                var distance = (point - targetPosition.Value).Length();
                if (distance > maxDistanceFromTarget.Value)
                    continue;
            }

            safePoints.Add(point);
        }

        return safePoints;
    }

    // 选择前N个点（考虑角度约束）
    private List<WPos> SelectBestPoints(List<WPos> sortedPoints)
    {
        var selected = new List<WPos>();

        foreach (var point in sortedPoints)
        {
            if (selected.Count >= count)
                break;

            // 检查角度约束
            if (centerPoint.HasValue && minAngleBetweenPoints.HasValue && selected.Count > 0)
            {
                var valid = true;
                var newAngle = Angle.FromDirection(point - centerPoint.Value);

                foreach (var existingPoint in selected)
                {
                    var existingAngle = Angle.FromDirection(existingPoint - centerPoint.Value);
                    var angleDiff = Math.Abs((newAngle - existingAngle).Normalized().Rad);
                    if (angleDiff < minAngleBetweenPoints.Value)
                    {
                        valid = false;
                        break;
                    }
                }

                if (!valid)
                    continue;
            }

            selected.Add(point);
        }

        return selected;
    }
}
