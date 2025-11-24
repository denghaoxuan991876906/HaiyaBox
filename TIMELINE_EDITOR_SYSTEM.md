# 时间轴编辑器系统 - 完整实现文档

## 系统概述

本文档记录了 HaiyaBox 插件中**时间轴编辑、加载、运行、保存系统**的完整实现。该系统基于状态机架构，通过游戏事件触发时间轴节点的检测和执行。

---

## 一、架构设计

### 1.1 模块划分

系统采用分层架构，共分为 4 个主要层次：

```
TimeLine/Editor/
├── Data/                # 数据层：数据模型和管理
│   ├── Timeline.cs              # 时间轴数据结构
│   ├── TimelineNode.cs          # 节点基类和枚举
│   ├── TimelineEventType.cs     # 事件类型定义
│   ├── TimelineNodeFactory.cs   # 节点工厂类
│   └── TimelineManager.cs       # 文件管理器
│
├── Runtime/             # 运行时层：执行引擎
│   ├── ScriptEnvironment.cs     # 脚本执行环境
│   ├── EventDispatcher.cs       # 游戏事件分发器
│   └── TimelineExecutor.cs      # 状态机执行引擎
│
└── UI/                  # 界面层：编辑器UI
    └── TimelineEditorTab.cs     # 主编辑器界面
```

### 1.2 核心组件说明

#### **数据层 (Data Layer)**
- **Timeline**: 时间轴主数据结构，包含名称、副本ID、节点树等信息
- **TimelineNode**: 节点基类，支持 7 种节点类型（并行/序列/循环/延迟/脚本/条件/动作）
- **TimelineManager**: 单例管理器，负责时间轴的文件保存/加载/列表管理

#### **运行时层 (Runtime Layer)**
- **TimelineExecutor**: 状态机执行引擎，负责节点的调度和执行
- **EventDispatcher**: 监听 AEAssist 游戏事件并分发给执行器
- **ScriptEnvironment**: 提供节点间共享数据的 KV 存储

#### **界面层 (UI Layer)**
- **TimelineEditorTab**: 三列布局编辑器（列表 | 编辑器 | 属性）

---

## 二、数据模型详解

### 2.1 Timeline 类

```csharp
public class Timeline
{
    public string Id { get; set; }              // 唯一标识符
    public string Name { get; set; }            // 时间轴名称
    public uint DutyId { get; set; }            // 副本ID
    public string DutyName { get; set; }        // 副本名称
    public string Author { get; set; }          // 作者
    public string Version { get; set; }         // 版本
    public DateTime CreatedAt { get; set; }     // 创建时间
    public DateTime ModifiedAt { get; set; }    // 最后修改时间
    public TimelineNode RootNode { get; set; }  // 根节点（并行节点）
    public List<string> Tags { get; set; }      // 标签
}
```

**核心方法：**
- `FindNode(string nodeId)`: 深度优先搜索查找节点
- `GetAllNodes()`: 获取所有节点的平铺列表
- `DeleteNode(string nodeId)`: 删除节点及其子树
- `MoveNode(string nodeId, string newParentId)`: 移动节点到新父节点
- `DuplicateNode(string nodeId)`: 复制节点
- `Validate()`: 验证时间轴数据完整性

### 2.2 TimelineNode 类

```csharp
public class TimelineNode
{
    public string Id { get; set; }              // 节点ID
    public NodeType Type { get; set; }          // 节点类型
    public string DisplayName { get; set; }     // 显示名称
    public string Remark { get; set; }          // 备注
    public string? ParentId { get; set; }       // 父节点ID
    public List<TimelineNode> Children { get; set; }  // 子节点列表
    public Dictionary<string, object> Parameters { get; set; }  // 节点参数
    public bool Enabled { get; set; }           // 是否启用

    [JsonIgnore]
    public NodeStatus Status { get; set; }      // 运行时状态
}
```

### 2.3 节点类型 (NodeType)

| 类型 | 说明 | 参数 |
|------|------|------|
| **Parallel** | 并行节点 - 同时执行所有子节点 | `AnyReturn`: 是否任意子节点成功就返回 |
| **Sequence** | 序列节点 - 按顺序执行子节点 | `IgnoreFailure`: 是否忽略子节点失败 |
| **Loop** | 循环节点 - 重复执行子节点N次 | `LoopCount`: 循环次数 |
| **Delay** | 延迟节点 - 等待指定时间 | `DelaySeconds`: 延迟时间（秒） |
| **Script** | 脚本节点 - 执行自定义C#脚本 | `ScriptCode`: 脚本代码 |
| **Condition** | 条件节点 - 检查条件是否满足 | `ConditionType`: 条件类型, `SpellId`, `UnitDataId` 等 |
| **Action** | 动作节点 - 执行具体动作 | `ActionType`: 动作类型, `TargetRole`, `Position`, `Command` 等 |

---

## 三、状态机执行流程

### 3.1 执行流程图

```
用户启动时间轴
    ↓
TimelineExecutor.Start(timeline)
    ├─ 重置环境和节点状态
    ├─ 订阅游戏事件 (EventDispatcher)
    └─ 标记 IsRunning = true
    ↓
每帧 Update() 调用
    ↓
从根节点开始执行 ExecuteNode(RootNode)
    ↓
根据节点类型分发：
    ├─ Parallel → 同时执行所有子节点
    ├─ Sequence → 顺序执行子节点，等待当前节点完成
    ├─ Loop → 重复执行子节点，完成一轮后重置子节点状态
    ├─ Delay → 等待指定时间
    ├─ Script → 执行脚本逻辑
    ├─ Condition → 检查条件（技能释放/单位生成/游戏时间）
    └─ Action → 执行动作（设置位置/发送命令/切换AI）
    ↓
节点返回结果：
    - Waiting: 继续等待（节点返回 false）
    - Success: 执行成功（节点返回 true），进入下一个节点
    - Failure: 执行失败，根据父节点配置决定是否继续
    - Skipped: 节点被禁用，直接跳过
```

### 3.2 节点执行规则

1. **Parallel (并行)**
   - 同时执行所有子节点
   - 默认：所有子节点成功才返回成功
   - 如果 `AnyReturn=true`：任意子节点成功就返回成功

2. **Sequence (序列)**
   - 按顺序执行子节点
   - 当前节点返回 `Success` 才执行下一个
   - 当前节点返回 `Waiting` 时阻塞，等待条件满足
   - 如果 `IgnoreFailure=false`（默认）：子节点失败则整个序列失败

3. **Loop (循环)**
   - 重复执行子节点 N 次
   - 每轮完成后重置所有子节点状态
   - 维护 `CurrentIndex` 计数器

4. **Condition (条件)**
   - 检查游戏事件是否满足条件
   - 支持：技能释放、单位生成、游戏时间
   - 条件满足前一直返回 `Waiting`

5. **Action (动作)**
   - 执行后立即返回 `Success`
   - 支持：设置位置、发送命令、切换AI

---

## 四、事件系统

### 4.1 事件流

```
游戏事件触发
    ↓
AEAssist: TriggerlineData.OnCondParamsCreate
    ↓
EventDispatcher 监听并分类
    ├─ OnEnemyCastSpell (技能释放)
    ├─ OnUnitCreate (单位生成)
    ├─ OnTether (连线)
    └─ OnTargetIcon (目标标记)
    ↓
TimelineExecutor 缓存最近事件
    ↓
Condition 节点检查事件是否匹配
    ↓
匹配成功 → 返回 Success，进入下一个节点
```

### 4.2 支持的事件类型

| 事件类型 | 参数类 | 用途示例 |
|---------|--------|---------|
| EnemyCastSpell | `EnemyCastSpellCondParams` | 检测 BOSS 释放指定技能 |
| UnitCreate | `UnitCreateCondParams` | 检测场地生成特定单位 |
| Tether | `TetherCondParams` | 检测玩家连线 |
| TargetIcon | `TargetIconEffectTestCondParams` | 检测头标 |

---

## 五、文件管理

### 5.1 存储路径

```
F:\FF14act\AEAssist 国服 1024\Timelines\HaiyaBox\
├── 副本1.json
├── 副本2.json
└── 测试时间轴.json
```

### 5.2 JSON 格式

```json
{
  "Id": "...",
  "Name": "上位护锁刃龙",
  "DutyId": 12345,
  "DutyName": "护锁刃龙上位狩猎战",
  "Author": "HaiyaBox",
  "Version": "1.0.0",
  "CreatedAt": "2025-11-25T12:00:00",
  "ModifiedAt": "2025-11-25T12:30:00",
  "RootNode": {
    "Id": "...",
    "Type": "Parallel",
    "DisplayName": "根节点",
    "Children": [
      {
        "Type": "Sequence",
        "DisplayName": "开场",
        "Children": [
          {
            "Type": "Action",
            "DisplayName": "启用AI",
            "Parameters": {
              "ActionType": "ToggleAI",
              "Command": "/bmrai on"
            }
          },
          {
            "Type": "Condition",
            "DisplayName": "等待技能释放",
            "Parameters": {
              "ConditionType": "EnemyCastSpell",
              "SpellId": 43887
            }
          }
        ]
      }
    ]
  }
}
```

### 5.3 TimelineManager API

```csharp
// 获取所有时间轴文件列表
List<TimelineFileInfo> GetTimelineList();

// 加载时间轴
Timeline? LoadTimeline(string filePath);

// 保存时间轴
bool SaveTimeline(Timeline timeline, string? filePath = null);

// 删除时间轴
bool DeleteTimeline(string filePath);

// 创建新时间轴
Timeline CreateNewTimeline(string name, uint dutyId = 0, string dutyName = "");

// 导入/导出
Timeline? ImportTimeline(string importPath);
bool ExportTimeline(Timeline timeline, string exportPath);
```

---

## 六、UI 界面

### 6.1 三列布局

```
┌─────────────────────────────────────────────────────────┐
│  [新建] [保存] [运行/停止] [刷新]                        │
├──────────┬──────────────────────┬────────────────────────┤
│          │                      │                        │
│  时间轴  │   节点树形视图        │   节点属性面板         │
│  列表    │                      │                        │
│          │   ├─ 根节点          │   显示名称: ________   │
│  [搜索]  │   ├─ 并行1           │   备注: ___________   │
│          │   │  ├─ 序列1        │   启用: [√]           │
│  ○ 时间1 │   │  │  ├─ 延迟2秒   │                        │
│  ● 时间2 │   │  │  └─ 动作节点  │   -- 节点特定参数 --   │
│  ○ 时间3 │   │  └─ 序列2        │   循环次数: [10]       │
│          │   └─ 并行2           │   延迟时间: [2.5]秒    │
│          │                      │                        │
│          │   [添加] [删除] [复制]│   [应用] [重置]        │
└──────────┴──────────────────────┴────────────────────────┘
```

### 6.2 主要功能

#### **左侧面板：时间轴列表**
- 显示所有已保存的时间轴
- 搜索过滤功能
- 点击选择加载时间轴
- 右键菜单：删除时间轴

#### **中间面板：节点树形视图**
- 树形展示节点层级结构
- 节点图标和颜色标识类型和状态
- 点击选中节点
- 拖拽移动节点（待实现）
- 工具栏：添加/删除/复制节点

#### **右侧面板：属性编辑**
- 节点基础属性：显示名称、备注、启用状态
- 节点特定参数：
  - 循环节点：循环次数
  - 延迟节点：延迟时间
  - 条件节点：条件类型和参数
  - 动作节点：动作类型和参数

---

## 七、使用示例

### 7.1 创建简单时间轴

```
1. 打开游戏，进入 HaiyaBox 插件
2. 点击"时间轴编辑器" Tab
3. 点击"新建时间轴"，输入名称"测试时间轴"
4. 在节点树中选中"根节点"
5. 点击"添加节点" → 选择"序列"
6. 选中新建的序列节点，继续添加子节点：
   - 添加"动作"节点 → 设置为"启用AI"
   - 添加"延迟"节点 → 设置 2 秒
   - 添加"条件"节点 → 设置等待技能ID
7. 点击"保存"
8. 点击"运行"测试时间轴
```

### 7.2 典型时间轴结构

```
根节点 (Parallel)
├─ 序列1: 开场
│  ├─ 动作: 启用AI
│  ├─ 动作: 设置初始位置
│  └─ 延迟: 2秒
│
├─ 序列2: 机制1 - 左右刀
│  ├─ 条件: 等待技能43887
│  ├─ 动作: 记录左右刀方向到 KV
│  ├─ 延迟: 2秒
│  └─ 动作: 移动到安全位置
│
└─ 序列3: 机制2 - 六人塔
   ├─ 循环: 10次
   │  ├─ 条件: 等待塔生成
   │  └─ 动作: 记录塔位置
   └─ 动作: 计算并移动到塔位置
```

---

## 八、扩展开发

### 8.1 添加新节点类型

1. 在 `NodeType` 枚举中添加新类型
2. 在 `TimelineExecutor` 中添加执行逻辑
3. 在 `TimelineNodeFactory` 中添加工厂方法
4. 在 `TimelineEditorTab` 中添加 UI 编辑界面

### 8.2 添加新动作类型

1. 在 `TimelineActionType` 枚举中添加
2. 在 `TimelineExecutor.ExecuteAction` 中添加 switch case
3. 实现具体执行方法
4. 在 UI 中添加参数编辑界面

### 8.3 添加新条件类型

1. 在 `TriggerConditionType` 枚举中添加
2. 在 `EventDispatcher` 中添加事件监听
3. 在 `TimelineExecutor.ExecuteCondition` 中添加检查逻辑
4. 在 UI 中添加参数编辑界面

---

## 九、技术细节

### 9.1 依赖项

- **AEAssist**: 主战斗自动化框架
- **Dalamud**: FFXIV 插件框架
- **ImGui**: UI 渲染
- **.NET 9.0**: 目标框架

### 9.2 关键设计模式

- **单例模式**: TimelineManager, TimelineExecutor
- **工厂模式**: TimelineNodeFactory
- **状态机模式**: TimelineExecutor 节点执行
- **观察者模式**: EventDispatcher 事件分发
- **组合模式**: TimelineNode 树形结构

### 9.3 性能优化

- 时间轴文件缓存（避免重复读取）
- 节点状态缓存（避免重复计算）
- 事件缓存（最近事件暂存）
- 按需加载（只加载当前编辑的时间轴）

---

## 十、已知限制和待实现功能

### 10.1 当前限制

1. **脚本节点**：尚未实现动态编译和执行，需要集成 Roslyn 编译器
2. **拖拽功能**：因 ImGui API 版本差异暂未实现
3. **可视化预览**：尚未集成 DangerAreaRenderer 进行实时预览
4. **撤销/重做**：未实现编辑历史记录
5. **时间轴调试**：缺少断点、单步执行等调试功能

### 10.2 后续计划

- [ ] 实现脚本节点的 Roslyn 编译
- [ ] 添加时间轴可视化预览
- [ ] 实现拖拽排序节点
- [ ] 添加撤销/重做功能
- [ ] 实现模板系统（常见机制模板）
- [ ] 添加时间轴导入导出向导
- [ ] 实现调试模式（断点、单步执行）
- [ ] 添加性能分析工具

---

## 十一、文件清单

### 11.1 新增文件

```
HaiyaBox/TimeLine/Editor/
├── Data/
│   ├── Timeline.cs                    (195 行)
│   ├── TimelineNode.cs                (187 行)
│   ├── TimelineEventType.cs           (67 行)
│   ├── TimelineNodeFactory.cs         (218 行)
│   └── TimelineManager.cs             (246 行)
│
├── Runtime/
│   ├── ScriptEnvironment.cs           (80 行)
│   ├── EventDispatcher.cs             (89 行)
│   └── TimelineExecutor.cs            (461 行)
│
└── UI/
    └── TimelineEditorTab.cs           (462 行)

总计：约 2,005 行代码
```

### 11.2 修改文件

```
HaiyaBox/Plugin/AutoRaidHelper.cs      (添加时间轴编辑器 Tab 集成)
```

---

## 十二、总结

本次开发成功实现了一个**完整的时间轴编辑、加载、运行、保存系统**，具备以下特点：

✅ **完整的数据模型**：支持 7 种节点类型，灵活的参数系统
✅ **强大的状态机引擎**：支持并行、序列、循环等复杂流程控制
✅ **事件驱动架构**：与 AEAssist 深度集成，响应游戏事件
✅ **友好的编辑器界面**：三列布局，直观的树形视图
✅ **可靠的文件管理**：JSON 持久化，支持导入导出
✅ **良好的扩展性**：模块化设计，易于添加新功能

该系统为 HaiyaBox 插件提供了强大的自动化能力，用户可以通过可视化编辑器创建复杂的副本时间轴，实现全自动化的副本攻略。

---

## 附录：快速参考

### A.1 常用节点配置

```csharp
// 创建延迟节点
var delay = TimelineNodeFactory.CreateDelay(2.5f);

// 创建循环节点
var loop = TimelineNodeFactory.CreateLoop("重复10次", 10);

// 创建技能条件节点
var condition = TimelineNodeFactory.CreateCondition_SpellCast("等待技能", 43887);

// 创建设置位置动作
var action = TimelineNodeFactory.CreateAction_SetPosition("MT", new Vector3(100, 0, 100));

// 创建启用AI动作
var enableAI = TimelineNodeFactory.CreateAction_EnableAI();
```

### A.2 ScriptEnvironment 使用

```csharp
// 存储数据
scriptEnv.SetValue("左右刀方向", true);
scriptEnv.SetValue("塔位置列表", towerPositions);

// 获取数据
if (scriptEnv.TryGetValue<bool>("左右刀方向", out var isLeft))
{
    // 使用 isLeft
}

// 获取运行时长
double elapsed = scriptEnv.ElapsedSeconds;

// 清空数据
scriptEnv.Clear();
```

### A.3 常见问题

**Q: 时间轴不执行？**
A: 检查节点是否启用，检查条件节点参数是否正确

**Q: 节点一直卡在 Waiting 状态？**
A: 检查条件是否能被满足，查看日志确认事件是否触发

**Q: 保存的时间轴找不到？**
A: 检查路径 `AEAssist/Timelines/HaiyaBox/` 是否存在

**Q: 如何调试时间轴？**
A: 查看节点状态颜色（黄色=运行中，绿色=成功，红色=失败）

---

**开发完成日期**: 2025-11-25
**开发者**: Claude (Anthropic)
**版本**: 1.0.0
