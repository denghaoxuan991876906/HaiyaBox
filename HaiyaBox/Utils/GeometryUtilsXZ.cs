using System.Numerics;

namespace HaiyaBox.Utils;

public static class GeometryUtilsXZ
{
    public static int PositionTo8Dir(Vector3 point, Vector3 centre)
    {
        // Dirs: N = 0, NE = 1, ..., NW = 7
        var r = Math.Round(4 - 4 * Math.Atan2(point.X - centre.X, point.Z - centre.Z) / Math.PI) % 8;
        return (int)r;
    }
    public static Vector3 ExtendPoint(Vector3 centerPoint, Vector3 referencePoint, float distanceToExtend)
    {
        // 将Vector3转换为Vector2进行平面计算
        // 转换规则: Vector3.x = Vector2.x, Vector3.z = Vector2.y
        Vector2 center2D = new Vector2(centerPoint.X, centerPoint.Z);
        Vector2 reference2D = new Vector2(referencePoint.X, referencePoint.Z);
        
        // 计算从中心点到参考点的方向向量
        Vector2 direction = reference2D - center2D;
        
        // 计算方向向量的单位向量（避免零向量情况）
        Vector2 unitDirection;
        if (direction.LengthSquared() < float.Epsilon)
        {
            // 如果参考点与中心点重合，默认沿X轴方向
            unitDirection =new Vector2(1, 0);
        }
        else
        {
            unitDirection = Normalized(direction);
        }
        
        // 计算延长后的2D点
        Vector2 extended2D = reference2D + unitDirection * distanceToExtend;
        
        // 将计算结果转换回Vector3，保留原始Y值
        return new Vector3(extended2D.X, referencePoint.Y, extended2D.Y);
    }
    // 标准化向量
    public static Vector2 Normalized( Vector2 vector)
    {
        float magnitude = (float)Math.Sqrt(vector.LengthSquared());
        if (magnitude < float.Epsilon)
            return new Vector2(0, 0);
            
        return new Vector2(vector.X / magnitude, vector.Y / magnitude);
    }
    /// <summary>
    /// 计算目标前向点
    /// </summary>
    /// <param name="forwardOrigin"></param>
    /// <param name="rot"></param>
    /// <param name="distance"></param>
    /// <returns></returns>
    public static Vector3 Forward(Vector3 forwardOrigin, float rot, float distance)
    {
        float sinRot = MathF.Sin(rot);
        float cosRot = MathF.Cos(rot);
        return new Vector3(
            forwardOrigin.X + distance * sinRot,
            forwardOrigin.Y,
            forwardOrigin.Z + distance * cosRot
        );
    }

    /// <summary>
    /// 向量绕指定点旋转（逆时针为正方向）
    /// </summary>
    /// <param name="originalVector">原始向量（或点坐标）</param>
    /// <param name="rotationCenter">旋转中心</param>
    /// <param name="degrees">旋转角度（度）</param>
    /// <returns>旋转后的向量</returns>
    public static Vector3 RotateAroundPoint(Vector3 originalVector3, Vector3 rotationCenter3, float degrees)
    {
        var originalVector = new Vector2(originalVector3.X, originalVector3.Z);
        var rotationCenter = new Vector2(rotationCenter3.X, rotationCenter3.Z);
        // 1. 角度转换：度 → 弧度（Math.Cos/Sin接收弧度参数）
        float radians = degrees * (float)(Math.PI / 180);
        float cos = (float)Math.Cos(radians);
        float sin = (float)Math.Sin(radians);

        // 2. 平移向量到原点（减去旋转中心）
        float offsetX = originalVector.X - rotationCenter.X;
        float offsetY = originalVector.Y - rotationCenter.Y;

        // 3. 应用旋转矩阵（绕原点旋转）
        float rotatedOffsetX = offsetX * cos - offsetY * sin;
        float rotatedOffsetY = offsetX * sin + offsetY * cos;

        // 4. 平移回原坐标系（加上旋转中心）
        return new Vector3(
            rotatedOffsetX + rotationCenter.X,
            0,
            rotatedOffsetY + rotationCenter.Y
        );
    }

    /// <summary>
    /// 重载：支持顺时针旋转
    /// </summary>
    public static Vector3 RotateAroundPointClockwise(Vector3 originalVector, Vector3 rotationCenter, float degrees)
    {
        return RotateAroundPoint(originalVector, rotationCenter, -degrees); // 角度取负
    }

    /// <summary>
    /// 在 XZ 平面计算两点的 2D 距离，忽略 Y。
    /// </summary>
    /// <param name="a">第一个点。</param>
    /// <param name="b">第二个点。</param>
    /// <returns>点 a 与点 b 在 XZ 平面上的距离。</returns>
    public static float DistanceXZ(Vector3 a, Vector3 b)
    {
        var dx = b.X - a.X;
        var dz = b.Z - a.Z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    /// <summary>
    /// 在 XZ 平面计算：以 basePos 为圆心，向量 (basePos→v1) 与 (basePos→v2) 的夹角（范围 0~180°）。
    /// </summary>
    /// <param name="v1">构成第一个向量的目标点。</param>
    /// <param name="v2">构成第二个向量的目标点。</param>
    /// <param name="basePos">作为圆心的基准点。</param>
    /// <returns>两个向量之间的夹角（单位为度）。</returns>
    public static float AngleXZ(Vector3 v1, Vector3 v2, Vector3 basePos)
    {
        var a = new Vector3(v1.X - basePos.X, 0, v1.Z - basePos.Z);
        var b = new Vector3(v2.X - basePos.X, 0, v2.Z - basePos.Z);

        var dot = Vector3.Dot(a, b);
        var magA = a.Length();
        var magB = b.Length();

        if (magA < 1e-6f || magB < 1e-6f)
            return 0f;

        var cosTheta = dot / (magA * magB);
        cosTheta = Math.Clamp(cosTheta, -1f, 1f);

        var rad = MathF.Acos(cosTheta);
        return rad * (180f / MathF.PI);
    }

    /// <summary>
    /// 弦长、角度（°）、半径之间的互算。
    /// 当一个参数为 null 时，根据其余两个进行计算。
    /// </summary>
    /// <param name="chord">弦长，若为 null 则将计算弦长。</param>
    /// <param name="angleDeg">角度（单位：度），若为 null 则将计算角度。</param>
    /// <param name="radius">半径，若为 null 则将计算半径。</param>
    /// <returns>
    /// 一个元组，其中 Item1 为计算得到的数值（可能为 null），Item2 为描述字符串。
    /// </returns>
    public static (float? value, string desc) ChordAngleRadius(float? chord, float? angleDeg, float? radius)
    {
        // 1) angle+radius => chord
        if (chord == null && angleDeg != null && radius != null)
        {
            var angleRad = angleDeg.Value * MathF.PI / 180f;
            var c = 2f * radius.Value * MathF.Sin(angleRad / 2f);
            return (c, "弦长");
        }

        // 2) chord+radius => angle
        if (angleDeg == null && chord != null && radius != null)
        {
            var x = chord.Value / (2f * radius.Value);
            x = Math.Clamp(x, -1f, 1f);
            var aRad = 2f * MathF.Asin(x);
            var aDeg = aRad * (180f / MathF.PI);
            return (aDeg, "角度(°)");
        }

        // 3) chord+angle => radius
        if (radius == null && chord != null && angleDeg != null)
        {
            var angleRad = angleDeg.Value * MathF.PI / 180f;
            var denominator = 2f * MathF.Sin(angleRad / 2f);
            if (Math.Abs(denominator) < 1e-6f)
                return (null, "角度过小,无法计算半径");
            var r = chord.Value / denominator;
            return (r, "半径");
        }

        return (null, "请只留一个值为空，其余两个有值");
    }

    /// <summary>
    /// 计算给定点相对于参考方向的偏移量。
    /// 参考方向由“场地中心”到“顶点”（apexPos）确定，
    /// 前进方向为从场地中心指向 apexPos，右侧方向为前进方向顺时针旋转 90°。
    /// </summary>
    /// <param name="point">目标点。</param>
    /// <param name="apexPos">参考顶点位置（通常为通过 Alt 键记录的点）。</param>
    /// <param name="stageCenter">场地中心坐标。</param>
    /// <returns>
    /// 一个元组，其中 Item1 为右侧偏移值，Item2 为前进偏移值。
    /// 正值表示点沿对应方向的正向偏移。
    /// </returns>
    public static (float offsetX, float offsetZ) CalculateOffsetFromReference(Vector3 point, Vector3 apexPos,
        Vector3 stageCenter)
    {
        // 计算前进方向：从场地中心指向 apexPos（忽略 Y 轴）。
        Vector3 dir = apexPos - stageCenter;
        dir.Y = 0;

        if (dir.LengthSquared() < 1e-6f)
        {
            float fallbackX = point.X - apexPos.X;
            float fallbackZ = point.Z - apexPos.Z;
            return (fallbackX, fallbackZ);
        }

        dir = Vector3.Normalize(dir);

        // 计算右侧方向：将前进方向顺时针旋转 90°。
        Vector3 perpendicular = new Vector3(-dir.Z, 0, dir.X);

        // 计算从场地中心到目标点的向量（忽略 Y 轴）。
        Vector3 diff = point - stageCenter;
        diff.Y = 0;

        float offsetZ = Vector3.Dot(diff, dir);
        float offsetX = Vector3.Dot(diff, perpendicular);

        return (offsetX, offsetZ);
    }

    /// <summary>
    /// 计算全圆均匀分布的坐标。
    /// x 轴正方向为正东，z 轴正方向为正南，y 坐标固定为 center 的 y 值。
    /// </summary>
    /// <param name="center">场地中心坐标。</param>
    /// <param name="radius">离中心点的距离。</param>
    /// <param name="firstOffsetAngle">第一人的偏移角度（相对于正北，顺时针）。</param>
    /// <param name="count">人数，小于等于 0 时返回空列表。</param>
    /// <param name="clockwise">分布方向：true 表示顺时针，false 表示逆时针。</param>
    /// <returns>均匀分布在圆周上的各点坐标列表。</returns>
    public static List<Vector3> ComputeFullCirclePositions(Vector3 center, float radius, float firstOffsetAngle,
        int count, bool clockwise)
    {
        var list = new List<Vector3>();
        if (count <= 0)
            return list;
        float stepDeg = 360f / count;
        for (int i = 0; i < count; i++)
        {
            float currentAngle = clockwise ? firstOffsetAngle + i * stepDeg : firstOffsetAngle - i * stepDeg;
            list.Add(CalcPosition(center, radius, currentAngle));
        }

        return list;
    }

    /// <summary>
    /// 根据给定的直线间距计算弧线上分布的坐标，
    /// 保证相邻两人的直线距离（弦长）等于 spacing。
    /// x 轴正方向为正东，z 轴正方向为正南，y 坐标固定为 center 的 y 值。
    /// </summary>
    /// <param name="center">场地中心坐标。</param>
    /// <param name="radius">离中心点的距离。</param>
    /// <param name="firstOffsetAngle">第一人的偏移角度（相对于正北，顺时针）。</param>
    /// <param name="count">人数，小于等于 0 或 radius 小于等于 0 时返回空列表。</param>
    /// <param name="clockwise">分布方向：true 表示顺时针，false 表示逆时针。</param>
    /// <param name="spacing">相邻两人之间的直线距离（弦长，与 radius 单位一致）。</param>
    /// <returns>沿弧线且保证相邻两人直线距离等于 spacing 的各点坐标列表。</returns>
    public static List<Vector3> ComputeArcPositionsByChordSpacing(Vector3 center, float radius, float firstOffsetAngle,
        int count, bool clockwise, float spacing)
    {
        var list = new List<Vector3>();
        if (count <= 0 || radius <= 0)
            return list;
        if (spacing > 2 * radius)
            return list;
        float deltaRad = 2f * MathF.Asin(spacing / (2f * radius));
        float stepDeg = deltaRad * (180f / MathF.PI);
        for (int i = 0; i < count; i++)
        {
            float currentAngle = clockwise ? firstOffsetAngle + i * stepDeg : firstOffsetAngle - i * stepDeg;
            list.Add(CalcPosition(center, radius, currentAngle));
        }

        return list;
    }

    /// <summary>
    /// 计算固定角度分布的坐标，每个点与相邻点和圆心所形成的角度均固定。
    /// x 轴正方向为正东，z 轴正方向为正南，y 坐标固定为 center 的 y 值。
    /// </summary>
    /// <param name="center">场地中心坐标。</param>
    /// <param name="radius">距离中心的半径。</param>
    /// <param name="firstOffsetAngle">第一人的偏移角度（相对于正北，顺时针）。</param>
    /// <param name="count">人数（小于等于 0 时返回空列表）。</param>
    /// <param name="clockwise">分布方向，true 表示顺时针，false 表示逆时针。</param>
    /// <param name="fixedAngle">相邻两人与中心连线之间的固定角度（单位：度）。</param>
    /// <returns>按照固定角度分布计算得到的坐标列表。</returns>
    public static List<Vector3> ComputePositionsByFixedAngle(Vector3 center, float radius, float firstOffsetAngle,
        int count, bool clockwise, float fixedAngle)
    {
        var list = new List<Vector3>();
        if (count <= 0)
            return list;

        for (int i = 0; i < count; i++)
        {
            float currentAngle = clockwise ? firstOffsetAngle + i * fixedAngle : firstOffsetAngle - i * fixedAngle;
            list.Add(CalcPosition(center, radius, currentAngle));
        }

        return list;
    }

    /// <summary>
    /// 根据给定的总计角度将点均匀分布在弧线上。
    /// x 轴正方向为正东，z 轴正方向为正南，y 坐标固定为 center 的 y 值。
    /// </summary>
    /// <param name="center">场地中心坐标。</param>
    /// <param name="radius">离中心点的距离。</param>
    /// <param name="firstOffsetAngle">第一人的偏移角度（相对于正北，顺时针）。</param>
    /// <param name="count">人数，小于等于 0 时返回空列表；若 count 为 1，则返回单个点。</param>
    /// <param name="clockwise">分布方向：true 表示顺时针，false 表示逆时针。</param>
    /// <param name="totalAngle">第一人与最后人各自与 center 连线所形成的夹角（单位：度）。</param>
    /// <returns>均匀分布在该弧线上的各点坐标列表。</returns>
    public static List<Vector3> ComputeArcPositionsByTotalAngle(Vector3 center, float radius, float firstOffsetAngle,
        int count, bool clockwise, float totalAngle)
    {
        var list = new List<Vector3>();
        if (count <= 0)
            return list;
        if (count == 1)
        {
            list.Add(CalcPosition(center, radius, firstOffsetAngle));
        }
        else
        {
            float stepDeg = totalAngle / (count - 1);
            for (int i = 0; i < count; i++)
            {
                float currentAngle = clockwise ? firstOffsetAngle + i * stepDeg : firstOffsetAngle - i * stepDeg;
                list.Add(CalcPosition(center, radius, currentAngle));
            }
        }

        return list;
    }

    /// <summary>
    /// 根据中心点、半径和给定角度计算圆周上的点坐标。
    /// 角度以度为单位，相对于正北（z 轴负方向），顺时针方向为正。
    /// </summary>
    /// <param name="center">圆心坐标；</param>
    /// <param name="radius">半径；</param>
    /// <param name="angleDeg">角度（度），相对于正北方向；</param>
    /// <returns>计算得到的点坐标，y 值固定为 center.Y。</returns>
    private static Vector3 CalcPosition(Vector3 center, float radius, float angleDeg)
    {
        float rad = angleDeg * ((float)Math.PI / 180f);
        float x = center.X + radius * MathF.Sin(rad);
        float z = center.Z - radius * MathF.Cos(rad);
        return new Vector3(x, center.Y, z);
    }
}