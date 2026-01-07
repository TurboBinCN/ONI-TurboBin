// DualHeadPlantComponent.cs
using Klei.AI;
using MutantFarmLab.tbbLibs;
using PeterHan.PLib.Core;
using System.Collections.Generic;
using UnityEngine;

namespace MutantFarmLab.mutantplants
{
    public class DualHeadReceptacleMarker : KMonoBehaviour
    {
        public GameObject primaryPlant;
    }
    public class DualHeadPlantComponent : KMonoBehaviour
    {
        public DualHeadPlantComponent twin;
        public PlantablePlot plot;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            var receptacleGo = plot?.gameObject;
            if (receptacleGo == null) return;
            DualHeadReceptacleMarker marker = receptacleGo.AddOrGet<DualHeadReceptacleMarker>();
            // 判断是否为“主株”：带有 DUAL_HEAD_MUT_ID 的 MutantPlant
            if (TryGetComponent(out MutantPlant mp) &&
                             mp.MutationIDs?.Contains(PlantMutationRegister.DUAL_HEAD_MUT_ID) == true)
            {
                // === 主株逻辑 ===
                if(marker.primaryPlant == null) marker.primaryPlant = gameObject;//为何丢失主株信息?
            }
            // === 子株逻辑 ===
            if (marker != null && marker.primaryPlant != gameObject)
            {
                // 找到主株，尝试配对
                var primaryComp = marker.primaryPlant.GetComponent<DualHeadPlantComponent>();
                if (primaryComp != null)
                {
                    PUtil.LogDebug($"[双头株] 开始双向配对与应用双头增益");
                    // 双向配对
                    SetTwin(primaryComp);

                    // 应用双头增益（由子株触发）
                    ApplyDualHeadBonuses(marker.primaryPlant, gameObject);
                }
            }
        }

        private void ApplyDualHeadBonuses(GameObject primary, GameObject secondary)
        {
            EstablishSymbiosis(primary, secondary);
        }

        protected override void OnCleanUp()
        {
            // 如果是主株，清理 receptacle 上的 marker 引用
            if (TryGetComponent(out MutantPlant mp) &&
                mp.MutationIDs?.Contains(PlantMutationRegister.DUAL_HEAD_MUT_ID) == true)
            {
                var receptacleGo = transform.parent?.gameObject;
                if (receptacleGo != null)
                {
                    var marker = receptacleGo.GetComponent<DualHeadReceptacleMarker>();
                    if (marker != null && marker.primaryPlant == gameObject)
                    {
                        marker.primaryPlant = null;
                        // 不 Destroy(marker)，保留组件避免反复 Add/Remove
                    }
                }
            }
            //清理增益
            BreakSymbiosis(gameObject);
            Unpair();
            base.OnCleanUp();
        }
        void BreakSymbiosis(GameObject plant)
        {
            Effects effectsComp = plant.gameObject.GetComponent<Effects>();
            if (effectsComp != null && effectsComp.HasEffect(MutantEffects.DUAL_HEAD_SYMBIOSIS)){ 
                effectsComp.RemoveImmunity(Db.Get().effects.Get(MutantEffects.DUAL_HEAD_SYMBIOSIS), "失去双生效果");
                var controller = plant.GetComponent<DualHeadSymbiosisEffectController>();
                controller.RemoveEffect();
            }

            effectsComp = plant.GetComponent<DualHeadPlantComponent>()?.twin.gameObject.GetComponent<Effects>();
            if (effectsComp != null && effectsComp.HasEffect(MutantEffects.DUAL_HEAD_SYMBIOSIS))
            {
                effectsComp.RemoveImmunity(Db.Get().effects.Get(MutantEffects.DUAL_HEAD_SYMBIOSIS), "失去双生效果");
                var controller = plant.GetComponent<DualHeadPlantComponent>().twin.GetComponent<DualHeadSymbiosisEffectController>();
                controller.RemoveEffect();
                Object.DestroyImmediate(controller);
            }

        }
        void EstablishSymbiosis(GameObject plantA, GameObject plantB)
        {
            Effects effectsComp = plantA.gameObject.AddOrGet<Effects>();
            if (effectsComp != null && !effectsComp.HasEffect(MutantEffects.DUAL_HEAD_SYMBIOSIS))
            {
                effectsComp.Add(MutantEffects.DUAL_HEAD_SYMBIOSIS, true);
                var controller = plantA.AddOrGet<DualHeadSymbiosisEffectController>();
                controller.twin = plantB;
                controller.ApplyEffect();
            }

            effectsComp = plantB.gameObject.AddOrGet<Effects>();
            if (effectsComp != null && !effectsComp.HasEffect(MutantEffects.DUAL_HEAD_SYMBIOSIS))
            {
                effectsComp.Add(MutantEffects.DUAL_HEAD_SYMBIOSIS, true);
                var controller = plantB.AddOrGet<DualHeadSymbiosisEffectController>();
                controller.twin = plantB;
                controller.ApplyEffect();
            }
        }
        public void SetTwin(DualHeadPlantComponent p)
        {
            if (twin == p) return;
            Unpair();
            twin = p;
            if (p != null && p.twin != this)
            {
                p.twin = this;
            }
        }

        public void Unpair()
        {
            if (twin != null)
            {
                // 断开双向引用
                if (twin.twin == this)
                {
                    twin.twin = null;
                }
                twin = null;
            }
        }
    }
}