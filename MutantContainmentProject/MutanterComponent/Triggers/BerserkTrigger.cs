using System.Collections.Generic;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent.Triggers
{
    [SkillTrigger("BerserkTrigger", 20, true)]
    public class BerserkTrigger : ISkillTrigger
    {
        public string TriggerName => "BerserkTrigger";
        public int Priority => 20;
        public bool IsPassive => true;

        public MutanterSkillComponent.SkillData Skill { get; set; }

        private const float HEALTH_THRESHOLD = 0.2f; // 生命值阈值（20%）
        private const float DAMAGE_REDUCTION = 0.8f; // 伤害减少80%
        
        public bool CheckCondition(GameObject caster, GameObject target = null)
        {
            var health = caster.GetComponent<Health>();
            if (health == null)
                return false;
            
            // 检查生命值是否低于阈值
            float healthPercentage = health.hitPoints / health.maxHitPoints;
            return healthPercentage <= HEALTH_THRESHOLD;
        }
        
        public int SelectSkill(GameObject caster, GameObject target, List<MutanterSkillComponent.SkillData> skills)
        {
            // 查找被动技能
            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i].isPassiveSkill)
                {
                    return i;
                }
            }
            
            return -1;
        }
        
        public void OnTriggerActivated(GameObject caster, MutanterSkillComponent.SkillData skill)
        {
            // 应用霸体效果：伤害减少80%
            var health = caster.GetComponent<Health>();
            if (health != null)
            {
                // 这里可以添加伤害减少的逻辑
                // 例如，添加一个Buff组件或者修改Health组件的伤害计算
                Debug.Log($"Berserk mode activated! Damage reduced by {DAMAGE_REDUCTION * 100}%");
            }
        }
    }
}
