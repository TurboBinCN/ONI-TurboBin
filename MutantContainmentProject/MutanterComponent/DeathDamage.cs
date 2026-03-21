using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    public class DeathDamage : KMonoBehaviour
    {
        public Tag attackTag = MutanterTags.PhysicalAttack;
        public float damageAmount = 12.5f;
        public float damageRadius = 5f;
        private bool hasTriggeredDeathDamage = false;

        private Health health;
        private Health Health => health ??= GetComponent<Health>();

        private LaserBeamController laserBeamController;
        public LaserBeamController LaserBeamController => laserBeamController ??= GetComponent<LaserBeamController>();

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.HealthChanged, OnHitPointsChanged);
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.HealthChanged);
            base.OnCleanUp();
        }

        private void OnHitPointsChanged(object data)
        {
            // 检查是否已经触发过死亡伤害
            if (hasTriggeredDeathDamage)
                return;

            // 检查生命值是否为0且已被击败
            if (Health != null && Health.hitPoints <= 0f)
            {
                hasTriggeredDeathDamage = true;
                TbbDebuger.LogDebug($"[DeathDamage] Creature died, releasing death damage to nearby units!");

                // 立即停止所有攻击行为
                var combatManager = GetComponent<MutanterCombatManager>();
                if (combatManager != null)
                {
                    combatManager.StopAllAttacks();
                    TbbDebuger.LogDebug($"[DeathDamage] Stopped all attacks via CombatManager for {gameObject.name}");
                }
                else
                {
                    // 备用方案：直接清理动画队列
                    var animController = gameObject.GetComponent<KBatchedAnimController>();
                    if (animController != null)
                    {
                        animController.ClearQueue();
                        TbbDebuger.LogDebug($"[DeathDamage] Cleared animation queue for {gameObject.name}");
                    }
                }

                // 找到范围内的所有单位并造成伤害
                var allGameObjects = GameObject.FindObjectsOfType<GameObject>();
                foreach (var obj in allGameObjects)
                {
                    if (obj != null && obj != gameObject)
                    {
                        // 检查距离
                        float distance = Vector3.Distance(transform.position, obj.transform.position);
                        if (distance <= damageRadius)
                        {
                            // 检查是否有Health组件
                            var targetHealth = obj.GetComponent<Health>();
                            if (targetHealth != null && targetHealth.IsDefeated() == false)
                            {
                                // 使用MutanterAttackSystem执行攻击
                                var attackSystem = GetComponent<MutanterAttackSystem>();
                                if (attackSystem != null)
                                {
                                    // 执行攻击
                                    bool success = attackSystem.TryExecuteAttack(obj, damageAmount, attackTag);
                                    if (success)
                                    {
                                        TbbDebuger.LogDebug($"[DeathDamage] Successfully dealt {damageAmount} damage with tag {attackTag} to {obj.name}");
                                    }
                                }
                            }
                        }
                    }
                }
                // 延迟触发 DeathMonitor 的死亡状态，确保 MutanterStateMachine 状态转换完成
                // 加长延时到 0.3 秒，确保状态转换和动画清理完成
                TbbDebuger.LogDebug($"[DeathDamage] Scheduling DeathMonitor trigger with 0.3s delay for {gameObject.name}");
                GameScheduler.Instance.Schedule("TriggerDeathMonitor", 0.3f, (_) => TriggerDeathMonitor());
            }
        }

        private void TriggerDeathMonitor()
        {
            var animController = gameObject.GetComponent<KBatchedAnimController>();
            // 激活激光粒子效果
            if (LaserBeamController != null)
            {
                LaserBeamController.ActiveParticle();
                LaserBeamController.StartBeamRotation();
                TbbDebuger.LogDebug($"[DeathDamage] Activated LaserBeamController for {gameObject.name}");
            }

            // 触发 DeathMonitor 的死亡状态
            var deathMonitor = gameObject.GetSMI<DeathMonitor.Instance>();
            if (deathMonitor != null)
            {
                // 使用通用死亡类型
                deathMonitor.Kill(Db.Get().Deaths.Generic);
                TbbDebuger.LogDebug($"[DeathDamage] Triggered DeathMonitor for {gameObject.name}");
                
                // 监听死亡动画完成事件，用于关闭激光粒子效果
                if (animController != null)
                {
                    System.Action<object> kAnimEvent = null;
                    kAnimEvent = (_) =>
                    {
                        if (LaserBeamController != null)
                        {
                            LaserBeamController.DeactiveParticle();
                            TbbDebuger.LogDebug($"[DeathDamage] Deactivated LaserBeamController for {gameObject.name}");
                        }
                        animController.gameObject.Unsubscribe((int)GameHashes.AnimQueueComplete, kAnimEvent);
                    };
                    animController.gameObject.Subscribe((int)GameHashes.AnimQueueComplete, kAnimEvent);
                }
            }
            else
            {
                // 备用方案：直接添加死亡标签并停止移动
                gameObject.AddTag(GameTags.Dead);
                var navigator = GetComponent<Navigator>();
                if (navigator != null)
                {
                    navigator.Stop();
                }
                
                // 播放死亡动画
                if (animController != null)
                {
                    animController.Play("death", KAnim.PlayMode.Once);
                    
                    // 监听动画完成事件
                    System.Action<object> kAnimEvent = null;
                    kAnimEvent = (_) =>
                    {
                        if (LaserBeamController != null)
                        {
                            LaserBeamController.DeactiveParticle();
                            TbbDebuger.LogDebug($"[DeathDamage] Deactivated LaserBeamController for {gameObject.name}");
                        }
                        animController.gameObject.Unsubscribe((int)GameHashes.AnimQueueComplete, kAnimEvent);
                    };
                    animController.gameObject.Subscribe((int)GameHashes.AnimQueueComplete, kAnimEvent);
                }
                
                TbbDebuger.LogDebug($"[DeathDamage] DeathMonitor not found, using fallback death handling for {gameObject.name}");
            }
        }
    }
}
