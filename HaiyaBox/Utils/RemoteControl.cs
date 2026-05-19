using System.Numerics;
using AEAssist.CombatRoutine.Module;
using AEAssist.Helper;

namespace HaiyaBox.Utils;

public static class RemoteControl
{
    public static string GetRoomId => XszRemote.GetRoomId();

    public static bool IsConnected()
    {
        return XszRemote.IsConnected();
    }

    public static void SetPos(string role, Vector3 pos)
    {
        XszRemote.SetPos(role, pos);
        DebugPoint.Add(pos);
    }

    public static void LockPos(string role, Vector3 pos, int duration)
    {
        XszRemote.LockPos(role, pos, duration);
    }

    public static void SlideTp(string role, Vector3 pos, long time)
    {
        XszRemote.SlideTp(role, pos, time);
    }

    public static void SetRot(string role, float rot)
    {
        XszRemote.SetRot(role, rot);
    }

    public static void MoveTo(string role, Vector3 pos)
    {
        XszRemote.MoveTo(role, pos);
    }

    public static void MoveStop(string role)
    {
        XszRemote.MoveStop(role);
    }

    public static void Stop(string role, bool stop)
    {
        XszRemote.Stop(role, stop);
    }

    public static void Jump(string role, bool jump)
    {
        XszRemote.Jump(role, jump);
    }

    public static void UseSkill(string role, uint skillId)
    {
        if (skillId == 0)
            return;
        XszRemote.UseSkill(role, skillId);
        ChatHelper.SendMessage($"/p {role}使用技能{skillId.GetSpell().Name}:{skillId}");
    }

    public static void UseSkillWithTarget(string role, uint skillId, uint targetId)
    {
        XszRemote.UseSkillWithTarget(role, skillId, targetId);
    }

    public static void SetTarget(string role, uint targetId)
    {
        XszRemote.SetTarget(role, targetId);
    }

    public static void Echo(string role, string msg)
    {
        XszRemote.Echo(role, msg);
    }

    public static void Cmd(string role, string cmd)
    {
        XszRemote.Cmd(role, cmd);
    }

    public static void Kick(string role)
    {
        XszRemote.Kick(role);
    }

    public static void SetRole(string role, string newRole)
    {
        XszRemote.SetRole(role, newRole);
    }

    public static string? GetRoleByPlayerName(string playerName)
    {
        return XszRemote.GetRoleByPlayerName(playerName);
    }

    public static string? GetRoleByPlayerCID(string playerCid)
    {
        return XszRemote.GetRoleByPlayerCID(playerCid);
    }

    public static string? GetNameByRole(string role)
    {
        return XszRemote.GetNameByRole(role);
    }

    public static int GetMemberCount()
    {
        return XszRemote.GetMemberCount();
    }

    public static int GetOnlineMemberCount()
    {
        return XszRemote.GetOnlineMemberCount();
    }

    public static async Task SlideMoveDelay(string s, Vector3 pos, long 战斗时间)
    {
        var 当前战斗时间 = AI.Instance.BattleData.CurrBattleTimeInMs;
        await Task.Delay((int)(战斗时间 - 当前战斗时间));
        XszRemote.SetPos(s, pos);
    }
}
