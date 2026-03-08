using Klei.AI;
using MutantContainmentProject.Skills;
using TUNING;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    public class MutanterAttackable : AttackableBase
    {
        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            // 设置属性转换器为攻击伤害
            attributeConverter = Db.Get().AttributeConverters.Get(MutanterAttributeConverters.AttributeAttackDamageConverterID);
            // 设置经验获取倍数
            this.attributeExperienceMultiplier = DUPLICANTSTATS.ATTRIBUTE_LEVELING.MOST_DAY_EXPERIENCE;
            // 设置技能组为正义
            this.skillExperienceSkillGroup = MutanterSkillGroups.SkillGroupRighteousnessID;
            // 设置技能经验倍数
            this.skillExperienceMultiplier = SKILLS.MOST_DAY_EXPERIENCE;
        }

        // 获取攻击速度倍数
        public float GetAttackSpeedMultiplier()
        {
            if (this.worker != null)
            {
                var attackSpeedConverter = Db.Get().AttributeConverters.Get(MutanterAttributeConverters.AttributeAttackSpeedConverterID);
                if (attackSpeedConverter != null)
                {
                    var converterInstance = attackSpeedConverter.Lookup(this.worker.gameObject);
                    if (converterInstance != null)
                    {
                        return Mathf.Max(0.1f, 1f + converterInstance.Evaluate());
                    }
                }
            }
            return 1f;
        }

        public override float GetEfficiencyMultiplier(WorkerBase worker)
        {
            // 计算攻击效率倍数，基于小人的攻击伤害属性
            if (worker != null)
            {
                var attackDamageConverter = Db.Get().AttributeConverters.Get(MutanterAttributeConverters.AttributeAttackDamageConverterID);
                if (attackDamageConverter != null)
                {
                    var converterInstance = attackDamageConverter.Lookup(worker.gameObject);
                    if (converterInstance != null)
                    {
                        return Mathf.Max(1f + converterInstance.Evaluate(), 0.1f);
                    }
                }
            }
            return 1f;
        }

        public new float GetDamageMultiplier()
        {
            // 计算伤害倍数，基于小人的攻击伤害属性
            if (this.worker != null)
            {
                var attackDamageConverter = Db.Get().AttributeConverters.Get(MutanterAttributeConverters.AttributeAttackDamageConverterID);
                if (attackDamageConverter != null)
                {
                    var converterInstance = attackDamageConverter.Lookup(this.worker.gameObject);
                    if (converterInstance != null)
                    {
                        return Mathf.Max(1f + converterInstance.Evaluate(), 0.1f);
                    }
                }
            }
            return 1f;
        }
    }
}