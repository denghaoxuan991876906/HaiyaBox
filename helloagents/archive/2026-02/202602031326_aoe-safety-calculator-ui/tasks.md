# 任务清单: aoe-safety-calculator-ui

> **@status:** completed | 2026-02-03 14:19

目录: `helloagents/plan/202602031326_aoe-safety-calculator-ui/`

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
总任务: 7
已完成: 5
完成率: 71%
已跳过: 1
待确认: 1
```

---

## 任务列表

### 1. UI 替换

- [√] 1.1 在 `HaiyaBox/UI/DangerAreaTab.cs` 移除旧危险区逻辑与 UI，新增 AOESafetyCalculator UI（开关/参数设置/当前数据列表）
  - 验证: 打开标签页确认旧区块消失，新区块出现

- [√] 1.2 在 `HaiyaBox/UI/DangerAreaTab.cs` 接入 AOEShape 列表与 overlay 刷新（`AOEShapeDebug.BuildDisplayObjectsFor` + `DangerAreaRenderer.UpdateObjects`）
  - 依赖: 1.1

- [-] 1.3 视需要调整标签标题（如“危险区域”→“AOE 安全”）
  - 验证: 插件主界面标签标题更新
  > 备注: 为保持现有标签习惯，本次未修改标签标题

### 2. 文档同步

- [√] 2.1 更新 `helloagents/modules/UI.md`，同步 DangerAreaTab 的职责与界面描述
  - 依赖: 1.1

- [√] 2.2 更新 `helloagents/modules/AOESafetyCalculator.md`，补充 UI 接入说明
  - 依赖: 1.1

- [√] 2.3 视实际变更更新 `helloagents/modules/Rendering.md`（overlay 复用说明）

### 3. 验证

- [?] 3.1 手动验证：开关可用，AOEShape 可新增/删除，列表与绘制一致
  - 依赖: 1.2

---

## 执行备注

> 执行过程中的重要记录

| 任务 | 状态 | 备注 |
|------|------|------|
