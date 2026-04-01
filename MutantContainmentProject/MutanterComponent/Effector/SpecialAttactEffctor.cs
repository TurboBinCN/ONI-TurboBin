using Klei.AI;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent.Effector
{
    [SkillEffector("SpecialAttact", 10)]
    public class SpecialAttactEffctor : KMonoBehaviour, ISkillEffector
    {
        public string EffectorName => "SpecialAttact";

        public int Priority => 10;

        public bool ApplyEffectorAfter(GameObject target, MutanterSkillComponent.SkillData skillData)
        {
            TbbDebuger.LogDebug($"应用效果器 {EffectorName} ApplyEffectorAfter");
            if (skillData.attackEffectors?.Count <= 0) return false;
            foreach (var attackEffector in skillData.attackEffectors)
            {
                var effects = attackEffector.effects;
                if (effects?.Count > 0)
                {
                    var EffectsInstance = target.GetComponent<Effects>();
                    foreach (var effect in effects)
                    {
                        EffectsInstance?.Add(effect, true);
                    }
                }
                var kMonoBehaviours = attackEffector.kMonoBehaviours;
                if (kMonoBehaviours?.Count > 0)
                {
                    foreach (var kMonoBehaviour in kMonoBehaviours)
                    {
                        if (target.GetComponent(kMonoBehaviour)) continue;
                        TbbDebuger.LogDebug($"添加组件 {kMonoBehaviour}");
                        target.AddComponent(kMonoBehaviour);
                    }
                }
                var callbackMethods = attackEffector.callbackMethods;
                if (callbackMethods?.Count > 0)
                {
                    foreach (var callbackMethod in callbackMethods)
                    {
                        callbackMethod?.Invoke(gameObject);
                    }
                }
            }

            return true;
        }

        public bool ApplyEffectorsBefore()
        {
            return true;
        }
    }
}
