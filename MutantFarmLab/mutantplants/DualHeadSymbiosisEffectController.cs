using Klei.AI;
using PeterHan.PLib.Core;
using System.Collections.Generic;
using UnityEngine;

namespace MutantFarmLab.mutantplants
{
    public class PlantStateSnapshot
    {
        public bool prefersDarkness;
        public float minLightLux;
        public List<AttributeModifier> attributeModifier = new();

    }
    public class DualHeadSymbiosisEffectController : KMonoBehaviour
    {
        public GameObject twin; // 配对的另一株
        public float maturity = 0;
        private PlantStateSnapshot _originalState;
        private bool _applied = false;

        public void ApplyEffect()
        {
            if (_applied) return;
            _applied = true;

            SaveOriginalState();
            SyncAttributesWithPartner();
        }

        public void RemoveEffect()
        {
            if (!_applied) return;
            _applied = false;

            RestoreOriginalState();
            twin = null;
        }

        private void SaveOriginalState()
        {
            var illum = GetComponent<IlluminationVulnerable>();
            _originalState = new PlantStateSnapshot
            {
                prefersDarkness = illum?.prefersDarkness ?? false,
                minLightLux = illum?.LightIntensityThreshold??0
            };
        }

        private void SyncAttributesWithPartner()
        {
            if (twin == null) return;

            //---Modifier：光照(MiniLightLux 取最大值，黑暗共享)
            var myIllum = GetComponent<IlluminationVulnerable>();
            var partnerIllum = twin.GetComponent<IlluminationVulnerable>();
            if (myIllum == null || partnerIllum == null)
            {
                myIllum = gameObject.AddOrGet<IlluminationVulnerable>();
                partnerIllum = twin.AddOrGet<IlluminationVulnerable>();
            }
            bool sharedDark = _originalState.prefersDarkness ||
                             (partnerIllum?.prefersDarkness ?? false);
            if (myIllum != null) {
                PUtil.LogDebug($"[双头株] 设置[{gameObject.name}]修改项-黑暗 ");
                myIllum.prefersDarkness = sharedDark; 
            }

            if(!sharedDark){
                float partnerMinLux = partnerIllum.LightIntensityThreshold;
                float targetMinLux = Mathf.Max(_originalState.minLightLux, partnerMinLux);

                // 应用差值 modifier
                float delta = targetMinLux - _originalState.minLightLux;
                if (delta != 0)
                {
                    _originalState.attributeModifier.Add(new AttributeModifier(
                        Db.Get().PlantAttributes.MinLightLux.Id,
                        delta,
                        "Dual-Head Symbiosis"
                    ));
                }
            }
            //----Modifier: 肥料 减半
            _originalState.attributeModifier.Add(new AttributeModifier(
                Db.Get().PlantAttributes.FertilizerUsageMod.Id,
                PlantMutationRegister.DUAL_HEAD_SYMBIOSIS_FertilizerUsageMod, // 减半（假设原值为1.0）
                "Dual-Head Symbiosis"
            ));
            //----Modifier: 生长周期 加权平均
            if(maturity == 0){
                float myMaturity = gameObject.GetAmounts().Get(Db.Get().Amounts.Maturity).maxAttribute.GetTotalValue();
                float twinMaturity = twin.GetAmounts().Get(Db.Get().Amounts.Maturity).maxAttribute.GetTotalValue();
                float maxValue = Mathf.Max(myMaturity, twinMaturity);
                float minValue = Mathf.Min(myMaturity, twinMaturity);

                float k = PlantMutationRegister.DUAL_HEAD_SYMBIOSIS_Maturity_RATIO;
                maturity = ((k / (1 + k)) * maxValue + (1 / (1 + k)) * minValue);

                twin.AddOrGet<DualHeadSymbiosisEffectController>().maturity = maturity;
            }
            float finalDelta = GetMaturityModifierDelta(gameObject,maturity);
            PUtil.LogDebug($"[双圣株] maturity：[{maturity}] finalDelta：[{finalDelta}]");
            _originalState.attributeModifier.Add(new AttributeModifier(
                Db.Get().Amounts.Maturity.maxAttribute.Id,
                finalDelta,
                "Dual-Head Symbiosis",
                true,false,true
            ));
            Growing growingA = gameObject.GetComponent<Growing>();
            AmountInstance growiingAAmountInstance = TbbHarmonyExtension.GetField(growingA, "maturity") as AmountInstance;
            growiingAAmountInstance.SetValue(twin.GetComponent<Growing>().PercentGrown()*maturity);
            //----Modifier: 收获增益
            _originalState.attributeModifier.Add(new AttributeModifier(
                Db.Get().PlantAttributes.YieldAmount.Id,
                PlantMutationRegister.DUAL_HEAD_SYMBIOSIS_YIELD_MOD,
                "Dual-Head Symbiosis",
                true, false, true
            ));
            foreach (var attr in _originalState.attributeModifier)
            {
                gameObject.GetComponent<Modifiers>().attributes.Add( attr );
                // 注意：这里我们仍依赖 attribute 查询是否实时生效
                PUtil.LogDebug($"[双头株] 设置[{gameObject.name}] 修改项 [{attr.AttributeId}] 最终值:[{gameObject.GetComponent<Modifiers>().attributes.Get(attr.AttributeId).GetTotalValue()}]");
            }
        }

        private void RestoreOriginalState()
        {
            // 恢复 prefersDarkness
            var illum = GetComponent<IlluminationVulnerable>();
            if (illum != null) illum.prefersDarkness = _originalState.prefersDarkness;

            // 移除所有我们添加的 modifiers
            var modifiers = gameObject.GetComponent<Modifiers>();
            if (modifiers != null)
            {
                // 注意：需要保存添加的 modifier 实例才能精准移除
                foreach (var modifier in _originalState.attributeModifier)
                {
                    if (modifier != null) modifiers.attributes.Remove(modifier);
                    PUtil.LogDebug($"[双头株]Remove AttrModifier [{modifier.AttributeId}] totalValue:[{gameObject.GetComponent<Modifiers>().attributes.Get(modifier.AttributeId).GetTotalValue()}]");
                }
            }
            PUtil.LogDebug($"[双头株] 恢复[{gameObject.name}]为原始状态。");
        }
        public float GetMaturityModifierDelta(GameObject plant,float targetMaturity)
        {
            AttributeInstance Instance = plant.GetComponent<Modifiers>().attributes.Get(Db.Get().Amounts.Maturity.maxAttribute.Id);
            ArrayRef<AttributeModifier> modifiers = Instance.Modifiers;
            float mutiplierNum = 0;
            float basevalue = Instance.Attribute.BaseValue;
            float sumNum = basevalue;
            for (int i = 0;i<modifiers.Count; i++)
            {
                if (!modifiers[i].UIOnly)
                {
                    if (modifiers[i].IsMultiplier) mutiplierNum += modifiers[i].Value;
                    else sumNum += modifiers[i].Value;
                }
            }
            return targetMaturity / sumNum - 1f - mutiplierNum;
        }
    }
}
