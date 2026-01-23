using System;
using System.Numerics;
using AEAssist;
using AEAssist.CombatRoutine.Trigger;
using AEAssist.Extension;
using AEAssist.Helper;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using HaiyaBox.Utils;

namespace HaiyaBox.Triggers.TriggerAction;

/// <summary>
/// 职能角色枚举
/// </summary>
public enum PartyRole
{
    /// <summary>
    /// 主坦克
    /// </summary>
    MT,

    /// <summary>
    /// 副坦克
    /// </summary>
    ST,

    /// <summary>
    /// 治疗1
    /// </summary>
    H1,

    /// <summary>
    /// 治疗2
    /// </summary>
    H2,

    /// <summary>
    /// 近战DPS 1
    /// </summary>
    D1,

    /// <summary>
    /// 近战DPS 2
    /// </summary>
    D2,

    /// <summary>
    /// 远程DPS 1
    /// </summary>
    D3,

    /// <summary>
    /// 远程DPS 2
    /// </summary>
    D4
}

public enum SkillType
{
    群减, 雪仇, T特殊减, T小单减, T40减, T铁壁, 无
}

/// <summary>
/// 使用技能动作
/// 通过遥控释放指定职能的技能
/// </summary>
public class 使用技能动作A : ITriggerAction
{
    /// <summary>
    /// 目标职能
    /// </summary>
    public PartyRole TargetRole { get; set; } = PartyRole.MT;

    public SkillType skillType { get; set; } = SkillType.无;

    /// <summary>
    /// 技能ID
    /// </summary>
    public uint SkillId { get; set; } = 0;

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName => "使用技能动作";

    /// <summary>
    /// 备注说明
    /// </summary>
    public string Remark { get; set; } = string.Empty;

    /// <summary>
    /// 绘制 ImGui 编辑界面
    /// </summary>
    public bool Draw()
    {
        var changed = false;

        // 职能选择（单选勾选框）
        ImGui.Text("目标职能");

        // 第一行：MT ST H1 H2
        if (ImGui.RadioButton("MT", TargetRole == PartyRole.MT))
        {
            TargetRole = PartyRole.MT;
            changed = true;
        }
        ImGui.SameLine();
        if (ImGui.RadioButton("ST", TargetRole == PartyRole.ST))
        {
            TargetRole = PartyRole.ST;
            changed = true;
        }
        ImGui.SameLine();
        if (ImGui.RadioButton("H1", TargetRole == PartyRole.H1))
        {
            TargetRole = PartyRole.H1;
            changed = true;
        }
        ImGui.SameLine();
        if (ImGui.RadioButton("H2", TargetRole == PartyRole.H2))
        {
            TargetRole = PartyRole.H2;
            changed = true;
        }

        // 第二行：D1 D2 D3 D4
        if (ImGui.RadioButton("D1", TargetRole == PartyRole.D1))
        {
            TargetRole = PartyRole.D1;
            changed = true;
        }
        ImGui.SameLine();
        if (ImGui.RadioButton("D2", TargetRole == PartyRole.D2))
        {
            TargetRole = PartyRole.D2;
            changed = true;
        }
        ImGui.SameLine();
        if (ImGui.RadioButton("D3", TargetRole == PartyRole.D3))
        {
            TargetRole = PartyRole.D3;
            changed = true;
        }
        ImGui.SameLine();
        if (ImGui.RadioButton("D4", TargetRole == PartyRole.D4))
        {
            TargetRole = PartyRole.D4;
            changed = true;
        }

        // 技能ID输入
        var skillId = (int)SkillId;
        if (ImGui.InputInt("技能ID", ref skillId, 1, 100))
        {
            SkillId = (uint)Math.Max(0, skillId);
            changed = true;
        }

        if (TargetRole is PartyRole.MT or PartyRole.ST)
        {
            ImGui.Text("坦克技能快捷选择");

            if (ImGui.RadioButton("群减", skillType == SkillType.群减))
            {
                skillType = SkillType.群减;
                changed = true;
            }
            ImGui.SameLine();
            if (ImGui.RadioButton("雪仇", skillType == SkillType.雪仇))
            {
                skillType = SkillType.雪仇;
                changed = true;
            }
            ImGui.SameLine();
            if (ImGui.RadioButton("T特殊减", skillType == SkillType.T特殊减))
            {
                skillType = SkillType.T特殊减;
                changed = true;
            }

            if (ImGui.RadioButton("T小单减", skillType == SkillType.T小单减))
            {
                skillType = SkillType.T小单减;
                changed = true;
            }
            ImGui.SameLine();

            if (ImGui.RadioButton("T40减", skillType == SkillType.T40减))
            {
                skillType = SkillType.T40减;
                changed = true;
            }
            ImGui.SameLine();
            if (ImGui.RadioButton("T铁壁", skillType == SkillType.T铁壁))
            {
                skillType = SkillType.T铁壁;
                changed = true;
            }
        }

        if (TargetRole is PartyRole.D1 or PartyRole.D2 or PartyRole.D3 or PartyRole.D4)
        {
            ImGui.Text("DPS技能快捷选择");
            if (ImGui.RadioButton("群减##dps", skillType == SkillType.群减))
            {
                skillType = SkillType.群减;
                changed = true;
            }
        }

        if (TargetRole is PartyRole.H1 or PartyRole.H2)
        {
            ImGui.Text("治疗技能快捷选择");
            if (ImGui.RadioButton("群减##healer", skillType == SkillType.群减))
            {
                skillType = SkillType.群减;
                changed = true;
            }
        }

        // 备注
        var remark = Remark;
        if (ImGui.InputText("备注", ref remark, 256))
        {
            Remark = remark;
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// 执行动作
    /// </summary>
    public bool Handle()
    {
        if (SkillId == 0 && skillType == SkillType.无)
        {
            LogHelper.Warning("[使用技能动作] 技能ID为0，跳过执行");
            return false;
        }

        try
        {
            // 检查遥控是否已连接
            if (!RemoteControl.IsConnected())
            {
                LogHelper.Warning("[使用技能动作] 遥控未连接，无法执行技能");
                return false;
            }

            // 获取职能字符串
            var roleStr = TargetRole.ToString();

            if (skillType != SkillType.无)
            {
                var player = GetPlayerByRole(roleStr);
                if (player == null)
                {
                    LogHelper.Warning($"[使用技能动作] 找不到职能 {roleStr} 的玩家");
                    return false;
                }

                if (skillType == SkillType.T特殊减)
                {
                    switch ((Job)player.ClassJob.RowId)
                    {
                        case Job.DRK:
                            RemoteControl.UseSkill(roleStr, 3634);
                            return true;
                        case Job.GNB:
                            RemoteControl.UseSkill(roleStr, 16140);
                            return true;
                        case Job.PLD:
                            RemoteControl.UseSkill(roleStr, 7385);
                            return true;
                        case Job.WAR:
                            RemoteControl.UseSkill(roleStr, 40);
                            return true;
                        case Job.MCH:
                            RemoteControl.UseSkill(roleStr, 2887);
                            return true;
                        case Job.RDM:
                            RemoteControl.UseSkill(roleStr, 25857);
                            return true;
                    }
                }

                if (TargetRole is PartyRole.MT or PartyRole.ST)
                {
                    if (skillType == SkillType.群减)
                    {
                        switch ((Job)player.ClassJob.RowId)
                        {
                            case Job.DRK:
                                RemoteControl.UseSkill(roleStr, 16471);
                                return true;
                            case Job.GNB:
                                RemoteControl.UseSkill(roleStr, 16160);
                                return true;
                            case Job.PLD:
                                RemoteControl.UseSkill(roleStr, 3540);
                                return true;
                            case Job.WAR:
                                RemoteControl.UseSkill(roleStr, 7388);
                                return true;
                        }
                    }

                    if (skillType == SkillType.T小单减)
                    {
                        switch ((Job)player.ClassJob.RowId)
                        {
                            case Job.DRK:
                                RemoteControl.UseSkill(roleStr, 7393);
                                return true;
                            case Job.GNB:
                                RemoteControl.UseSkill(roleStr, 25758);
                                return true;
                            case Job.PLD:
                                RemoteControl.UseSkill(roleStr, 25746);
                                return true;
                            case Job.WAR:
                                RemoteControl.UseSkill(roleStr, 25751);
                                return true;
                        }
                    }

                    if (skillType == SkillType.T40减)
                    {
                        switch ((Job)player.ClassJob.RowId)
                        {
                            case Job.DRK:
                                RemoteControl.UseSkill(roleStr, 36927);
                                return true;
                            case Job.GNB:
                                RemoteControl.UseSkill(roleStr, 36935);
                                return true;
                            case Job.PLD:
                                RemoteControl.UseSkill(roleStr, 36920);
                                return true;
                            case Job.WAR:
                                RemoteControl.UseSkill(roleStr, 36923);
                                return true;
                        }
                    }

                    if (skillType == SkillType.T铁壁)
                    {
                        RemoteControl.UseSkill(roleStr, 7531);
                        return true;
                    }

                    if (skillType == SkillType.雪仇)
                    {
                        RemoteControl.UseSkill(roleStr, 7535);
                        return true;
                    }
                }

                if (TargetRole is PartyRole.D1 or PartyRole.D2 or PartyRole.D3 or PartyRole.D4)
                {
                    if (skillType == SkillType.群减)
                    {
                        switch (player.ClassJob.Value.JobType)
                        {
                            case 3:
                                RemoteControl.UseSkill(roleStr, 7549);
                                return true;
                            case 5:
                                RemoteControl.UseSkill(roleStr, 7560);
                                return true;
                        }

                        switch ((Job)player.ClassJob.RowId)
                        {
                            case Job.DNC:
                                RemoteControl.UseSkill(roleStr, 16012);
                                return true;
                            case Job.MCH:
                                RemoteControl.UseSkill(roleStr, 16889);
                                return true;
                            case Job.BRD:
                                RemoteControl.UseSkill(roleStr, 7405);
                                return true;
                        }
                    }
                }
            }

            // 如果指定了技能ID，直接使用
            RemoteControl.UseSkill(roleStr, SkillId);

            LogHelper.Debug($"[使用技能动作] 向 {roleStr} 发送技能 {SkillId}");
            return true;
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[使用技能动作] 执行失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 根据职能获取玩家
    /// </summary>
    private IBattleChara? GetPlayerByRole(string targetRole)
    {
        foreach (var member in PartyHelper.Party)
        {
            if (member == null)
                continue;

            var role = string.Empty;
            var name = member.Name.TextValue;
            if (!string.IsNullOrEmpty(name))
                role = XszRemote.GetRoleByPlayerName(name);


            // 匹配职能
            if (!string.IsNullOrEmpty(role) && role.Equals(targetRole, System.StringComparison.OrdinalIgnoreCase))
                return member;
        }

        return null;
    }
}
