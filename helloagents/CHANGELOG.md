# 变更日志

## [Unreleased]

### 新增
- **[Rendering]**: SafeZone 自动绘制（DistanceField 轮廓 + 安全点标注），支持副本切换/团灭/战斗重置自动清理
  - 方案: [202602061852_auto-draw-distancefield](archive/2026-02/202602061852_auto-draw-distancefield/)

- **[AOESafetyCalculator]**: SafeZoneDrawRegistry 跟踪计算器/安全点供自动绘制
  - 方案: [202602061852_auto-draw-distancefield](archive/2026-02/202602061852_auto-draw-distancefield/)

- **[UI]**: DangerAreaTab 与 GeometryTab 增加 SafeZone 自动绘制开关、统计与清理入口
  - 方案: [202602061852_auto-draw-distancefield](archive/2026-02/202602061852_auto-draw-distancefield/)

- **[Settings]**: FaGeneralSetting 增加 SafeZoneAutoDrawEnabled 配置
  - 方案: [202602061852_auto-draw-distancefield](archive/2026-02/202602061852_auto-draw-distancefield/)

- **[Tests]**: DistanceFieldContourBuilder 单元测试
  - 方案: [202602061852_auto-draw-distancefield](archive/2026-02/202602061852_auto-draw-distancefield/)

- **[AOESafetyCalculator]**: ForbiddenZone 增加 Name 并支持按名清除单个禁止区域
  - 类型: 微调（无方案包）
  - 文件: HaiyaBox/AOESafetyCalculator/SafetyZone/ForbiddenZone.cs
  - 文件: HaiyaBox/AOESafetyCalculator/SafetyZone/SafeZoneCalculator.cs

- **[Tests]**: 新增 SafeZoneCalculator 按名清除禁止区域测试
  - 类型: 微调（无方案包）
  - 文件: tests/HaiyaBox.Tests/HaiyaBox.Tests.csproj
  - 文件: tests/HaiyaBox.Tests/SafetyZone/ForbiddenZoneNameTests.cs

## [0.4.5] - 2026-02-05

### 微调
- **[UI]**: DangerAreaTab 场地范围绘制改为红色并上浮 1 个单位
  - 类型: 微调（无方案包）
  - 文件: HaiyaBox/UI/DangerAreaTab.cs:59-62
  - 文件: HaiyaBox/UI/DangerAreaTab.cs:833-839

## [0.4.4] - 2026-02-04

### 微调
- **[UI]**: DangerAreaTab 补充场地范围与参考点绘制，并放大安全点显示
  - 类型: 微调（无方案包）
  - 文件: HaiyaBox/UI/DangerAreaTab.cs:59-63
  - 文件: HaiyaBox/UI/DangerAreaTab.cs:201-360
  - 文件: HaiyaBox/UI/DangerAreaTab.cs:832-887

## [0.4.3] - 2026-02-04

### 新增
- **[Plugin]**: XSZToolbox 遥控新增延迟移动/传送及集合控制 IPC 封装
  - 方案: [202602040331_xsz-ipc-move-managed](archive/2026-02/202602040331_xsz-ipc-move-managed/)

- **[Utils]**: XszRemote 补齐延迟移动/传送与集合控制接口
  - 方案: [202602040331_xsz-ipc-move-managed](archive/2026-02/202602040331_xsz-ipc-move-managed/)

## [0.4.2] - 2026-02-03

### 变更
- **[UI]**: DangerAreaTab 增加场地参数与安全点计算参数区域，支持计算安全点并绘制
  - 方案: [202602031642_aoe-safety-ui-params](archive/2026-02/202602031642_aoe-safety-ui-params/)

## [0.4.1] - 2026-02-03

### 修复
- **[Plugin]**: XSZ IPC 未就绪时房间ID/连接状态返回空值避免异常
  - 方案: [202602031551_xsz-ipc-roomid-guard](archive/2026-02/202602031551_xsz-ipc-roomid-guard/)

- **[UI]**: AutomationTab 显示 IPC 未就绪提示
  - 方案: [202602031551_xsz-ipc-roomid-guard](archive/2026-02/202602031551_xsz-ipc-roomid-guard/)

## [0.4.0] - 2026-02-03

### 变更
- **[UI]**: DangerAreaTab 替换为 AOESafetyCalculator 形状配置与绘制界面
  - 方案: [202602031326_aoe-safety-calculator-ui](archive/2026-02/202602031326_aoe-safety-calculator-ui/)

- **[AOESafetyCalculator]**: 接入 AOEShape 列表驱动的 overlay 绘制
  - 方案: [202602031326_aoe-safety-calculator-ui](archive/2026-02/202602031326_aoe-safety-calculator-ui/)

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
