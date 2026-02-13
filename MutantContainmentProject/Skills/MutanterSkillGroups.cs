using Database;
using System.Collections.Generic;
using TBBHe.TbbLib.Debuger;

namespace MutantContainmentProject.Skills
{
    public class MutanterSkillGroups
    {
        public static string SkillGroupContainID = "Contain";
        public static string SkillGroupWillPowerID = "WillPower";
        public static string SkillGroupDisciplineID = "Discipline";
        public static string SkillRighteousnessID = "Righteousness";
        public static SkillGroup SkillGroupBravery()
        {
            var skillgroup = new SkillGroup(SkillGroupContainID, MutanterChoreTypes.ChoreTypeContainID, STRINGS.SKILLGROUP.CONTAIN.NAME, "icon_errand_dig", "icon_archetype_dig");

            skillgroup.relevantAttributes = new List<Klei.AI.Attribute>
            {
                Db.Get().ChoreGroups.TryGet(MutanterChoreGroups.ChoreGroupContainID).attribute
            };
            skillgroup.requiredChoreGroups = new List<string>
            {
                MutanterChoreGroups.ChoreGroupContainID
            };

            return skillgroup;
        }
        public static SkillGroup SkillGroupWillPower()
        {
            var skillgroup = new SkillGroup(SkillGroupWillPowerID, MutanterChoreTypes.ChoreTypeWillPowerID, STRINGS.SKILLGROUP.WILLPOWER.NAME, "icon_errand_dig", "icon_archetype_dig");

            skillgroup.relevantAttributes = new List<Klei.AI.Attribute>
            {
                Db.Get().ChoreGroups.TryGet(MutanterChoreGroups.ChoreGroupWillPowerID).attribute
            };
            skillgroup.requiredChoreGroups = new List<string>
            {
                MutanterChoreGroups.ChoreGroupWillPowerID
            };

            return skillgroup;
        }
        public static SkillGroup SkillGroupDiscipline()
        {
            var skillgroup = new SkillGroup(SkillGroupDisciplineID, MutanterChoreTypes.ChoreTypeDisciplineID, STRINGS.SKILLGROUP.DISCIPLINE.NAME, "icon_errand_dig", "icon_archetype_dig");

            skillgroup.relevantAttributes = new List<Klei.AI.Attribute>
            {
                Db.Get().ChoreGroups.TryGet(MutanterChoreGroups.ChoreGroupDisciplineID).attribute
            };
            skillgroup.requiredChoreGroups = new List<string>
            {
                MutanterChoreGroups.ChoreGroupDisciplineID
            };

            return skillgroup;
        }
        public static SkillGroup SkillRighteousness()
        {
            var skillgroup = new SkillGroup(SkillRighteousnessID, MutanterChoreTypes.ChoreTypeRighteousnessID, STRINGS.SKILLGROUP.RIGHTEOUSNESS.NAME, "icon_errand_dig", "icon_archetype_dig");

            skillgroup.relevantAttributes = new List<Klei.AI.Attribute>
            {
                Db.Get().ChoreGroups.TryGet(MutanterChoreGroups.ChoreGroupRighteousnessID).attribute
            };
            skillgroup.requiredChoreGroups = new List<string>
            {
                MutanterChoreGroups.ChoreGroupRighteousnessID
            };

            return skillgroup;
        }
    }
}
