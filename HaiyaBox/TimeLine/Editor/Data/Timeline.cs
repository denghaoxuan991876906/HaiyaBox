using System;
using System.Collections.Generic;

namespace HaiyaBox.TimeLine.Editor.Data;

/// <summary>
/// 时间轴数据结构
/// </summary>
public class Timeline
{
    /// <summary>时间轴唯一标识符</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>时间轴名称</summary>
    public string Name { get; set; } = "新时间轴";

    /// <summary>副本ID</summary>
    public uint DutyId { get; set; }

    /// <summary>副本名称</summary>
    public string DutyName { get; set; } = string.Empty;

    /// <summary>作者</summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>版本</summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>描述</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>最后修改时间</summary>
    public DateTime ModifiedAt { get; set; } = DateTime.Now;

    /// <summary>根节点（始终为并行节点）</summary>
    public TimelineNode RootNode { get; set; }

    /// <summary>标签（用于分类和搜索）</summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// 构造函数 - 初始化根节点
    /// </summary>
    public Timeline()
    {
        RootNode = new TimelineNode
        {
            Type = NodeType.Parallel,
            DisplayName = "根节点",
            Remark = "时间轴根节点，自动创建"
        };
    }

    /// <summary>
    /// 查找节点（深度优先搜索）
    /// </summary>
    public TimelineNode? FindNode(string nodeId)
    {
        return FindNodeRecursive(RootNode, nodeId);
    }

    private TimelineNode? FindNodeRecursive(TimelineNode node, string nodeId)
    {
        if (node.Id == nodeId)
            return node;

        foreach (var child in node.Children)
        {
            var found = FindNodeRecursive(child, nodeId);
            if (found != null)
                return found;
        }

        return null;
    }

    /// <summary>
    /// 获取所有节点（平铺列表）
    /// </summary>
    public List<TimelineNode> GetAllNodes()
    {
        var nodes = new List<TimelineNode>();
        CollectNodesRecursive(RootNode, nodes);
        return nodes;
    }

    private void CollectNodesRecursive(TimelineNode node, List<TimelineNode> collection)
    {
        collection.Add(node);
        foreach (var child in node.Children)
        {
            CollectNodesRecursive(child, collection);
        }
    }

    /// <summary>
    /// 获取父节点
    /// </summary>
    public TimelineNode? GetParentNode(TimelineNode node)
    {
        if (node.ParentId == null)
            return null;

        return FindNode(node.ParentId);
    }

    /// <summary>
    /// 获取父节点
    /// </summary>
    public TimelineNode? GetParentNode(string nodeId)
    {
        var node = FindNode(nodeId);
        return node != null ? GetParentNode(node) : null;
    }

    /// <summary>
    /// 删除节点（及其所有子节点）
    /// </summary>
    public bool DeleteNode(string nodeId)
    {
        // 不能删除根节点
        if (nodeId == RootNode.Id)
            return false;

        var node = FindNode(nodeId);
        if (node == null)
            return false;

        var parent = GetParentNode(node);
        if (parent == null)
            return false;

        return parent.RemoveChild(node);
    }

    /// <summary>
    /// 移动节点到新父节点
    /// </summary>
    public bool MoveNode(string nodeId, string newParentId, int index = -1)
    {
        // 不能移动根节点
        if (nodeId == RootNode.Id)
            return false;

        var node = FindNode(nodeId);
        var newParent = FindNode(newParentId);

        if (node == null || newParent == null)
            return false;

        // 检查是否尝试移动到自己的子节点（会造成循环引用）
        if (IsDescendant(newParent, node))
            return false;

        // 从原父节点移除
        var oldParent = GetParentNode(node);
        oldParent?.RemoveChild(node);

        // 添加到新父节点
        node.ParentId = newParentId;
        if (index >= 0 && index < newParent.Children.Count)
        {
            newParent.Children.Insert(index, node);
        }
        else
        {
            newParent.Children.Add(node);
        }

        ModifiedAt = DateTime.Now;
        return true;
    }

    /// <summary>
    /// 检查 possibleDescendant 是否是 ancestor 的后代
    /// </summary>
    private bool IsDescendant(TimelineNode possibleDescendant, TimelineNode ancestor)
    {
        var current = possibleDescendant;
        while (current.ParentId != null)
        {
            if (current.ParentId == ancestor.Id)
                return true;

            current = FindNode(current.ParentId);
            if (current == null)
                break;
        }
        return false;
    }

    /// <summary>
    /// 复制节点
    /// </summary>
    public TimelineNode? DuplicateNode(string nodeId)
    {
        var node = FindNode(nodeId);
        if (node == null || node == RootNode)
            return null;

        var parent = GetParentNode(node);
        if (parent == null)
            return null;

        var clone = node.Clone();
        parent.AddChild(clone);

        ModifiedAt = DateTime.Now;
        return clone;
    }

    /// <summary>
    /// 验证时间轴数据完整性
    /// </summary>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("时间轴名称不能为空");

        if (RootNode == null)
            errors.Add("缺少根节点");
        else
            ValidateNodeRecursive(RootNode, errors);

        return errors;
    }

    private void ValidateNodeRecursive(TimelineNode node, List<string> errors)
    {
        // 验证节点基本信息
        if (string.IsNullOrWhiteSpace(node.DisplayName))
            errors.Add($"节点 {node.Id} 缺少显示名称");

        // 验证特定节点类型的参数
        switch (node.Type)
        {
            case NodeType.Loop:
                if (!node.Parameters.ContainsKey(LoopNodeParams.LoopCount) ||
                    !int.TryParse(node.Parameters[LoopNodeParams.LoopCount]?.ToString(), out int loopCount) ||
                    loopCount <= 0)
                {
                    errors.Add($"循环节点 '{node.DisplayName}' 缺少有效的循环次数");
                }
                break;

            case NodeType.Delay:
                if (!node.Parameters.ContainsKey(DelayNodeParams.DelaySeconds) ||
                    !float.TryParse(node.Parameters[DelayNodeParams.DelaySeconds]?.ToString(), out float delay) ||
                    delay < 0)
                {
                    errors.Add($"延迟节点 '{node.DisplayName}' 缺少有效的延迟时间");
                }
                break;

            case NodeType.Script:
                if (!node.Parameters.ContainsKey(ScriptNodeParams.ScriptCode) ||
                    string.IsNullOrWhiteSpace(node.Parameters[ScriptNodeParams.ScriptCode]?.ToString()))
                {
                    errors.Add($"脚本节点 '{node.DisplayName}' 缺少脚本代码");
                }
                break;
        }

        // 递归验证子节点
        foreach (var child in node.Children)
        {
            ValidateNodeRecursive(child, errors);
        }
    }
}
