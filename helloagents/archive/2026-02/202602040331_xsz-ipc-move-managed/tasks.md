> **@status:** completed | 2026-02-04 03:33

# 任务清单: xsz-ipc-move-managed

目录: helloagents/plan/202602040331_xsz-ipc-move-managed/

---

## 任务状态符号说明

| 符号 | 状态 | 说明 |
|------|------|------|
| [ ] | pending | 待执行 |
| [√] | completed | 已完成 |
| [X] | failed | 执行失败 |
| [-] | skipped | 已跳过 |
| [?] | uncertain | 待确认 |

---

## 执行状态
`yaml
总任务: 4
已完成: 4
完成率: 100%
`

---

## 任务列表

### 1. Plugin

- [√] 1.1 在 HaiyaBox/HaiyaBox/Plugin/XSZToolboxIpc.cs 注册并封装 MoveManaged/SetPosManaged/SetMoveAssemble/SetMoveAssembleDelay
  - 验证: 编译通过，IPC 调用接口存在

### 2. Utils

- [√] 2.1 在 HaiyaBox/HaiyaBox/Utils/XszRemote.cs 添加对应静态入口
  - 验证: 对外可直接调用新 IPC

### 3. 知识库

- [√] 3.1 更新 helloagents/modules/Plugin.md 记录新增 IPC
  - 验证: 文档描述与代码一致

- [√] 3.2 更新 helloagents/CHANGELOG.md 记录本次新增
  - 验证: 格式符合变更日志规范

---

## 执行备注

> 执行过程中的重要记录

| 任务 | 状态 | 备注 |
|------|------|------|
| 知识库补充 | √ | 更新 helloagents/modules/Utils.md 补齐 XszRemote 新接口 |
