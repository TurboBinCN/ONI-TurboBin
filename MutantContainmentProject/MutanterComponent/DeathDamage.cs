using Klei.AI;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    public class DeathDamage : KMonoBehaviour
    {
        public Tag attackTag = MutanterTags.PhysicalAttack;
        public float damageAmount = 12.5f;
        public float damageRadius = 5f;
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
                TbbDebuger.LogDebug($"[DeathDamage] Creature died, releasing death damage to nearby units!");
                
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
            }
        }
    }
}
