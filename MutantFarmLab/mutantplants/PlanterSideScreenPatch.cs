using HarmonyLib;
using PeterHan.PLib.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MutantFarmLab.mutantplants
{
    [HarmonyPatch]
    public class PlanterSideScreenPatch
    {
        static MethodBase TargetMethod() {
            return AccessTools.Method(typeof(PlanterSideScreen), "get_selectedSubspecies");
        }
        [HarmonyPrefix]
        public static bool Prefix(PlanterSideScreen __instance, ref Tag  __result)
        {
            PUtil.LogDebug($"[get_selectedSubspecies] Prefix called.");
            PUtil.LogDebug($"[get_selectedSubspecies] this.targetReceptacle: {PlantMigrationHelper.GetField(__instance, "targetReceptacle")?? "NULL"}");
            if(PlantMigrationHelper.GetField(__instance, "targetReceptacle") == null)
            {
                __result = null;
                return false;
            }
            return true; 
        }
    }
    [HarmonyPatch]
    public class PlanterSideScreenPatch2
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(PlanterSideScreen), "set_selectedSubspecies");
        }
        [HarmonyPrefix]
        public static bool Prefix(PlanterSideScreen __instance, Tag value)
        {
            PUtil.LogDebug($"[set_selectedSubspecies] Prefix called.");
            PUtil.LogDebug($"[set_selectedSubspecies] this.targetReceptacle: {PlantMigrationHelper.GetField(__instance, "targetReceptacle") ?? "NULL"}");

            if (PlantMigrationHelper.GetField(__instance, "targetReceptacle") == null)
            {
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch]
    public class ReceptacleSideScreenPatch
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(ReceptacleSideScreen), "OnOccupantValidChanged");
        }
        [HarmonyPrefix]
        public static bool Prefix(ReceptacleSideScreen __instance, object _)
        {
            PUtil.LogDebug($"[OnOccupantValidChanged] Prefix called.");
            PUtil.LogDebug($"[OnOccupantValidChanged] this.targetReceptacle: {PlantMigrationHelper.GetField(__instance, "targetReceptacle") ?? "NULL"}");

            return true;
        }
    }
}
