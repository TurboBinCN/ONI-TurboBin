using TBB.He.TbbLib.Debuger;
using UnityEngine;
using static KAnim;

namespace MutantContainmentProject.MutanterComponent
{
    public class MutanterCombatManager : KMonoBehaviour
    {
        // 攻击状态跟踪
        private bool isAttacking = false;

        // 技能系统
        private MutanterSkillSystem skillSystem;

        // 动画协调器
        private AnimationCoordinator animationCoordinator;

        // 状态管理器
        private StateManager stateManager;

        // 攻击系统
        private MutanterAttackSystem attackSystem;

        // 技能组件
        private MutanterSkillComponent skillComponent;

        protected override void OnSpawn()
        {
            base.OnSpawn();

            // 初始化组件
            attackSystem = GetComponent<MutanterAttackSystem>();
            skillComponent = GetComponent<MutanterSkillComponent>();

            // 初始化系统
            skillSystem = new MutanterSkillSystem(this);
            animationCoordinator = new AnimationCoordinator(this);
            stateManager = new StateManager(this);
        }
        public void TryExecuteSkill(string extraAnimationEffectId, float damageAmount = 0f, bool playAnimation = true)
        {
            if (skillComponent != null)
            {
                bool skillSuccess = skillComponent.TryExecuteSkill(extraAnimationEffectId, damageAmount, playAnimation);
                if (skillSuccess)
                {
                    TbbDebuger.LogDebug($"[MutanterCombatManager] 执行技能攻击 for {gameObject.name}");
                }
            }
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

            if (skillComponent != null)
            {
                bool skillSuccess = skillComponent.TryExecuteSkill(target);
                if (skillSuccess)
                {
                    return true;
                }
            }

            // 尝试执行基础攻击
            if (attackSystem != null)
            {
                bool attackSuccess = attackSystem.TryExecuteAttack(target);
                if (attackSuccess)
                {
                    return true;
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

        // 技能系统
        public class MutanterSkillSystem
        {
            private MutanterCombatManager manager;

            public MutanterSkillSystem(MutanterCombatManager manager)
            {
                this.manager = manager;
            }

            // 技能相关逻辑
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

        // 状态管理器
        public class StateManager
        {
            private MutanterCombatManager manager;

            public StateManager(MutanterCombatManager manager)
            {
                this.manager = manager;
            }

            // 状态相关逻辑
        }
    }
}
