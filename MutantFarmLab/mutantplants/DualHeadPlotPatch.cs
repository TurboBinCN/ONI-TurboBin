using HarmonyLib;
using UnityEngine;
using MutantFarmLab.mutantplants;

namespace MutantFarmLab.patches
{
    /******************************************************************************
     * 补丁1：植株生成后执行【100%匹配源码】
     * 目标方法：SingleEntityReceptacle.ConfigureOccupyingObject(GameObject source)
     * 核心作用：1.关闭autoReplaceEntity，允许双株共存 2.给双头株挂载DHP组件
     *****************************************************************************/
    [HarmonyPatch(typeof(SingleEntityReceptacle), "ConfigureOccupyingObject")]
    public static class DualHeadPlotConfigPatch
    {
        [HarmonyPostfix]
        public static void Postfix(SingleEntityReceptacle __instance, GameObject source)
        {
            // 基础空值校验，杜绝空引用报错
            if (__instance == null || source == null) return;

            // 判定：新生成的植株是否为【双头变异株】（种子→植株变异属性已原生赋值）
            if (source.TryGetComponent(out MutantPlant mutantComp)
                && mutantComp.MutationIDs != null
                && mutantComp.MutationIDs.Contains(DualHeadPlantComponent.DUAL_HEAD_MUT_ID))
            {
                __instance.autoReplaceEntity = false; // ✅ 关闭替换规则，核心！双株共存的关键
                source.gameObject.AddOrGet<DualHeadPlantComponent>(); // ✅ 缺氧框架标准挂载写法，无重复
            }
        }
    }

    /******************************************************************************
     * 补丁2：种植前判定执行【100%匹配源码】
     * 目标方法：SingleEntityReceptacle.IsValidEntity(GameObject candidate)
     * 核心作用：严格按你的规则放行 → 无植株走原生 / 有植株+带DHP→允许种第二株
     *****************************************************************************/
    [HarmonyPatch(typeof(SingleEntityReceptacle), "IsValidEntity")]
    public static class DualHeadPlotValidPatch
    {
        public static bool Prefix(ref bool __result, SingleEntityReceptacle __instance, GameObject candidate)
        {
            // ===== 规则1：地块无植株 → 直接走原生判定逻辑 =====
            GameObject existPlant = __instance.Occupant;
            if (existPlant == null)
            {
                return true; // ✅ 放行原生逻辑，不干预空地块种植
            }

            // ===== 规则2：地块有植株 → 校验是否挂载DHP组件 =====
            bool hasDHPComponent = existPlant.TryGetComponent(out DualHeadPlantComponent _);
            if (hasDHPComponent)
            {
                __result = true; // ✅ 强制判定「合法可种植」
                return false;    // ✅ 终止原生逻辑，直接生效我们的判定结果
            }

            // ===== 兜底规则：有植株但无DHP（普通植株）→ 走原生判定，拒绝种植 =====
            return true;
        }
    }
}