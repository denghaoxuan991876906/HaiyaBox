# 模块索引

> 通过此文件快速定位模块文档

## 模块清单

| 模块 | 职责 | 状态 | 文档 |
|------|------|------|------|
| Hooks | 游戏事件钩子和拦截 | ✅ | [Hooks.md](./Hooks.md) |
| Plugin | 插件核心功能和服务 | ✅ | [Plugin.md](./Plugin.md) |
| Rendering | 渲染功能（危险区域显示等） | ✅ | [Rendering.md](./Rendering.md) |
| Settings | 配置和设置管理 | ✅ | [Settings.md](./Settings.md) |
| Triggers | 触发器系统（条件和动作） | ✅ | [Triggers.md](./Triggers.md) |
| UI | 用户界面标签页 | ✅ | [UI.md](./UI.md) |
| Utils | 通用工具类 | ✅ | [Utils.md](./Utils.md) |
| Data | 副本数据（M11S, M12S 等） | ✅ | [Data.md](./Data.md) |

## 模块依赖关系

```
Plugin (核心)
  ├─> Hooks (事件监听)
  ├─> Rendering (渲染输出)
  ├─> Settings (配置管理)
  ├─> Triggers (触发器)
  ├─> UI (界面显示)
  ├─> Utils (工具支持)
  └─> Data (数据支持)

Utils (工具层)
  └─> 被: Plugin, Rendering, UI, Triggers

Data (数据层)
  └─> 被: Plugin, Triggers
```

## 状态说明
- ✅ 稳定
- 🚧 开发中
- 📝 规划中
