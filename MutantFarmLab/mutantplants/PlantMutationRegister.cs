using Database;
using HarmonyLib;
using Klei.AI;
using PeterHan.PLib.Core;
using STRINGS;
using TUNING;
using UnityEngine;

namespace MutantFarmLab.mutantplants
{
    /// <summary>
    /// 自定义变异注册器（挂载式接入原生系统）
    /// ✅ 已删除MutantDefs依赖，所有常量内联，零逻辑改动
    /// </summary>
    public static class PlantMutationRegister
    {
        // ========== 抗辐籽变异 ==========
        private const string RAD_RESIST_MUT_ID = "RadiationResistSeedMutation";
        private const float MIN_RADIATION_REQ = 250f;
        private const float FERTILIZER_COST_MOD = 1.25f;
        private const float GROWTH_SPEED_MOD = 1.5f;
        private const float YIELD_MOD = -0.2f;
        private const int MIN_LIGHT_REQ = 800;

        // ========== 双头株变异 ==========
        public static bool DUAL_HEAD_ENABLED = false; //双头株变异开关
        public static string DUAL_HEAD_MUT_ID = "DualHeadPlantMutation";
        public static float DUAL_SINGLE_HEAD_YIELD_MOD = -0.3f;
        private const float DUAL_MIN_RADIATION_ADD = 250f;//辐射门槛+250（原生变异标配）
        private const float DUAL_FERTILIZER_COST_MOD = 1.5f;//养料消耗+50%
        private const float DUAL_GROWTH_CYCLE_MOD = 1.2f;//生长周期+20%
        private const int DUAL_MIN_LIGHT_ADD = 500;//光照需求+500勒克斯
        private const string DUAL_SOUND_EVENT = "Plant_mutation_Leaf";//变异音效（复用原生绿叶变异）

        //== 辐光菌==
        public static string ACTINO_MUT_ID = "ActinobacteriaMutation";
        public static float ACTINO_FERTILIZER_COST_MOD = -0.6f; //养料消耗-60%
        public static float ACTINO_IRRAGATION_COST = -0.6f; //灌溉消耗-60%
        public static float ACTINO_GROWTH_CYCLE_MOD = 4.0f; //生长周期+400%
        public static int ACTINO_LIGHT_LUX = 1800; //光照强度1800lux
        public static int ACTINO_MIN_RADIATION = 60; //辐射强度
        public static float ACTINO_TEMP_RANGE_MOD = 0.6f;//生存温度范围+60%

        //==原油富集==
        public static string OIL_ENRICH_MUT_ID = "OilEnrichMutation";
        public static float OIL_ENRICH_CARBONGAS_MOD = 0.6667f; //二氧化碳消耗100kg/600s = 0.6667kg/s
        public static float OIL_ENRICH_YIELD_MOD = -0.5f; //产量-50%
        public static float OIL_ENRICH_GROWTH_CYCLE_MOD = 2f; //生长周期+200%
        private const float OIL_ENRICH_MIN_RADIATION_ADD = 250f;//辐射门槛+250
        /// <summary>
        /// 注册所有自定义变异（入口方法）
        /// </summary>
        public static void RegisterAllCustomMutations()
        {
            PUtil.LogDebug("[原生挂载] 开始注册自定义变异到原生系统...");
            RegisterRadiationResistMutation();
            if(DUAL_HEAD_ENABLED) RegisterDualHeadMutation();
            RegisterActinobacteriaMutation();
            RegisterOilEnrichMutation();
            PUtil.LogDebug("[原生挂载] 所有自定义变异注册完成！");
        }

        /// <summary>
        /// 抗辐籽变异：完整原生挂载流程，零逻辑改动
        /// </summary>
        private static void RegisterRadiationResistMutation()
        {
            PlantMutation radMut = new PlantMutation(
                id: RAD_RESIST_MUT_ID,
                name: STRINGS.ELEMENT.RADSEED.NAME,
                desc: STRINGS.ELEMENT.RADSEED.DESC)
                .AttributeModifier(Db.Get().PlantAttributes.MinRadiationThreshold, MIN_RADIATION_REQ, false)
                .AttributeModifier(Db.Get().PlantAttributes.FertilizerUsageMod, FERTILIZER_COST_MOD - 1f, true)
                .AttributeModifier(Db.Get().Amounts.Maturity.maxAttribute, GROWTH_SPEED_MOD - 1f, true)
                .AttributeModifier(Db.Get().PlantAttributes.MinLightLux, MIN_LIGHT_REQ, false)
                .AttributeModifier(Db.Get().PlantAttributes.YieldAmount, YIELD_MOD, false)
                .BonusCrop(RadiationResistSeedConfig.ID, 1f)
                .VisualTint(0.1f, 0.3f, 0.5f)
                .AddSoundEvent("Plant_mutation_Leaf");

            Db.Get().PlantMutations.Add(radMut);
            PUtil.LogDebug($"[原生挂载] 变异「{RAD_RESIST_MUT_ID}」已存入原生PlantMutations仓库");
        }

        /// <summary>
        /// 【核心新增】双头株变异：完整原生挂载流程，落地所有核心玩法
        /// ✅ 养料+50% | 周期+20% | 单头-30% | 光照+500lux | 辐射+250
        /// ✅ 适配双头独立成熟、同步收获、分头品质判定、优质掉种逻辑
        /// </summary>
        private static void RegisterDualHeadMutation()
        {
            // 1. 创建双头株变异实例，配置核心数值属性（严格对齐设计）
            PlantMutation dualHeadMut = new PlantMutation(
                    id: DUAL_HEAD_MUT_ID,
                    name: STRINGS.ELEMENT.DULHEAD.NAME,
                    desc: STRINGS.ELEMENT.DULHEAD.DESC)
                // ✅ 基础门槛：最小辐射+250（原生辐射变异标配，必加）
                .AttributeModifier(Db.Get().PlantAttributes.MinRadiationThreshold, DUAL_MIN_RADIATION_ADD, false)
                // ✅ 养料消耗+50%（倍率模式，原生规范：传「增量值=目标值-1」）
                .AttributeModifier(Db.Get().PlantAttributes.FertilizerUsageMod, DUAL_FERTILIZER_COST_MOD - 1f, true)
                // ✅ 生长周期+20%（Maturity.max越大，生长越慢，严格对应周期延长）
                .AttributeModifier(Db.Get().Amounts.Maturity.maxAttribute, DUAL_GROWTH_CYCLE_MOD - 1f, true)
                // ✅ 光照需求+500勒克斯（基础值叠加，提升种植门槛）
                .AttributeModifier(Db.Get().PlantAttributes.MinLightLux, DUAL_MIN_LIGHT_ADD, false)
                // ✅ 单头产量-30%（最终整体产量+70%，由双头机制兜底）
                .AttributeModifier(Db.Get().PlantAttributes.YieldAmount, DUAL_SINGLE_HEAD_YIELD_MOD, true)
                // ✅ 视觉标识：嫩绿色调，区分原生变异，贴合双生植物特征
                .VisualTint(0.2f, 0.4f, 0.2f)
                // ✅ 变异音效：复用原生绿叶变异音效，保证沉浸感
                .AddSoundEvent(DUAL_SOUND_EVENT);

            // 2. 关键：将双头株变异存入原生PlantMutations仓库（与你的抗辐籽逻辑完全一致）
            Db.Get().PlantMutations.Add(dualHeadMut);
            PUtil.LogDebug($"[原生挂载] 变异「{DUAL_HEAD_MUT_ID}」已存入原生PlantMutations仓库，数值配置生效！");
        }
        private static void RegisterActinobacteriaMutation()
        {
            PlantMutation actinoMut = new PlantMutation(
                id: ACTINO_MUT_ID,
                name: STRINGS.ELEMENT.ACTINO.NAME,
                desc: "A mutation that allows plants to thrive in low light and nutrient conditions.")
                .AttributeModifier(Db.Get().PlantAttributes.FertilizerUsageMod, ACTINO_FERTILIZER_COST_MOD, true)
                .AttributeModifier(Db.Get().Amounts.Irrigation.maxAttribute, ACTINO_IRRAGATION_COST, true)
                .AttributeModifier(Db.Get().PlantAttributes.WiltTempRangeMod, ACTINO_TEMP_RANGE_MOD,true)
                .AttributeModifier(Db.Get().Amounts.Maturity.maxAttribute, ACTINO_GROWTH_CYCLE_MOD, true)
                .VisualSymbolOverride("snapTo_mutate1", "mutantfarmlab_mutant_snaps_kanim", "light1")
                .VisualSymbolOverride("snapTo_mutate2", "mutantfarmlab_mutant_snaps_kanim", "light");
            Db.Get().PlantMutations.Add(actinoMut);
            PUtil.LogDebug($"[原生挂载] 变异「{ACTINO_MUT_ID}」已存入原生PlantMutations仓库");
        }
        private static void RegisterOilEnrichMutation()
        {
            PlantMutation oilEnrichMut = new PlantMutation(
                id: OIL_ENRICH_MUT_ID,
                name: STRINGS.ELEMENT.OILENRICH.NAME,
                desc: STRINGS.ELEMENT.OILENRICH.DESC)
                .AttributeModifier(Db.Get().PlantAttributes.MinRadiationThreshold, OIL_ENRICH_MIN_RADIATION_ADD, false)
                .AttributeModifier(Db.Get().PlantAttributes.YieldAmount, OIL_ENRICH_YIELD_MOD, true)
                .AttributeModifier(Db.Get().Amounts.Maturity.maxAttribute, OIL_ENRICH_GROWTH_CYCLE_MOD, true)
                .VisualTint(0f, 0f, 0.0f)
                .AddSoundEvent("Plant_mutation_Leaf");
            Db.Get().PlantMutations.Add(oilEnrichMut);

            PUtil.LogDebug($"[原生挂载] 变异「{OIL_ENRICH_MUT_ID}」已存入原生PlantMutations仓库");
        }
        /// <summary>
        /// 工具方法：获取基础种子预制体
        /// </summary>
        private static GameObject GetBaseSeedPrefab()
        {
            return Assets.GetPrefab("BasicSeed");
        }
        //给每个变异株生成时确保挂载组件
        [HarmonyPatch(typeof(MutantPlant), "OnSpawn")]
        public static class EnsureComponentOnSpawn
        {
            [HarmonyPostfix]
            public static void Postfix(MutantPlant __instance)
            {
                if (__instance.gameObject.HasTag(GameTags.Plant))
                {
                    if(__instance.MutationIDs?.Contains(PlantMutationRegister.ACTINO_MUT_ID) == true)
                        __instance.gameObject.AddOrGet<ActinoMutantionComponent>();
                    else if (DUAL_HEAD_ENABLED && __instance.MutationIDs?.Contains(PlantMutationRegister.DUAL_HEAD_MUT_ID) == true){
                        //功能暂时关掉，严重问题没解决
                        __instance.gameObject.AddOrGet<DualHeadPlantComponent>();
                    }
                    else if (__instance.MutationIDs?.Contains(PlantMutationRegister.OIL_ENRICH_MUT_ID) == true)
                        __instance.gameObject.AddOrGet<OilEnrichedMutantComponent>();

                }
            }
        }
    }
}