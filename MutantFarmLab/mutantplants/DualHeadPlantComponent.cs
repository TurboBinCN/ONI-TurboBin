using System;
using Klei.AI;
using KSerialization;
using UnityEngine;
using STRINGS;
using Database;

namespace MutantFarmLab.mutantplants
{
    [SerializationConfig(MemberSerialization.OptIn)]
    [AddComponentMenu("MutantFarmLab/DualHeadPlantComponent")]
    public class DualHeadPlantComponent : KMonoBehaviour
    {
        [Serialize] public float head1Maturity = 0f;
        [Serialize] public float head2Maturity = 0f;
        [Serialize] public int head1Quality = 0;
        [Serialize] public int head2Quality = 0;
        private const int HIGH_QUALITY_THRESHOLD = 80;
        public static readonly Tag DUAL_HEAD_TAG = TagManager.Create("DualHeadMutation");
        public static readonly Tag DUAL_HEAD_PLANT_TAG = TagManager.Create("DualHeadPlantMutation");
        private MutantPlant mutantPlant;
        private Crop cropComponent;
        private Klei.AI.Attributes attributes;
        private AttributeInstance harvestTimeAttr;
        private AttributeInstance yieldAmountAttr;

        public static string DUAL_HEAD_MUT_ID = PlantMutationRegister.DUAL_HEAD_MUT_ID;
        public static float DUAL_SINGLE_HEAD_YIELD_MOD = PlantMutationRegister.DUAL_SINGLE_HEAD_YIELD_MOD;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            // 仅获取核心组件，移除SeedProducer（变异植物不用）
            mutantPlant = GetComponent<MutantPlant>();
            cropComponent = GetComponent<Crop>();
            attributes = GetComponent<Modifiers>().attributes;

            // 正确获取官方属性实例
            harvestTimeAttr = attributes.Get(Db.Get().PlantAttributes.HarvestTime);
            yieldAmountAttr = attributes.Get(Db.Get().PlantAttributes.YieldAmount);

            // 仅变异植株激活逻辑
            if (mutantPlant != null && mutantPlant.MutationIDs.Contains(DUAL_HEAD_MUT_ID))
            {
                Subscribe((int)GameHashes.Harvest, OnHarvest);
                Subscribe((int)GameHashes.Grow, OnMaturityUpdate);
                Debug.Log("[双头株] 变异植株组件激活，无种子掉落！");
            }
            else
            {
                this.enabled = false;
            }
        }

        // 双头独立成熟更新（不变）
        private void OnMaturityUpdate(object data)
        {
            if (harvestTimeAttr == null) return;
            float growthSpeed = 1f / harvestTimeAttr.GetTotalValue();
            float deltaGrowth = Time.deltaTime * growthSpeed / 600f;

            head1Maturity = Mathf.Min(1f, head1Maturity + deltaGrowth);
            head2Maturity = Mathf.Min(1f, head2Maturity + deltaGrowth);

            if (head1Maturity >= 1f && head1Quality == 0)
                head1Quality = UnityEngine.Random.Range(0, 101);
            if (head2Maturity >= 1f && head2Quality == 0)
                head2Quality = UnityEngine.Random.Range(0, 101);
        }

        // ✅ 核心修改：移除所有种子生成逻辑，仅保留双头成熟判定+产量修正
        private void OnHarvest(object data)
        {
            if (cropComponent == null || mutantPlant == null
                || !mutantPlant.MutationIDs.Contains(DUAL_HEAD_MUT_ID))
                return;

            // 规则1：双头未熟 → 强制拦截收获
            if (head1Maturity < 1f || head2Maturity < 1f)
            {
                Debug.Log($"[双头株] 拒绝收获！头1进度：{head1Maturity * 100:F1}% | 头2进度：{head2Maturity * 100:F1}%");
                Trigger((int)GameHashes.Cancel);
                return;
            }

            // 规则2：双头成熟 → 产量修正（单头-30%，整体+40%）
            float baseYield = yieldAmountAttr.GetTotalValue();
            float singleHeadYield = baseYield * (1 + DUAL_SINGLE_HEAD_YIELD_MOD);
            float finalTotalYield = singleHeadYield * 2;

            // 直接生成修正后产量的作物，无种子掉落
            cropComponent.SpawnSomeFruit(new Tag(cropComponent.cropVal.cropId), finalTotalYield);
            Debug.Log($"[双头株] 收获成功！基础产量：{baseYield:F1} | 最终产量：{finalTotalYield:F1}");

            // 重置状态，准备下一轮生长
            ResetDualHeadState();
            Trigger((int)GameHashes.HarvestComplete);
        }

        private void ResetDualHeadState()
        {
            head1Maturity = 0f;
            head2Maturity = 0f;
            head1Quality = 0;
            head2Quality = 0;
        }
    }
}