using Klei.AI;
using MutantContainmentProject.MutanterEffect;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.Buildings
{
    public class MutanterSecurableMonitor : GameStateMachine<MutanterSecurableMonitor, MutanterSecurableMonitor.Instance, IStateMachineTarget, MutanterSecurableMonitor.Def>
    {
        public override void InitializeStates(out StateMachine.BaseState default_state)
        {
            default_state = this.root;
            //this.root.ToggleBehaviour(MutanterTags.ShouldBeSeCured, (smi) => smi.ShouldBeSecured(), null);
        }
        public class Instance : GameInstance
        {
            public ContainmentMonitor.Instance _targetContainmentMonitor;
            private Instance states;
            private const float timeRemainningWarning = 200f;

            public Instance States
            {
                get
                {
                    if (states == null)
                    {
                        states = controller.GetSMI<Instance>();
                    }
                    return states;
                }
            }
            public Instance(IStateMachineTarget master, Def def) : base(master, def)
            {

            }
            public void GoInToContaiment()
            {
                //TODO 添加收容Effect
                ApplyEffect(gameObject, MutanterEffects.MUTANTER_CONTAINED_EFFECT);
            }
            public void GoOutOfContainment()
            {
                //TODO 移除收容effect
                RemoveEffect(gameObject, MutanterEffects.MUTANTER_CONTAINED_EFFECT);
            }
            private void ApplyEffect(GameObject mutanter, string effect_id)
            {
                TbbDebuger.LogDebug($"[{mutanter.name}]应用Effect");
                Effects effects = mutanter.GetComponent<Effects>();
                EffectInstance effectInstance = effects.Get(effect_id);
                if (effectInstance != null)
                {
                    effectInstance.timeRemaining = effectInstance.effect.duration;
                }
                else
                {
                    effects.Add(effect_id, true);
                }
            }
            private void RemoveEffect(GameObject mutanter, string effect_id)
            {
                TbbDebuger.LogDebug($"[{mutanter.name}] 移除Effect");
                Effects effects = mutanter.GetComponent<Effects>();
                EffectInstance effectInstance = effects.Get(effect_id);
                if (effectInstance != null)
                {
                    effects.Remove(effect_id);
                }
            }
            private void ApplyModifier(GameObject mutanter, Tag modifierTag)
            {
                var modifiers = mutanter.GetComponent<Modifiers>();
                if (modifiers != null && !modifiers.HasTag(modifierTag.Name))
                {
                    modifiers.AddTag(modifierTag.Name); // Apply the containment effect
                    TbbDebuger.LogDebug($"Applied Containment Monitor effect to {mutanter.name}");
                }
            }

            private void RemoveModifier(GameObject mutanter, Tag modifierTag)
            {
                var effects = mutanter.GetComponent<Modifiers>();
                if (effects != null && effects.HasTag(modifierTag.Name))
                {
                    effects.RemoveTag(modifierTag.Name); // Remove the containment effect
                    TbbDebuger.LogDebug($"Removed Containment Monitor effect from {mutanter.name}");
                }
            }
            public bool ShouldBeSecured()
            {
                if (_targetContainmentMonitor == null || !_targetContainmentMonitor.IsRunning()) return false;
                Effects effects = gameObject.GetComponent<Effects>();
                EffectInstance effectInstance = effects.Get(MutanterEffects.MUTANTER_CONTAINED_EFFECT);
                if (effectInstance == null || effectInstance.timeRemaining < timeRemainningWarning) return true;

                return false;
            }

            internal void SetContainmentMonitor(ContainmentMonitor.Instance instance)
            {
                _targetContainmentMonitor = instance;
            }
        }
        public class Def : BaseDef { }
    }
}
