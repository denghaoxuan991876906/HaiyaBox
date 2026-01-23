# 任务清单: debugpoint-renderer-refactor

> **@status:** completed | 2026-01-23 17:15

目录: `helloagents/plan/202601231710_debugpoint-renderer-refactor/`

---

## 任务状态符号说明

| 符号 | 状态 | 说明 |
|------|------|------|
| `[ ]` | pending | 待执行 |
| `[√]` | completed | 已完成 |
| `[X]` | failed | 执行失败 |
| `[-]` | skipped | 已跳过 |
| `[?]` | uncertain | 待确认 |

---

## 执行状态
```yaml
总任务: 6
已完成: 5
完成率: 83%
```

---

## 任务列表

### 1. Rendering 模块扩展

- [√] 1.1 在 `HaiyaBox/Rendering/DangerAreaRenderer.cs` 中添加临时对象支持
  - 添加了 `_tempObjectCallbacks` 回调列表
  - 添加了 `RegisterTempObjectCallback()` 和 `UnregisterTempObjectCallback()` 方法
  - 修改 `Draw()` 方法，每帧调用回调获取临时对象并渲染
  - 验证: 编译通过，现有危险区域显示功能正常

### 2. DebugPoint 重构

- [√] 2.1 在 `HaiyaBox/Utils/DebugPoint.cs` 中移除旧的渲染方式
  - 移除了 `_subscribed` 字段
  - 移除了 `Initialize()` 和 `Dispose()` 中的 Draw 事件订阅
  - 移除了独立的 `Render()` 私有方法
  - 依赖: 无

- [√] 2.2 实现 `GetDisplayObjects()` 方法
  - 创建了 `private static List<DisplayObject> GetDisplayObjects()`
  - 将 Point 列表转换为 DisplayObjectLine 和 DisplayObjectDot
  - 将 DebugPointWithText 字典转换为 DisplayObjectDot 和 DisplayObjectText
  - 依赖: 2.1

- [√] 2.3 修改初始化逻辑
  - 修改 `Initialize(DangerAreaRenderer)` 方法接受渲染器参数
  - 内部注册 `GetDisplayObjects` 回调到 DangerAreaRenderer
  - `Dispose()` 方法中注销回调
  - 依赖: 1.1, 2.2

### 3. 集成与测试

- [√] 3.1 修改插件入口代码
  - 在 `DangerAreaTab` 中添加 `Renderer` 公共属性暴露渲染器
  - 修改 `AutoRaidHelper.OnLoad()` 调用 `DebugPoint.Initialize(_dangerAreaTab.Renderer)`
  - 验证: 插件正常启动

- [?] 3.2 功能验证
  - 在游戏中添加调试点，验证能正常显示
  - 验证红色路径和序号显示
  - 验证带标签的调试点显示（绿色圆点+黄色文本）
  - 验证 DebugPoint.Clear() 能正确清空
  - 验证: 游戏画面中调试点正确显示，与危险区域显示一致

---

## 执行备注

> 执行过程中的重要记录

| 任务 | 状态 | 备注 |
|------|------|------|
| 3.2 | [?] | 需要在游戏中验证，已实现代码变更 |
