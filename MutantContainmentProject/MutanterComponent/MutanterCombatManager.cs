using TBB.He.TbbLib.Debuger;
using UnityEngine;
using static KAnim;

namespace MutantContainmentProject.MutanterComponent
{
    public class MutanterCombatManager : KMonoBehaviour
    {
        // 攻击状态跟踪
        private bool isAttacking = false;

        //基础技能攻击能力
        public bool AbilityOfBasicAttaction = true;

        // 动画协调器
        private AnimationCoordinator animationCoordinator;

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
            animationCoordinator = new AnimationCoordinator(this);
        }
        public bool MutiSegmentDamage() {
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

            if (skillComponent != null)
            {
                bool skillSuccess = skillComponent.TryExecuteSkill(target);
                if (skillSuccess)
                {
                    return true;
                }
            }

            // 尝试执行基础攻击
            if (AbilityOfBasicAttaction && attackSystem != null)
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
