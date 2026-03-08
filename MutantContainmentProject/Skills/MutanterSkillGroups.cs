using Database;
using Klei.AI;
using System.Collections.Generic;
using TUNING;

namespace MutantContainmentProject.Skills
{
    public class MutanterSkillGroups
    {
        public static SkillGroup Bravery;
        public static SkillGroup Defense;
        public static SkillGroup Discipline;
        public static SkillGroup Righteousness;

        public static string SkillGroupBraveryID = "Bravery";
        public static string SkillGroupDefenseID = "Defense";
        public static string SkillGroupDisciplineID = "Discipline";
        public static string SkillGroupRighteousnessID = "Righteousness";
        public static SkillGroup SkillGroupBravery()
        {
            Bravery = new MutanterSkillGroup(SkillGroupBraveryID, "Bravery", STRINGS.DUPLICANTS.SKILLGROUPS.BRAVERY.NAME, "icon_errand_bravery", "icon_archetype_bravery");
            Bravery.relevantAttributes = new List<Attribute>()
            {
                Db.Get().Attributes.Get(MutanterAttributes.AttributeBraveryID)
            };
            Bravery.requiredChoreGroups = new List<string>();
            Bravery.allowAsAptitude = true;

            if (!DUPLICANTSTATS.ARCHETYPE_TRAIT_EXCLUSIONS.ContainsKey(SkillGroupBraveryID))
            {
                DUPLICANTSTATS.ARCHETYPE_TRAIT_EXCLUSIONS.Add(SkillGroupBraveryID, new List<string>());
            }

            // 添加技能组到ARCHETYPE_BIONIC_TRAIT_COMPATIBILITY
            // 为空列表，表示这些技能组与仿生小人的特质不兼容，不会在仿生小人上生成
            if (!DUPLICANTSTATS.ARCHETYPE_BIONIC_TRAIT_COMPATIBILITY.ContainsKey(SkillGroupBraveryID))
            {
                DUPLICANTSTATS.ARCHETYPE_BIONIC_TRAIT_COMPATIBILITY.Add(SkillGroupBraveryID, new List<string>());
            }

            return Bravery;
        }

        public static SkillGroup SkillGroupDefense()
        {
            Defense = new MutanterSkillGroup(SkillGroupDefenseID, MutanterChoreGroups.ChoreGroupDefenseID, STRINGS.DUPLICANTS.SKILLGROUPS.DEFENSE.NAME, "icon_errand_metal_resistance", "icon_archetype_metal_resistance");
            Defense.relevantAttributes = new List<Attribute>()
            {
                Db.Get().Attributes.Get(MutanterAttributes.AttributeDefenseID)
            };
            Defense.requiredChoreGroups = new List<string>();
            Defense.allowAsAptitude = true;

            // 为防御技能组添加排除特质
            if (!DUPLICANTSTATS.ARCHETYPE_TRAIT_EXCLUSIONS.ContainsKey(SkillGroupDefenseID))
            {
                DUPLICANTSTATS.ARCHETYPE_TRAIT_EXCLUSIONS.Add(SkillGroupDefenseID, new List<string>());
            }

            // 添加技能组到ARCHETYPE_BIONIC_TRAIT_COMPATIBILITY
            // 为空列表，表示这些技能组与仿生小人的特质不兼容，不会在仿生小人上生成
            if (!DUPLICANTSTATS.ARCHETYPE_BIONIC_TRAIT_COMPATIBILITY.ContainsKey(SkillGroupDefenseID))
            {
                DUPLICANTSTATS.ARCHETYPE_BIONIC_TRAIT_COMPATIBILITY.Add(SkillGroupDefenseID, new List<string>());
            }
            return Defense;
        }

        public static SkillGroup SkillGroupDiscipline()
        {
            Discipline = new MutanterSkillGroup(SkillGroupDisciplineID, MutanterChoreGroups.ChoreGroupDisciplineID, STRINGS.DUPLICANTS.SKILLGROUPS.DISCIPLINE.NAME, "icon_errand_discipline", "icon_archetype_discipline");
            Discipline.relevantAttributes = new List<Attribute>
            {
                Db.Get().Attributes.Get(MutanterAttributes.AttributeDisciplineID)
            };
            Discipline.requiredChoreGroups = new List<string>();
            Discipline.allowAsAptitude = true;

            // 为自律技能组添加排除特质
            if (!DUPLICANTSTATS.ARCHETYPE_TRAIT_EXCLUSIONS.ContainsKey(SkillGroupDisciplineID))
            {
                DUPLICANTSTATS.ARCHETYPE_TRAIT_EXCLUSIONS.Add(SkillGroupDisciplineID, new List<string>());
            }

            // 添加技能组到ARCHETYPE_BIONIC_TRAIT_COMPATIBILITY
            // 为空列表，表示这些技能组与仿生小人的特质不兼容，不会在仿生小人上生成
            if (!DUPLICANTSTATS.ARCHETYPE_BIONIC_TRAIT_COMPATIBILITY.ContainsKey(SkillGroupDisciplineID))
            {
                DUPLICANTSTATS.ARCHETYPE_BIONIC_TRAIT_COMPATIBILITY.Add(SkillGroupDisciplineID, new List<string>());
            }
            return Discipline;
        }

        public static SkillGroup SkillGroupRighteousness()
        {
            Righteousness = new MutanterSkillGroup(SkillGroupRighteousnessID, MutanterChoreGroups.ChoreGroupRighteousnessID, STRINGS.DUPLICANTS.SKILLGROUPS.RIGHTEOUSNESS.NAME, "icon_errand_righteousness", "icon_archetype_righteousness");
            Righteousness.relevantAttributes = new List<Attribute>
            {
                Db.Get().Attributes.Get(MutanterAttributes.AttributeRighteousnessID)
            };
            Righteousness.requiredChoreGroups = new List<string>();
            Righteousness.allowAsAptitude = true;

            // 为正义技能组添加排除特质
            if (!DUPLICANTSTATS.ARCHETYPE_TRAIT_EXCLUSIONS.ContainsKey(SkillGroupRighteousnessID))
            {
                DUPLICANTSTATS.ARCHETYPE_TRAIT_EXCLUSIONS.Add(SkillGroupRighteousnessID, new List<string>());
            }

            // 添加技能组到ARCHETYPE_BIONIC_TRAIT_COMPATIBILITY
            // 为空列表，表示这些技能组与仿生小人的特质不兼容，不会在仿生小人上生成
            if (!DUPLICANTSTATS.ARCHETYPE_BIONIC_TRAIT_COMPATIBILITY.ContainsKey(SkillGroupRighteousnessID))
            {
                DUPLICANTSTATS.ARCHETYPE_BIONIC_TRAIT_COMPATIBILITY.Add(SkillGroupRighteousnessID, new List<string>());
            }
            return Righteousness;
        }
    }
}