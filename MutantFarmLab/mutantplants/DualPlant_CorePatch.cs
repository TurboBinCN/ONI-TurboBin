using HarmonyLib;
using MutantFarmLab;
using MutantFarmLab.mutantplants;
using UnityEngine;

namespace MutantFarmLab.patches
{
    [HarmonyPatch(typeof(PlantablePlot), "ConfigureOccupyingObject")]
    public static class DualPlant_Attach
    {
        [HarmonyPostfix]
        public static void Postfix(PlantablePlot __instance, GameObject newPlant)
        {
            if (__instance == null || newPlant == null) return;

            // ✅ 核心修复：删除ToString()，直接用Tag匹配（致命错误！）
            if (newPlant.TryGetComponent(out MutantPlant mp)
                && mp.MutationIDs.Contains(DualHeadPlantComponent.DUAL_HEAD_PLANT_TAG.ToString()))
            {
                __instance.autoReplaceEntity = false;
                if (!newPlant.TryGetComponent(out DualHeadPlantComponent _))
                {
                    newPlant.AddComponent<DualHeadPlantComponent>();
                    Debug.Log($"[双头株] 成功挂载标记组件：{newPlant.name}");
                }
            }
        }
    }

    [HarmonyPatch(typeof(SingleEntityReceptacle), nameof(SingleEntityReceptacle.IsValidEntity))]
    public static class DualPlant_Valid
    {
        [HarmonyPrefix]
        public static bool Prefix(ref bool __result, SingleEntityReceptacle __instance)
        {
            if (__instance.Occupant != null && __instance.Occupant.TryGetComponent(out DualHeadPlantComponent _))
            {
                __result = true;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(SingleEntityReceptacle), nameof(SingleEntityReceptacle.ForceDeposit))]
    public static class DualPlant_ForceDeposit
    {
        [HarmonyPrefix]
        public static bool Prefix(SingleEntityReceptacle __instance, GameObject depositedObject)
        {
            if (__instance.Occupant != null && __instance.Occupant.TryGetComponent(out DualHeadPlantComponent _))
            {
                Traverse.Create(__instance).Method("OnDepositObject", depositedObject).GetValue();
                return false;
            }
            return true;
        }
    }

}