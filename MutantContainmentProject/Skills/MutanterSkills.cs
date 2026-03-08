using Database;
using System.Collections.Generic;

namespace MutantContainmentProject.Skills
{
    public class MutanterSkills
    {
        //勇气 I/II/III 最大生命
        public static readonly string SkillBraveryIID = "SkillBraveryI";
        public static readonly string SkillBraveryIIID = "SkillBraveryII";
        public static readonly string SkillBraveryIIIID = "SkillBraveryIII";
        //防御 I/II/III 降低物理和精神伤害
        public static readonly string SkillDefenseIID = "SkillDefenseI";
        public static readonly string SkillDefenseIIID = "SkillDefenseII";
        public static readonly string SkillDefenseIIIID = "SkillDefenseIII";
        //自律 I/II/III 增加成功率 工作速度
        public static readonly string SkillDisciplineIID = "SkillDisciplineI";
        public static readonly string SkillDisciplineIIID = "SkillDisciplineII";
        public static readonly string SkillDisciplineIIIID = "SkillDisciplineIII";
        //正义 I/II/III 攻击伤害 攻击速度
        public static readonly string SkillRighteousnessIID = "SkillRighteousnessI";
        public static readonly string SkillRighteousnessIIID = "SkillRighteousnessII";
        public static readonly string SkillRighteousnessIIIID = "SkillRighteousnessIII";
        public static Skill SkillBraveryI()
        {
            var skill = new Skill(SkillBraveryIID, STRINGS.SKILLS.BRAVERYI.NAME, STRINGS.SKILLS.BRAVERYI.DESCRIPTION, 0, hat: "hat_role_mining1", badge: "skillbadge_role_bravery1", skillGroup: MutanterSkillGroups.SkillGroupBraveryID, new List<SkillPerk>
            {
                Db.Get().SkillPerks.TryGet(MutanterSkillPerks.IncreaseHitPointsSmall)
            }, null, "Minion", null, null);
            return skill;
        }
        public static Skill SkillBraveryII()
        {
            var skill = new Skill(SkillBraveryIIID, STRINGS.SKILLS.BRAVERYII.NAME, STRINGS.SKILLS.BRAVERYII.DESCRIPTION, 1, hat: "hat_role_mining2", badge: "skillbadge_role_bravery2", skillGroup: MutanterSkillGroups.SkillGroupBraveryID, new List<SkillPerk>
            {
                Db.Get().SkillPerks.TryGet(MutanterSkillPerks.IncreaseHitPointsMedium)
            }, new List<string>
            {
                SkillBraveryIID
            }, "Minion", null, null);

            return skill;
        }
        public static Skill SkillBraveryIII()
        {
            var skill = new Skill(SkillBraveryIIIID, STRINGS.SKILLS.BRAVERYIII.NAME, STRINGS.SKILLS.BRAVERYIII.DESCRIPTION, 2, hat: "hat_role_mining3", badge: "skillbadge_role_bravery3", skillGroup: MutanterSkillGroups.SkillGroupBraveryID, new List<SkillPerk>
            {
                Db.Get().SkillPerks.TryGet(MutanterSkillPerks.IncreaseHitPointsLarge)
            }, new List<string>
            {
                SkillBraveryIIID
            }, "Minion", null, null);
            return skill;
        }
        public static Skill SkillDefenseI()
        {
            var skill = new Skill(SkillDefenseIID, STRINGS.SKILLS.DEFENSEI.NAME, STRINGS.SKILLS.DEFENSEI.DESCRIPTION, 0, hat: "hat_role_mining1", badge: "skillbadge_role_metal_resistance1", skillGroup: MutanterSkillGroups.SkillGroupDefenseID, new List<SkillPerk>
            {
                Db.Get().SkillPerks.TryGet(MutanterSkillPerks.IncreaseDefenseSmall)
            }, null, "Minion", null, null);
            return skill;
        }
        public static Skill SkillDefenseII()
        {
            var skill = new Skill(SkillDefenseIIID, STRINGS.SKILLS.DEFENSEII.NAME, STRINGS.SKILLS.DEFENSEII.DESCRIPTION, 1, hat: "hat_role_mining1", badge: "skillbadge_role_metal_resistance2", skillGroup: MutanterSkillGroups.SkillGroupDefenseID, new List<SkillPerk>
            {
                Db.Get().SkillPerks.TryGet(MutanterSkillPerks.IncreaseDefenseMedium)
            }, null, "Minion", null, null);
            return skill;
        }
        public static Skill SkillDefenseIII()
        {
            var skill = new Skill(SkillDefenseIIIID, STRINGS.SKILLS.DEFENSEIII.NAME, STRINGS.SKILLS.DEFENSEIII.DESCRIPTION, 2, hat: "hat_role_mining1", badge: "skillbadge_role_metal_resistance3", skillGroup: MutanterSkillGroups.SkillGroupDefenseID, new List<SkillPerk>
            {
                Db.Get().SkillPerks.TryGet(MutanterSkillPerks.IncreaseDefenseLarge)
            }, null, "Minion", null, null);
            return skill;
        }
        public static Skill SkillDisciplineI()
        {
            var skill = new Skill(SkillDisciplineIID, STRINGS.SKILLS.DISCIPLINEI.NAME, STRINGS.SKILLS.DISCIPLINEI.DESCRIPTION, 0, hat: "hat_role_mining1", badge: "skillbadge_role_discipline1", skillGroup: MutanterSkillGroups.SkillGroupDisciplineID, new List<SkillPerk>
            {
                Db.Get().SkillPerks.TryGet(MutanterSkillPerks.IncreaseDisciplineSmall)
            }, null, "Minion", null, null);
            return skill;
        }
        public static Skill SkillDisciplineII()
        {
            var skill = new Skill(SkillDisciplineIIID, STRINGS.SKILLS.DISCIPLINEII.NAME, STRINGS.SKILLS.DISCIPLINEII.DESCRIPTION, 1, hat: "hat_role_mining1", badge: "skillbadge_role_discipline2", skillGroup: MutanterSkillGroups.SkillGroupDisciplineID, new List<SkillPerk>
            {
                Db.Get().SkillPerks.TryGet(MutanterSkillPerks.IncreaseDisciplineMedium)
            }, null, "Minion", null, null);
            return skill;
        }
        public static Skill SkillDisciplineIII()
        {
            var skill = new Skill(SkillDisciplineIIIID, STRINGS.SKILLS.DISCIPLINEIII.NAME, STRINGS.SKILLS.DISCIPLINEIII.DESCRIPTION, 2, hat: "hat_role_mining1", badge: "skillbadge_role_discipline3", skillGroup: MutanterSkillGroups.SkillGroupDisciplineID, new List<SkillPerk>
            {
                Db.Get().SkillPerks.TryGet(MutanterSkillPerks.IncreaseDisciplineLarge)
            }, null, "Minion", null, null);
            return skill;
        }
        public static Skill SkillRighteousnessI()
        {
            var skill = new Skill(SkillRighteousnessIID, STRINGS.SKILLS.RIGHTEOUSNESSI.NAME, STRINGS.SKILLS.RIGHTEOUSNESSI.DESCRIPTION, 0, hat: "hat_role_mining1", badge: "skillbadge_role_righteousness1", skillGroup: MutanterSkillGroups.SkillGroupRighteousnessID, new List<SkillPerk>
            {
                Db.Get().SkillPerks.TryGet(MutanterSkillPerks.IncreaseRighteousnessSmall)
            }, null, "Minion", null, null);
            return skill;
        }
        public static Skill SkillRighteousnessII()
        {
            var skill = new Skill(SkillRighteousnessIIID, STRINGS.SKILLS.RIGHTEOUSNESSII.NAME, STRINGS.SKILLS.RIGHTEOUSNESSII.DESCRIPTION, 1, hat: "hat_role_mining1", badge: "skillbadge_role_righteousness2", skillGroup: MutanterSkillGroups.SkillGroupRighteousnessID, new List<SkillPerk>
            {
                Db.Get().SkillPerks.TryGet(MutanterSkillPerks.IncreaseRighteousnessMedium)
            }, null, "Minion", null, null);
            return skill;
        }
        public static Skill SkillRighteousnessIII()
        {
            var skill = new Skill(SkillRighteousnessIIIID, STRINGS.SKILLS.RIGHTEOUSNESSIII.NAME, STRINGS.SKILLS.RIGHTEOUSNESSIII.DESCRIPTION, 2, hat: "hat_role_mining1", badge: "skillbadge_role_righteousness3", skillGroup: MutanterSkillGroups.SkillGroupRighteousnessID, new List<SkillPerk>
            {
                Db.Get().SkillPerks.TryGet(MutanterSkillPerks.IncreaseRighteousnessLarge)
            }, null, "Minion", null, null);
            return skill;
        }
    }
}
