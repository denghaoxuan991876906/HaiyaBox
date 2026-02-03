# 变更提案: xsz-ipc-roomid-guard

## 元信息
`yaml
类型: 修复
方案类型: implementation
优先级: P1
状态: 草稿
创建: 2026-02-03
`

---

## 1. 需求

### 背景
AutomationTab 在 IPC 方法尚未注册时调用 XSZToolbox.RemoteControl.GetRoomId，触发 IpcNotReadyError 异常日志。

### 目标
- IPC 未就绪时不抛异常，保证 UI 渲染稳定
- UI 明确提示 IPC 未就绪/未注册

### 约束条件
`yaml
时间约束: 无
性能约束: UI 每帧渲染不增加明显开销
兼容性约束: 不改变现有 IPC API 签名
业务约束: 保持现有遥控流程与按钮逻辑不变
`

### 验收标准
- [ ] IPC 未注册时 GetRoomId 不再抛异常日志
- [ ] AutomationTab 显示房间 ID 未就绪提示
- [ ] 已连接时显示真实房间 ID 与连接状态

---

## 2. 方案

### 技术方案
- 在 XSZToolboxIpc.GetRoomId() 与 IsConnected() 捕获 IpcNotReadyError，返回 
ull/false
- 在 AutomationTab.Draw() 缓存房间 ID 并在空值时显示提示文本

### 影响范围
`yaml
涉及模块:
  - Plugin: XSZToolboxIpc IPC 容错处理
  - UI: AutomationTab 遥控状态提示
预计变更文件: 2
`

### 风险评估
| 风险 | 等级 | 应对 |
|------|------|------|
| 遮蔽其他 IPC 异常 | 低 | 仅捕获 IpcNotReadyError，保留其他异常抛出 |

---

## 3. 技术设计（可选）

本次为小范围修复，不涉及架构调整或 API 设计变更。

---

## 4. 核心场景

### 场景: 遥控状态展示
**模块**: UI
**条件**: IPC 未注册或未就绪，GetRoomId 返回空值
**行为**: UI 显示 房间id：未就绪并提示 IPC 未就绪/未注册
**结果**: 用户可感知遥控状态，UI 渲染无异常日志

---

## 5. 技术决策

本次变更不涉及需要记录的技术决策。
