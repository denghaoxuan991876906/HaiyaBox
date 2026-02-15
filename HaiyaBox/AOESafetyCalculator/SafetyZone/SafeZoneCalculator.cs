using System.Runtime.CompilerServices;
using AOESafetyCalculator.Core;
using AOESafetyCalculator.DistanceField;
using AOESafetyCalculator.Shapes;

namespace AOESafetyCalculator.SafetyZone;

/// <summary>
/// 安全区域计算器
/// </summary>
/// <remarks>
/// <para>基于多个禁止区域计算安全位置和安全方向，用于路径规划和位置决策。</para>
/// <para>支持链式调用，可简化配置：</para>
/// <code>
/// var calculator = new SafeZoneCalculator()
///     .SetCircleArena(center, 40f)
///     .AddCircle(dangerPos, 8f)
///     .AddRect(from, to, 5f);
/// 
/// var safePoints = calculator.FindSafePositions(8, DateTime.Now)
///     .NearTarget(bossPos, 20f)
///     .Execute();
/// </code>
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

    #region 场地边界便捷方法

    /// <summary>
    /// 设置圆形场地边界
    /// </summary>
    /// <param name="center">场地中心点</param>
    /// <param name="radius">场地半径（米）</param>
    /// <returns>返回 this 以支持链式调用</returns>
    /// <example>
    /// <code>calculator.SetCircleArena(new WPos(100, 100), 40f);</code>
    /// </example>
    public SafeZoneCalculator SetCircleArena(WPos center, float radius)
    {
        SetArenaBounds(new CircleArenaBounds(center, radius));
        return this;
    }

    /// <summary>
    /// 设置矩形场地边界（使用角度指定朝向）
    /// </summary>
    /// <param name="center">矩形中心点</param>
    /// <param name="direction">矩形朝向（长边方向）。角度约定：0°=北，90°=东，180°=南，270°=西</param>
    /// <param name="halfWidth">半宽（短边的一半，米）</param>
    /// <param name="halfLength">半长（长边的一半，米）</param>
    /// <returns>返回 this 以支持链式调用</returns>
    /// <example>
    /// <code>// 朝南的矩形场地，宽20米，长30米
    /// calculator.SetRectArena(center, 180f.Degrees(), 10f, 15f);</code>
    /// </example>
    public SafeZoneCalculator SetRectArena(WPos center, Angle direction, float halfWidth, float halfLength)
    {
        SetArenaBounds(new RectArenaBounds(center, direction.ToDirection(), halfWidth, halfLength));
        return this;
    }

    /// <summary>
    /// 设置矩形场地边界（默认朝北，即 0° 方向）
    /// </summary>
    /// <param name="center">矩形中心点</param>
    /// <param name="halfWidth">半宽（短边的一半，米）</param>
    /// <param name="halfLength">半长（长边的一半，米）</param>
    /// <returns>返回 this 以支持链式调用</returns>
    /// <example>
    /// <code>// 默认朝北的矩形场地
    /// calculator.SetRectArena(center, 10f, 15f);</code>
    /// </example>
    public SafeZoneCalculator SetRectArena(WPos center, float halfWidth, float halfLength)
    {
        SetArenaBounds(new RectArenaBounds(center, new WDir(0, 1), halfWidth, halfLength));
        return this;
    }

    /// <summary>
    /// 设置矩形场地边界（从起点到终点）
    /// </summary>
    /// <param name="from">起点（矩形的一端中心）</param>
    /// <param name="to">终点（矩形的另一端中心）</param>
    /// <param name="halfWidth">半宽（米）</param>
    /// <returns>返回 this 以支持链式调用</returns>
    /// <example>
    /// <code>// 从 A 点延伸到 B 点的矩形区域
    /// calculator.SetRectArenaFromTo(posA, posB, 5f);</code>
    /// </example>
    public SafeZoneCalculator SetRectArenaFromTo(WPos from, WPos to, float halfWidth)
    {
        var dir = (to - from).Normalized();
        SetArenaBounds(new RectArenaBounds(from, dir, halfWidth, (to - from).Length()));
        return this;
    }

    /// <summary>
    /// 设置矩形场地边界（使用方向向量）
    /// </summary>
    /// <param name="center">矩形中心点</param>
    /// <param name="direction">矩形朝向（长边方向的单位向量）</param>
    /// <param name="halfWidth">半宽（短边的一半，米）</param>
    /// <param name="halfLength">半长（长边的一半，米）</param>
    /// <returns>返回 this 以支持链式调用</returns>
    public SafeZoneCalculator SetRectArena(WPos center, WDir direction, float halfWidth, float halfLength)
    {
        SetArenaBounds(new RectArenaBounds(center, direction, halfWidth, halfLength));
        return this;
    }

    #endregion

    #region 禁止区域便捷方法

    /// <summary>
    /// 添加圆形危险区域
    /// </summary>
    /// <param name="center">圆心位置</param>
    /// <param name="radius">半径（米）</param>
    /// <param name="activation">激活时间（null 表示立即激活）</param>
    /// <returns>返回 this 以支持链式调用</returns>
    /// <example>
    /// <code>calculator.AddCircle(bossPos, 8f);</code>
    /// </example>
    public SafeZoneCalculator AddCircle(WPos center, float radius, DateTime? activation = null)
    {
        AddForbiddenZone(new ForbiddenZone
        {
            Shape = new SDCircle(center, radius),
            Activation = activation ?? DateTime.Now
        });
        return this;
    }

    /// <summary>
    /// 添加环形危险区域（圆环）
    /// </summary>
    /// <param name="center">圆心位置</param>
    /// <param name="innerRadius">内圈半径（米）</param>
    /// <param name="outerRadius">外圈半径（米）</param>
    /// <param name="activation">激活时间（null 表示立即激活）</param>
    /// <returns>返回 this 以支持链式调用</returns>
    /// <example>
    /// <code>// 内圈5米到外圈15米的环形危险区
    /// calculator.AddDonut(center, 5f, 15f);</code>
    /// </example>
    public SafeZoneCalculator AddDonut(WPos center, float innerRadius, float outerRadius, DateTime? activation = null)
    {
        AddForbiddenZone(new ForbiddenZone
        {
            Shape = new SDDonut(center, innerRadius, outerRadius),
            Activation = activation ?? DateTime.Now
        });
        return this;
    }

    /// <summary>
    /// 添加扇形危险区域（锥形）
    /// </summary>
    /// <param name="origin">扇形顶点位置</param>
    /// <param name="radius">扇形半径（米）</param>
    /// <param name="centerDir">扇形中心方向。角度约定：0°=北，90°=东，180°=南，270°=西</param>
    /// <param name="halfAngle">扇形半角（从中心线到边缘的角度）</param>
    /// <param name="activation">激活时间（null 表示立即激活）</param>
    /// <returns>返回 this 以支持链式调用</returns>
    /// <example>
    /// <code>// 朝北的90度扇形，半径20米
    /// calculator.AddCone(origin, 20f, 0f.Degrees(), 45f.Degrees());</code>
    /// </example>
    public SafeZoneCalculator AddCone(WPos origin, float radius, Angle centerDir, Angle halfAngle, DateTime? activation = null)
    {
        AddForbiddenZone(new ForbiddenZone
        {
            Shape = new SDCone(origin, radius, centerDir, halfAngle),
            Activation = activation ?? DateTime.Now
        });
        return this;
    }

    /// <summary>
    /// 添加扇形环危险区域
    /// </summary>
    /// <param name="origin">扇形顶点位置</param>
    /// <param name="innerRadius">内圈半径（米）</param>
    /// <param name="outerRadius">外圈半径（米）</param>
    /// <param name="centerDir">扇形中心方向。角度约定：0°=北，90°=东，180°=南，270°=西</param>
    /// <param name="halfAngle">扇形半角</param>
    /// <param name="activation">激活时间（null 表示立即激活）</param>
    /// <returns>返回 this 以支持链式调用</returns>
    public SafeZoneCalculator AddDonutSector(WPos origin, float innerRadius, float outerRadius, Angle centerDir, Angle halfAngle, DateTime? activation = null)
    {
        AddForbiddenZone(new ForbiddenZone
        {
            Shape = new SDDonutSector(origin, innerRadius, outerRadius, centerDir, halfAngle),
            Activation = activation ?? DateTime.Now
        });
        return this;
    }

    /// <summary>
    /// 添加矩形危险区域（使用角度指定朝向）
    /// </summary>
    /// <param name="origin">矩形原点位置</param>
    /// <param name="direction">矩形朝向。角度约定：0°=北，90°=东，180°=南，270°=西</param>
    /// <param name="lenFront">原点前方长度（米）</param>
    /// <param name="lenBack">原点后方长度（米）</param>
    /// <param name="halfWidth">半宽（米）</param>
    /// <param name="activation">激活时间（null 表示立即激活）</param>
    /// <returns>返回 this 以支持链式调用</returns>
    /// <example>
    /// <code>// 朝南的矩形：前方40米，后方0米，宽40米
    /// calculator.AddRect(origin, 180f.Degrees(), 40f, 0f, 20f);</code>
    /// </example>
    public SafeZoneCalculator AddRect(WPos origin, Angle direction, float lenFront, float lenBack, float halfWidth, DateTime? activation = null)
    {
        AddForbiddenZone(new ForbiddenZone
        {
            Shape = new SDRect(origin, direction, lenFront, lenBack, halfWidth),
            Activation = activation ?? DateTime.Now
        });
        return this;
    }

    /// <summary>
    /// 添加矩形危险区域（使用方向向量）
    /// </summary>
    /// <param name="origin">矩形原点位置</param>
    /// <param name="direction">矩形朝向（方向向量）</param>
    /// <param name="lenFront">原点前方长度（米）</param>
    /// <param name="lenBack">原点后方长度（米）</param>
    /// <param name="halfWidth">半宽（米）</param>
    /// <param name="activation">激活时间（null 表示立即激活）</param>
    /// <returns>返回 this 以支持链式调用</returns>
    public SafeZoneCalculator AddRect(WPos origin, WDir direction, float lenFront, float lenBack, float halfWidth, DateTime? activation = null)
    {
        AddForbiddenZone(new ForbiddenZone
        {
            Shape = new SDRect(origin, direction, lenFront, lenBack, halfWidth),
            Activation = activation ?? DateTime.Now
        });
        return this;
    }

    /// <summary>
    /// 添加矩形危险区域（从起点到终点）
    /// </summary>
    /// <param name="from">起点位置</param>
    /// <param name="to">终点位置</param>
    /// <param name="halfWidth">半宽（米）</param>
    /// <param name="activation">激活时间（null 表示立即激活）</param>
    /// <returns>返回 this 以支持链式调用</returns>
    /// <example>
    /// <code>// 从 A 点到 B 点的矩形危险区，宽度10米
    /// calculator.AddRectFromTo(posA, posB, 5f);</code>
    /// </example>
    public SafeZoneCalculator AddRectFromTo(WPos from, WPos to, float halfWidth, DateTime? activation = null)
    {
        AddForbiddenZone(new ForbiddenZone
        {
            Shape = new SDRect(from, to, halfWidth),
            Activation = activation ?? DateTime.Now
        });
        return this;
    }

    /// <summary>
    /// 添加十字形危险区域
    /// </summary>
    /// <param name="origin">十字中心位置</param>
    /// <param name="direction">十字朝向。角度约定：0°=北，90°=东，180°=南，270°=西</param>
    /// <param name="length">臂长（米）</param>
    /// <param name="halfWidth">臂的半宽（米）</param>
    /// <param name="activation">激活时间（null 表示立即激活）</param>
    /// <returns>返回 this 以支持链式调用</returns>
    public SafeZoneCalculator AddCross(WPos origin, Angle direction, float length, float halfWidth, DateTime? activation = null)
    {
        AddForbiddenZone(new ForbiddenZone
        {
            Shape = new SDCross(origin, direction, length, halfWidth),
            Activation = activation ?? DateTime.Now
        });
        return this;
    }

    /// <summary>
    /// 添加胶囊形危险区域（使用角度指定朝向）
    /// </summary>
    /// <param name="origin">胶囊起点位置</param>
    /// <param name="direction">胶囊朝向。角度约定：0°=北，90°=东，180°=南，270°=西</param>
    /// <param name="length">胶囊长度（米）</param>
    /// <param name="radius">胶囊端点半径（米）</param>
    /// <param name="activation">激活时间（null 表示立即激活）</param>
    /// <returns>返回 this 以支持链式调用</returns>
    public SafeZoneCalculator AddCapsule(WPos origin, Angle direction, float length, float radius, DateTime? activation = null)
    {
        AddForbiddenZone(new ForbiddenZone
        {
            Shape = new SDCapsule(origin, direction, length, radius),
            Activation = activation ?? DateTime.Now
        });
        return this;
    }

    /// <summary>
    /// 添加胶囊形危险区域（使用方向向量）
    /// </summary>
    /// <param name="origin">胶囊起点位置</param>
    /// <param name="direction">胶囊朝向（方向向量）</param>
    /// <param name="length">胶囊长度（米）</param>
    /// <param name="radius">胶囊端点半径（米）</param>
    /// <param name="activation">激活时间（null 表示立即激活）</param>
    /// <returns>返回 this 以支持链式调用</returns>
    public SafeZoneCalculator AddCapsule(WPos origin, WDir direction, float length, float radius, DateTime? activation = null)
    {
        AddForbiddenZone(new ForbiddenZone
        {
            Shape = new SDCapsule(origin, direction, length, radius),
            Activation = activation ?? DateTime.Now
        });
        return this;
    }

    /// <summary>
    /// 添加弧形胶囊危险区域（沿圆弧延伸的胶囊）
    /// </summary>
    /// <param name="start">弧形起点位置</param>
    /// <param name="orbitCenter">轨道圆心位置</param>
    /// <param name="angularLength">弧长角度（正值逆时针，负值顺时针）</param>
    /// <param name="tubeRadius">管道半径（米）</param>
    /// <param name="activation">激活时间（null 表示立即激活）</param>
    /// <returns>返回 this 以支持链式调用</returns>
    public SafeZoneCalculator AddArcCapsule(WPos start, WPos orbitCenter, Angle angularLength, float tubeRadius, DateTime? activation = null)
    {
        AddForbiddenZone(new ForbiddenZone
        {
            Shape = new SDArcCapsule(start, orbitCenter, angularLength, tubeRadius),
            Activation = activation ?? DateTime.Now
        });
        return this;
    }

    /// <summary>
    /// 添加半平面危险区域
    /// </summary>
    /// <param name="point">边界上的一个点</param>
    /// <param name="normal">法向量（指向危险区域内部）</param>
    /// <param name="activation">激活时间（null 表示立即激活）</param>
    /// <returns>返回 this 以支持链式调用</returns>
    public SafeZoneCalculator AddHalfPlane(WPos point, WDir normal, DateTime? activation = null)
    {
        AddForbiddenZone(new ForbiddenZone
        {
            Shape = new SDHalfPlane(point, normal),
            Activation = activation ?? DateTime.Now
        });
        return this;
    }

    /// <summary>
    /// 添加自定义形状的危险区域
    /// </summary>
    /// <param name="shape">形状距离场</param>
    /// <param name="activation">激活时间（null 表示立即激活）</param>
    /// <returns>返回 this 以支持链式调用</returns>
    public SafeZoneCalculator AddShape(ShapeDistance shape, DateTime? activation = null)
    {
        AddForbiddenZone(new ForbiddenZone
        {
            Shape = shape,
            Activation = activation ?? DateTime.Now
        });
        return this;
    }

    /// <summary>
    /// 添加 AOE 形状危险区域（与 UI 中使用的形状类型相同）
    /// </summary>
    /// <param name="shape">AOE 形状（如 AOEShapeCircle, AOEShapeCone, AOEShapeRect 等）</param>
    /// <param name="origin">形状原点位置</param>
    /// <param name="rotation">形状朝向。角度约定：0°=北，90°=东，180°=南，270°=西</param>
    /// <param name="activation">激活时间（null 表示立即激活）</param>
    /// <returns>返回 this 以支持链式调用</returns>
    /// <example>
    /// <code>
    /// // 圆形危险区，半径8米
    /// calculator.AddAOEShape(new AOEShapeCircle(8f), bossPos, 0f.Degrees());
    /// 
    /// // 扇形危险区，半径20米，90度角，朝北
    /// calculator.AddAOEShape(new AOEShapeCone(20f, 45f.Degrees()), bossPos, 0f.Degrees());
    /// 
    /// // 矩形危险区，前方40米，宽20米，朝南
    /// calculator.AddAOEShape(new AOEShapeRect(40f, 10f), origin, 180f.Degrees());
    /// 
    /// // 反转区域（形状内安全，形状外危险）
    /// calculator.AddAOEShape(new AOEShapeCircle(8f, invertForbiddenZone: true), safePos, 0f.Degrees());
    /// </code>
    /// </example>
    public SafeZoneCalculator AddAOEShape(AOEShape shape, WPos origin, Angle rotation, DateTime? activation = null)
    {
        var distance = shape.InvertForbiddenZone
            ? shape.InvertedDistance(origin, rotation)
            : shape.Distance(origin, rotation);
        
        AddForbiddenZone(new ForbiddenZone
        {
            Shape = distance,
            Activation = activation ?? DateTime.Now
        });
        return this;
    }

    #endregion

    #region 原有方法

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
    /// <returns>当前场地边界，未设置时返回 null</returns>
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
    /// 按名称清除一个禁止区域
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
    /// <returns>true 表示安全，false 表示在禁止区域内或场地外</returns>
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
    /// <list type="bullet">
    ///   <item><description>正值：到最近危险区域边界的距离（安全）</description></item>
    ///   <item><description>负值：在危险区域内的深度（危险）</description></item>
    ///   <item><description>float.MaxValue：无危险区域</description></item>
    /// </list>
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
    /// <param name="radius">搜索半径（米）</param>
    /// <param name="currentTime">当前时间</param>
    /// <param name="gridResolution">网格分辨率（米，默认1.0）</param>
    /// <returns>最安全的位置，如果所有位置都不安全则返回中心点</returns>
    public WPos FindSafestPosition(WPos center, float radius, DateTime currentTime, float gridResolution = 1.0f)
    {
        if (radius <= 0)
            throw new ArgumentException("Radius must be positive", nameof(radius));
        if (gridResolution <= 0)
            throw new ArgumentException("Grid resolution must be positive", nameof(gridResolution));

        var bestPosition = center;
        var bestDistance = DistanceToNearestDanger(center, currentTime);

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
    /// 查找多个安全位置（使用场地边界作为搜索范围）
    /// </summary>
    /// <param name="count">需要的安全位置数量</param>
    /// <param name="currentTime">当前时间</param>
    /// <returns>安全位置查询构建器，支持链式调用配置约束</returns>
    /// <exception cref="InvalidOperationException">未设置场地边界时抛出</exception>
    /// <example>
    /// <code>
    /// var points = calculator.FindSafePositions(8, DateTime.Now)
    ///     .NearTarget(bossPos, 20f)
    ///     .MinDistanceBetween(4f)
    ///     .Execute();
    /// </code>
    /// </example>
    public SafePositionQuery FindSafePositions(int count, DateTime currentTime)
    {
        if (arenaBounds == null)
            throw new InvalidOperationException("必须先调用 SetArenaBounds 或 SetCircleArena/SetRectArena 设置场地边界");

        return new SafePositionQuery(this, count, arenaBounds.Center, arenaBounds.ApproximateRadius, currentTime, arenaBounds);
    }

    /// <summary>
    /// 查找多个安全位置（手动指定搜索范围）
    /// </summary>
    /// <param name="count">需要的安全位置数量</param>
    /// <param name="searchCenter">搜索区域中心</param>
    /// <param name="searchRadius">搜索半径（米）</param>
    /// <param name="currentTime">当前时间</param>
    /// <returns>安全位置查询构建器，支持链式调用配置约束</returns>
    public SafePositionQuery FindSafePositions(int count, WPos searchCenter, float searchRadius, DateTime currentTime)
    {
        return new SafePositionQuery(this, count, searchCenter, searchRadius, currentTime);
    }

    internal IReadOnlyList<ForbiddenZone> GetZones() => zones;

    #endregion
}
