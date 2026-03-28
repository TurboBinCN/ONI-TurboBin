using Klei.AI;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{

    // 心理攻击示例
    public class PsychologicalAttack : IMutanterAttackBehavior
    {
        private readonly float _stressAmount;
        private readonly float _cooldown;
        private float _lastExecutedTime;

        public PsychologicalAttack(float stress = 5f, float cooldown = 5f)
        {
            _stressAmount = stress;
            _cooldown = cooldown;
            _lastExecutedTime = -_cooldown;
        }

        public Tag GetTag() => MutanterTags.PsychologicalAttack; // 自定义标签

        public float GetCooldown() => _cooldown;

        public bool CanExecute(IStateMachineTarget attacker, GameObject target)
        {
            if (Time.time - _lastExecutedTime < _cooldown) return false;
            return target != null; // 示例：只要目标存在即可
        }

        public bool Execute(IStateMachineTarget attacker, GameObject target)
        {
            return Execute(attacker, target, 1.0f);
        }

        public bool Execute(IStateMachineTarget attacker, GameObject target, float effectImpact)
        {
            if (!CanExecute(attacker, target)) return false;

            Debug.Log($"[PsychologicalAttack] {attacker.name} is psychologically attacking {target.name}!");

            // 使用MutanterAttackSystem执行攻击
            var attackSystem = attacker.gameObject.GetComponent<MutanterAttackSystem>();
            if (attackSystem != null)
            {
                float stressAmount = _stressAmount * effectImpact;
                bool success = attackSystem.ExecuteStressAttack(target, stressAmount, out _);
                _lastExecutedTime = Time.time;
                return success;
            }

            // 降级处理：直接执行攻击
            var amounts = target.GetAmounts();
            if (amounts != null)
            {
                var stressAmountComp = amounts.Get(Db.Get().Amounts.Stress);
                if (stressAmountComp != null)
                {
                    // 计算精神抗性影响
                    float mentalResistanceFactor = 1f;
                    var attributes = target.GetAttributes();
                    if (attributes != null)
                    {
                        var mentalResistanceAttribute = attributes.Get("MentalResistance");
                        if (mentalResistanceAttribute != null)
                        {
                            float mentalResistanceValue = mentalResistanceAttribute.GetTotalValue();
                            mentalResistanceFactor = Mathf.Max(0.1f, 1f - (mentalResistanceValue * 0.1f));
                        }
                    }
                    
                    float effectiveStressAmount = _stressAmount * mentalResistanceFactor * effectImpact;
                    stressAmountComp.value = Mathf.Min(stressAmountComp.value + effectiveStressAmount, 100f);
                    TbbDebuger.LogDebug($"[PsychologicalAttack] {target.name} stress increased to {stressAmountComp.value}%, effective amount: {effectiveStressAmount}");
                    _lastExecutedTime = Time.time;
                    return true;
                }
            }

            return false;
        }
    }
}
