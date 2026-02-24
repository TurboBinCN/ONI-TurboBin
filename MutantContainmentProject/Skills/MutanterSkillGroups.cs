using Database;
using System.Collections.Generic;

namespace MutantContainmentProject.Skills
{
    public class MutanterSkillGroups
    {
        public static string SkillGroupBraveryID = "Bravery";
        public static string SkillGroupMentalResistanceID = "MentalResistance";
        public static string SkillGroupDisciplineID = "Discipline";
        public static string SkillGroupRighteousnessID = "Righteousness";
        public static SkillGroup SkillGroupBravery()
        {
            var skillgroup = new SkillGroup(SkillGroupBraveryID, MutanterChoreTypes.ChoreTypeContainID, STRINGS.SKILLGROUP.CONTAIN.NAME, "icon_errand_dig", "icon_archetype_dig");

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
        public static SkillGroup SkillGroupMentalResistance()
        {
            var skillgroup = new SkillGroup(SkillGroupMentalResistanceID, MutanterChoreTypes.ChoreTypeMentalResistanceID, STRINGS.SKILLGROUP.MENTALRESISTANCE.NAME, "icon_errand_dig", "icon_archetype_dig");

            skillgroup.relevantAttributes = new List<Klei.AI.Attribute>
            {
                Db.Get().ChoreGroups.TryGet(MutanterChoreGroups.ChoreGroupMentalResistanceID).attribute
            };
            skillgroup.requiredChoreGroups = new List<string>
            {
                MutanterChoreGroups.ChoreGroupMentalResistanceID
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
        public static SkillGroup SkillGroupRighteousness()
        {
            var skillgroup = new SkillGroup(SkillGroupRighteousnessID, MutanterChoreTypes.ChoreTypeRighteousnessID, STRINGS.SKILLGROUP.RIGHTEOUSNESS.NAME, "icon_errand_dig", "icon_archetype_dig");

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
