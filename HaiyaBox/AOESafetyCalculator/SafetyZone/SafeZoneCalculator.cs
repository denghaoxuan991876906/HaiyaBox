using System.Runtime.CompilerServices;
using AOESafetyCalculator.Core;
using AOESafetyCalculator.DistanceField;

namespace AOESafetyCalculator.SafetyZone;

/// <summary>
/// 安全区域计算器（Safe Zone Calculator）
/// </summary>
/// <remarks>
/// 基于多个禁止区域计算安全位置和安全方向
/// 用于路径规划和位置决策
/// </remarks>
[SkipLocalsInit]
public sealed class SafeZoneCalculator
{
    private readonly List<ForbiddenZone> zones = [];
    private ArenaBounds? arenaBounds;

    public SafeZoneCalculator()
    {
        SafeZoneDrawRegistry.Register(this);
    }

    /// <summary>
    /// 设置场地边界
    /// </summary>
    /// <param name="bounds">场地边界（圆形或矩形）</param>
    public void SetArenaBounds(ArenaBounds bounds)
    {
        arenaBounds = bounds;
        SafeZoneDrawRegistry.Touch(this);
    }

    /// <summary>
    /// 获取当前场地边界
    /// </summary>
    public ArenaBounds? GetArenaBounds() => arenaBounds;

    /// <summary>
    /// 添加禁止区域
    /// </summary>
    /// <param name="zone">要添加的禁止区域</param>
    public void AddForbiddenZone(ForbiddenZone zone)
    {
        zones.Add(zone);
        SafeZoneDrawRegistry.Touch(this);
    }

    /// <summary>
    /// 添加多个禁止区域
    /// </summary>
    /// <param name="zones">要添加的禁止区域集合</param>
    public void AddForbiddenZones(IEnumerable<ForbiddenZone> zones)
    {
        this.zones.AddRange(zones);
        SafeZoneDrawRegistry.Touch(this);
    }

    /// <summary>
    /// 清除所有禁止区域
    /// </summary>
    public void Clear()
    {
        zones.Clear();
        SafeZoneDrawRegistry.ClearCalculator(this);
        SafeZoneDrawRegistry.Touch(this);
    }

    /// <summary>
    /// 按名称清除一个禁止区域（通常 name 唯一）
    /// </summary>
    /// <param name="name">禁止区域名称</param>
    /// <returns>true 表示已清除，false 表示未找到</returns>
    public bool ClearForbiddenZoneByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Zone name must be provided", nameof(name));

        for (var i = zones.Count - 1; i >= 0; i--)
        {
            if (zones[i].Name == name)
            {
                zones.RemoveAt(i);
                SafeZoneDrawRegistry.ClearCalculator(this);
                SafeZoneDrawRegistry.Touch(this);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 检查指定位置是否安全
    /// </summary>
    /// <param name="position">要检查的位置</param>
    /// <param name="currentTime">当前时间</param>
    /// <returns>true 表示安全，false 表示在禁止区域内</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSafe(WPos position, DateTime currentTime)
    {
        if (arenaBounds != null && !arenaBounds.Contains(position))
        {
            return false;
        }
        foreach (var zone in zones)
        {
            if (zone.IsActive(currentTime) && zone.Contains(position))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 计算指定位置到最近危险区域的距离
    /// </summary>
    /// <param name="position">要计算的位置</param>
    /// <param name="currentTime">当前时间</param>
    /// <returns>
    /// 距离值：
    /// - 正值：到最近危险区域边界的距离（安全）
    /// - 负值：在危险区域内的深度（危险）
    /// - float.MaxValue：无危险区域
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float DistanceToNearestDanger(WPos position, DateTime currentTime)
    {
        var minDistance = float.MaxValue;

        foreach (var zone in zones)
        {
            if (zone.IsActive(currentTime))
            {
                var distance = zone.Distance(position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }
        }

        return minDistance;
    }

    /// <summary>
    /// 查找最安全的方向（远离所有危险区域）
    /// </summary>
    /// <param name="position">当前位置</param>
    /// <param name="currentTime">当前时间</param>
    /// <param name="sampleCount">采样方向数量（默认8个方向）</param>
    /// <returns>最安全的方向，如果所有方向都不安全则返回零向量</returns>
    public WDir FindSafestDirection(WPos position, DateTime currentTime, int sampleCount = 8)
    {
        if (sampleCount <= 0)
            throw new ArgumentException("Sample count must be positive", nameof(sampleCount));

        var bestDirection = new WDir();
        var bestDistance = float.MinValue;

        // 采样多个方向
        for (var i = 0; i < sampleCount; i++)
        {
            var angle = new Angle(2 * MathF.PI * i / sampleCount);
            var direction = angle.ToDirection();
            var testPosition = position + direction;

            var distance = DistanceToNearestDanger(testPosition, currentTime);
            if (distance > bestDistance)
            {
                bestDistance = distance;
                bestDirection = direction;
            }
        }

        return bestDirection;
    }

    /// <summary>
    /// 在指定区域内查找最安全的位置
    /// </summary>
    /// <param name="center">搜索区域中心</param>
    /// <param name="radius">搜索半径</param>
    /// <param name="currentTime">当前时间</param>
    /// <param name="gridResolution">网格分辨率（默认1.0）</param>
    /// <returns>最安全的位置，如果所有位置都不安全则返回中心点</returns>
    public WPos FindSafestPosition(WPos center, float radius, DateTime currentTime, float gridResolution = 1.0f)
    {
        if (radius <= 0)
            throw new ArgumentException("Radius must be positive", nameof(radius));
        if (gridResolution <= 0)
            throw new ArgumentException("Grid resolution must be positive", nameof(gridResolution));

        var bestPosition = center;
        var bestDistance = DistanceToNearestDanger(center, currentTime);

        // 网格采样
        var steps = (int)(radius / gridResolution);
        for (var x = -steps; x <= steps; x++)
        {
            for (var z = -steps; z <= steps; z++)
            {
                var offset = new WDir(x * gridResolution, z * gridResolution);
                if (offset.LengthSq() > radius * radius)
                    continue;

                var testPosition = center + offset;
                var distance = DistanceToNearestDanger(testPosition, currentTime);

                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    bestPosition = testPosition;
                }
            }
        }

        return bestPosition;
    }

    /// <summary>
    /// 获取所有活跃的禁止区域数量
    /// </summary>
    /// <param name="currentTime">当前时间</param>
    /// <returns>活跃的禁止区域数量</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetActiveZoneCount(DateTime currentTime)
    {
        var count = 0;
        foreach (var zone in zones)
        {
            if (zone.IsActive(currentTime))
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// 获取所有活跃的禁止区域
    /// </summary>
    /// <param name="currentTime">当前时间</param>
    /// <returns>活跃的禁止区域集合</returns>
    public IEnumerable<ForbiddenZone> GetActiveZones(DateTime currentTime)
    {
        foreach (var zone in zones)
        {
            if (zone.IsActive(currentTime))
            {
                yield return zone;
            }
        }
    }

    /// <summary>
    /// 查找多个安全位置（使用场地边界，支持链式调用配置约束）
    /// </summary>
    /// <param name="count">需要的安全位置数量</param>
    /// <param name="currentTime">当前时间</param>
    /// <returns>安全位置查询构建器</returns>
    /// <exception cref="InvalidOperationException">未设置场地边界时抛出</exception>
    public SafePositionQuery FindSafePositions(int count, DateTime currentTime)
    {
        if (arenaBounds == null)
            throw new InvalidOperationException("必须先调用 SetArenaBounds 设置场地边界");

        return new SafePositionQuery(this, count, arenaBounds.Center, arenaBounds.ApproximateRadius, currentTime, arenaBounds);
    }

    /// <summary>
    /// 查找多个安全位置（手动指定搜索范围，支持链式调用配置约束）
    /// </summary>
    /// <param name="count">需要的安全位置数量</param>
    /// <param name="searchCenter">搜索区域中心</param>
    /// <param name="searchRadius">搜索半径</param>
    /// <param name="currentTime">当前时间</param>
    /// <returns>安全位置查询构建器</returns>
    public SafePositionQuery FindSafePositions(int count, WPos searchCenter, float searchRadius, DateTime currentTime)
    {
        return new SafePositionQuery(this, count, searchCenter, searchRadius, currentTime);
    }

    internal IReadOnlyList<ForbiddenZone> GetZones() => zones;
}
