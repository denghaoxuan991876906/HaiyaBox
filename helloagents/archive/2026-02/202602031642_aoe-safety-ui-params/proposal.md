# 变更提案: aoe-safety-ui-params

## 元信息
`yaml
类型: 变更
方案类型: implementation
优先级: P1
状态: 草稿
创建: 2026-02-03
`

---

## 1. 需求

### 背景
DangerAreaTab 已替换为 AOESafetyCalculator UI，但缺少场地参数与安全点计算参数配置区域，无法在新 UI 中设置安全点计算所需的输入。

### 目标
- 在 DangerAreaTab 补充 场地参数设置和安全点计算参数设置区域
- 支持从 AOEShape 列表生成禁止区并计算安全点
- 计算结果可在 UI 查看并驱动 overlay 显示

### 约束条件
`yaml
时间约束: 无
性能约束: 计算触发于手动按钮，避免每帧重算
兼容性约束: 不改变现有 AOEShape 配置与绘制流程
业务约束: UI 文案保持中文
`

### 验收标准
- [ ] 可设置场地参数（圆形/矩形、中心、尺寸、朝向）
- [ ] 可设置安全点计算参数（数量、搜索范围、最小间距、参考点/最大距离）
- [ ] 触发计算后显示安全点结果并更新 overlay

---

## 2. 方案

### 技术方案
- 在 DangerAreaTab 增加场地参数与安全点参数 UI 区块
- 使用 SafeZoneCalculator + AOEShape.Distance 构建 ForbiddenZone
- 计算结果缓存并在 overlay 同步时绘制安全点

### 影响范围
`yaml
涉及模块:
  - UI: DangerAreaTab
  - AOESafetyCalculator: SafeZoneCalculator/ForbiddenZone 使用
预计变更文件: 2
`

### 风险评估
| 风险 | 等级 | 应对 |
|------|------|------|
| 计算参数无效导致异常 | 低 | 输入校验 + 捕获异常并提示 |

---

## 3. 技术设计（可选）

本次为 UI 扩展与计算调用，未引入新架构。

---

## 4. 核心场景

### 场景: 设置场地参数
**模块**: UI
**条件**: 用户打开 DangerAreaTab
**行为**: 选择场地类型并设置中心/尺寸/朝向
**结果**: 计算安全点时使用场地边界

### 场景: 计算安全点
**模块**: UI
**条件**: 用户设置安全点参数并点击计算
**行为**: 根据 AOEShape 生成禁止区并调用计算器
**结果**: UI 显示安全点并更新 overlay

---

## 5. 技术决策

本次变更不涉及需要记录的技术决策。
