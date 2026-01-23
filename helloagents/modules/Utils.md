# Utils

## 职责

通用工具类，提供各种辅助功能，包括危险区域计算、调试点绘制、事件记录等。

## 接口定义

### 主要类
| 类名 | 说明 |
|------|------|
| DangerArea | 危险区域工具类 |
| DebugPoint | 调试点工具（使用 DangerAreaRenderer 渲染） |
| EventRecordManager | 事件记录管理器 |
| Utilities | 通用工具方法 |
| VIPHelper | VIP 辅助工具 |
| XszRemote | XSZ 远程控制 |
| RemoteControl | 远程控制工具 |
| GeometryUtilsXZ | 几何工具类 |
| MechanismState | 机制状态管理 |

### DebugPoint 公共 API
| 方法 | 说明 |
|---------|------|
| `Initialize(DangerAreaRenderer)` | 初始化，注册渲染回调 |
| `Dispose()` | 清理，注销渲染回调 |
| `Add(Vector3)` | 添加位置点 |
| `Clear()` | 清空所有调试点 |
| `Point` | 位置点列表 |
| `DebugPointWithText` | 带标签的调试点字典 |

### 公共 API
| 类/方法 | 说明 |
|---------|------|
| Utilities.{方法} | 各种通用辅助方法 |
| GeometryUtilsXZ.{方法} | 几何计算相关 |
| EventRecordManager.Record() | 记录事件 |

## 行为规范

### 调试点绘制
**条件**: 用户调用 `DebugPoint.Add()` 添加位置点
**行为**:
  1. 将位置点存储到内部列表
  2. 在渲染时通过回调转换为 DisplayObject
  3. 提交给 DangerAreaRenderer 绘制
**结果**: 游戏画面中显示红色路径和带序号的圆点

### 带标签的调试点
**条件**: 用户向 `DebugPointWithText` 字典添加带标签的位置
**行为**:
  1. 存储标签和位置
  2. 在渲染时转换为 DisplayObjectDot 和 DisplayObjectText
  3. 提交给 DangerAreaRenderer 绘制
**结果**: 游戏画面中显示绿色圆点和黄色文本标签

### 工具方法调用
**条件**: 需要特定功能
**行为**: 调用对应的工具方法
**结果**: 返回计算结果或执行操作

## 依赖关系

```yaml
依赖: Rendering (DebugPoint 需要 DangerAreaRenderer)
被依赖: Plugin, UI, Triggers, Data
```
