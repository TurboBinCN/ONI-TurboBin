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

        public override float GetEfficiencyMultiplier(WorkerBase worker)
        {
            // 计算攻击效率倍数，基于小人的攻击伤害属性
            if (worker != null)
            {
                var attribute = Db.Get().Attributes.Get(MutanterAttributes.AttributeAttackDamageID);
                if (attribute != null)
                {
                    return Mathf.Max(1f + worker.GetAttributeConverter(attribute.Id).Evaluate(), 0.1f);
                }
            }
            return 1f;
        }

        public new float GetDamageMultiplier()
        {
            // 计算伤害倍数，基于小人的攻击伤害属性
            if (this.worker != null)
            {
                var attribute = Db.Get().Attributes.Get(MutanterAttributes.AttributeAttackDamageID);
                if (attribute != null)
                {
                    return Mathf.Max(1f + this.worker.GetAttributeConverter(attribute.Id).Evaluate(), 0.1f);
                }
            }
            return 1f;
        }
    }
}