using System.Collections.Generic;
using UnityEngine;
using static MutantContainmentProject.MutanterComponent.MutanterSkillComponent;

namespace MutantContainmentProject.MutanterComponent
{
    public class FixerWhitePrayerSkillEffect : IExtraAnimationEffect
    {
        private FixerWhitePrayerSkillController damageReflectionController;

        public FixerWhitePrayerSkillEffect(FixerWhitePrayerSkillController controller)
        {
            this.damageReflectionController = controller;
        }

        public void Activate()
        {
            damageReflectionController?.StartPrayer();
        }

        public void Deactivate()
        {
            damageReflectionController?.DeactivatePrayer();
        }
        public List<KPrefabID> GetAttackTargets()
        {
            return damageReflectionController?.GetAttackTargets() ?? new List<KPrefabID>();
        }
    }
    public class FixerWhitePrayerSkillController : DamageReflectionController
    {
        private Health health;
        private Health HealthCom => health ??= gameObject.GetComponent<Health>();

        private MutanterCombatManager combatManager;
        private MutanterCombatManager CombatManager => combatManager ??= gameObject.GetComponent<MutanterCombatManager>();

        private float initialHealth = 0f;
        private float lastHealth = 0f;
        private float healthThreshold = 0.3f; // 30% 生命值阈值
        private float lastDamageTime = 0f;
        private float resetTime = 5f; // 5秒内无伤害则重置
        private float[] fixedThresholds = new float[] { 0.7f, 0.4f, 0.1f }; // 70%、40%、10% 固定阈值
        private bool[] thresholdTriggered = new bool[] { false, false, false }; // 记录阈值是否已触发

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.HealthChanged, OnHealthChanged);

            // 初始化生命值
            if (HealthCom != null)
            {
                initialHealth = HealthCom.maxHitPoints;
                lastHealth = HealthCom.hitPoints;
            }

            // 初始化阈值触发数组
            thresholdTriggered = new bool[] { false, false, false };
        }
        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.HealthChanged, OnHealthChanged);
            base.OnCleanUp();
        }
        public void OnHealthChanged(object data)
        {
            // 检查是否长时间无伤害
            if (lastDamageTime > 0 && Time.time - lastDamageTime > resetTime)
            {
                // 重置伤害计时
                lastDamageTime = 0f;
            }
            if (HealthCom == null || initialHealth <= 0)
                return;

            float currentHealth = HealthCom.hitPoints;
            float healthLost = lastHealth - currentHealth;

            // 检查是否有生命值损失
            if (healthLost <= 0)
            {
                // 长时间无伤害，重置计时
                lastDamageTime = 0f;
                lastHealth = currentHealth;
                return;
            }

            // 更新最后伤害时间
            lastDamageTime = Time.time;

            // 计算当前生命值比例
            float currentHealthRatio = currentHealth / initialHealth;

            // 检查固定阈值（70%、40%、10%）
            for (int i = 0; i < fixedThresholds.Length; i++)
            {
                if (!thresholdTriggered[i] && currentHealthRatio <= fixedThresholds[i])
                {
                    thresholdTriggered[i] = true;
                    // 触发固定阈值，播放动画
                    StartPrayer();
                    CombatManager.TryExecuteSkill(GetType().Name, 0, true);
                    lastHealth = currentHealth;
                    return;
                }
            }

            // 检查30%损失阈值
            float healthLostRatio = healthLost / initialHealth;
            if (healthLostRatio >= healthThreshold)
            {
                StartPrayer();
                CombatManager.TryExecuteSkill(GetType().Name, 0, true);
            }

            // 更新上次生命值
            lastHealth = currentHealth;
        }
        public void DeactivatePrayer()
        {
            base.DeactivateDamage();
        }
        public void StartPrayer()
        {
            base.ActiveDamage();
        }

        public List<KPrefabID> GetAttackTargets()
        {

            return base.GetAttackTargets();
        }
    }
}
