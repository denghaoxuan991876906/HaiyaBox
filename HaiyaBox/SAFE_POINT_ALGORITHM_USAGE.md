# 安全点计算算法使用文档

## 概述

HaiyaBox 提供了一个强大的安全点计算算法，用于在有危险区域的场景中自动计算出安全的站位点。算法支持：
- 圆形和矩形危险区域
- 矩形或圆形限制范围
- 近战组和远程组分配
- 危险区域持续时间管理
- 高性能网格采样算法

## 核心类

### 1. `SafePointCalculator`
主要计算类，位于 `HaiyaBox.Utils` 命名空间。

### 2. 危险区域类
- `DangerArea` (基类)
- `CircleDangerArea` (圆形危险区)
- `RectangleDangerArea` (矩形危险区)

### 3. `Point` 类
二维坐标点，用于 XZ 平面计算。

## 完整使用示例

### 示例 1：基础使用 - 矩形限制范围

```csharp
using HaiyaBox.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;

// 1. 创建计算器实例
var calculator = new SafePointCalculator();

// 2. 定义参考点（通常是BOSS位置或场地中心）
var referencePoint = new Point(100, 100); // XZ坐标

// 3. 创建危险区域列表
var dangerAreas = new List<DangerArea>();

// 添加圆形危险区域（例如：BOSS的AOE技能）
dangerAreas.Add(new CircleDangerArea
{
    Center = new Point(105, 105),
    Radius = 8.0,
    Duration = 15.0,  // 持续15秒
    CreatedTime = DateTime.Now
});

// 添加矩形危险区域（例如：直线AOE）
dangerAreas.Add(new RectangleDangerArea
{
    Center = new Point(95, 100),
    Width = 5.0,
    Height = 20.0,
    Rotation = 45.0,  // 旋转45度
    Duration = 10.0,  // 持续10秒
    CreatedTime = DateTime.Now
});

// 4. 设置矩形限制范围（例如：战斗场地边界）
var rectCenter = new Point(100, 100);
var rectLength = 40.0; // X方向长度
var rectWidth = 40.0;  // Z方向宽度
var rectParams = new Tuple<Point, double, double>(rectCenter, rectLength, rectWidth);

// 5. 调用计算方法
try
{
    List<Point> safePoints = calculator.FindSafePoints(
        limitType: LimitRangeType.Rectangle,
        rectLimitParams: rectParams,
        circleLimitParams: null,  // 使用矩形范围时为null
        dangerAreas: dangerAreas,
        referencePoint: referencePoint,
        minSafePointDistance: 3.0,    // 安全点之间的最小间距
        closeToRefCount: 3,           // 近战组数量（贴近参考点）
        maxFarDistance: 25.0,         // 远程组的最大距离
        sampleStep: 0.5,              // 采样步长（越小越精确但越慢）
        totalSafePointCount: 8        // 需要的总安全点数量
    );

    // 6. 使用结果
    Console.WriteLine($"成功计算出 {safePoints.Count} 个安全点");

    // 前 closeToRefCount 个点是近战组
    for (int i = 0; i < 3 && i < safePoints.Count; i++)
    {
        Console.WriteLine($"近战点 {i + 1}: ({safePoints[i].X:F2}, {safePoints[i].Y:F2})");
    }

    // 剩余的点是远程组
    for (int i = 3; i < safePoints.Count; i++)
    {
        Console.WriteLine($"远程点 {i - 2}: ({safePoints[i].X:F2}, {safePoints[i].Y:F2})");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"计算失败: {ex.Message}");
}
```

### 示例 2：使用圆形限制范围

```csharp
using HaiyaBox.Utils;
using System;
using System.Collections.Generic;

var calculator = new SafePointCalculator();
var referencePoint = new Point(100, 100);

// 创建危险区域
var dangerAreas = new List<DangerArea>
{
    new CircleDangerArea
    {
        Center = new Point(100, 100),
        Radius = 5.0,
        Duration = 0  // 0表示永久存在
    }
};

// 设置圆形限制范围
var circleCenter = new Point(100, 100);
var circleRadius = 20.0;
var circleParams = new Tuple<Point, double>(circleCenter, circleRadius);

// 计算安全点
List<Point> safePoints = calculator.FindSafePoints(
    limitType: LimitRangeType.Circle,
    rectLimitParams: null,  // 使用圆形范围时为null
    circleLimitParams: circleParams,
    dangerAreas: dangerAreas,
    referencePoint: referencePoint,
    minSafePointDistance: 3.0,
    closeToRefCount: 4,
    maxFarDistance: 18.0,
    sampleStep: 0.5,
    totalSafePointCount: 8
);
```

### 示例 3：与 Vector3 坐标转换

游戏中通常使用 Vector3 (X, Y, Z) 坐标，算法使用 Point (X, Y) 代表 XZ 平面：

```csharp
using System.Numerics;
using HaiyaBox.Utils;

// Vector3 转 Point（忽略Y轴高度）
Vector3 bossPosition = new Vector3(100.5f, 10.0f, 95.3f);
Point referencePoint = Point.FromVector3(bossPosition);
// 结果: Point(100.5, 95.3)  // X, Z

// Point 转 Vector3（Y轴设为0）
Point safePoint = new Point(105.2, 98.7);
Vector3 worldPosition = Point.ToVector3(safePoint);
// 结果: Vector3(105.2, 0, 98.7)  // X, 0, Z

// 如果需要保留高度
Vector3 worldPositionWithHeight = Point.ToVector3(safePoint);
worldPositionWithHeight.Y = bossPosition.Y;  // 使用BOSS的高度
```

### 示例 4：危险区域持续时间管理

```csharp
using HaiyaBox.Utils;
using System;
using System.Collections.Generic;

var dangerAreas = new List<DangerArea>();

// 添加一个持续10秒的危险区域
var tempDanger = new CircleDangerArea
{
    Center = new Point(100, 100),
    Radius = 10.0,
    Duration = 10.0,
    CreatedTime = DateTime.Now
};
dangerAreas.Add(tempDanger);

// 检查是否过期
if (tempDanger.IsExpired())
{
    Console.WriteLine("危险区域已过期");
}

// 获取剩余时间
double remaining = tempDanger.GetRemainingTime();
Console.WriteLine($"剩余时间: {remaining:F1} 秒");

// 自动清理过期的危险区域
int removedCount = dangerAreas.RemoveAll(area => area.IsExpired());
Console.WriteLine($"清理了 {removedCount} 个过期的危险区域");
```

### 示例 5：实际战斗应用（在触发器中使用）

```csharp
using AEAssist;
using AEAssist.CombatRoutine.Module.Target;
using HaiyaBox.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

// 在AEAssist触发器的Action中使用
public class CalculateSafePositionAction : ITriggerAction
{
    public string DisplayName => "计算安全位置";
    public string Remark => "根据场上危险区域计算安全点";

    public async Task Execute(ITriggerContext context)
    {
        // 1. 获取BOSS位置作为参考点
        var boss = TargetMgr.Instance.Enemys.Values
            .FirstOrDefault(e => e.IsBoss() && e.IsTargetable);

        if (boss == null) return;

        var referencePoint = Point.FromVector3(boss.Position);

        // 2. 创建危险区域（从游戏事件中获取）
        var dangerAreas = new List<DangerArea>();

        // 假设从某个事件记录中获取到危险区域信息
        foreach (var aoeEvent in GetAOEEvents())
        {
            dangerAreas.Add(new CircleDangerArea
            {
                Center = Point.FromVector3(aoeEvent.Position),
                Radius = aoeEvent.Radius,
                Duration = aoeEvent.Duration,
                CreatedTime = DateTime.Now
            });
        }

        // 3. 设置场地限制（假设是矩形场地）
        var fieldCenter = new Point(100, 100);
        var rectParams = new Tuple<Point, double, double>(fieldCenter, 40.0, 40.0);

        // 4. 计算安全点
        var calculator = new SafePointCalculator();
        var safePoints = calculator.FindSafePoints(
            limitType: LimitRangeType.Rectangle,
            rectLimitParams: rectParams,
            dangerAreas: dangerAreas,
            referencePoint: referencePoint,
            minSafePointDistance: 3.0,
            closeToRefCount: 2,      // 坦克和近战DPS
            maxFarDistance: 25.0,
            sampleStep: 0.5,
            totalSafePointCount: 8
        );

        // 5. 根据职业分配位置
        var player = Svc.ClientState.LocalPlayer;
        if (player != null)
        {
            Point targetPoint;

            // 近战职业使用近战点（前2个）
            if (IsMelee(player))
            {
                targetPoint = safePoints[0]; // 或根据某种规则选择
            }
            else // 远程职业使用远程点（后6个）
            {
                targetPoint = safePoints[3]; // 或根据某种规则选择
            }

            // 6. 移动到安全点
            Vector3 worldPos = Point.ToVector3(targetPoint);
            worldPos.Y = player.Position.Y;
            await MoveTo(worldPos);
        }
    }

    private bool IsMelee(Dalamud.Game.ClientState.Objects.Types.GameObject player)
    {
        // 实现职业判断逻辑
        return true;
    }

    private async Task MoveTo(Vector3 position)
    {
        // 实现移动逻辑
    }

    private IEnumerable<AOEEvent> GetAOEEvents()
    {
        // 从事件记录系统获取AOE信息
        yield break;
    }
}

public class AOEEvent
{
    public Vector3 Position { get; set; }
    public float Radius { get; set; }
    public double Duration { get; set; }
}
```

## 参数说明

### FindSafePoints 方法参数详解

| 参数名 | 类型 | 说明 | 推荐值 |
|--------|------|------|--------|
| `limitType` | `LimitRangeType` | 限制范围类型 (Rectangle/Circle) | 根据场地形状选择 |
| `rectLimitParams` | `Tuple<Point, double, double>` | 矩形参数：(中心点, 长度, 宽度) | 根据实际场地大小 |
| `circleLimitParams` | `Tuple<Point, double>` | 圆形参数：(圆心, 半径) | 根据实际场地大小 |
| `dangerAreas` | `List<DangerArea>` | 危险区域列表 | 从游戏事件收集 |
| `referencePoint` | `Point` | 参考点（通常是BOSS位置） | BOSS当前位置 |
| `minSafePointDistance` | `double` | 安全点之间的最小间距 | 3.0 (角色碰撞体积) |
| `closeToRefCount` | `int` | 近战组数量 | 2-4 (根据队伍配置) |
| `maxFarDistance` | `double` | 远程组距参考点的最大距离 | 15.0-25.0 |
| `sampleStep` | `double` | 采样步长，越小越精确但计算越慢 | 0.5 (平衡性能和精度) |
| `totalSafePointCount` | `int` | 需要计算的总安全点数量 | 8 (标准8人队) |

## 性能优化建议

### 1. 采样步长选择
- **精确场景** (`sampleStep = 0.1-0.3`): 危险区域密集，需要精确计算
  - 耗时：20-100ms
  - 适用：高难度副本的复杂机制

- **平衡场景** (`sampleStep = 0.5`): 默认推荐值
  - 耗时：5-20ms
  - 适用：大多数场景

- **快速场景** (`sampleStep = 1.0-2.0`): 危险区域稀疏，要求快速响应
  - 耗时：1-5ms
  - 适用：简单机制或实时反应场景

### 2. 限制范围优化
- 尽量设置合理的限制范围，不要过大
- 使用圆形限制范围比矩形略快（少一次旋转变换）

### 3. 危险区域管理
- 及时清理过期的危险区域：`dangerAreas.RemoveAll(a => a.IsExpired())`
- 避免添加过多不必要的危险区域

## 算法特性

### 1. 最小间距保证
算法使用 `> minSafePointDistance + epsilon` (epsilon=0.01) 判断，确保点之间的距离**严格大于**最小间距，避免重叠。

### 2. 近战/远程分组
- 前 `closeToRefCount` 个点：按距离参考点从近到远排序，优先贴近BOSS
- 后续点：在 `maxFarDistance` 范围内自由分布

### 3. 边界约束
所有安全点都保证在限制范围内，且不在任何危险区域中。

### 4. 容错处理
如果找不到足够的安全点，会返回所有可找到的点，并在控制台输出警告。

## 常见问题

### Q1: 为什么计算不出8个点？
**A:** 可能的原因：
- `minSafePointDistance` 设置过大
- 危险区域占据了过多空间
- 限制范围设置过小
- `sampleStep` 过大导致漏掉潜在的安全点

**解决方案**：
- 减小 `minSafePointDistance` (如从3.0改为2.5)
- 增大 `maxFarDistance`
- 减小 `sampleStep` (如从0.5改为0.3)

### Q2: 计算耗时过长（>100ms）
**A:** 优化方法：
- 增大 `sampleStep` (如从0.5改为1.0)
- 减小限制范围大小
- 清理过期或不必要的危险区域

### Q3: 如何处理不规则场地？
**A:**
- 使用多个矩形/圆形危险区域"挖空"不可站立区域
- 或者使用圆形限制范围近似

### Q4: 持续时间为0的危险区域会被清理吗？
**A:** 不会。`Duration = 0` 表示永久存在，`IsExpired()` 会返回 `false`。

## 线程安全说明

`SafePointCalculator` 本身是无状态的，可以安全地在多线程中使用。但是：
- `DangerArea` 的 `IsExpired()` 和 `GetRemainingTime()` 依赖 `DateTime.Now`，是线程安全的
- 如果多个线程共享同一个 `dangerAreas` 列表，需要外部同步

## 更新日志

### v1.2 (当前版本)
- ✨ 新增危险区域持续时间功能
- ✨ 新增自动清理过期危险区域
- 🐛 修复最小间距判断的浮点数精度问题
- 🐛 修复近战组可能重叠的问题
- ⚡ 优化算法性能

### v1.1
- ✨ 新增圆形限制范围支持
- ✨ 新增边界可视化
- ✨ 新增计算时间显示

### v1.0
- 🎉 初始版本发布

## 联系与反馈

如有问题或建议，请在项目仓库提交 Issue。
