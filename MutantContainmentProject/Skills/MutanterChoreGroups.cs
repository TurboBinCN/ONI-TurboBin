using Database;
using TBB.He.TbbLib.Utils;

namespace MutantContainmentProject.Skills
{
    public class MutanterChoreGroups
    {
        public static readonly string ChoreGroupContainID = "Contain";
        public static readonly string ChoreGroupBraveryID = "Bravery";
        public static readonly string ChoreGroupDefenseID = "Defense";
        public static readonly string ChoreGroupDisciplineID = "Discipline";
        public static readonly string ChoreGroupRighteousnessID = "Righteousness";
        public static void ChoreGroupContain(ChoreGroups __instance)
        {
            TbbHarmonyExtension.CallMethod(__instance, "Add",
                ChoreGroupContainID, STRINGS.DUPLICANTS.CHOREGROUPS.CONTAIN.NAME.ToString(), Db.Get().Attributes.TryGet(MutanterAttributes.AttributeBraveryID),
                "icon_errand_bravery", 2, true
            );
        }
        public static void ChoreGroupBravery(ChoreGroups __instance)
        {
            TbbHarmonyExtension.CallMethod(__instance, "Add",
                ChoreGroupBraveryID, STRINGS.DUPLICANTS.CHOREGROUPS.BRAVERY.NAME.ToString(), Db.Get().Attributes.TryGet(MutanterAttributes.AttributeBraveryID),
                "icon_errand_bravery", 2, true
            );
        }
        public static void ChoreGroupDefense(ChoreGroups __instance)
        {
            TbbHarmonyExtension.CallMethod(__instance, "Add",
                ChoreGroupDefenseID, STRINGS.DUPLICANTS.CHOREGROUPS.DEFENSE.NAME.ToString(), Db.Get().Attributes.TryGet(MutanterAttributes.AttributeDefenseID),
                "icon_errand_metal_resistance", 2, true
            );
        }
        public static void ChoreGroupDiscipline(ChoreGroups __instance)
        {
            TbbHarmonyExtension.CallMethod(__instance, "Add",
                ChoreGroupDisciplineID, STRINGS.DUPLICANTS.CHOREGROUPS.DISCIPLINE.NAME.ToString(), Db.Get().Attributes.TryGet(MutanterAttributes.AttributeDisciplineID),
                "icon_errand_discipline", 2, true
            );
        }
        public static void ChoreGroupRighteousness(ChoreGroups __instance)
        {
            TbbHarmonyExtension.CallMethod(__instance, "Add",
                ChoreGroupRighteousnessID, STRINGS.DUPLICANTS.CHOREGROUPS.RIGHTEOUSNESS.NAME.ToString(), Db.Get().Attributes.TryGet(MutanterAttributes.AttributeRighteousnessID),
                "icon_errand_righteousness", 2, true
            );
        }
    }
}
