using System.Runtime.CompilerServices;
using AOESafetyCalculator.DistanceField;

namespace AOESafetyCalculator.SafetyZone;

/// <summary>
/// 禁止区域（Forbidden Zone）
/// </summary>
/// <remarks>
/// 表示一个危险区域，包含形状和激活时间信息
/// 用于安全区域计算和路径规划
/// </remarks>
[SkipLocalsInit]
public sealed record class ForbiddenZone
{
    /// <summary>
    /// 禁止区域名称（用于唯一标识和按名清除）
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 禁止区域的形状距离场
    /// </summary>
    /// <remarks>
    /// 使用距离场表示区域边界：
    /// - 距离 ≤ 0：在禁止区域内（危险）
    /// - 距离 > 0：在禁止区域外（安全）
    /// </remarks>
    public required ShapeDistance Shape { get; init; }

    /// <summary>
    /// 禁止区域的激活时间
    /// </summary>
    /// <remarks>
    /// - DateTime.MinValue：立即激活
    /// - 其他值：在指定时间激活
    /// 用于预测未来的危险区域
    /// </remarks>
    public DateTime Activation { get; init; } = DateTime.MinValue;

    /// <summary>
    /// 检查指定位置是否在禁止区域内
    /// </summary>
    /// <param name="position">要检查的位置</param>
    /// <returns>true 表示在禁止区域内，false 表示在区域外</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(Core.WPos position) => Shape.Contains(position);

    /// <summary>
    /// 计算指定位置到禁止区域边界的距离
    /// </summary>
    /// <param name="position">要计算距离的位置</param>
    /// <returns>
    /// 距离值：
    /// - 负值：在禁止区域内
    /// - 正值：在禁止区域外
    /// - 零：在边界上
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Distance(Core.WPos position) => Shape.Distance(position);

    /// <summary>
    /// 检查禁止区域是否已激活
    /// </summary>
    /// <param name="currentTime">当前时间</param>
    /// <returns>true 表示已激活，false 表示未激活</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsActive(DateTime currentTime) => currentTime >= Activation;
}
