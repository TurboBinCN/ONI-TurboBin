using HarmonyLib;
using MutantFarmLab.mutantplants;
using MutantFarmLab.tbbLibs;
using PeterHan.PLib.Core;
using System;
using UnityEngine;

namespace MutantFarmLab.patches
{
    /******************************************************************************
     * 补丁1：植株生成后执行【100%匹配源码】
     * 目标方法：PlantablePlot.ConfigureOccupyingObject(GameObject source)
     * 核心作用：1.关闭autoReplaceEntity，允许双株共存 2.给双头株挂载DHP组件 
     * 3.给第二株挂载DHCom组件
     *****************************************************************************/
    [HarmonyPatch(typeof(PlantablePlot), "ConfigureOccupyingObject")]
    public static class DualHeadPlotConfigPatch
    {
        [HarmonyPostfix]
        public static void Postfix(PlantablePlot __instance, GameObject newPlant)
        {
            if (__instance == null || newPlant == null) return;
            var receptacleGo = __instance.gameObject;
            var marker = receptacleGo.GetComponent<DualHeadReceptacleMarker>();

            // 情况1：新植物是双头变异株 → 成为第一株
            if (newPlant.TryGetComponent(out MutantPlant mutant)
                && mutant.MutationIDs?.Contains(PlantMutationRegister.DUAL_HEAD_MUT_ID) == true)
            {
                var dualHeadPlantCom = newPlant.AddOrGet<DualHeadPlantComponent>();
                dualHeadPlantCom.RootPlotGameObject = __instance.gameObject;
                dualHeadPlantCom.StartDualHead();

                if (marker == null)
                {
                    marker = receptacleGo.AddComponent<DualHeadReceptacleMarker>();
                }
                marker.primaryPlant = newPlant;
                PUtil.LogDebug($"[双头株] 母株挂载 DualHeadPlantComponent marker: [{marker.primaryPlant.name}]");
                // 锁定 receptacle
                __instance.autoReplaceEntity = false;
            }
            // 情况2：receptacle 已有 Marker → 此次是第二株
            else if (marker != null && marker.primaryPlant != null)
            {
                var secondDHP = newPlant.AddOrGet<DualHeadPlantComponent>();
                secondDHP.RootPlotGameObject = __instance.gameObject;
                secondDHP.StartDualHead();
                var firstDHP = marker.primaryPlant.GetComponent<DualHeadPlantComponent>();

                if (firstDHP != null && secondDHP != null)
                {
                    firstDHP.SetTwin(secondDHP);
                    // secondDHP.SetPartner(firstDHP); // SetPartner 内部会处理双向
                }
                PUtil.LogDebug($"[双头株] 子株挂载 DualHeadPlantComponent Partner: [子:{secondDHP.name} 母:{marker.primaryPlant.name}]");
            }
        }
    }
    /******************************************************************************
     * 补丁2：种植前判定执行【100%匹配源码】
     * 目标方法：SingleEntityReceptacle.IsValidEntity(GameObject candidate)
     * 核心作用：无植株走原生 / 有植株->挂载带DHP→允许种第二株
     *****************************************************************************/
    [HarmonyPatch(typeof(SingleEntityReceptacle), nameof(SingleEntityReceptacle.IsValidEntity))]
    public static class SingleEntityReceptacle_IsValidEntity_Patch
    {
        public static bool Prefix(SingleEntityReceptacle __instance, GameObject candidate, ref bool __result)
        {
            try
            {
                // 1. 确保这个 receptacle 属于可耕种地块
                var plot = __instance.GetComponent<PlantablePlot>();
                if (plot == null) return true; // 不是种植地块，走默认逻辑

                // 2. 获取当前已种的植物（如果有的话）
                GameObject existPlant = __instance?.Occupant;
                if (existPlant == null) return true; // ✅ 放行原生逻辑，不干预空地块种植
                                                     // 3. 检查当前已种是否是双头突变植物
                TbbDebuger.PrintGameObjectFullInfo(existPlant);
                var mutantComp = existPlant.GetComponent<MutantPlant>();
                if (mutantComp == null || !mutantComp.MutationIDs.Contains(PlantMutationRegister.DUAL_HEAD_MUT_ID))
                    return true; // 不是双头突变，走默认逻辑（拒绝第二株）

                // 4.检查当前已种植物是否挂载DHP组件 没有即挂载=====
                existPlant.AddOrGet<DualHeadPlantComponent>();

                PUtil.LogDebug($"[双头株] 所有 IsValidEntity 检查已完成 -> 允许种植第二株");
                __result = true; // ✅ 强制判定「合法可种植」
                return false;    // ✅ 终止原生逻辑，直接生效我们的判定结果
            }
            catch (Exception ex)
            {
                PUtil.LogError($"[双头株] 操作异常: {ex}");
                return true;
            }
            finally
            {
                __result = true;
            }

        }
    }



}