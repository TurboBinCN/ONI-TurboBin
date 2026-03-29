using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;
using UnityEngine;
using static KAnim;

namespace MutantContainmentProject.MutanterComponent
{
    public class MutanterCombatManager : KMonoBehaviour
    {
        // 攻击状态跟踪
        private bool isAttacking = false;
        public bool IsAttacking => isAttacking;

        // 动画协调器
        private AnimationCoordinator animationCoordinator;

        // 攻击系统
        private MutanterAttackSystem attackSystem;

        // 技能组件
        private MutanterSkillComponent skillComponent;

        // 攻击策略管理器
        private AttackStrategyManager strategyManager;
        // 初始化策略管理器
        private AttackStrategyManager StrategyManager => strategyManager ??= gameObject.AddOrGet<AttackStrategyManager>();

        // 状态机实例
        private MutanterStateMachine.StatesInstance stateMachineInstance;

        // 技能执行队列
        private struct SkillExecutionRequest
        {
            public string skillName;
            public int priority;
            public GameObject target;
            public int skillLevel;
            public float damageAmount;
        }

        private List<SkillExecutionRequest> executionQueue = new List<SkillExecutionRequest>();
        private bool isProcessingQueue = false;
        protected override void OnSpawn()
        {
            base.OnSpawn();
            // 初始化组件
            attackSystem = GetComponent<MutanterAttackSystem>();
            skillComponent = GetComponent<MutanterSkillComponent>();
            StrategyManager.Initialize();
            stateMachineInstance = gameObject.GetSMI<MutanterStateMachine.StatesInstance>();

            // 初始化系统
            animationCoordinator = new AnimationCoordinator(this);
        }
        public bool MutiSegmentDamage()
        {
            return false;
        }
        /// <summary>
        /// 尝试执行技能攻击
        /// </summary>
        /// <param name="skillName">技能名称</param>
        /// <param name="damageAmount">伤害金额</param>
        /// <param name="playAnimation">是否播放动画</param>
        /// <param name="AsyncAttack">是否异步攻击</param>
        /// <returns>是否成功执行技能攻击</returns>
        public bool TryExecuteSkill(string skillName, float damageAmount = 0f)
        {
            // 检查是否正在攻击
            if (isAttacking) return false;

            if (skillComponent != null)
            {
                bool skillSuccess = skillComponent.TryExecuteSkill(skillName, damageAmount);
                return skillSuccess;
            }
            return false;
        }
        /// <summary>
        /// 尝试执行攻击
        /// </summary>
        /// <param name="target">攻击目标</param>
        /// <returns>是否成功执行攻击</returns>
        public bool TryExecuteAttack(GameObject target)
        {
            // 检查是否正在攻击
            if (isAttacking)
            {
                return false;
            }

            // 检查生命值
            var health = GetComponent<Health>();
            if (health != null && health.hitPoints <= 0f)
            {
                return false;
            }

            // 使用策略管理器执行攻击
            return StrategyManager.ExecuteAttack(target);
        }

        /// <summary>
        /// 设置攻击状态
        /// </summary>
        /// <param name="attacking">是否正在攻击</param>
        public void SetAttacking(bool attacking)
        {
            isAttacking = attacking;
        }
        /// <summary>
        /// 播放动画 用于三联动画播放
        /// </summary>
        /// <param name="animationName">动画名称</param>
        /// <param name="duration">{animationName}_loop动画的持续时间</param>
        /// <param name="onComplete">动画完成回调</param>
        public void PlayAnimation(string animationName, float duration, System.Action onComplete = null)
        {
            animationCoordinator.PlayAnimation(animationName, duration, onComplete);
        }
        /// <summary>
        /// 播放动画
        /// </summary>
        /// <param name="animationName">动画名称</param>
        /// <param name="playMode">播放模式</param>
        /// <param name="onComplete">动画完成回调</param>
        public void PlayAnimation(string animationName, KAnim.PlayMode playMode, System.Action onComplete = null)
        {
            animationCoordinator.PlayAnimation(animationName, playMode, onComplete);
        }

        /// <summary>
        /// 清理动画队列
        /// </summary>
        public void ClearAnimationQueue()
        {
            animationCoordinator.ClearQueue();
        }

        /// <summary>
        /// 停止所有攻击
        /// </summary>
        public void StopAllAttacks()
        {
            SetAttacking(false);
            ClearAnimationQueue();
        }

        /// <summary>
        /// 检查所有技能是否冷却完成
        /// </summary>
        /// <returns>所有技能是否冷却完成</returns>
        public bool AreAnySkillsReady()
        {
            if (skillComponent == null || skillComponent.skills == null || skillComponent.skills.Count == 0)
            {
                return true;
            }

            foreach (var skill in skillComponent.skills)
            {
                if (Time.time - skill.lastUseTime > skill.cooldown)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 检查是否有任何可用的攻击策略（包括技能和基础攻击）
        /// </summary>
        /// <param name="target">攻击目标</param>
        /// <returns>是否有可用的攻击策略</returns>
        public bool HasAnyAvailableAttackStrategy(GameObject target)
        {
            return StrategyManager.HasAnyAvailableStrategy(target);
        }

        /// <summary>
        /// 队列技能执行
        /// </summary>
        /// <param name="skillName">技能名称</param>
        /// <param name="priority">优先级</param>
        /// <param name="target">目标</param>
        /// <param name="skillLevel">技能等级</param>
        /// <param name="damageAmount">伤害量</param>
        public void QueueSkillExecution(string skillName, int priority = 0, GameObject target = null, int skillLevel = 0, float damageAmount = 0f)
        {
            var request = new SkillExecutionRequest
            {
                skillName = skillName,
                priority = priority,
                target = target,
                skillLevel = skillLevel,
                damageAmount = damageAmount
            };

            executionQueue.Add(request);
            executionQueue.Sort((a, b) => b.priority.CompareTo(a.priority));

            if (!isProcessingQueue)
            {
                ProcessQueue();
            }
        }

        /// <summary>
        /// 处理技能执行队列
        /// </summary>
        private void ProcessQueue()
        {
            if (executionQueue.Count == 0)
            {
                isProcessingQueue = false;
                return;
            }

            isProcessingQueue = true;
            var request = executionQueue[0];
            executionQueue.RemoveAt(0);

            // 锁定状态机
            LockStateMachine();

            // 执行技能
            bool success = TryExecuteSkill(request.skillName, request.damageAmount);

            // 技能执行完成后会通过UnlockStateMachineAfterAnimation方法解锁状态机
            // 这里不再立即解锁

            // 处理下一个技能
            // 注意：这里需要在动画完成后再处理下一个技能
        }

        /// <summary>
        /// 动画完成后解锁状态机并继续处理队列
        /// </summary>
        public void UnlockStateMachineAfterAnimation()
        {
            // 解锁状态机
            UnlockStateMachine();

            // 处理下一个技能
            ProcessQueue();
        }

        /// <summary>
        /// 锁定状态机
        /// </summary>
        private void LockStateMachine()
        {
            if (stateMachineInstance != null)
            {
                stateMachineInstance.LockStateMachine();
                TbbDebuger.LogDebug($"[MutanterCombatManager] 锁定状态机");
            }
        }

        /// <summary>
        /// 解锁状态机
        /// </summary>
        private void UnlockStateMachine()
        {
            if (stateMachineInstance != null)
            {
                stateMachineInstance.UnlockStateMachine();
                TbbDebuger.LogDebug($"[MutanterCombatManager] 解锁状态机");
            }
        }

        /// <summary>
        /// 清空技能执行队列
        /// </summary>
        public void ClearSkillQueue()
        {
            executionQueue.Clear();
        }

        // 动画协调器
        public class AnimationCoordinator
        {
            private MutanterCombatManager manager;
            private KBatchedAnimController animController;

            public AnimationCoordinator(MutanterCombatManager manager)
            {
                this.manager = manager;
                this.animController = manager.gameObject.GetComponent<KBatchedAnimController>();
            }
            public void PlayAnimation(string animationName, float duration, System.Action onComplete = null)
            {
                if (animController != null)
                {
                    animController.Play($"{animationName}_pre", PlayMode.Once);
                    animController.Queue($"{animationName}_loop", PlayMode.Loop);
                    GameScheduler.Instance.Schedule($"{animationName}_animation_Play", duration, (_) =>
                    {
                        animController.Play($"{animationName}_pst", PlayMode.Once);
                    });
                    if (onComplete != null)
                    {
                        System.Action<object> kAnimEvent = null;
                        kAnimEvent = (_) =>
                        {
                            var currentAnim = animController.GetCurrentAnim();
                            if (currentAnim != null && currentAnim.name == $"{animationName}_pst")
                            {
                                onComplete();
                            }
                            else
                            {
                                string currentAnimName = currentAnim != null ? currentAnim.name : "null";
                            }
                            animController.gameObject.Unsubscribe((int)GameHashes.AnimQueueComplete, kAnimEvent);
                        };
                        animController.gameObject.Subscribe((int)GameHashes.AnimQueueComplete, kAnimEvent);
                    }
                }
            }
            /// <summary>
            /// 播放动画
            /// </summary>
            public void PlayAnimation(string animationName, KAnim.PlayMode playMode, System.Action onComplete = null)
            {
                if (animController != null && !string.IsNullOrEmpty(animationName))
                {
                    animController.Play(animationName, playMode);

                    if (onComplete != null)
                    {
                        System.Action<object> kAnimEvent = null;
                        kAnimEvent = (_) =>
                        {
                            onComplete();
                            animController.gameObject.Unsubscribe((int)GameHashes.AnimQueueComplete, kAnimEvent);
                        };
                        animController.gameObject.Subscribe((int)GameHashes.AnimQueueComplete, kAnimEvent);
                    }
                }
            }

            /// <summary>
            /// 清理动画队列
            /// </summary>
            public void ClearQueue()
            {
                if (animController != null)
                {
                    animController.ClearQueue();
                }
            }
        }
    }
}
