using System.Runtime.CompilerServices;

namespace AOESafetyCalculator.Core;

/// <summary>
/// 角度结构体 - 对浮点数的封装，以弧度存储角度值
/// 提供类型安全和便捷的角度操作
/// </summary>
/// <remarks>
/// 设计说明：
/// - 内部以弧度（Radians）存储，避免频繁转换
/// - 提供度数（Degrees）属性用于显示和调试
/// - 封装常用的三角函数和角度计算
///
/// 世界坐标系中的角度约定：
/// - 0 度/弧度 表示 "北"/"上" 方向，对应向量 (0, 1)
/// - 角度顺时针增加
/// - 90 度 表示 "东"/"右" 方向，对应向量 (1, 0)
/// - 180 度 表示 "南"/"下" 方向，对应向量 (0, -1)
/// - 270 度 表示 "西"/"左" 方向，对应向量 (-1, 0)
///
/// 常用场景：
/// - 角色朝向
/// - AOE 扇形方向
/// - 技能释放方向
/// - 位置计算中的旋转
/// </remarks>
/// <param name="rad">弧度值</param>
[SkipLocalsInit]
public readonly struct Angle(float rad)
{
    /// <summary>
    /// 角度的弧度值（内部存储）
    /// </summary>
    /// <remarks>
    /// 取值范围：理论上无限制，但通常在 [-π, π] 或 [0, 2π] 范围内
    /// 使用 Normalized() 方法可将角度规范化到 [-π, π] 范围
    /// </remarks>
    public readonly float Rad = rad;

    #region 常量定义

    /// <summary>
    /// 弧度转角度的乘数：180 / π ≈ 57.2958
    /// </summary>
    public const float RadToDeg = 180f / MathF.PI;

    /// <summary>
    /// 角度转弧度的乘数：π / 180 ≈ 0.01745
    /// </summary>
    public const float DegToRad = MathF.PI / 180f;

    /// <summary>
    /// π/2 ≈ 1.5708（90度）
    /// </summary>
    public const float HalfPi = MathF.PI / 2f;

    /// <summary>
    /// 2π ≈ 6.2832（360度，一个完整圆周）
    /// </summary>
    public const float DoublePI = MathF.Tau;

    #endregion

    #region 预定义角度数组

    /// <summary>
    /// 四个斜角方向（东北、东南、西南、西北）
    /// </summary>
    public static readonly Angle[] AnglesIntercardinals = [-45.003f.Degrees(), 44.998f.Degrees(), 134.999f.Degrees(), -135.005f.Degrees()];

    /// <summary>
    /// 四个正方向（东、南、西、北）
    /// </summary>
    public static readonly Angle[] AnglesCardinals = [-90.004f.Degrees(), -0.003f.Degrees(), 180f.Degrees(), 89.999f.Degrees()];

    #endregion

    #region 属性

    /// <summary>
    /// 获取角度的度数表示
    /// </summary>
    public readonly float Deg => Rad * RadToDeg;

    #endregion

    #region 方向转换方法

    /// <summary>
    /// 从方向向量创建角度
    /// </summary>
    public static Angle FromDirection(WDir dir) => new(MathF.Atan2(dir.X, dir.Z));

    /// <summary>
    /// 将角度转换为单位方向向量
    /// </summary>
    public readonly WDir ToDirection()
    {
        var (sin, cos) = ((float, float))Math.SinCos(Rad);
        return new(sin, cos);
    }

    #endregion

    #region 运算符重载

    public static bool operator ==(Angle left, Angle right) => left.Rad == right.Rad;
    public static bool operator !=(Angle left, Angle right) => left.Rad != right.Rad;
    public static Angle operator +(Angle a, Angle b) => new(a.Rad + b.Rad);
    public static Angle operator -(Angle a, Angle b) => new(a.Rad - b.Rad);
    public static Angle operator -(Angle a) => new(-a.Rad);
    public static Angle operator *(Angle a, float b) => new(a.Rad * b);
    public static Angle operator *(float a, Angle b) => new(a * b.Rad);
    public static Angle operator /(Angle a, float b) => new(a.Rad / b);
    public static bool operator >(Angle a, Angle b) => a.Rad > b.Rad;
    public static bool operator <(Angle a, Angle b) => a.Rad < b.Rad;
    public static bool operator >=(Angle a, Angle b) => a.Rad >= b.Rad;
    public static bool operator <=(Angle a, Angle b) => a.Rad <= b.Rad;

    #endregion

    #region 数学运算方法

    public readonly Angle Abs() => new(Math.Abs(Rad));
    public readonly float Sin() => (float)Math.Sin(Rad);
    public readonly float Cos() => (float)Math.Cos(Rad);
    public readonly float Tan() => (float)Math.Tan(Rad);
    public static Angle Atan2(float y, float x) => new(MathF.Atan2(y, x));
    public static Angle Asin(float x) => new((float)Math.Asin(x));
    public static Angle Acos(float x) => new((float)Math.Acos(x));
    public readonly Angle Round(float roundToNearestDeg) => (MathF.Round(Deg / roundToNearestDeg) * roundToNearestDeg).Degrees();

    #endregion

    #region 角度规范化和比较方法

    public readonly Angle Normalized()
    {
        var r = Rad;
        while (r < -MathF.PI)
            r += DoublePI;
        while (r > MathF.PI)
            r -= DoublePI;
        return new(r);
    }

    public readonly bool AlmostEqual(Angle other, float epsRad) => Math.Abs((this - other).Normalized().Rad) <= epsRad;

    public readonly Angle DistanceToAngle(Angle other) => (other - this).Normalized();

    public readonly Angle DistanceToRange(Angle min, Angle max)
    {
        var width = (max - min) * 0.5f;
        var midDist = DistanceToAngle((min + max) * 0.5f);
        return midDist.Rad > width.Rad ? midDist - width : midDist.Rad < -width.Rad ? midDist + width : default;
    }

    public readonly Angle ClosestInRange(Angle min, Angle max)
    {
        var width = (max - min) * 0.5f;
        var midDist = DistanceToAngle((min + max) * 0.5f);
        return midDist.Rad > width.Rad ? min : midDist.Rad < -width.Rad ? max : this;
    }

    #endregion

    #region Object 重写

    public override readonly string ToString() => Deg.ToString("f3", System.Globalization.CultureInfo.InvariantCulture);
    public readonly bool Equals(Angle other) => Rad == other.Rad;
    public override readonly bool Equals(object? obj) => obj is Angle other && Equals(other);
    public override readonly int GetHashCode() => Rad.GetHashCode();

    #endregion
}

/// <summary>
/// 角度扩展方法
/// 提供从数值类型创建 Angle 的便捷方法
/// </summary>
[SkipLocalsInit]
public static class AngleExtensions
{
    public static Angle Radians(this float radians) => new(radians);
    public static Angle Degrees(this float degrees) => new(degrees * Angle.DegToRad);
    public static Angle Degrees(this int degrees) => new(degrees * Angle.DegToRad);
}

/// <summary>
/// 余弦值倒数常量
/// 用于多边形近似圆形时的半径修正
/// </summary>
[SkipLocalsInit]
public static class CosPI
{
    public const float Pi8th = 1.082392f;
    public const float Pi28th = 1.006328f;
    public const float Pi32th = 1.004839f;
    public const float Pi36th = 1.00382f;
    public const float Pi40th = 1.0030922f;
    public const float Pi48th = 1.0021457f;
    public const float Pi60th = 1.0013723f;
    public const float Pi64th = 1.001206f;
    public const float Pi148th = 1.000225f;
}
