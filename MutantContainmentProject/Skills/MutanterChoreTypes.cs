using Database;
using TBB.He.TbbLib.Utils;

namespace MutantContainmentProject.Skills
{
    public class MutanterChoreTypes
    {
        public static readonly string ChoreTypeContainID = "Contain";
        public static readonly string ChoreTypeDefenseID = "Defense";
        public static readonly string ChoreTypeDisciplineID = "Discipline";
        public static readonly string ChoreTypeRighteousnessID = "Righteousness";

        public static void ChoreTypeContain(ChoreTypes __instance)
        {
            TbbHarmonyExtension.InvokeMethod(__instance, "Add", new object[] {
                ChoreTypeContainID, new string[]{ChoreTypeContainID}, "", new string[0], STRINGS.DUPLICANTS.CHORES.CONTAIN.NAME, STRINGS.DUPLICANTS.CHORES.CONTAIN.STATUS, STRINGS.DUPLICANTS.CHORES.CONTAIN.TOOLTIP, false, 5000, null
            });
        }
        public static void ChoreTypeDefense(ChoreTypes __instance)
        {
            TbbHarmonyExtension.InvokeMethod(__instance, "Add", new object[] {
                ChoreTypeDefenseID, new string[]{ChoreTypeDefenseID}, "", new string[0], STRINGS.DUPLICANTS.CHORES.DEFENSE.NAME, STRINGS.DUPLICANTS.CHORES.DEFENSE.STATUS, STRINGS.DUPLICANTS.CHORES.DEFENSE.TOOLTIP, false, 5000, null
            });
        }
        public static void ChoreTypeDiscipline(ChoreTypes __instance)
        {
            TbbHarmonyExtension.InvokeMethod(__instance, "Add", new object[] {
                ChoreTypeDisciplineID, new string[]{ChoreTypeDisciplineID}, "", new string[0], STRINGS.DUPLICANTS.CHORES.DISCIPLINE.NAME, STRINGS.DUPLICANTS.CHORES.DISCIPLINE.STATUS, STRINGS.DUPLICANTS.CHORES.DISCIPLINE.TOOLTIP, false, 5000, null
            });
        }
        public static void ChoreTypeRighteousness(ChoreTypes __instance)
        {
            TbbHarmonyExtension.InvokeMethod(__instance, "Add", new object[] {
                ChoreTypeRighteousnessID, new string[]{ChoreTypeRighteousnessID}, "", new string[0], STRINGS.DUPLICANTS.CHORES.RIGHTEOUSNESS.NAME, STRINGS.DUPLICANTS.CHORES.RIGHTEOUSNESS.STATUS, STRINGS.DUPLICANTS.CHORES.RIGHTEOUSNESS.TOOLTIP, false, 5000, null
            });
        }
    }
}
