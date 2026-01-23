# 变更日志

## [Unreleased]

## [0.2.0] - 2026-01-23

### 新增
- **[Rendering]**: DangerAreaRenderer 支持临时对象回调渲染
  - 方案: [202601231710_debugpoint-renderer-refactor](archive/2026-01/202601231710_debugpoint-renderer-refactor/)
  - 决策: debugpoint-renderer-refactor#D001(选择 DangerAreaRenderer 的覆盖窗口渲染方式)

### 修复
- **[Utils]**: 修复 DebugPoint 渲染不稳定问题
  - 方案: [202601231710_debugpoint-renderer-refactor](archive/2026-01/202601231710_debugpoint-renderer-refactor/)
  - 将 DebugPoint 从前景绘制 API 改为使用 DangerAreaRenderer 的覆盖窗口渲染

### 变更
- **[Utils]**: DebugPoint API 变更
  - `Initialize()` 现在需要传入 `DangerAreaRenderer` 参数
  - 移除了独立的 Draw 事件订阅机制
  - 内部使用回调机制向 DangerAreaRenderer 提供显示对象

- **[UI]**: DangerAreaTab 添加 `Renderer` 公共属性
  - 暴露 DangerAreaRenderer 实例供外部模块使用

- **[Plugin]**: AutoRaidHelper 初始化逻辑更新
  - 修改 `DebugPoint.Initialize()` 调用，传入 DangerAreaRenderer 实例

### 微调
- (无)

### 回滚
- (无)

---

## [0.1.0] - 2026-01-23

### 新增
- **[知识库]**: 初始化知识库
  - 创建 INDEX.md, context.md, CHANGELOG.md
  - 创建模块索引和模块文档
  - 创建归档索引

### 修复
- (无)

### 微调
- (无)

### 回滚
- (无)
