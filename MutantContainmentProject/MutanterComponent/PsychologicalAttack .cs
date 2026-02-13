using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{

    // 心理攻击示例
    public class PsychologicalAttack : IMutanterAttackBehavior
    {
        private readonly float _insanityAmount;
        private readonly float _cooldown;
        private float _lastExecutedTime;

        public PsychologicalAttack(float insanity = 5f, float cooldown = 5f)
        {
            _insanityAmount = insanity;
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

            // 这里调用影响理智的逻辑，例如：
            //var sanity = target.GetComponent<SanityMonitor>(); // 假设存在这样的组件
            //if (sanity != null)
            //{
            //    // sanity.AdjustInsanity(_insanityAmount); // 假设的方法
            //}

            _lastExecutedTime = Time.time;
            return true;
        }
    }
}
