using System.Numerics;
using AEAssist;
using AEAssist.Helper;
using HaiyaBox.Settings;

namespace HaiyaBox.Utils;

public static class RemoteControl
{
    public static string GetRoomId => FullAutoSettings.Instance.AutomationSettings.XszRemoteEnabled ? XszRemote.GetRoomId() : RemoteControlHelper.RoomId;
    
    public static bool IsConnected() => FullAutoSettings.Instance.AutomationSettings.XszRemoteEnabled ? XszRemote.IsConnected() : true;
    
    public static void SetPos(string role, Vector3 pos) 
    {
        if (FullAutoSettings.Instance.AutomationSettings.XszRemoteEnabled)
            XszRemote.SetPos(role, pos);
        else
            RemoteControlHelper.SetPos(role, pos);
        DebugPoint.Add(pos);
    }
    
    public static void LockPos(string role, Vector3 pos, int duration) 
    {
        if (FullAutoSettings.Instance.AutomationSettings.XszRemoteEnabled)
            XszRemote.LockPos(role, pos, duration);
        else
            RemoteControlHelper.LockPos(role, pos, duration);
    }
    
    public static void SlideTp(string role, Vector3 pos, long time) 
    {
        if (FullAutoSettings.Instance.AutomationSettings.XszRemoteEnabled)
            XszRemote.SlideTp(role, pos, time);
        else 
            RemoteControlHelper.SlideTp(role, pos, time);
    }
    
    public static void SetRot(string role, float rot) 
    {
        if (FullAutoSettings.Instance.AutomationSettings.XszRemoteEnabled)
            XszRemote.SetRot(role, rot);
        else 
            RemoteControlHelper.SetRot(role, rot);
    }
    
    public static void MoveTo(string role, Vector3 pos) 
    {
        if (FullAutoSettings.Instance.AutomationSettings.XszRemoteEnabled)
            XszRemote.MoveTo(role, pos);
        else
            RemoteControlHelper.MoveTo(role, pos);
    }
    
    public static void MoveStop(string role) 
    {
        if (FullAutoSettings.Instance.AutomationSettings.XszRemoteEnabled)
            XszRemote.MoveStop(role);
    }
    
    public static void Stop(string role, bool stop) 
    {
        if (FullAutoSettings.Instance.AutomationSettings.XszRemoteEnabled)
            XszRemote.Stop(role, stop);
        else
            RemoteControlHelper.Stop(role, stop);
    }
    
    public static void Jump(string role, bool jump) 
    {
        if (FullAutoSettings.Instance.AutomationSettings.XszRemoteEnabled)
            XszRemote.Jump(role, jump);
        else 
            RemoteControlHelper.Jump(role, jump);
    }
    
    public static void UseSkill(string role, uint skillId) 
    {
        if (skillId == 0)
            return;
        if (FullAutoSettings.Instance.AutomationSettings.XszRemoteEnabled)
        {
            XszRemote.UseSkill(role, skillId);
            ChatHelper.SendMessage($"/p {role}使用技能{skillId.GetSpell().Name}:{skillId}");
        }
        else
        {
            RemoteControlHelper.UseSkill(role, skillId);
            ChatHelper.SendMessage($"/p {role}使用技能{skillId.GetSpell().Name}:{skillId}");
        }
    }
    
    public static void UseSkillWithTarget(string role, uint skillId, uint targetId) 
    {
        if (FullAutoSettings.Instance.AutomationSettings.XszRemoteEnabled)
            XszRemote.UseSkillWithTarget(role, skillId, targetId);
    }
    
    public static void SetTarget(string role, uint targetId) 
    {
        if (FullAutoSettings.Instance.AutomationSettings.XszRemoteEnabled)
            XszRemote.SetTarget(role, targetId);
        else 
            RemoteControlHelper.SetTarget(role, targetId);
    }
    
    public static void Echo(string role, string msg) 
    {
        if (FullAutoSettings.Instance.AutomationSettings.XszRemoteEnabled)
            XszRemote.Echo(role, msg);
        else RemoteControlHelper.Echo(role, msg);
    }
    
    public static void Cmd(string role, string cmd) 
    {
        if (FullAutoSettings.Instance.AutomationSettings.XszRemoteEnabled)
            XszRemote.Cmd(role, cmd);
        else
            RemoteControlHelper.Cmd(role, cmd);
    }
    
    public static void Kick(string role) 
    {
        if (FullAutoSettings.Instance.AutomationSettings.XszRemoteEnabled)
            XszRemote.Kick(role);
        else 
            RemoteControlHelper.Kick(role);
    }
    
    public static void SetRole(string role, string newRole) 
    {
        if (FullAutoSettings.Instance.AutomationSettings.XszRemoteEnabled)
            XszRemote.SetRole(role, newRole);
        else 
            RemoteControlHelper.SetRole(role, newRole);
    }
    
    public static string? GetRoleByPlayerName(string playerName) 
    {
        if (FullAutoSettings.Instance.AutomationSettings.XszRemoteEnabled)
            return XszRemote.GetRoleByPlayerName(playerName);
        else
            return RemoteControlHelper.GetRoleByPlayerName(playerName);
    }
    
    public static string? GetRoleByPlayerCID(string playerCid) 
    {
        if (FullAutoSettings.Instance.AutomationSettings.XszRemoteEnabled)
            return XszRemote.GetRoleByPlayerCID(playerCid);
        else 
            return RemoteControlHelper.GetRoleByPlayerCID(playerCid);
    }
    
    public static int GetMemberCount() 
    {
        if (FullAutoSettings.Instance.AutomationSettings.XszRemoteEnabled)
            return XszRemote.GetMemberCount();
        // RemoteControlHelper doesn't seem to have GetMemberCount, so we'll just use XszRemote when enabled
        return 0;
    }
    
    public static int GetOnlineMemberCount() 
    {
        if (FullAutoSettings.Instance.AutomationSettings.XszRemoteEnabled)
            return XszRemote.GetOnlineMemberCount();
        // RemoteControlHelper doesn't seem to have GetOnlineMemberCount, so we'll just use XszRemote when enabled
        return 0;
    }
}