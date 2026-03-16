using Klei.AI;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    public class DeathDamage : KMonoBehaviour
    {
        public string damageType = "Mental";
        public float damageAmount = 5f;
        private Health health;
        private AmountInstance hitPointsInstance;
        private bool hasTriggeredDeathDamage = false;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            // 查找健康组件
            health = GetComponent<Health>();
            if (health != null)
            {
                // 获取生命值实例并订阅变化事件
                hitPointsInstance = Db.Get().Amounts.HitPoints.Lookup(gameObject);
                if (hitPointsInstance != null)
                {
                    hitPointsInstance.OnDelta += OnHitPointsChanged;
                }
            }
        }

        protected override void OnCleanUp()
        {
            // 取消订阅生命值变化事件
            if (hitPointsInstance != null)
            {
                hitPointsInstance.OnDelta -= OnHitPointsChanged;
            }
            base.OnCleanUp();
        }

        private void OnHitPointsChanged(float delta)
        {
            // 检查是否已经触发过死亡伤害
            if (hasTriggeredDeathDamage)
                return;
                
            // 检查生命值是否为0且已被击败
            if (health != null && health.hitPoints <= 0f && health.IsDefeated())
            {
                hasTriggeredDeathDamage = true;
                TbbDebuger.LogDebug($"[DeathDamage] Creature died, releasing death damage to all staff!");
                
                // 找到所有职员并造成伤害
                var duplicants = GameObject.FindObjectsOfType<MinionIdentity>();
                foreach (var dupe in duplicants)
                {
                    if (dupe != null && dupe.gameObject != null)
                    {
                        // 使用MutanterAttackSystem执行攻击
                        var attackSystem = GetComponent<MutanterAttackSystem>();
                        if (attackSystem != null)
                        {
                            // 执行精神伤害（通过压力值攻击模拟）
                            bool success = attackSystem.ExecuteStressAttack(dupe.gameObject, damageAmount);
                            if (success)
                            {
                                TbbDebuger.LogDebug($"[DeathDamage] Successfully dealt {damageAmount} mental damage to {dupe.name}");
                            }
                        }
                        else
                        {
                            // 降级处理：直接执行攻击
                            var dupeHealth = dupe.GetComponent<Health>();
                            if (dupeHealth != null)
                            {
                                // 造成精神伤害
                                dupeHealth.Damage(damageAmount);
                                TbbDebuger.LogDebug($"[DeathDamage] Fallback: Dealt {damageAmount} mental damage to {dupe.name}");
                            }
                        }
                    }
                }
            }
        }
    }
}
