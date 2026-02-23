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
        //谨慎 I/II/III 增加最大精神
        public static readonly string SkillCautionIID = "SkillCautionI";
        public static readonly string SkillCautionIIID = "SkillCautionII";
        public static readonly string SkillCautionIIIID = "SkillCautionIII";
        //自律 I/II/III 增加成功率 工作速度
        public static readonly string SkillDisciplineIID = "SkillDisciplineI";
        public static readonly string SkillDisciplineIIID = "SkillDisciplineII";
        public static readonly string SkillDisciplineIIIID = "SkillDisciplineIII";
        //正义 I/II/III 攻击速度 移动速度
        public static readonly string SkillRighteousnessIID = "SkillRighteousnessI";
        public static readonly string SkillRighteousnessIIID = "SkillRighteousnessII";
        public static readonly string SkillRighteousnessIIIID = "SkillRighteousnessIII";
        public static Skill SkillBraveryI()
        {
            var skill = new Skill(SkillBraveryIID, STRINGS.SKILLS.BRAVERYI.NAME, STRINGS.SKILLS.BRAVERYI.DESCRIPTION, 0, hat: "hat_role_mining1", badge: "skillbadge_role_mining1", skillGroup: MutanterSkillGroups.SkillGroupContainID, new List<SkillPerk>
            {
                Db.Get().SkillPerks.TryGet(MutanterSkillPerks.IncreaseHitPointsSmall)
            }, null, "Minion", null, null);
            return skill;
        }
        public static Skill SkillBraveryII()
        {
            var skill = new Skill(SkillBraveryIIID, STRINGS.SKILLS.BRAVERYII.NAME, STRINGS.SKILLS.BRAVERYII.DESCRIPTION, 1, hat: "hat_role_mining2", badge: "skillbadge_role_mining2", skillGroup: MutanterSkillGroups.SkillGroupContainID, new List<SkillPerk>
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
            var skill = new Skill(SkillBraveryIIIID, STRINGS.SKILLS.BRAVERYIII.NAME, STRINGS.SKILLS.BRAVERYIII.DESCRIPTION, 2, hat: "hat_role_mining3", badge: "skillbadge_role_mining3", skillGroup: MutanterSkillGroups.SkillGroupContainID, new List<SkillPerk>
            {
                Db.Get().SkillPerks.TryGet(MutanterSkillPerks.IncreaseHitPointsLarge)
            }, new List<string>
            {
                SkillBraveryIIID
            }, "Minion", null, null);
            return skill;
        }
        public static Skill SkillCautionI()
        {
            var skill = new Skill(SkillCautionIID, STRINGS.SKILLS.CAUTIONI.NAME, STRINGS.SKILLS.CAUTIONI.DESCRIPTION, 0, hat: "hat_role_mining1", badge: "skillbadge_role_mining1", skillGroup: MutanterSkillGroups.SkillGroupWillPowerID, new List<SkillPerk>
            {
                Db.Get().SkillPerks.TryGet(MutanterSkillPerks.IncreaseWillPowerSmall)
            }, null, "Minion", null, null);
            return skill;
        }
        public static Skill SkillCautionII()
        {
            var skill = new Skill(SkillCautionIIID, STRINGS.SKILLS.CAUTIONII.NAME, STRINGS.SKILLS.CAUTIONII.DESCRIPTION, 1, hat: "hat_role_mining1", badge: "skillbadge_role_mining1", skillGroup: MutanterSkillGroups.SkillGroupWillPowerID, new List<SkillPerk>
            {
                Db.Get().SkillPerks.TryGet(MutanterSkillPerks.IncreaseWillPowerMedium)
            }, null, "Minion", null, null);
            return skill;
        }
        public static Skill SkillCautionIII()
        {
            var skill = new Skill(SkillCautionIIIID, STRINGS.SKILLS.CAUTIONIII.NAME, STRINGS.SKILLS.CAUTIONIII.DESCRIPTION, 2, hat: "hat_role_mining1", badge: "skillbadge_role_mining1", skillGroup: MutanterSkillGroups.SkillGroupWillPowerID, new List<SkillPerk>
            {
                Db.Get().SkillPerks.TryGet(MutanterSkillPerks.IncreaseWillPowerLarge)
            }, null, "Minion", null, null);
            return skill;
        }
        public static Skill SkillDisciplineI()
        {
            var skill = new Skill(SkillDisciplineIID, STRINGS.SKILLS.DISCIPLINEI.NAME, STRINGS.SKILLS.DISCIPLINEI.DESCRIPTION, 0, hat: "hat_role_mining1", badge: "skillbadge_role_mining1", skillGroup: MutanterSkillGroups.SkillGroupDisciplineID, new List<SkillPerk>
            {
                Db.Get().SkillPerks.TryGet(MutanterSkillPerks.IncreaseSuccessRateLow),
                Db.Get().SkillPerks.TryGet(MutanterSkillPerks.IncreaseWorkingSpeedSmall)
            }, null, "Minion", null, null);
            return skill;
        }
        public static Skill SkillDisciplineII()
        {
            var skill = new Skill(SkillDisciplineIIID, STRINGS.SKILLS.DISCIPLINEII.NAME, STRINGS.SKILLS.DISCIPLINEII.DESCRIPTION, 1, hat: "hat_role_mining1", badge: "skillbadge_role_mining1", skillGroup: MutanterSkillGroups.SkillGroupDisciplineID, new List<SkillPerk>
            {
                Db.Get().SkillPerks.TryGet(MutanterSkillPerks.IncreaseSuccessRateMedium),
                Db.Get().SkillPerks.TryGet(MutanterSkillPerks.IncreaseWorkingSpeedMedium)
            }, null, "Minion", null, null);
            return skill;
        }
        public static Skill SkillDisciplineIII()
        {
            var skill = new Skill(SkillDisciplineIIIID, STRINGS.SKILLS.DISCIPLINEIII.NAME, STRINGS.SKILLS.DISCIPLINEIII.DESCRIPTION, 2, hat: "hat_role_mining1", badge: "skillbadge_role_mining1", skillGroup: MutanterSkillGroups.SkillGroupDisciplineID, new List<SkillPerk>
            {
                Db.Get().SkillPerks.TryGet(MutanterSkillPerks.IncreaseSuccessRateHigh),
                Db.Get().SkillPerks.TryGet(MutanterSkillPerks.IncreaseWorkingSpeedLarge)
            }, null, "Minion", null, null);
            return skill;
        }
        public static Skill SkillRighteousnessI()
        {
            var skill = new Skill(SkillRighteousnessIID, STRINGS.SKILLS.RIGHTEOUSNESSI.NAME, STRINGS.SKILLS.RIGHTEOUSNESSI.DESCRIPTION, 0, hat: "hat_role_mining1", badge: "skillbadge_role_mining1", skillGroup: MutanterSkillGroups.SkillRighteousnessID, new List<SkillPerk>
            {
                Db.Get().SkillPerks.TryGet(MutanterSkillPerks.IncreaseAttackDamageSmall)
            }, null, "Minion", null, null);
            return skill;
        }
        public static Skill SkillRighteousnessII()
        {
            var skill = new Skill(SkillRighteousnessIIID, STRINGS.SKILLS.RIGHTEOUSNESSII.NAME, STRINGS.SKILLS.RIGHTEOUSNESSII.DESCRIPTION, 1, hat: "hat_role_mining1", badge: "skillbadge_role_mining1", skillGroup: MutanterSkillGroups.SkillRighteousnessID, new List<SkillPerk>
            {
                Db.Get().SkillPerks.TryGet(MutanterSkillPerks.IncreaseAttackDamageMedium)
            }, null, "Minion", null, null);
            return skill;
        }
        public static Skill SkillRighteousnessIII()
        {
            var skill = new Skill(SkillRighteousnessIIIID, STRINGS.SKILLS.RIGHTEOUSNESSIII.NAME, STRINGS.SKILLS.RIGHTEOUSNESSIII.DESCRIPTION, 2, hat: "hat_role_mining1", badge: "skillbadge_role_mining1", skillGroup: MutanterSkillGroups.SkillRighteousnessID, new List<SkillPerk>
            {
                Db.Get().SkillPerks.TryGet(MutanterSkillPerks.IncreaseAttackDamageLarge)
            }, null, "Minion", null, null);
            return skill;
        }
    }
}
