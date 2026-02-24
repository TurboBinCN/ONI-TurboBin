using Database;
using TBB.He.TbbLib.Utils;

namespace MutantContainmentProject.Skills
{
    public class MutanterChoreGroups
    {
        public static readonly string ChoreGroupContainID = "Contain";
        public static readonly string ChoreGroupMentalResistanceID = "MentalResistance";
        public static readonly string ChoreGroupDisciplineID = "Discipline";
        public static readonly string ChoreGroupRighteousnessID = "Righteousness";
        public static void ChoreGroupContain(ChoreGroups __instance)
        {
            object choregroupObj;
            bool success = TbbHarmonyExtension.InvokeMethod(out choregroupObj, __instance, "Add", new object[] {
                ChoreGroupContainID, STRINGS.CHOREGROUPS.CONTAIN.NAME.ToString(),Db.Get().Attributes.TryGet(MutanterAttributes.AttributeBraveryID),
                "icon_errand_dig",2, true
            });
        }
        public static void ChoreGroupMentalResistance(ChoreGroups __instance)
        {
            object choregroupObj;
            bool success = TbbHarmonyExtension.InvokeMethod(out choregroupObj, __instance, "Add", new object[] {
                ChoreGroupMentalResistanceID, STRINGS.CHOREGROUPS.MENTALRESISTANCE.NAME.ToString(),Db.Get().Attributes.TryGet(MutanterAttributes.AttributeMentalResistanceID),
                "icon_errand_dig",2, true
            });
        }
        public static void ChoreGroupDiscipline(ChoreGroups __instance)
        {
            object choregroupObj;
            bool success = TbbHarmonyExtension.InvokeMethod(out choregroupObj, __instance, "Add", new object[] {
                ChoreGroupDisciplineID, STRINGS.CHOREGROUPS.DISCIPLINE.NAME.ToString(),Db.Get().Attributes.TryGet(MutanterAttributes.AttributeSuccessRateID),
                "icon_errand_dig",2, true
            });
        }
        public static void ChoreGroupRighteousness(ChoreGroups __instance)
        {
            object choregroupObj;
            bool success = TbbHarmonyExtension.InvokeMethod(out choregroupObj, __instance, "Add", new object[] {
                ChoreGroupRighteousnessID, STRINGS.CHOREGROUPS.RIGHTEOUSNESS.NAME.ToString(),Db.Get().Attributes.TryGet(MutanterAttributes.AttributeAttackDamageID),
                "icon_errand_dig",2, true
            });
        }
    }
}
