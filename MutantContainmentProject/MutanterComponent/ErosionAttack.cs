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
            return Execute(attacker, target, 1.0f);
        }

        public bool Execute(IStateMachineTarget attacker, GameObject target, float effectImpact)
        {
            if (!CanExecute(attacker, target)) return false;

            TbbDebuger.LogDebug($"[ErosionAttack] {attacker.name} is eroding {target.name}!");
            bool success = false;
            // 使用MutanterAttackSystem执行攻击
            var attackSystem = GetAttackSystem(attacker.gameObject);
            if (attackSystem != null)
            {
                float damage = _damageAmount * effectImpact;
                float stressAmount = _stressAmount * effectImpact;
                success = attackSystem.ExecuteCombinedAttack(target, damage, stressAmount);
                _lastExecutedTime = Time.time;
                return success;
            }

            // 降级处理：直接执行攻击
            // 1. 减少目标的生命值
            var health = target.GetComponent<Health>();
            if (health != null)
            {
                float damage = _damageAmount * effectImpact;
                health.Damage(damage);
                TbbDebuger.LogDebug($"[ErosionAttack] {target.name} took {damage} damage (effect impact: {effectImpact})");
                success = true;
            }

            // 2. 增加目标的压力值
            var amounts = target.GetAmounts();
            if (amounts != null)
            {
                var stressAmountComp = amounts.Get(Db.Get().Amounts.Stress);
                if (stressAmountComp != null)
                {
                    float stressIncrease = _stressAmount * effectImpact;
                    stressAmountComp.value = Mathf.Min(stressAmountComp.value + stressIncrease, 100f);
                    TbbDebuger.LogDebug($"[ErosionAttack] {target.name} stress increased to {stressAmountComp.value}% (effect impact: {effectImpact})");
                    success = true;
                }
            }

            _lastExecutedTime = Time.time;
            return success;
        }

        private MutanterAttackSystem GetAttackSystem(GameObject gameObject)
        {
            return gameObject.GetComponent<MutanterAttackSystem>();
        }
    }
}