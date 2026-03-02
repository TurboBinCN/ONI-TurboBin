using Klei.AI;
using MutantContainmentProject.MutanterTraits;
using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    /**
     * 攻击行为集合 (MutanterAttackBehaviors)
        功能: 不再是单一的 AttackStates，而是一组可插拔的攻击行为。由 MutanterStateMachine 根据当前状态和特性选择执行。
        4种伤害与抗性：物理、精神、侵蚀、灵魂
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
        /// 初始化攻击行为列表。可以根据配置或标签动态添加。
        /// </summary>
        private void InitializeBehaviors()
        {
            // 清空现有列表
            _availableBehaviors.Clear();
            _behaviorLastExecutionTimes.Clear();

            // --- 根据标签添加攻击行为 ---
            // 获取 KPrefabID 来检查标签
            var kPrefabID = GetComponent<KPrefabID>();
            if (kPrefabID != null)
            {
                //物理攻击
                if (kPrefabID.HasTag(MutanterTags.PhysicalAttack))
                    _availableBehaviors.Add(new MeleeAttack());
                // 心理攻击
                if (kPrefabID.HasTag(MutanterTags.PsychologicalAttack))
                {
                    _availableBehaviors.Add(new PsychologicalAttack());
                }

                // 侵蚀攻击
                if (kPrefabID.HasTag(MutanterTags.ErosionAttack))
                {
                    _availableBehaviors.Add(new ErosionAttack());
                }

                // 灵魂攻击
                if (kPrefabID.HasTag(MutanterTags.SoulAttack))
                {
                    _availableBehaviors.Add(new SoulAttack());
                }
            }
            //默认添加物理攻击标签
            if(_availableBehaviors.Count == 0) _availableBehaviors.Add(new MeleeAttack());
        }

        /// <summary>
        /// 尝试执行一个合适的攻击行为。
        /// </summary>
        /// <param name="target">由MutanterStateMachine决定攻击行为</param>
        /// <param name="insanityValue">理智值，用于决定攻击类型</param>
        /// <returns>是否成功执行了一个行为</returns>
        public bool TryExecuteAttack(GameObject target, float insanityValue = 100f)
        {
            if (target == null)
            {
                return false;
            }

            // 选择行为的逻辑
            IMutanterAttackBehavior selectedBehavior = SelectBehavior(target, insanityValue);

            if (selectedBehavior != null)
            {
                bool success = selectedBehavior.Execute(this, target);
                if (success)
                {
                    _behaviorLastExecutionTimes[selectedBehavior] = Time.time; // 更新执行时间
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 尝试执行一个合适的攻击行为，对多个目标。
        /// </summary>
        /// <param name="targets">目标列表</param>
        /// <param name="insanityValue">理智值，用于决定攻击类型</param>
        /// <returns>是否成功执行了行为</returns>
        public bool TryExecuteAttack(List<KPrefabID> targets, float insanityValue = 100f)
        {
            bool success = false;
            if (targets == null || targets.Count == 0)
            {
                return false;
            }

            foreach (var target in targets)
            {
                if (target != null && target.gameObject != null)
                {
                    if (TryExecuteAttack(target.gameObject, insanityValue))
                    {
                        success = true;
                    }
                }
            }

            return success;
        }

        /// <summary>
        /// 根据当前状态、目标和可用行为，选择一个要执行的行为。
        /// 这个方法是核心逻辑，可以根据 EmotionMonitor 和 MutanterTraits 进行调整。
        /// </summary>
        /// <param name="target">当前攻击目标</param>
        /// <returns>选中的攻击行为，如果无合适行为则返回 null</returns>
        private IMutanterAttackBehavior SelectBehavior(GameObject target)
        {
            return SelectBehavior(target, 100f);
        }

        /// <summary>
        /// 根据当前状态、目标、理智值和可用行为，选择一个要执行的行为。
        /// </summary>
        /// <param name="target">当前攻击目标</param>
        /// <param name="insanityValue">理智值，用于决定攻击类型</param>
        /// <returns>选中的攻击行为，如果无合适行为则返回 null</returns>
        private IMutanterAttackBehavior SelectBehavior(GameObject target, float insanityValue)
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
                if (behavior.CanExecute(this, target))
                {
                    candidates.Add(behavior);
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            // 根据理智值选择攻击类型
            IMutanterAttackBehavior selectedBehavior = null;
            
            if (insanityValue < 20f)
            {
                // 理智值低，使用物理攻击
                selectedBehavior = candidates.Find(b => b is MeleeAttack);
            }
            else if (insanityValue < 40f)
            {
                // 理智值较低，使用心理攻击
                selectedBehavior = candidates.Find(b => b is PsychologicalAttack);
            }
            else if (insanityValue < 60f)
            {
                // 理智值中等，使用侵蚀攻击
                selectedBehavior = candidates.Find(b => b is ErosionAttack);
            }
            else
            {
                // 理智值高，使用灵魂攻击
                selectedBehavior = candidates.Find(b => b is SoulAttack);
            }

            // 如果没有找到对应类型的攻击，随机选择一个
            if (selectedBehavior == null)
            {
                int randomIndex = UnityEngine.Random.Range(0, candidates.Count);
                selectedBehavior = candidates[randomIndex];
            }

            return selectedBehavior;
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
                TbbDebuger.LogDebug($"[MutanterAttackBehaviors] Added behavior: {behavior.GetType().Name}");
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
                TbbDebuger.LogDebug($"[MutanterAttackBehaviors] Removed behavior: {behavior.GetType().Name}");
            }
        }

        // --- 辅助方法 ---
        // 可能需要一些辅助方法来查询当前状态，例如 IsTargetSensitiveToLight
        // private bool IsTargetSensitiveToLight(GameObject target) { ... }
    }

}
