# Plugin

## 职责

插件核心功能和服务管理，包括自动副本助手、宝箱开启、IPC 通信等。

## 接口定义

### 主要类
| 类名 | 说明 |
|------|------|
| AutoRaidHelper | 自动副本战斗助手 |
| TreasureOpenerService | 宝箱自动开启服务 |
| XSZToolboxIpc | XSZ 工具箱 IPC 通信 |

### 公共 API
| 方法 | 说明 |
|------|------|
| AutoRaidHelper.Execute() | 执行自动战斗逻辑 |
| TreasureOpenerService.Open() | 开启宝箱 |
| XSZToolboxIpc.GetRoomId() | 获取房间ID（未就绪返回 null） |
| XSZToolboxIpc.IsConnected() | 获取连接状态（未就绪返回 false） |
| XSZToolboxIpc.Cmd() | 发送遥控命令 |
| XSZToolboxIpc.MoveManaged() | 延迟移动到目标位置 |
| XSZToolboxIpc.SetPosManaged() | 延迟传送到目标位置 |
| XSZToolboxIpc.SetMoveAssemble() | 设置集合信息 |
| XSZToolboxIpc.SetMoveAssembleDelay() | 设置集合补偿时间 |

## 行为规范

### 自动战斗
**条件**: 进入副本战斗状态
**行为**: 根据战斗数据自动执行技能和移动
**结果**: 自动完成副本战斗流程

### IPC 通信
**条件**: 需要与 XSZ 工具箱通信
**行为**: 通过 IPC 协议发送和接收消息，IPC 未就绪时返回空值避免异常
**结果**: 实现跨插件通信并保持 UI 渲染稳定

### 调试绘制初始化
**条件**: 插件加载或卸载
**行为**:
  1. 初始化 DebugPoint 与 AOEShapeDebug 的渲染回调
  2. 释放时注销回调并清理状态
**结果**: 调试渲染与插件生命周期同步

## 依赖关系

```yaml
依赖: Hooks, Settings, Utils, Data
被依赖: UI
```
