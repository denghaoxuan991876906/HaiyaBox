using System.Numerics;
using HaiyaBox.Plugin;

namespace HaiyaBox.Utils;

public class XszRemote
{
    /// <summary>
    /// XSZToolboxIpc的静态实例
    /// </summary>
    public static XSZToolboxIpc? Instance { get; internal set; }

    /// <summary>
    /// 获取当前房间ID
    /// </summary>
    /// <returns>房间ID，如果未连接则返回null</returns>
    public static string? GetRoomId()
    {
        return Instance?.GetRoomId();
    }

    /// <summary>
    /// 检查是否已连接到远程控制服务
    /// </summary>
    /// <returns>是否已连接</returns>
    public static bool IsConnected()
    {
        return Instance?.IsConnected() ?? false;
    }

    /// <summary>
    /// 设置指定角色的位置
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="pos">目标位置</param>
    public static void SetPos(string role, Vector3 pos)
    {
        Instance?.SetPos(role, pos);
    }

    /// <summary>
    /// 锁定指定角色的位置
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="pos">锁定位置</param>
    /// <param name="duration">锁定持续时间(毫秒)</param>
    public static void LockPos(string role, Vector3 pos, int duration)
    {
        Instance?.LockPos(role, pos, duration);
    }

    /// <summary>
    /// 滑动传送指定角色到目标位置
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="pos">目标位置</param>
    /// <param name="time">滑动时间(毫秒)</param>
    public static void SlideTp(string role, Vector3 pos, long time)
    {
        Instance?.SlideTp(role, pos, time);
    }

    /// <summary>
    /// 延迟移动指定角色到目标位置
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="pos">目标位置</param>
    /// <param name="battleTimeMs">目标战斗时间(毫秒)</param>
    public static void MoveManaged(string role, Vector3 pos, int battleTimeMs)
    {
        Instance?.MoveManaged(role, pos, battleTimeMs);
    }

    /// <summary>
    /// 延迟传送指定角色到目标位置
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="pos">目标位置</param>
    /// <param name="battleTimeMs">目标战斗时间(毫秒)</param>
    public static void SetPosManaged(string role, Vector3 pos, int battleTimeMs)
    {
        Instance?.SetPosManaged(role, pos, battleTimeMs);
    }

    /// <summary>
    /// 设置集合信息
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="assembleMode">集合模式</param>
    /// <param name="assemblePoint">集合预留点</param>
    public static void SetMoveAssemble(string role, string assembleMode, Vector3 assemblePoint)
    {
        Instance?.SetMoveAssemble(role, assembleMode, assemblePoint);
    }

    /// <summary>
    /// 设置集合补偿时间
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="delayMs">补偿时间(毫秒)</param>
    public static void SetMoveAssembleDelay(string role, int delayMs)
    {
        Instance?.SetMoveAssembleDelay(role, delayMs);
    }

    /// <summary>
    /// 设置指定角色的旋转角度
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="rot">旋转角度</param>
    public static void SetRot(string role, float rot)
    {
        Instance?.SetRot(role, rot);
    }

    /// <summary>
    /// 移动指定角色到目标位置
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="pos">目标位置</param>
    public static void MoveTo(string role, Vector3 pos)
    {
        Instance?.MoveTo(role, pos);
    }

    /// <summary>
    /// 停止指定角色的移动
    /// </summary>
    /// <param name="role">角色名称</param>
    public static void MoveStop(string role)
    {
        Instance?.MoveStop(role);
    }

    /// <summary>
    /// 停止指定角色的所有操作
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="stop">是否停止</param>
    public static void Stop(string role, bool stop)
    {
        Instance?.Stop(role, stop);
    }

    /// <summary>
    /// 让指定角色跳跃
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="jump">是否跳跃</param>
    public static void Jump(string role, bool jump)
    {
        Instance?.Jump(role, jump);
    }

    /// <summary>
    /// 让指定角色使用技能
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="skillId">技能ID</param>
    public static void UseSkill(string role, uint skillId)
    {
        Instance?.UseSkill(role, skillId);
    }

    /// <summary>
    /// 让指定角色对目标使用技能
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="skillId">技能ID</param>
    /// <param name="targetId">目标ID</param>
    public static void UseSkillWithTarget(string role, uint skillId, uint targetId)
    {
        Instance?.UseSkillWithTarget(role, skillId, targetId);
    }

    /// <summary>
    /// 设置指定角色的目标
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="targetId">目标ID</param>
    public static void SetTarget(string role, uint targetId)
    {
        Instance?.SetTarget(role, targetId);
    }

    /// <summary>
    /// 向指定角色发送回声消息
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="msg">消息内容</param>
    public static void Echo(string role, string msg)
    {
        Instance?.Echo(role, msg);
    }

    /// <summary>
    /// 在指定角色上执行命令
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="cmd">命令内容</param>
    public static void Cmd(string role, string cmd)
    {
        Instance?.Cmd(role, cmd);
    }

    /// <summary>
    /// 踢出指定角色
    /// </summary>
    /// <param name="role">角色名称</param>
    public static void Kick(string role)
    {
        Instance?.Kick(role);
    }

    /// <summary>
    /// 设置指定角色的角色
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="newRole">新角色名称</param>
    public static void SetRole(string role, string newRole)
    {
        Instance?.SetRole(role, newRole);
    }

    /// <summary>
    /// 根据玩家名称获取角色
    /// </summary>
    /// <param name="playerName">玩家名称</param>
    /// <returns>角色名称，如果未找到则返回null</returns>
    public static string? GetRoleByPlayerName(string playerName)
    {
        return Instance?.GetRoleByPlayerName(playerName);
    }

    /// <summary>
    /// 根据玩家CID获取角色
    /// </summary>
    /// <param name="playerCid">玩家CID</param>
    /// <returns>角色名称，如果未找到则返回null</returns>
    public static string? GetRoleByPlayerCID(string playerCid)
    {
        return Instance?.GetRoleByPlayerCID(playerCid);
    }

    /// <summary>
    /// 获取成员总数
    /// </summary>
    /// <returns>成员总数</returns>
    public static int GetMemberCount()
    {
        return Instance?.GetMemberCount() ?? 0;
    }

    /// <summary>
    /// 获取在线成员数量
    /// </summary>
    /// <returns>在线成员数量</returns>
    public static int GetOnlineMemberCount()
    {
        return Instance?.GetOnlineMemberCount() ?? 0;
    }
}
