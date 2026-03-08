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
            object choregroupObj;
            bool success = TbbHarmonyExtension.InvokeMethod(out choregroupObj, __instance, "Add", new object[] {
                ChoreGroupContainID, STRINGS.DUPLICANTS.CHOREGROUPS.CONTAIN.NAME.ToString(),Db.Get().Attributes.TryGet(MutanterAttributes.AttributeBraveryID),
                "icon_errand_bravery",2, true
            });
        }
        public static void ChoreGroupBravery(ChoreGroups __instance)
        {
            object choregroupObj;
            bool success = TbbHarmonyExtension.InvokeMethod(out choregroupObj, __instance, "Add", new object[] {
                ChoreGroupBraveryID, STRINGS.DUPLICANTS.CHOREGROUPS.BRAVERY.NAME.ToString(),Db.Get().Attributes.TryGet(MutanterAttributes.AttributeBraveryID),
                "icon_errand_bravery",2, true
            });
        }
        public static void ChoreGroupDefense(ChoreGroups __instance)
        {
            object choregroupObj;
            bool success = TbbHarmonyExtension.InvokeMethod(out choregroupObj, __instance, "Add", new object[] {
                ChoreGroupDefenseID, STRINGS.DUPLICANTS.CHOREGROUPS.DEFENSE.NAME.ToString(),Db.Get().Attributes.TryGet(MutanterAttributes.AttributeDefenseID),
                "icon_errand_metal_resistance",2, true
            });
        }
        public static void ChoreGroupDiscipline(ChoreGroups __instance)
        {
            object choregroupObj;
            bool success = TbbHarmonyExtension.InvokeMethod(out choregroupObj, __instance, "Add", new object[] {
                ChoreGroupDisciplineID, STRINGS.DUPLICANTS.CHOREGROUPS.DISCIPLINE.NAME.ToString(),Db.Get().Attributes.TryGet(MutanterAttributes.AttributeDisciplineID),
                "icon_errand_discipline",2, true
            });
        }
        public static void ChoreGroupRighteousness(ChoreGroups __instance)
        {
            object choregroupObj;
            bool success = TbbHarmonyExtension.InvokeMethod(out choregroupObj, __instance, "Add", new object[] {
                ChoreGroupRighteousnessID, STRINGS.DUPLICANTS.CHOREGROUPS.RIGHTEOUSNESS.NAME.ToString(),Db.Get().Attributes.TryGet(MutanterAttributes.AttributeRighteousnessID),
                "icon_errand_righteousness",2, true
            });
        }
    }
}
