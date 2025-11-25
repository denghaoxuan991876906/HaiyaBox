using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Loader;
using AEAssist.Helper;
using Dalamud.Bindings.ImGui;
using HaiyaBox.TimeLine.Editor.Data;
using HaiyaBox.TimeLine.Editor.Runtime;

namespace HaiyaBox.TimeLine.Editor.UI;

/// <summary>
/// 时间轴编辑器 Tab
/// </summary>
public class TimelineEditorTab : IDisposable
{
    /// <summary>时间轴管理器</summary>
    private readonly TimelineManager _timelineManager = TimelineManager.Instance;

    /// <summary>时间轴执行器</summary>
    private readonly TimelineExecutor _executor = new();

    /// <summary>当前编辑的时间轴</summary>
    private Timeline? _currentTimeline;

    /// <summary>当前选中的节点</summary>
    private TimelineNode? _selectedNode;

    /// <summary>时间轴文件列表</summary>
    private List<TimelineFileInfo> _timelineList = new();

    /// <summary>Expanded node ids to keep tree state.</summary>
    private readonly HashSet<string> _expandedNodeIds = new();

    /// <summary>时间轴文件列表是否需要刷新</summary>
    private bool _needRefreshList = true;

    /// <summary>新时间轴名称输入</summary>
    private string _newTimelineName = "新时间轴";

    /// <summary>搜索过滤文本</summary>
    private string _searchFilter = string.Empty;

    /// <summary>
    /// 加载时调用
    /// </summary>
    public void OnLoad(AssemblyLoadContext loadContext)
    {
        LogHelper.Print("[时间轴编辑器] 初始化");
    }

    /// <summary>
    /// 每帧更新
    /// </summary>
    public void Update()
    {
        _executor.Update();
    }

    /// <summary>
    /// 绘制 UI
    /// </summary>
    public void Draw()
    {
        // 刷新时间轴列表
        if (_needRefreshList)
        {
            _timelineList = _timelineManager.GetTimelineList();
            _needRefreshList = false;
        }

        // 主工具栏
        DrawToolbar();

        ImGui.Separator();

        // 三列布局
        DrawThreeColumnLayout();
    }

    /// <summary>
    /// 绘制工具栏
    /// </summary>
    private void DrawToolbar()
    {
        // 新建按钮
        if (ImGui.Button("新建时间轴"))
        {
            ImGui.OpenPopup("CreateTimelinePopup");
        }

        ImGui.SameLine();

        // 保存按钮
        ImGui.BeginDisabled(_currentTimeline == null);
        if (ImGui.Button("保存"))
        {
            SaveCurrentTimeline();
        }
        ImGui.EndDisabled();

        ImGui.SameLine();

        // 运行/停止按钮
        if (_executor.IsRunning)
        {
            if (ImGui.Button("停止"))
            {
                _executor.Stop();
            }

            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.2f, 1f, 0.2f, 1f), $"运行中 ({_executor.ScriptEnv.ElapsedSeconds:F1}秒)");
        }
        else
        {
            ImGui.BeginDisabled(_currentTimeline == null);
            if (ImGui.Button("运行"))
            {
                if (_currentTimeline != null)
                {
                    _executor.Start(_currentTimeline);
                }
            }
            ImGui.EndDisabled();
        }

        ImGui.SameLine();

        // 刷新列表按钮
        if (ImGui.Button("刷新列表"))
        {
            _needRefreshList = true;
        }

        // 新建时间轴弹窗
        DrawCreateTimelinePopup();
    }

    /// <summary>
    /// 绘制新建时间轴弹窗
    /// </summary>
    private void DrawCreateTimelinePopup()
    {
        if (ImGui.BeginPopup("CreateTimelinePopup"))
        {
            ImGui.Text("创建新时间轴");
            ImGui.Separator();

            ImGui.InputText("时间轴名称", ref _newTimelineName, 100);

            ImGui.Spacing();

            if (ImGui.Button("创建", new Vector2(120, 0)))
            {
                if (!string.IsNullOrWhiteSpace(_newTimelineName))
                {
                    _currentTimeline = _timelineManager.CreateNewTimeline(_newTimelineName);
                    _selectedNode = _currentTimeline.RootNode;
                    ResetExpansionState(_currentTimeline);
                    ImGui.CloseCurrentPopup();

                    LogHelper.Print($"[时间轴编辑器] 创建新时间轴: {_newTimelineName}");
                }
            }

            ImGui.SameLine();

            if (ImGui.Button("取消", new Vector2(120, 0)))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    /// <summary>
    /// 绘制三列布局
    /// </summary>
    private void DrawThreeColumnLayout()
    {
        var avail = ImGui.GetContentRegionAvail();

        // 左侧：时间轴列表（25%）
        ImGui.BeginChild("TimelineListPanel", new Vector2(avail.X * 0.25f, avail.Y), true);
        DrawTimelineListPanel();
        ImGui.EndChild();

        ImGui.SameLine();

        // 中间：节点树形视图和编辑器（50%）
        ImGui.BeginChild("EditorPanel", new Vector2(avail.X * 0.5f, avail.Y), true);
        DrawEditorPanel();
        ImGui.EndChild();

        ImGui.SameLine();

        // 右侧：属性面板（25%）
        ImGui.BeginChild("PropertiesPanel", new Vector2(avail.X * 0.25f, avail.Y), true);
        DrawPropertiesPanel();
        ImGui.EndChild();
    }

    /// <summary>
    /// 绘制时间轴列表面板
    /// </summary>
    private void DrawTimelineListPanel()
    {
        ImGui.Text("时间轴列表");
        ImGui.Separator();

        // 搜索框
        ImGui.InputTextWithHint("##Search", "搜索...", ref _searchFilter, 100);

        ImGui.Spacing();

        // 时间轴列表
        foreach (var info in _timelineList)
        {
            // 应用搜索过滤
            if (!string.IsNullOrEmpty(_searchFilter) &&
                !info.TimelineName.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) &&
                !info.DutyName.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            bool isSelected = _currentTimeline != null && info.TimelineName == _currentTimeline.Name;

            if (ImGui.Selectable($"{info.TimelineName}##{info.FilePath}", isSelected))
            {
                LoadTimeline(info.FilePath);
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text($"文件: {info.FileName}");
                ImGui.Text($"副本: {info.DutyName}");
                ImGui.Text($"作者: {info.Author}");
                ImGui.Text($"版本: {info.Version}");
                ImGui.Text($"修改时间: {info.ModifiedAt:yyyy-MM-dd HH:mm:ss}");
                ImGui.EndTooltip();
            }

            // 右键菜单
            if (ImGui.BeginPopupContextItem($"TimelineContext_{info.FilePath}"))
            {
                if (ImGui.MenuItem("删除"))
                {
                    _timelineManager.DeleteTimeline(info.FilePath);
                    _needRefreshList = true;

                    if (_currentTimeline != null && info.TimelineName == _currentTimeline.Name)
                    {
                        _currentTimeline = null;
                        _selectedNode = null;
                        _expandedNodeIds.Clear();
                    }
                }

                ImGui.EndPopup();
            }
        }
    }

    /// <summary>
    /// 绘制编辑器面板
    /// </summary>
    private void DrawEditorPanel()
    {
        if (_currentTimeline == null)
        {
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "请创建或加载一个时间轴");
            return;
        }

        ImGui.Text($"时间轴: {_currentTimeline.Name}");
        ImGui.Separator();

        // 添加节点工具栏
        DrawNodeToolbar();

        ImGui.Separator();

        // 节点树形视图
        DrawNodeTree(_currentTimeline.RootNode);
    }

    /// <summary>
    /// 绘制节点工具栏
    /// </summary>
    private void DrawNodeToolbar()
    {
        if (ImGui.Button("添加节点"))
        {
            ImGui.OpenPopup("AddNodePopup");
        }

        // 添加节点弹窗
        if (ImGui.BeginPopup("AddNodePopup"))
        {
            ImGui.Text("选择节点类型:");
            ImGui.Separator();

            if (ImGui.MenuItem("并行")) AddNode(NodeType.Parallel);
            if (ImGui.MenuItem("序列")) AddNode(NodeType.Sequence);
            if (ImGui.MenuItem("循环")) AddNode(NodeType.Loop);
            if (ImGui.MenuItem("延迟")) AddNode(NodeType.Delay);
            if (ImGui.MenuItem("脚本")) AddNode(NodeType.Script);
            if (ImGui.MenuItem("条件")) AddNode(NodeType.Condition);
            if (ImGui.MenuItem("动作")) AddNode(NodeType.Action);

            ImGui.EndPopup();
        }

        ImGui.SameLine();

        ImGui.BeginDisabled(_selectedNode == null || _selectedNode == _currentTimeline.RootNode);
        if (ImGui.Button("删除节点"))
        {
            if (_selectedNode != null && _currentTimeline != null)
            {
                _currentTimeline.DeleteNode(_selectedNode.Id);
                _selectedNode = null;
            }
        }
        ImGui.EndDisabled();

        ImGui.SameLine();

        ImGui.BeginDisabled(_selectedNode == null || _selectedNode == _currentTimeline.RootNode);
        if (ImGui.Button("复制节点"))
        {
            if (_selectedNode != null && _currentTimeline != null)
            {
                var clone = _currentTimeline.DuplicateNode(_selectedNode.Id);
                if (clone != null)
                {
                    _selectedNode = clone;
                }
            }
        }
        ImGui.EndDisabled();
    }

    /// <summary>
    /// 绘制节点树
    /// </summary>
    private void DrawNodeTree(TimelineNode node, int depth = 0)
    {
        var icon = TimelineNodeFactory.GetNodeTypeIcon(node.Type);
        var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;

        if (_selectedNode == node)
            flags |= ImGuiTreeNodeFlags.Selected;

        if (node.Children.Count == 0)
            flags |= ImGuiTreeNodeFlags.Leaf;

        if (_expandedNodeIds.Contains(node.Id))
        {
            ImGui.SetNextItemOpen(true);
        }

        // 节点颜色（根据状态）
        var color = node.Status switch
        {
            NodeStatus.Running => new Vector4(1f, 1f, 0f, 1f),  // 黄色
            NodeStatus.Success => new Vector4(0.2f, 1f, 0.2f, 1f),  // 绿色
            NodeStatus.Failure => new Vector4(1f, 0.2f, 0.2f, 1f),  // 红色
            _ => new Vector4(1f, 1f, 1f, 1f)  // 白色
        };

        ImGui.PushStyleColor(ImGuiCol.Text, color);
        bool isOpen = ImGui.TreeNodeEx($"{icon} {node.DisplayName}##{node.Id}", flags);
        ImGui.PopStyleColor();

        // 点击选中
        if (ImGui.IsItemClicked())
        {
            _selectedNode = node;
        }

        if (isOpen)
        {
            _expandedNodeIds.Add(node.Id);
        }
        else
        {
            _expandedNodeIds.Remove(node.Id);
        }

        // TODO: 拖拽功能待实现（需要适配 ImGui API 版本）
        // 暂时使用复制/移动按钮替代

        // 绘制子节点
        if (isOpen)
        {
            foreach (var child in node.Children)
            {
                DrawNodeTree(child, depth + 1);
            }

            ImGui.TreePop();
        }
    }

    /// <summary>
    /// 绘制属性面板
    /// </summary>
    private void DrawPropertiesPanel()
    {
        if (_selectedNode == null)
        {
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "请选择一个节点");
            return;
        }

        ImGui.Text("节点属性");
        ImGui.Separator();

        // 基础属性
        ImGui.Text($"类型: {_selectedNode.Type}");
        ImGui.Text($"状态: {_selectedNode.Status}");

        ImGui.Spacing();

        // 显示名称
        var displayName = _selectedNode.DisplayName;
        if (ImGui.InputText("显示名称", ref displayName, 100))
        {
            _selectedNode.DisplayName = displayName;
        }

        // 备注
        var remark = _selectedNode.Remark;
        if (ImGui.InputTextMultiline("备注", ref remark, 500, new Vector2(-1, 80)))
        {
            _selectedNode.Remark = remark;
        }

        // 启用/禁用
        var enabled = _selectedNode.Enabled;
        if (ImGui.Checkbox("启用", ref enabled))
        {
            _selectedNode.Enabled = enabled;
        }

        ImGui.Separator();

        // 根据节点类型绘制特定参数
        DrawNodeSpecificProperties(_selectedNode);
    }

    /// <summary>
    /// 绘制节点特定属性
    /// </summary>
    private void DrawNodeSpecificProperties(TimelineNode node)
    {
        switch (node.Type)
        {
            case NodeType.Loop:
                DrawLoopProperties(node);
                break;

            case NodeType.Delay:
                DrawDelayProperties(node);
                break;

            case NodeType.Condition:
                DrawConditionProperties(node);
                break;

            case NodeType.Action:
                DrawActionProperties(node);
                break;
        }
    }

    /// <summary>
    /// 绘制循环节点属性
    /// </summary>
    private void DrawLoopProperties(TimelineNode node)
    {
        ImGui.Text("循环设置");

        int loopCount = node.Parameters.GetValueOrDefault(LoopNodeParams.LoopCount, 1) is int lc ? lc : 1;
        if (ImGui.InputInt("循环次数", ref loopCount))
        {
            node.Parameters[LoopNodeParams.LoopCount] = Math.Max(1, loopCount);
        }
    }

    /// <summary>
    /// 绘制延迟节点属性
    /// </summary>
    private void DrawDelayProperties(TimelineNode node)
    {
        ImGui.Text("延迟设置");

        float delaySeconds = node.Parameters.GetValueOrDefault(DelayNodeParams.DelaySeconds, 0f) is float d ? d : 0f;
        if (ImGui.InputFloat("延迟时间（秒）", ref delaySeconds, 0.1f, 1f))
        {
            node.Parameters[DelayNodeParams.DelaySeconds] = Math.Max(0f, delaySeconds);
        }
    }

    /// <summary>
    /// 绘制条件节点属性
    /// </summary>
    private void DrawConditionProperties(TimelineNode node)
    {
        ImGui.Text("条件设置");

        // TODO: 实现条件类型选择和参数编辑
        ImGui.TextColored(new Vector4(1f, 1f, 0f, 1f), "条件编辑器开发中...");
    }

    /// <summary>
    /// 绘制动作节点属性
    /// </summary>
    private void DrawActionProperties(TimelineNode node)
    {
        ImGui.Text("动作设置");

        // TODO: 实现动作类型选择和参数编辑
        ImGui.TextColored(new Vector4(1f, 1f, 0f, 1f), "动作编辑器开发中...");
    }

    /// <summary>
    /// 添加节点
    /// </summary>
    private void AddNode(NodeType type)
    {
        if (_currentTimeline == null)
            return;

        // 如果没有选中节点，添加到根节点
        var parent = _selectedNode ?? _currentTimeline.RootNode;

        // 创建新节点
        TimelineNode newNode = type switch
        {
            NodeType.Parallel => TimelineNodeFactory.CreateParallel(),
            NodeType.Sequence => TimelineNodeFactory.CreateSequence(),
            NodeType.Loop => TimelineNodeFactory.CreateLoop("循环", 1),
            NodeType.Delay => TimelineNodeFactory.CreateDelay(1.0f),
            NodeType.Script => TimelineNodeFactory.CreateScript("脚本节点"),
            NodeType.Condition => TimelineNodeFactory.CreateCondition_GameTime("条件", 0),
            NodeType.Action => TimelineNodeFactory.CreateAction_Command("", "动作节点"),
            _ => new TimelineNode { Type = type, DisplayName = "新节点" }
        };

        parent.AddChild(newNode);
        _selectedNode = newNode;
        ExpandNodePath(parent);

        ImGui.CloseCurrentPopup();
    }

    /// <summary>
    /// 加载时间轴
    /// </summary>
    private void LoadTimeline(string filePath)
    {
        var timeline = _timelineManager.LoadTimeline(filePath);
        if (timeline != null)
        {
            _currentTimeline = timeline;
            _selectedNode = timeline.RootNode;
            ResetExpansionState(timeline);
        }
    }

    /// <summary>
    /// 保存当前时间轴
    /// </summary>
    private void SaveCurrentTimeline()
    {
        if (_currentTimeline != null)
        {
            _timelineManager.SaveTimeline(_currentTimeline);
            _needRefreshList = true;
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _executor.Dispose();
        LogHelper.Print("[时间轴编辑器] 已释放资源");
    }

    /// <summary>Clear cached expansion state and keep root expanded.</summary>
    private void ResetExpansionState(Timeline timeline)
    {
        _expandedNodeIds.Clear();
        _expandedNodeIds.Add(timeline.RootNode.Id);
    }

    /// <summary>Ensure timeline path that contains this node stays expanded.</summary>
    private void ExpandNodePath(TimelineNode node)
    {
        if (_currentTimeline == null)
            return;

        var current = node;
        while (current != null)
        {
            _expandedNodeIds.Add(current.Id);
            current = _currentTimeline.GetParentNode(current);
        }
    }
}
