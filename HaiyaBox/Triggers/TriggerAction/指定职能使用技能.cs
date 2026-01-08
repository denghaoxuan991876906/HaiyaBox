using System;
using System.Numerics;
using AEAssist;
using AEAssist.CombatRoutine.Trigger;
using AEAssist.Extension;
using AEAssist.Helper;
using Dalamud.Bindings.ImGui;
using ECommons.DalamudServices;
using HaiyaBox.Utils;

namespace HaiyaBox.Triggers.TriggerAction;

internal class 指定职能使用技能: ITriggerAction
{
    public string DisplayName { get; } = "指定职能使用技能";
    public string Remark { get; set; } = string.Empty; // 为 Remark 提供默认值
    

    public Dictionary<string, bool> roles = new()
    {
        { "MT", false },
        { "ST", false },
        { "H1", false },
        { "H2", false },
        { "D1", false },
        { "D2", false },
        { "D3", false },
        { "D4", false }
    };

    public bool toTarget;
    public uint skillTarget;
    public int selectIndex = 0;
    public uint skillId;

    public bool Draw()
    {
        ImGui.Text("指定职能使用技能");

        foreach (var role in roles.Keys.ToList())
        {
            bool isChecked = roles[role];
            if (ImGui.Checkbox(role, ref isChecked))
            {
                roles[role] = isChecked;
            }
            ImGui.SameLine();
        }
        ImGui.NewLine();
        ImGui.InputUInt("技能id", ref skillId);
        ImGui.Checkbox("指定目标", ref toTarget);
        if (toTarget)
        {
            ImGui.SliderInt("目标选择", ref selectIndex, -1, 15);
            ImGui.Text("1:自己 2:队伍2 3:队伍3 4:队伍4 5:队伍5 6:队伍6 7:队伍7 8:队伍8 9:目标 10:目标的目标 11:填入id");
            ImGui.InputUInt("目标id", ref skillTarget);
        }

        return true;
    }

    public bool Handle()
    {
        uint targetId;
        var party = Svc.Party;
        switch (selectIndex)
        {
            case 1 :
                targetId = 0;
                break;
            case 2 :
                targetId = party[1].ObjectId;
                break;
            case 3:
                targetId = party[2].ObjectId;
                break;
            case 4:
                targetId = party[3].ObjectId;
                break;
            case 5:
                targetId = party[4].ObjectId;
                break;
            case 6:
                targetId = party[5].ObjectId;
                break;
            case 7:
                targetId = party[6].ObjectId;
                break;
            case 8:
                targetId = party[7].ObjectId;
                break;
            case 9:
                targetId = Core.Me.GetCurrTarget().EntityId;
                break;
            case 10:
                targetId = Core.Me.GetCurrTarget().GetCurrTarget().EntityId;
                break;
            case 11:
                targetId = skillTarget;
                break;
            default:
                return true;
        }

        foreach (var role in roles)
        {
            if (role.Value)
            {
                if (toTarget)
                {
                    XszRemote.UseSkillWithTarget(role.Key, skillId, targetId);
                    ChatHelper.SendMessage($"/p 使用技能（指定目标）:{role.Key} 技能id：{skillId} 技能名:{skillId.GetSpell().Name} 目标id:{targetId}");
                }
                else

                {
                    ChatHelper.SendMessage($"/p 使用技能:{role.Key} 技能id：{skillId} 技能名:{skillId.GetSpell().Name}");
                    XszRemote.UseSkill(role.Key, skillId);
                    XszRemote.Cmd(role.Key, $"/ac {skillId.GetSpell().Name}");
                    XszRemote.Cmd(role.Key, $"/ac {skillId.GetSpell().Name}");
                    XszRemote.Cmd(role.Key, $"/ac {skillId.GetSpell().Name}");
                }
            }
        }
        return true;
    }

}