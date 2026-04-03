using System.Collections.Generic;
using System.Linq;
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

        // 技能组件
        private MutanterSkillComponent skillComponent;

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

        private List<SkillExecutionRequest> executionQueue = new();
        private bool isProcessingQueue = false;
        protected override void OnSpawn()
        {
            base.OnSpawn();
            // 初始化组件
            skillComponent = GetComponent<MutanterSkillComponent>();
            stateMachineInstance = gameObject.GetSMI<MutanterStateMachine.StatesInstance>();

            // 初始化系统
            animationCoordinator = new AnimationCoordinator(this);
        }

        /// <summary>
        /// 执行技能攻击
        /// </summary>
        /// <param name="skillName">技能名称</param>
        /// <param name="damageAmount">伤害金额</param>
        /// <returns>是否成功执行技能攻击</returns>
        public bool ExecuteSkill(string skillName, float damageAmount = 0f)
        {
            // 直接执行技能（内部会调用ExecuteSkill）
            if (skillComponent != null)
            {
                return skillComponent.TryExecuteSkill(skillName, damageAmount);
            }
            return false;
        }
        /// <summary>
        /// 执行攻击（统一入口）
        /// </summary>
        /// <param name="target">攻击目标</param>
        /// <returns>是否成功执行攻击</returns>
        public bool ExecuteAttack(GameObject target)
        {
            // 检查生命值
            var health = GetComponent<Health>();
            if (health != null && health.hitPoints <= 0f)
            {
                return false;
            }

            // 选择一个可用技能执行
            if (skillComponent != null)
            {
                foreach (var skill in skillComponent.skills)
                {
                    if (!skill.isPassiveSkill && (skill.isFirstUse || Time.time - skill.lastUseTime >= skill.cooldown))
                    {
                        return ExecuteSkill(skill.name, 0f);
                    }
                }
            }

            return false;
        }
        /// <summary>
        /// 队列执行攻击（统一入口）
        /// </summary>
        /// <param name="target">攻击目标</param>
        /// <returns>是否成功加入队列</returns>
        public bool QueueExecuteAttack(GameObject target)
        {
            // 检查生命值
            var health = GetComponent<Health>();
            if (health != null && health.hitPoints <= 0f)
            {
                return false;
            }

            // 选择一个可用技能加入队列
            if (skillComponent != null)
            {
                foreach (var skill in skillComponent.skills)
                {
                    if (!skill.isPassiveSkill && (skill.isFirstUse || Time.time - skill.lastUseTime >= skill.cooldown))
                    {
                        QueueSkill(skill.name, 50, target);
                        return true;
                    }
                }
            }

            return false;
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
        /// 检查是否有可用的技能
        /// </summary>
        /// <returns>是否有可用的技能</returns>
        public bool HasAvailableSkill()
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
        /// 队列技能执行
        /// </summary>
        /// <param name="skillName">技能名称</param>
        /// <param name="priority">优先级</param>
        /// <param name="target">目标</param>
        /// <param name="skillLevel">技能等级</param>
        /// <param name="damageAmount">伤害量</param>
        public void QueueSkill(string skillName, int priority = 0, GameObject target = null, int skillLevel = 0, float damageAmount = 0f)
        {
            if (!skillComponent.IsSkillCooldown(skillName)) return;
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
            TbbDebuger.LogDebug($"[MutanterCombatManager] 开始处理技能队列，队列长度: {executionQueue.Count}");
            if (executionQueue.Count == 0)
            {
                TbbDebuger.LogDebug($"[MutanterCombatManager] 技能队列为空，停止处理");
                isProcessingQueue = false;
                return;
            }

            isProcessingQueue = true;
            var request = executionQueue[0];
            executionQueue.RemoveAt(0);
            TbbDebuger.LogDebug($"[MutanterCombatManager] 处理技能: {request.skillName}，优先级: {request.priority}，剩余队列长度: {executionQueue.Count}");

            // 锁定状态机
            LockStateMachine();

            // 执行技能
            TbbDebuger.LogDebug($"[MutanterCombatManager] 执行技能: {request.skillName}");
            bool success = ExecuteSkill(request.skillName, request.damageAmount);
            TbbDebuger.LogDebug($"[MutanterCombatManager] 技能执行结果: {success}");


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
            TbbDebuger.LogDebug($"[MutanterCombatManager] 开始解锁状态机并处理下一个技能");
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
            private KBatchedAnimController AnimController => animController ??= manager.gameObject.GetComponent<KBatchedAnimController>();

            public AnimationCoordinator(MutanterCombatManager manager)
            {
                this.manager = manager;
            }
            public void PlayAnimation(string animationName, float duration, System.Action onComplete = null)
            {
                if (AnimController != null)
                {
                    AnimController.Play($"{animationName}_pre", PlayMode.Once);
                    AnimController.Queue($"{animationName}_loop", PlayMode.Loop);
                    GameScheduler.Instance.Schedule($"{animationName}_animation_Play", duration, (_) =>
                    {
                        AnimController.Play($"{animationName}_pst", PlayMode.Once);
                    });
                    if (onComplete != null)
                    {
                        void kAnimEvent(object _)
                        {
                            var currentAnim = AnimController.GetCurrentAnim();
                            if (currentAnim != null && currentAnim.name == $"{animationName}_pst")
                            {
                                onComplete();
                            }
                            else
                            {
                                string currentAnimName = currentAnim != null ? currentAnim.name : "null";
                            }
                            AnimController.gameObject.Unsubscribe((int)GameHashes.AnimQueueComplete, kAnimEvent);
                        }

                        AnimController.gameObject.Subscribe((int)GameHashes.AnimQueueComplete, kAnimEvent);
                    }
                }
            }
            /// <summary>
            /// 播放动画
            /// </summary>
            public void PlayAnimation(string animationName, KAnim.PlayMode playMode, System.Action onComplete = null)
            {
                if (AnimController != null && !string.IsNullOrEmpty(animationName))
                {
                    AnimController.Play(animationName, playMode);

                    if (onComplete != null)
                    {
                        void kAnimEvent(object _)
                        {
                            onComplete();
                            AnimController.gameObject.Unsubscribe((int)GameHashes.AnimQueueComplete, kAnimEvent);
                        }

                        AnimController.gameObject.Subscribe((int)GameHashes.AnimQueueComplete, kAnimEvent);
                    }
                }
            }

            /// <summary>
            /// 清理动画队列
            /// </summary>
            public void ClearQueue()
            {
                AnimController?.ClearQueue();
            }
        }
    }
}
