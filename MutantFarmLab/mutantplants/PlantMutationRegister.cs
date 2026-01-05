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

        /// <summary>
        /// 注册所有自定义变异（入口方法）
        /// </summary>
        public static void RegisterAllCustomMutations()
        {
            Debug.Log("[原生挂载] 开始注册自定义变异到原生系统...");
            RegisterRadiationResistMutation();
            Debug.Log("[原生挂载] 所有自定义变异注册完成！");
        }

        /// <summary>
        /// 抗辐籽变异：完整原生挂载流程，零逻辑改动
        /// </summary>
        private static void RegisterRadiationResistMutation()
        {
            PlantMutation radMut = new PlantMutation(
                id: RAD_RESIST_MUT_ID,
                name: "抗辐籽变异",
                desc: "植株对辐射有适应性，成熟后额外产出抗辐籽，食用可清除辐射")
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
        /// 工具方法：获取基础种子预制体
        /// </summary>
        private static GameObject GetBaseSeedPrefab()
        {
            return Assets.GetPrefab("BasicSeed");
        }
    }
}