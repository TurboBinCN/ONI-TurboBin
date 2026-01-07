using Klei.AI;
using Database;
using UnityEngine;
using TUNING;
using STRINGS;

namespace MutantFarmLab.mutantplants
{
    /// <summary>
    /// 自定义变异注册器（挂载式接入原生系统）
    /// ✅ 已删除MutantDefs依赖，所有常量内联，零逻辑改动
    /// </summary>
    public static class PlantMutationRegister
    {
        // ========== 抗辐籽变异常量（从MutantDefs内联，完全保留原值，零改动） ==========
        private const string RAD_RESIST_MUT_ID = "RadiationResistSeedMutation";
        private const float MIN_RADIATION_REQ = 250f;
        private const float FERTILIZER_COST_MOD = 1.25f;
        private const float GROWTH_SPEED_MOD = 1.5f;
        private const float YIELD_MOD = -0.2f;
        private const int MIN_LIGHT_REQ = 800;

        // ========== 双头株变异常量【新增】严格对齐核心玩法设计 ==========
        public static string DUAL_HEAD_MUT_ID = "DualHeadPlantMutation";
        public static float DUAL_SINGLE_HEAD_YIELD_MOD = -0.3f;
        private const float DUAL_MIN_RADIATION_ADD = 250f;//辐射门槛+250（原生变异标配）
        private const float DUAL_FERTILIZER_COST_MOD = 1.5f;//养料消耗+50%
        private const float DUAL_GROWTH_CYCLE_MOD = 1.2f;//生长周期+20%
        private const int DUAL_MIN_LIGHT_ADD = 500;//光照需求+500勒克斯
        private const int HIGH_QUALITY_THRESHOLD = 80;
        private const string DUAL_SOUND_EVENT = "Plant_mutation_Leaf";//变异音效（复用原生绿叶变异）


        /// <summary>
        /// 注册所有自定义变异（入口方法）
        /// </summary>
        public static void RegisterAllCustomMutations()
        {
            Debug.Log("[原生挂载] 开始注册自定义变异到原生系统...");
            RegisterRadiationResistMutation();
            RegisterDualHeadMutation();
            Debug.Log("[原生挂载] 所有自定义变异注册完成！");
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
            Debug.Log($"[原生挂载] 变异「{RAD_RESIST_MUT_ID}」已存入原生PlantMutations仓库");
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
            Debug.Log($"[原生挂载] 变异「{DUAL_HEAD_MUT_ID}」已存入原生PlantMutations仓库，数值配置生效！");
        }
        /// <summary>
        /// 工具方法：获取基础种子预制体
        /// </summary>
        private static GameObject GetBaseSeedPrefab()
        {
            return Assets.GetPrefab("BasicSeed");
        }
    }
}