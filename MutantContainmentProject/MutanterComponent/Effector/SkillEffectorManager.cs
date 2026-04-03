using System;
using System.Collections.Generic;
using System.Linq;
using TBB.He.TbbLib.Debuger;
using UnityEngine;
using static MutantContainmentProject.MutanterComponent.MutanterSkillComponent;

namespace MutantContainmentProject.MutanterComponent.Effector
{
    public class SkillEffectorManager : KMonoBehaviour
    {
        private Dictionary<string, Type> effetors = new();

        protected override void OnSpawn()
        {
            base.OnSpawn();
            RegisterEffectors();
        }
        protected override void OnCleanUp()
        {
            base.OnCleanUp();
        }

        private void RegisterEffectors()
        {
            var effctorTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(ISkillEffector).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);
            foreach (var type in effctorTypes)
            {
                if (type.GetCustomAttributes(typeof(SkillEffectorAttribute), false)
                    .FirstOrDefault() is SkillEffectorAttribute attribute)
                {
                    try
                    {
                        effetors.Add(attribute.Name, type);
                        TbbDebuger.LogDebug($"注册效果器 {attribute.Name}");
                    }
                    catch (Exception e)
                    {
                        TbbDebuger.LogError($"Failed to create effctorTypes instance: {e.Message}");
                    }
                }
            }
        }

        public void LoadEffectors(GameObject gameObject, List<SkillData> skills)
        {
            foreach (var skill in skills)
            {
                foreach (var effector in skill.attackEffectors)
                {
                    if (effetors.TryGetValue(effector.attackEffectorName, out Type effectorType))
                    {
                        if (gameObject.GetComponent(effectorType) is not ISkillEffector)
                        {
                            ISkillEffector component = gameObject.AddComponent(effectorType) as ISkillEffector;
                            TbbDebuger.LogDebug($"挂载效果器组件 {effectorType} [{component}] 到 {gameObject.name}");
                        }
                    }
                }
            }
        }
        public void ApplyEffectorsBefore(SkillData skill)
        {
            if (skill.attackEffectors.Count <= 0) return;
            foreach (var effector in skill.attackEffectors)
            {
                if (effetors.TryGetValue(effector.attackEffectorName, out Type effectorType))
                {
                    TbbDebuger.LogDebug($"应用效果器 {effector.attackEffectorName} ApplyEffectorsBefore");
                    var component = gameObject.GetComponent(effectorType) as ISkillEffector;
                    component?.ApplyEffectorsBefore();
                }
            }
        }
        public void ApplyEffectorsAfter(GameObject target, SkillData skill)
        {
            if (skill.attackEffectors.Count <= 0) return;
            foreach (var effector in skill.attackEffectors)
            {
                if (effetors.TryGetValue(effector.attackEffectorName, out Type effectorType))
                {
                    var component = gameObject.GetComponent(effectorType) as ISkillEffector;
                    TbbDebuger.LogDebug($"应用效果器 {effector.attackEffectorName} [{component}] ApplyEffectorsAfter");
                    component?.ApplyEffectorAfter(target, skill);
                }
            }
        }
    }
}
