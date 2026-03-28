using System.Collections.Generic;
using System.Linq;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent.Triggers
{
    [SkillTrigger("DistanceTrigger", 10)]
    public class DistanceTrigger : ISkillTrigger
    {
        public string TriggerName => "DistanceTrigger";
        public int Priority => 10;
        public bool IsPassive => false;

        public MutanterSkillComponent.SkillData Skill { get; set; }

        private float maxDistance = 5f; // 默认最大距离

        public DistanceTrigger() { }

        public DistanceTrigger(float maxDistance)
        {
            this.maxDistance = maxDistance;
        }

        public bool CheckCondition(GameObject caster, GameObject target = null)
        {
            if (target == null)
                return false;

            int targetCell = Grid.PosToCell(target.transform.position);
            int currentCell = Grid.PosToCell(caster.transform.position);
            float distance = Mathf.Abs(Grid.CellToPos2D(targetCell).x - Grid.CellToPos2D(currentCell).x);

            return distance <= maxDistance;
        }

        public int SelectSkill(GameObject caster, GameObject target, List<MutanterSkillComponent.SkillData> skills)
        {
            int selectedSkillIndex = -1;
            float highestDamage = 0f;

            for (int i = 0; i < skills.Count; i++)
            {
                var skill = skills[i];
                
                if (!skill.isPassiveSkill && (skill.isFirstUse || Time.time - skill.lastUseTime >= skill.cooldown))
                {
                    // 查找当前技能的DistanceTrigger配置
                    if (skill.triggers != null)
                    {
                        var distanceTriggerData = skill.triggers.FirstOrDefault(t => t.triggerName == "DistanceTrigger");
                        if (!distanceTriggerData.IsNullOrDestroyed())
                        {
                            // 解析Range属性
                            float skillMaxDistance = maxDistance; // 默认使用技能的range
                            if (distanceTriggerData.properties != null && distanceTriggerData.properties.TryGetValue("Range", out var rangeValue))
                            {
                                if (rangeValue is int intRange)
                                {
                                    skillMaxDistance = intRange;
                                }
                                else if (rangeValue is float floatRange)
                                {
                                    skillMaxDistance = floatRange;
                                }
                            }

                            // 计算距离
                            float distance = CalculateDistance(caster, target);

                            if (distance <= skillMaxDistance)
                            {
                                // 获取技能伤害值
                                float skillDamage = GetSkillDamage(skill);
                                if (selectedSkillIndex == -1 || skillDamage > highestDamage)
                                {
                                    selectedSkillIndex = i;
                                    highestDamage = skillDamage;
                                }
                            }
                        }
                    }
                }
            }

            return selectedSkillIndex;
        }

        private float CalculateDistance(GameObject caster, GameObject target)
        {
            int targetCell = Grid.PosToCell(target.transform.position);
            int currentCell = Grid.PosToCell(caster.transform.position);
            Vector2 targetPos = Grid.CellToPos2D(targetCell);
            Vector2 currentPos = Grid.CellToPos2D(currentCell);
            return Vector2.Distance(targetPos, currentPos);
        }

        private float GetSkillDamage(MutanterSkillComponent.SkillData skill)
        {
            // 从攻击效果中计算总伤害
            float totalDamage = 0f;
            if (skill.attackEffectors != null)
            {
                foreach (var effector in skill.attackEffectors)
                {
                    totalDamage += effector.damageAmount;
                }
            }
            return totalDamage;
        }

        public void OnTriggerActivated(GameObject caster, MutanterSkillComponent.SkillData skill)
        {
            // 触发器激活时的回调
        }

    }
}

