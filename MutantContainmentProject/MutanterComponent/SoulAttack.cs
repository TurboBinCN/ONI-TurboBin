using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{

    // 灵魂攻击：按照百分比扣减生命值
    public class SoulAttack : IMutanterAttackBehavior
    {
        private readonly float _damagePercentage;
        private readonly float _cooldown;
        private float _lastExecutedTime;

        public SoulAttack(float damagePercentage = 0.2f, float cooldown = 8f)
        {
            _damagePercentage = damagePercentage; // 0.2 表示 20%
            _cooldown = cooldown;
            _lastExecutedTime = -_cooldown;
        }

        public Tag GetTag() => MutanterTags.SoulAttack; // 自定义标签

        public float GetCooldown() => _cooldown;

        public bool CanExecute(IStateMachineTarget attacker, GameObject target)
        {
            if (Time.time - _lastExecutedTime < _cooldown) return false;
            return target != null; // 灵魂攻击可以远程执行
        }

        public bool Execute(IStateMachineTarget attacker, GameObject target)
        {
            return Execute(attacker, target, 1.0f);
        }

        public bool Execute(IStateMachineTarget attacker, GameObject target, float effectImpact)
        {
            if (!CanExecute(attacker, target)) return false;

            TbbDebuger.LogDebug($"[SoulAttack] {attacker.name} is attacking {target.name}'s soul!");

            // 计算伤害值
            float damageAmount = 0f;
            var health = target.GetComponent<Health>();
            if (health != null)
            {
                float maxHitPoints = health.maxHitPoints;
                damageAmount = maxHitPoints * _damagePercentage * effectImpact;
            }

            // 使用MutanterAttackSystem执行攻击
            var attackSystem = GetAttackSystem(attacker.gameObject);
            if (attackSystem != null)
            {
                bool success = attackSystem.ExecuteHealthAttack(target, damageAmount, out _);
                _lastExecutedTime = Time.time;
                return success;
            }

            // 降级处理：直接执行攻击
            if (health != null)
            {
                health.Damage(damageAmount);
                TbbDebuger.LogDebug($"[SoulAttack] {target.name} took {damageAmount} damage ({_damagePercentage * 100 * effectImpact}% of max HP, effect impact: {effectImpact})");
                _lastExecutedTime = Time.time;
                return true;
            }

            return false;
        }

        private MutanterAttackSystem GetAttackSystem(GameObject gameObject)
        {
            return gameObject.GetComponent<MutanterAttackSystem>();
        }
    }
}