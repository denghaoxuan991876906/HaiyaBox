# 变更提案: aoe-safety-calculator-ui

## 元信息
```yaml
类型: 重构/优化
方案类型: implementation
优先级: P1
状态: 草稿
创建: 2026-02-03
```

---

## 1. 需求

### 背景
插件现有“危险区域”UI/逻辑基于自定义 DangerArea 与 SafePointCalculator。项目已新增 AOESafetyCalculator，需要用新库替换旧危险区相关逻辑，并重建 UI。

### 目标
- 移除旧危险区 UI 与相关调用
- 使用 AOESafetyCalculator 的 AOEShape 作为危险区表示
- 新 UI 提供：开关、AOEShape 参数设置、当前 AOEShape 数据展示/管理
- 保持 overlay 渲染开关对 DebugPoint/AOEShapeDebug 可用

### 约束条件
```yaml
时间约束: 无
性能约束: UI 交互与绘制应保持流畅（默认少量 AOEShape）
兼容性约束: 不影响其他标签页与 DebugPoint/AOEShapeDebug 的绘制
业务约束: 不保留旧危险区 UI 与配置入口
```

### 验收标准
- [ ] 旧危险区相关 UI 已移除（DangerAreaTab 不再显示旧配置/计算/结果）
- [ ] 新 UI 包含开关、AOEShape 参数设置、当前 AOEShape 数据列表
- [ ] AOEShape 可新增/删除，列表展示与数量统计准确
- [ ] overlay 开关可控制绘制且不影响 DebugPoint/AOEShapeDebug

---

## 2. 方案

### 技术方案
- 重写 `DangerAreaTab`：移除旧危险区逻辑，改为 AOESafetyCalculator UI
- 维护 AOEShape 列表（含 origin/rotation/invert）作为当前危险区数据
- 使用 `AOEShapeDebug.BuildDisplayObjectsFor` 生成绘制对象，交由 `DangerAreaRenderer` 渲染
- 保留 `OverlayEnabled` 与 `Renderer` 接口，确保现有 DebugPoint/AOEShapeDebug 兼容

### 影响范围
```yaml
涉及模块:
  - UI: DangerAreaTab UI 改造
  - Rendering: overlay 复用
  - AOESafetyCalculator: 引入 AOEShape 作为危险区数据结构
预计变更文件: 1-2
```

### 风险评估
| 风险 | 等级 | 应对 |
|------|------|------|
| UI 替换导致交互遗漏 | 中 | 对照需求逐项实现并手动验收 |
| overlay 刷新不一致 | 低 | 统一通过标记刷新与 Update 驱动 |

---

## 3. 核心场景

### 场景: 配置 AOEShape 列表
**模块**: UI
**条件**: 用户打开 AOESafetyCalculator 标签页
**行为**: 选择形状类型、填写参数并添加到列表
**结果**: 列表显示当前 AOEShape 数据

### 场景: 切换绘制开关
**模块**: Rendering
**条件**: 用户切换绘制开关
**行为**: 启用/禁用 overlay 绘制并刷新显示
**结果**: 游戏画面显示/隐藏 AOEShape

---

## 4. 技术决策

> 本次为 UI 替换与库接入，不涉及架构/技术选型调整，暂不记录决策。