# 变更提案: xsz-ipc-move-managed

## 元信息
`yaml
类型: 新增
方案类型: implementation
优先级: P1
状态: 草稿
创建: 2026-02-04
`

---

## 1. 需求

### 背景
XSZToolbox 新增 MoveManaged / SetPosManaged / SetMoveAssemble / SetMoveAssembleDelay IPC，需要在插件侧补齐调用接口，供遥控与自动化使用。

### 目标
- 在 XSZToolboxIpc 中注册并封装新 IPC
- 在 XszRemote 暴露对应静态方法

### 约束条件
`yaml
时间约束: 无
性能约束: 调用仅在用户触发时执行
兼容性约束: 不改变现有 IPC API
业务约束: 保持现有遥控接口命名风格
`

### 验收标准
- [ ] 新 IPC 方法可正常调用且无编译错误
- [ ] XszRemote 提供对应静态入口

---

## 2. 方案

### 技术方案
- 在 XSZToolboxIpc 增加 4 个 ICallGateSubscriber 并实现封装方法
- 在 XszRemote 添加静态代理方法

### 影响范围
`yaml
涉及模块:
  - Plugin: XSZToolboxIpc IPC 封装
  - Utils: XszRemote 静态入口
预计变更文件: 2
`

### 风险评估
| 风险 | 等级 | 应对 |
|------|------|------|
| IPC 名称或签名不匹配 | 低 | 按提供的名称/参数定义实现 |

---

## 3. 技术设计（可选）

本次为接口补齐，不涉及架构调整。

---

## 4. 核心场景

### 场景: 延迟移动/传送
**模块**: Plugin/Utils
**条件**: 触发遥控延迟移动或传送
**行为**: 调用 MoveManaged/SetPosManaged 并传入目标战斗时间
**结果**: XSZToolbox 按战斗时间调度移动/传送

---

## 5. 技术决策

本次变更不涉及需要记录的技术决策。
