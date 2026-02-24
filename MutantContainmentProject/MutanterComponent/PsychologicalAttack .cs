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
            if (!CanExecute(attacker, target)) return false;

            Debug.Log($"[PsychologicalAttack] {attacker.name} is psychologically attacking {target.name}!");

            // 增加目标的压力值
            var amounts = target.GetAmounts();
            if (amounts != null)
            {
                var stressAmount = amounts.Get(Db.Get().Amounts.Stress);
                if (stressAmount != null)
                {
                    // 计算精神抗性影响
                    float mentalResistanceFactor = 1f;
                    
                    // 从目标的属性中获取MentalResistance值作为精神抗性
                    var attributes = target.GetAttributes();
                    if (attributes != null)
                    {
                        var mentalResistanceAttribute = attributes.Get("MentalResistance");
                        if (mentalResistanceAttribute != null)
                        {
                            // MentalResistance值越高，精神抗性越强，压力增长越慢
                            float mentalResistanceValue = mentalResistanceAttribute.GetTotalValue();
                            mentalResistanceFactor = Mathf.Max(0.1f, 1f - (mentalResistanceValue * 0.1f));
                        }
                    }
                    
                    // 应用精神抗性
                    float effectiveStressAmount = _stressAmount * mentalResistanceFactor;
                    stressAmount.value = Mathf.Min(stressAmount.value + effectiveStressAmount, 100f);
                    TbbDebuger.LogDebug($"[PsychologicalAttack] {target.name} stress increased to {stressAmount.value}%, effective amount: {effectiveStressAmount}, resistance factor: {mentalResistanceFactor}");
                }
            }

            _lastExecutedTime = Time.time;
            return true;
        }
    }
}
