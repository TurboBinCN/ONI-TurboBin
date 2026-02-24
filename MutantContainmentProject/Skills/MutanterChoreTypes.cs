using Database;
using TBB.He.TbbLib.Utils;

namespace MutantContainmentProject.Skills
{
    public class MutanterChoreTypes
    {
        public static readonly string ChoreTypeContainID = "Contain";
        public static readonly string ChoreTypeMentalResistanceID = "MentalResistance";
        public static readonly string ChoreTypeDisciplineID = "Discipline";
        public static readonly string ChoreTypeRighteousnessID = "Righteousness";

        public static void ChoreTypeContain(ChoreTypes __instance)
        {
            TbbHarmonyExtension.InvokeMethod(__instance, "Add", new object[] {
                ChoreTypeContainID, new string[]{ChoreTypeContainID}, "", new string[0], STRINGS.CHORES.CONTAIN.NAME, STRINGS.CHORES.CONTAIN.STATUS, STRINGS.CHORES.CONTAIN.TOOLTIP, false, 5000, null
            });
        }
        public static void ChoreTypeMentalResistance(ChoreTypes __instance)
        {
            TbbHarmonyExtension.InvokeMethod(__instance, "Add", new object[] {
                ChoreTypeMentalResistanceID, new string[]{ChoreTypeMentalResistanceID}, "", new string[0], STRINGS.CHORES.MENTALRESISTANCE.NAME, STRINGS.CHORES.MENTALRESISTANCE.STATUS, STRINGS.CHORES.MENTALRESISTANCE.TOOLTIP, false, 5000, null
            });
        }
        public static void ChoreTypeDiscipline(ChoreTypes __instance)
        {
            TbbHarmonyExtension.InvokeMethod(__instance, "Add", new object[] {
                ChoreTypeDisciplineID, new string[]{ChoreTypeDisciplineID}, "", new string[0], STRINGS.CHORES.DISCIPLINE.NAME, STRINGS.CHORES.DISCIPLINE.STATUS, STRINGS.CHORES.DISCIPLINE.TOOLTIP, false, 5000, null
            });
        }
        public static void ChoreTypeRighteousness(ChoreTypes __instance)
        {
            TbbHarmonyExtension.InvokeMethod(__instance, "Add", new object[] {
                ChoreTypeRighteousnessID, new string[]{ChoreTypeRighteousnessID}, "", new string[0], STRINGS.CHORES.RIGHTEOUSNESS.NAME, STRINGS.CHORES.RIGHTEOUSNESS.STATUS, STRINGS.CHORES.RIGHTEOUSNESS.TOOLTIP, false, 5000, null
            });
        }
    }
}
