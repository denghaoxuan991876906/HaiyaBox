﻿using AEAssist.AEPlugin;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using System.Numerics;
using ECommons.DalamudServices;

namespace HaiyaBox.Plugin;

/// <summary>
    /// XSZToolbox IPC客户端实现
    /// </summary>
    public class XSZToolboxIpc : IDisposable
    {
        private bool _disposedValue;

        /// <summary>
        /// 房间ID订阅者
        /// </summary>
        private ICallGateSubscriber<string?> _roomIdSubscriber;

        /// <summary>
        /// 连接状态订阅者
        /// </summary>
        private ICallGateSubscriber<bool> _isConnectedSubscriber;

        /// <summary>
        /// 设置位置订阅者
        /// </summary>
        private ICallGateSubscriber<string, Vector3, object> _setPosSubscriber;

        /// <summary>
        /// 锁定位置订阅者
        /// </summary>
        private ICallGateSubscriber<string, Vector3, int, object> _lockPosSubscriber;

        /// <summary>
        /// 滑动传送订阅者
        /// </summary>
        private ICallGateSubscriber<string, Vector3, long, object> _slideTpSubscriber;

        /// <summary>
        /// 延迟移动订阅者
        /// </summary>
        private ICallGateSubscriber<string, Vector3, long, object> _moveManagedSubscriber;

        /// <summary>
        /// 延迟传送订阅者
        /// </summary>
        private ICallGateSubscriber<string, Vector3, long, object> _setPosManagedSubscriber;

        /// <summary>
        /// 设置集合信息订阅者
        /// </summary>
        private ICallGateSubscriber<string, string, Vector3, object> _setMoveAssembleSubscriber;

        /// <summary>
        /// 设置集合补偿时间订阅者
        /// </summary>
        private ICallGateSubscriber<string, int, object> _setMoveAssembleDelaySubscriber;

        /// <summary>
        /// 设置旋转订阅者
        /// </summary>
        private ICallGateSubscriber<string, float, object> _setRotSubscriber;

        /// <summary>
        /// 移动到位置订阅者
        /// </summary>
        private ICallGateSubscriber<string, Vector3, object> _moveToSubscriber;

        /// <summary>
        /// 停止移动订阅者
        /// </summary>
        private ICallGateSubscriber<string, object> _moveStopSubscriber;

        /// <summary>
        /// 停止所有动作订阅者
        /// </summary>
        private ICallGateSubscriber<string, bool, object> _stopSubscriber;

        /// <summary>
        /// 跳跃订阅者
        /// </summary>
        private ICallGateSubscriber<string, bool, object> _jumpSubscriber;

        /// <summary>
        /// 使用技能订阅者
        /// </summary>
        private ICallGateSubscriber<string, uint, object> _useSkillSubscriber;

        /// <summary>
        /// 使用技能(带目标)订阅者
        /// </summary>
        private ICallGateSubscriber<string, uint, uint, object> _useSkillWithTargetSubscriber;

        /// <summary>
        /// 设置目标订阅者
        /// </summary>
        private ICallGateSubscriber<string, uint, object> _setTargetSubscriber;

        /// <summary>
        /// 发送消息订阅者
        /// </summary>
        private ICallGateSubscriber<string, string, object> _echoSubscriber;

        /// <summary>
        /// 执行命令订阅者
        /// </summary>
        private ICallGateSubscriber<string, string, object> _cmdSubscriber;

        /// <summary>
        /// 踢出房间订阅者
        /// </summary>
        private ICallGateSubscriber<string, object> _kickSubscriber;

        /// <summary>
        /// 设置角色订阅者
        /// </summary>
        private ICallGateSubscriber<string, string, object> _setRoleSubscriber;

        /// <summary>
        /// 通过玩家名称获取角色订阅者
        /// </summary>
        private ICallGateSubscriber<string, string?> _getRoleByPlayerNameSubscriber;

        /// <summary>
        /// 通过玩家CID获取角色订阅者
        /// </summary>
        private ICallGateSubscriber<string, string?> _getRoleByPlayerCidSubscriber;

        /// <summary>
        /// 获取成员数量订阅者
        /// </summary>
        private ICallGateSubscriber<int> _getMemberCountSubscriber;

        /// <summary>
        /// 获取在线成员数量订阅者
        /// </summary>
        private ICallGateSubscriber<int> _getOnlineMemberCountSubscriber;

        /// <summary>
        /// 构造函数
        /// </summary>
        public XSZToolboxIpc()
        {
            InitializeSubscribers();
        }

    /// <summary>
        /// 初始化所有IPC订阅者
        /// </summary>
        private void InitializeSubscribers()
        {
            _roomIdSubscriber = Svc.PluginInterface.GetIpcSubscriber<string?>("XSZToolbox.RemoteControl.GetRoomId");
            _isConnectedSubscriber = Svc.PluginInterface.GetIpcSubscriber<bool>("XSZToolbox.RemoteControl.IsConnected");
            _setPosSubscriber = Svc.PluginInterface.GetIpcSubscriber<string, Vector3, object>("XSZToolbox.RemoteControl.SetPos");
            _lockPosSubscriber = Svc.PluginInterface.GetIpcSubscriber<string, Vector3, int, object>("XSZToolbox.RemoteControl.LockPos");
            _slideTpSubscriber = Svc.PluginInterface.GetIpcSubscriber<string, Vector3, long, object>("XSZToolbox.RemoteControl.SlideTp");
            _moveManagedSubscriber = Svc.PluginInterface.GetIpcSubscriber<string, Vector3, long, object>("XSZToolbox.RemoteControl.MoveManaged");
            _setPosManagedSubscriber = Svc.PluginInterface.GetIpcSubscriber<string, Vector3, long, object>("XSZToolbox.RemoteControl.SetPosManaged");
            _setMoveAssembleSubscriber = Svc.PluginInterface.GetIpcSubscriber<string, string, Vector3, object>("XSZToolbox.RemoteControl.SetMoveAssemble");
            _setMoveAssembleDelaySubscriber = Svc.PluginInterface.GetIpcSubscriber<string, int, object>("XSZToolbox.RemoteControl.SetMoveAssembleDelay");
            _setRotSubscriber = Svc.PluginInterface.GetIpcSubscriber<string, float, object>("XSZToolbox.RemoteControl.SetRot");
            _moveToSubscriber = Svc.PluginInterface.GetIpcSubscriber<string, Vector3, object>("XSZToolbox.RemoteControl.MoveTo");
            _moveStopSubscriber = Svc.PluginInterface.GetIpcSubscriber<string, object>("XSZToolbox.RemoteControl.MoveStop");
            _stopSubscriber = Svc.PluginInterface.GetIpcSubscriber<string, bool, object>("XSZToolbox.RemoteControl.Stop");
            _jumpSubscriber = Svc.PluginInterface.GetIpcSubscriber<string, bool, object>("XSZToolbox.RemoteControl.Jump");
            _useSkillSubscriber = Svc.PluginInterface.GetIpcSubscriber<string, uint, object>("XSZToolbox.RemoteControl.UseSkill");
            _useSkillWithTargetSubscriber = Svc.PluginInterface.GetIpcSubscriber<string, uint, uint, object>("XSZToolbox.RemoteControl.UseSkillWithTarget");
            _setTargetSubscriber = Svc.PluginInterface.GetIpcSubscriber<string, uint, object>("XSZToolbox.RemoteControl.SetTarget");
            _echoSubscriber = Svc.PluginInterface.GetIpcSubscriber<string, string, object>("XSZToolbox.RemoteControl.Echo");
            _cmdSubscriber = Svc.PluginInterface.GetIpcSubscriber<string, string, object>("XSZToolbox.RemoteControl.Cmd");
            _kickSubscriber = Svc.PluginInterface.GetIpcSubscriber<string, object>("XSZToolbox.RemoteControl.Kick");
            _setRoleSubscriber = Svc.PluginInterface.GetIpcSubscriber<string, string, object>("XSZToolbox.RemoteControl.SetRole");
            _getRoleByPlayerNameSubscriber = Svc.PluginInterface.GetIpcSubscriber<string, string?>("XSZToolbox.RemoteControl.GetRoleByPlayerName");
            _getRoleByPlayerCidSubscriber = Svc.PluginInterface.GetIpcSubscriber<string, string?>("XSZToolbox.RemoteControl.GetRoleByPlayerCID");
            _getMemberCountSubscriber = Svc.PluginInterface.GetIpcSubscriber<int>("XSZToolbox.RemoteControl.GetMemberCount");
            _getOnlineMemberCountSubscriber = Svc.PluginInterface.GetIpcSubscriber<int>("XSZToolbox.RemoteControl.GetOnlineMemberCount");
        }

    /// <summary>
    /// 获取当前房间ID
    /// </summary>
    /// <returns>房间ID，如果未连接则返回null</returns>
    public string? GetRoomId()
    {
        try
        {
            return _roomIdSubscriber?.InvokeFunc();
        }
        catch (Dalamud.Plugin.Ipc.Exceptions.IpcNotReadyError)
        {
            return null;
        }
    }

    /// <summary>
    /// 检查是否已连接到远程控制服务
    /// </summary>
    /// <returns>是否已连接</returns>
    public bool IsConnected()
    {
        try
        {
            return _isConnectedSubscriber?.InvokeFunc() ?? false;
        }
        catch (Dalamud.Plugin.Ipc.Exceptions.IpcNotReadyError)
        {
            return false;
        }
    }

    /// <summary>
    /// 设置指定角色的位置
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="pos">目标位置</param>
    public void SetPos(string role, Vector3 pos)
    {
        _setPosSubscriber?.InvokeAction(role, pos);
    }

    /// <summary>
    /// 锁定指定角色的位置
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="pos">锁定位置</param>
    /// <param name="duration">锁定持续时间(毫秒)</param>
    public void LockPos(string role, Vector3 pos, int duration)
    {
        _lockPosSubscriber?.InvokeAction(role, pos, duration);
    }

    /// <summary>
    /// 滑动传送指定角色到目标位置
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="pos">目标位置</param>
    /// <param name="time">滑动时间(毫秒)</param>
        public void SlideTp(string role, Vector3 pos, long time)
        {
            _slideTpSubscriber?.InvokeAction(role, pos, time);
        }

        /// <summary>
        /// 延迟移动指定角色到目标位置
        /// </summary>
        /// <param name="role">角色名称</param>
        /// <param name="pos">目标位置</param>
        /// <param name="battleTimeMs">目标战斗时间(毫秒)</param>
        public void MoveManaged(string role, Vector3 pos, long battleTimeMs)
        {
            _moveManagedSubscriber?.InvokeAction(role, pos, battleTimeMs);
        }

        /// <summary>
        /// 延迟传送指定角色到目标位置
        /// </summary>
        /// <param name="role">角色名称</param>
        /// <param name="pos">目标位置</param>
        /// <param name="battleTimeMs">目标战斗时间(毫秒)</param>
        public void SetPosManaged(string role, Vector3 pos, long battleTimeMs)
        {
            _setPosManagedSubscriber?.InvokeAction(role, pos, battleTimeMs);
        }

        /// <summary>
        /// 设置集合信息
        /// </summary>
        /// <param name="role">角色名称</param>
        /// <param name="assembleMode">集合模式</param>
        /// <param name="assemblePoint">集合预留点</param>
        public void SetMoveAssemble(string role, string assembleMode, Vector3 assemblePoint)
        {
            _setMoveAssembleSubscriber?.InvokeAction(role, assembleMode, assemblePoint);
        }

        /// <summary>
        /// 设置集合补偿时间
        /// </summary>
        /// <param name="role">角色名称</param>
        /// <param name="delayMs">补偿时间(毫秒)</param>
        public void SetMoveAssembleDelay(string role, int delayMs)
        {
            _setMoveAssembleDelaySubscriber?.InvokeAction(role, delayMs);
        }

    /// <summary>
    /// 设置指定角色的旋转角度
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="rot">旋转角度</param>
    public void SetRot(string role, float rot)
    {
        _setRotSubscriber?.InvokeAction(role, rot);
    }

    /// <summary>
    /// 移动指定角色到目标位置
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="pos">目标位置</param>
    public void MoveTo(string role, Vector3 pos)
    {
        _moveToSubscriber?.InvokeAction(role, pos);
    }

    /// <summary>
    /// 停止指定角色的移动
    /// </summary>
    /// <param name="role">角色名称</param>
    public void MoveStop(string role)
    {
        _moveStopSubscriber?.InvokeAction(role);
    }

    /// <summary>
    /// 停止指定角色的所有操作
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="stop">是否停止</param>
    public void Stop(string role, bool stop)
    {
        _stopSubscriber?.InvokeAction(role, stop);
    }

    /// <summary>
    /// 让指定角色跳跃
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="jump">是否跳跃</param>
    public void Jump(string role, bool jump)
    {
        _jumpSubscriber?.InvokeAction(role, jump);
    }

    /// <summary>
    /// 让指定角色使用技能
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="skillId">技能ID</param>
    public void UseSkill(string role, uint skillId)
    {
        _useSkillSubscriber?.InvokeAction(role, skillId);
    }

    /// <summary>
    /// 让指定角色对目标使用技能
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="skillId">技能ID</param>
    /// <param name="targetId">目标ID</param>
    public void UseSkillWithTarget(string role, uint skillId, uint targetId)
    {
        _useSkillWithTargetSubscriber?.InvokeAction(role, skillId, targetId);
    }

    /// <summary>
    /// 设置指定角色的目标
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="targetId">目标ID</param>
    public void SetTarget(string role, uint targetId)
    {
        _setTargetSubscriber?.InvokeAction(role, targetId);
    }

    /// <summary>
    /// 向指定角色发送回声消息
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="msg">消息内容</param>
    public void Echo(string role, string msg)
    {
        _echoSubscriber?.InvokeAction(role, msg);
    }

    /// <summary>
    /// 在指定角色上执行命令
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="cmd">命令内容</param>
    public void Cmd(string role, string cmd)
    {
        _cmdSubscriber?.InvokeAction(role, cmd);
    }

    /// <summary>
    /// 踢出指定角色
    /// </summary>
    /// <param name="role">角色名称</param>
    public void Kick(string role)
    {
        _kickSubscriber?.InvokeAction(role);
    }

    /// <summary>
    /// 设置指定角色的角色
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="newRole">新角色名称</param>
    public void SetRole(string role, string newRole)
    {
        _setRoleSubscriber?.InvokeAction(role, newRole);
    }

    /// <summary>
    /// 根据玩家名称获取角色
    /// </summary>
    /// <param name="playerName">玩家名称</param>
    /// <returns>角色名称，如果未找到则返回null</returns>
    public string? GetRoleByPlayerName(string playerName)
    {
        return _getRoleByPlayerNameSubscriber?.InvokeFunc(playerName);
    }

    /// <summary>
    /// 根据玩家CID获取角色
    /// </summary>
    /// <param name="playerCid">玩家CID</param>
    /// <returns>角色名称，如果未找到则返回null</returns>
    public string? GetRoleByPlayerCID(string playerCid)
    {
        return _getRoleByPlayerCidSubscriber?.InvokeFunc(playerCid);
    }

    /// <summary>
    /// 获取成员总数
    /// </summary>
    /// <returns>成员总数</returns>
    public int GetMemberCount()
    {
        return _getMemberCountSubscriber?.InvokeFunc() ?? 0;
    }

    /// <summary>
    /// 获取在线成员数量
    /// </summary>
    /// <returns>在线成员数量</returns>
    public int GetOnlineMemberCount()
    {
        return _getOnlineMemberCountSubscriber?.InvokeFunc() ?? 0;
    }

    /// <summary>
    /// 释放所有资源
    /// </summary>
    public void Dispose()
    {
        // IPC订阅者不需要手动注销，Dalamud会自动处理
    }
}
