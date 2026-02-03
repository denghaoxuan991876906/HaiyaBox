# AOESafetyCalculator

## 职责

提供 AOE 距离场、危险区域判定与安全位置搜索能力，支持圆形、矩形、扇形、环形等常见 AOE 形状。

## 接口定义

### 核心类型
| 类型 | 说明 |
|------|------|
| WPos | 世界坐标（XZ 平面） |
| WDir | 世界方向向量（XZ 平面） |
| Angle | 角度封装（弧度存储） |

### 距离场
| 类 | 说明 |
|----|------|
| ShapeDistance | 距离场基类（负值=内部，正值=外部） |
| SDCircle/SDRect/SDCone/SDDonut | 基础形状距离场 |
| SDUnion/SDIntersection | 组合形状 |

### 安全区计算
| 类 | 说明 |
|----|------|
| SafeZoneCalculator | 安全性判定与安全点查询 |
| SafePositionQuery | 链式约束查询 |
| ForbiddenZone | 禁止区域定义 |
| ArenaBounds | 场地边界（圆形/矩形） |

### AOE 形状
| 类 | 说明 |
|----|------|
| AOEShapeCircle/Cone/Rect/Donut | 常见 AOE 形状 |
| AOEShapeTriCone/Cross/Capsule/ArcCapsule | 复合 AOE 形状 |
| Distance/InvertedDistance | 基于距离场的签名距离计算 |

## 行为规范

### 安全性判定
**条件**: 调用 `SafeZoneCalculator.IsSafe(position, time)`
**行为**:
  1. 若已设置场地边界，边界外直接判定为不安全
  2. 仅对已激活的禁止区域做包含判定
**结果**: 返回是否安全

### 距离场计算
**条件**: 调用 `AOEShape.Distance/InvertedDistance`
**行为**:
  1. 根据形状类型创建对应的距离场
  2. `InvertForbiddenZone=true` 时切换为反向距离场
**结果**: 返回可用于距离查询的 ShapeDistance 实例

### UI 形状配置
**条件**: DangerAreaTab 添加/删除 AOEShape
**行为**: 以 AOEShape 保存危险区数据并刷新 overlay
**结果**: AOE 形状在游戏画面中可视化

## 依赖关系

```yaml
依赖: (无外部依赖)
被依赖: Rendering, UI, Triggers, Utils (潜在使用)
```
