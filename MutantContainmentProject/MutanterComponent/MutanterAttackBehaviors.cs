using Klei.AI;
using MutantContainmentProject.MutanterTraits;
using System.Collections.Generic;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    /**
     * 攻击行为集合 (MutanterAttackBehaviors)
        功能: 不再是单一的 AttackStates，而是一组可插拔的攻击行为。由 MutanterStateMachine 根据当前状态和特性选择执行。
        行为示例:
        4种伤害与抗性：物理、精神、侵蚀、灵魂
        MeleeAttack: 基础近战攻击。
        RangedAttack: 投射物或能量攻击。
        PsychologicalAttack: 降低目标理智值，可能伴有视觉或音效干扰。
        AreaEffectAttack: 影响一片区域，如释放毒气、精神波动。
        SpecialAttack: 与特定特性绑定的独特攻击方式（如“血肉触手”、“梦境入侵”）。
        SummonAttack: 召唤辅助单位或陷阱。
        设计原则: 每种攻击行为都是一个独立模块，可以根据 EmotionMonitor 的状态（如愤怒时使用更强力的攻击）和 MutanterTraits 的特性（如“光敏”实体在黑暗中使用光攻击）进行动态调整。
     */
    public class MutanterAttackBehaviors : KMonoBehaviour
    {
        // 存储所有可用的攻击行为实例
        private List<IMutanterAttackBehavior> _availableBehaviors = new List<IMutanterAttackBehavior>();

        // 记录每个行为的最后执行时间，用于冷却管理
        private Dictionary<IMutanterAttackBehavior, float> _behaviorLastExecutionTimes = new Dictionary<IMutanterAttackBehavior, float>();

        protected override void OnSpawn()
        {
            base.OnSpawn();
            InitializeBehaviors(); // 初始化时注册默认或配置的行为
        }

        /// <summary>
        /// 初始化攻击行为列表。可以根据配置或特性动态添加。
        /// </summary>
        private void InitializeBehaviors()
        {
            // 清空现有列表
            _availableBehaviors.Clear();

            // --- 示例：根据特性或配置添加行为 ---
            // 基础近战总是可用
            _availableBehaviors.Add(new MeleeAttack());

            // 如果有心理攻击特性，则添加
            var modifiers = GetComponent<Modifiers>();
            if (modifiers != null && modifiers.initialTraits.Contains(MutanterTraitDb.MutanterPsychological))
            {
                _availableBehaviors.Add(new PsychologicalAttack());
            }

            // 可以从配置文件、数据表或特性系统读取来决定添加哪些行为
            // 例如: _availableBehaviors.Add(LoadBehaviorFromConfig("RangedAttack"));
        }

        /// <summary>
        /// 尝试执行一个合适的攻击行为。
        /// </summary>
        /// <param name="target">由MutanterStateMachine决定攻击行为</param>
        /// <returns>是否成功执行了一个行为</returns>
        public bool TryExecuteAttack(GameObject target )
        {
            if (target == null)
            {
                Debug.LogWarning("[MutanterAttackBehaviors] No target available to attack.");
                return false;
            }

            // 选择行为的逻辑可以更复杂，这里是一个简单的例子
            IMutanterAttackBehavior selectedBehavior = SelectBehavior(target);

            if (selectedBehavior != null)
            {
                bool success = selectedBehavior.Execute(this, target); // 注意：这里的 'this' 是行为集合本身，实际调用时需要传入正确的攻击者实例 (e.g., base.smi or the creature component)
                if (success)
                {
                    _behaviorLastExecutionTimes[selectedBehavior] = Time.time; // 更新执行时间
                    Debug.Log($"[MutanterAttackBehaviors] Executed behavior: {selectedBehavior.GetType().Name}");
                    return true;
                }
            }

            Debug.Log("[MutanterAttackBehaviors] No suitable behavior found or execution failed.");
            return false;
        }

        /// <summary>
        /// 根据当前状态、目标和可用行为，选择一个要执行的行为。
        /// 这个方法是核心逻辑，可以根据 EmotionMonitor 和 MutanterTraits 进行调整。
        /// </summary>
        /// <param name="target">当前攻击目标</param>
        /// <returns>选中的攻击行为，如果无合适行为则返回 null</returns>
        private IMutanterAttackBehavior SelectBehavior(GameObject target)
        {
            List<IMutanterAttackBehavior> candidates = new List<IMutanterAttackBehavior>();

            foreach (var behavior in _availableBehaviors)
            {
                // 检查冷却时间
                if (_behaviorLastExecutionTimes.TryGetValue(behavior, out float lastTime))
                {
                    if (Time.time - lastTime < behavior.GetCooldown())
                    {
                        continue; // 跳过仍在冷却中的行为
                    }
                }

                // 检查行为自身条件
                if (behavior.CanExecute(this, target)) // 注意：同样需要传递正确的攻击者实例
                {
                    candidates.Add(behavior);
                }
            }

            if (candidates.Count == 0) return null;

            // --- 示例决策逻辑 ---
            // 1. 根据情绪选择 (假设存在情绪影响)
            if (false) // 假设 Emotion 枚举和字段
            {
                // 优先选择高伤害或特殊的行为
                var highDmgBehavior = candidates.Find(b => b is MeleeAttack); // 简单示例
                if (highDmgBehavior != null) return highDmgBehavior;
            }

            // 2. 根据特性选择 (例如，对光敏感的敌人优先用光攻击)
            // if (IsTargetSensitiveToLight(target)) { ... }

            // 3. 随机选择一个可用的
            int randomIndex = UnityEngine.Random.Range(0, candidates.Count);
            return candidates[randomIndex];

            // 更复杂的决策可以基于目标类型、距离、行为效果等
        }

        /// <summary>
        /// 添加一个新的攻击行为到可用列表中。
        /// </summary>
        /// <param name="behavior">要添加的行为实例</param>
        public void AddBehavior(IMutanterAttackBehavior behavior)
        {
            if (!_availableBehaviors.Contains(behavior))
            {
                _availableBehaviors.Add(behavior);
                Debug.Log($"[MutanterAttackBehaviors] Added behavior: {behavior.GetType().Name}");
            }
        }

        /// <summary>
        /// 移除一个攻击行为。
        /// </summary>
        /// <param name="behavior">要移除的行为实例</param>
        public void RemoveBehavior(IMutanterAttackBehavior behavior)
        {
            if (_availableBehaviors.Remove(behavior))
            {
                _behaviorLastExecutionTimes.Remove(behavior); // 同步移除其冷却记录
                Debug.Log($"[MutanterAttackBehaviors] Removed behavior: {behavior.GetType().Name}");
            }
        }

        // --- 辅助方法 ---
        // 可能需要一些辅助方法来查询当前状态，例如 IsTargetSensitiveToLight
        // private bool IsTargetSensitiveToLight(GameObject target) { ... }
    }

}
