using Klei.AI;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{

    // 侵蚀攻击：同时减少生命值和增加压力值
    public class ErosionAttack : IMutanterAttackBehavior
    {
        private readonly float _damageAmount;
        private readonly float _stressAmount;
        private readonly float _cooldown;
        private float _lastExecutedTime;

        public ErosionAttack(float damage = 8f, float stress = 10f, float cooldown = 4f)
        {
            _damageAmount = damage;
            _stressAmount = stress;
            _cooldown = cooldown;
            _lastExecutedTime = -_cooldown;
        }

        public Tag GetTag() => MutanterTags.ErosionAttack; // 自定义标签

        public float GetCooldown() => _cooldown;

        public bool CanExecute(IStateMachineTarget attacker, GameObject target)
        {
            if (Time.time - _lastExecutedTime < _cooldown) return false;
            return target != null && Vector3.Distance(attacker.transform.position, target.transform.position) <= 2.5f; // 攻击距离
        }

        public bool Execute(IStateMachineTarget attacker, GameObject target)
        {
            if (!CanExecute(attacker, target)) return false;

            TbbDebuger.LogDebug($"[ErosionAttack] {attacker.name} is eroding {target.name}!");

            // 1. 减少目标的生命值
            var health = target.GetComponent<Health>();
            if (health != null)
            {
                health.Damage(_damageAmount);
                TbbDebuger.LogDebug($"[ErosionAttack] {target.name} took {_damageAmount} damage");
            }

            // 2. 增加目标的压力值
            var amounts = target.GetAmounts();
            if (amounts != null)
            {
                var stressAmount = amounts.Get(Db.Get().Amounts.Stress);
                if (stressAmount != null)
                {
                    stressAmount.value = Mathf.Min(stressAmount.value + _stressAmount, 100f);
                    TbbDebuger.LogDebug($"[ErosionAttack] {target.name} stress increased to {stressAmount.value}%");
                }
            }

            _lastExecutedTime = Time.time;
            return true;
        }
    }
}