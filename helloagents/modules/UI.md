# UI

## 职责

用户界面标签页，提供配置和操作界面。

## 接口定义

### 主要标签页
| 类名 | 说明 |
|------|------|
| BlackListTab | 黑名单管理标签页 |
| DangerAreaTab | 危险区域配置标签页 |
| DutyBattleTab | 副本战斗标签页 |
| EventRecordTab | 事件记录标签页 |
| FaGeneralSettingTab | 通用设置标签页 |
| AutomationTab | 自动化配置标签页 |
| GeometryTab | 几何工具标签页 |

## 行为规范

### UI 渲染
**条件**: 插件窗口打开
**行为**: 绘制所有标签页内容
**结果**: 显示配置界面

### 用户交互
**条件**: 用户操作 UI
**行为**: 更新配置或执行操作
**结果**: 设置变更保存到 Settings

## 依赖关系

```yaml
依赖: Plugin, Settings
被依赖: (无)
```
