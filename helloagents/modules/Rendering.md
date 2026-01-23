# Rendering

## 职责

游戏内渲染功能，主要用于危险区域显示、调试点绘制和可视化。

## 接口定义

### 主要类
| 类名 | 说明 |
|------|------|
| DangerAreaDisplay | 危险区域显示管理 |
| DangerAreaRenderer | 危险区域渲染器，支持临时对象渲染 |

### DangerAreaRenderer 公共 API
| 方法 | 说明 |
|------|------|
| `UpdateObjects(IEnumerable<DisplayObject>)` | 更新持久显示对象 |
| `AddTempObjects(IEnumerable<DisplayObject>)` | 添加临时显示对象（每帧后清空） |
| `ClearTempObjects()` | 清空临时显示对象 |
| `RegisterTempObjectCallback(Func<List<DisplayObject>>)` | 注册临时对象回调（每帧调用） |
| `UnregisterTempObjectCallback(Func<List<DisplayObject>>)` | 注销临时对象回调 |
| `Enabled` | 启用/禁用渲染 |

## 行为规范

### 危险区域显示
**条件**: 副本中存在危险区域
**行为**: 在游戏场景中绘制危险区域标记
**结果**: 玩家可以看到危险区域范围

### 临时对象渲染
**条件**: 外部模块注册了回调或添加了临时对象
**行为**: 每帧调用回调获取临时对象并渲染到覆盖窗口
**结果**: 动态内容（如调试点）显示在游戏画面中

### 渲染更新
**条件**: 每帧渲染循环
**行为**: 更新所有活动区域和临时对象的渲染状态
**结果**: 实时显示当前危险区域和临时对象

## 依赖关系

```yaml
依赖: Utils, Data
被依赖: Plugin, Utils (DebugPoint)
```
