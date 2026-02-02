# 变更日志

## [Unreleased]

## [0.3.2] - 2026-02-02

### 微调
- **[Tests]**: 清理 AOESafetyCalculator 测试工程与用例
  - 类型: 微调（无方案包）
  - 文件: tests/AOESafetyCalculator.Tests/AOESafetyCalculator.Tests.csproj:1-22
  - 文件: tests/AOESafetyCalculator.Tests/AOESafetyCalculatorBehaviorTests.cs:1-52

## [0.3.1] - 2026-02-02

### 微调
- **[Project]**: 移除 AOESafetyCalculator.Tests 的 InternalsVisibleTo
  - 类型: 微调（无方案包）
  - 文件: HaiyaBox/HaiyaBox.csproj:38-48

- **[Tests]**: 移除 AOEShapeDebug 调试绘制单元测试
  - 类型: 微调（无方案包）
  - 文件: tests/AOESafetyCalculator.Tests/AOEShapeDebugTests.cs:1-58

## [0.3.0] - 2026-02-02

### 新增
- **[Utils]**: 增加 AOEShapeDebug 调试绘制，支持 AOE 轮廓显示与颜色分桶
  - 方案: [202602030248_aoe-shape-debug-render](archive/2026-02/202602030248_aoe-shape-debug-render/)
  - 决策: aoe-shape-debug-render#D001(复用 DangerAreaRenderer 临时回调绘制)

### 变更
- **[Plugin]**: AutoRaidHelper 生命周期中初始化并释放 AOEShapeDebug
  - 方案: [202602030248_aoe-shape-debug-render](archive/2026-02/202602030248_aoe-shape-debug-render/)

## [0.2.1] - 2026-02-02

### 修复
- **[AOESafetyCalculator]**: 补全 AOE 距离场实现并修复安全区边界判定与最小距离夹紧
  - 方案: [202602022147_aoe-safety-fixes](archive/2026-02/202602022147_aoe-safety-fixes/)

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
