using System;
using System.Collections.Generic;
using System.Linq;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent.Triggers
{
    public class SkillTriggerManager : KMonoBehaviour
    {
        private List<ISkillTrigger> triggers = new();
        private Dictionary<string, Type> passiveTriggers = new();

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            RegisterTriggers();
        }

        private void RegisterTriggers()
        {
            // 反射获取所有实现了ISkillTrigger接口的类
            var triggerTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(IPassiveSkillTrigger).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);

            foreach (var type in triggerTypes)
            {
                if (type.GetCustomAttributes(typeof(SkillTriggerAttribute), false)
                    .FirstOrDefault() is SkillTriggerAttribute attribute)
                {
                    try
                    {
                        if (attribute.IsPassive)
                        {
                            passiveTriggers.Add(attribute.Name, type);
                            TbbDebuger.LogDebug($"[SkillTriggerManager] 添加被动触发器: {attribute.Name} 实体： [{gameObject?.name}] 等待被动触发器挂载 ");
                        }
                        else
                        {
                            if (Activator.CreateInstance(type) is ISkillTrigger trigger)
                            {
                                triggers.Add(trigger);
                                TbbDebuger.LogDebug($"[SkillTriggerManager] 注册主动触发器: {trigger.TriggerName} 实体： [{gameObject?.name}]");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        TbbDebuger.LogWarning($"Failed to create trigger instance: {e.Message}");
                    }
                }else{
                    TbbDebuger.LogDebug($"[SkillTriggerManager] 注册触发器失败: {type.Name} 实体： [{gameObject?.name}]");
                }
            }

            // 根据优先级排序
            triggers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        /// <summary>
        /// 执行主动触发器的技能选择逻辑
        /// </summary>
        /// <param name="caster">施法者</param>
        /// <param name="target">目标</param>
        /// <param name="skills">可用技能列表</param>
        /// <returns>选中的技能索引，-1表示没有选中</returns>
        public int SelectSkill(GameObject caster, GameObject target, List<MutanterSkillComponent.SkillData> skills)
        {
            TbbDebuger.LogDebug($"[SkillTriggerManager] 执行主动触发器选择技能 for {caster.name} {target.name} triggers: {triggers.Count}");
            foreach (var trigger in triggers)
            {
                int skillIndex = trigger.SelectSkill(caster, target, skills);
                if (skillIndex != -1)
                {
                    var skill = skills[skillIndex];
                    trigger.OnTriggerActivated(caster, skill);
                    return skillIndex;
                }
            }

            return -1;
        }

        /// <summary>
        /// 执行被动触发器的逻辑
        /// </summary>
        /// <param name="caster">施法者</param>
        /// <param name="skills">可用技能列表</param>
        public void ExecutePassiveTriggers(GameObject caster, List<MutanterSkillComponent.SkillData> skills)
        {
            foreach (var skill in skills)
            {
                if (!skill.isPassiveSkill) continue;
                foreach (var trigger in skill.triggers)
                {
                    if (passiveTriggers.TryGetValue(trigger.triggerName, out Type triggerType))
                    {
                        if (gameObject.GetComponent(triggerType) is not IPassiveSkillTrigger component)
                        {
                            component = gameObject.AddComponent(triggerType) as IPassiveSkillTrigger;
                            TbbDebuger.LogDebug($"[SkillTriggerManager] 挂载被动触发器: {trigger.triggerName} 实体： [{gameObject?.name}]");
                        }
                        component.Skill = skill;
                    }else{
                        TbbDebuger.LogWarning($"[SkillTriggerManager] 挂载被动触发器: {trigger.triggerName} 实体： [{gameObject?.name}] 失败");
                    }
                }
            }
        }
    }
}
