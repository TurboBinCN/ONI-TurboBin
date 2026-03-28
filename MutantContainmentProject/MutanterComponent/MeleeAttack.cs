using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{

    // 基础近战攻击示例
    public class MeleeAttack : IMutanterAttackBehavior
    {
        private readonly float _baseDamage;
        private readonly float _cooldown;
        private float _lastExecutedTime;

        public MeleeAttack(float damage = 10f, float cooldown = 2f)
        {
            _baseDamage = damage;
            _cooldown = cooldown;
            _lastExecutedTime = -_cooldown; // 初始时视为已冷却
        }

        public Tag GetTag() => GameTags.Creatures.Attack; // 使用游戏内置标签

        public float GetCooldown() => _cooldown;

        public bool CanExecute(IStateMachineTarget attacker, GameObject target)
        {
            if (Time.time - _lastExecutedTime < _cooldown) return false;
            // 这里可以添加距离检查等逻辑
            return target != null && Vector3.Distance(attacker.transform.position, target.transform.position) <= 2.0f; // 示例距离
        }

        public bool Execute(IStateMachineTarget attacker, GameObject target)
        {
            return Execute(attacker, target, 1.0f);
        }

        public bool Execute(IStateMachineTarget attacker, GameObject target, float effectImpact)
        {
            if (!CanExecute(attacker, target)) return false;

            Debug.Log($"[MeleeAttack] {attacker.name} is attacking {target.name} with melee!");

            // 使用MutanterAttackSystem执行攻击
            var attackSystem = GetAttackSystem(attacker.gameObject);
            if (attackSystem != null)
            {
                float damage = _baseDamage * effectImpact;
                bool success = attackSystem.ExecuteHealthAttack(target, damage, out _);
                _lastExecutedTime = Time.time;
                return success;
            }

            // 降级处理：直接执行攻击
            var health = target.GetComponent<Health>();
            if (health != null)
            {
                float damage = _baseDamage * effectImpact;
                health.Damage(damage);
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
