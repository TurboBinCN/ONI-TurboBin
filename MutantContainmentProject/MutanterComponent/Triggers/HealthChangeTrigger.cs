using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent.Triggers
{
    [SkillTrigger("HealthChangeTrigger", 10, true)]
    public class HealthChangeTrigger : KMonoBehaviour, IPassiveSkillTrigger
    {
        public string TriggerName => "HealthChangeTrigger";
        public int Priority => 10;
        public bool IsPassive => true;

        private float healthChangeDelta = 0.3f;
        private Health health;
        private Health HealthCom => health ??= GetComponent<Health>();
        private MutanterCombatManager combatManager;
        private MutanterCombatManager CombatManager => combatManager ??= gameObject.GetComponent<MutanterCombatManager>();

        public MutanterSkillComponent.SkillData Skill { get; set; }

        private float initialHealth = 0f;
        private float lastHealth = 0f;
        private float healthThreshold = 0.3f; // 30% 生命值阈值
        private float lastDamageTime = 0f;
        private float resetTime = 5f; // 5秒无伤害后重置
        private float[] fixedThresholds = new float[] { 0.7f, 0.4f, 0.1f }; // 70%、40%、10% 固定阈值
        private bool[] thresholdTriggered = new bool[] { false, false, false }; // 记录阈值是否已触发

        protected override void OnSpawn()
        {
            base.OnSpawn();

            foreach (var item in Skill.triggers)
            {
                if (item.triggerName == TriggerName)
                {
                    if (item.properties.TryGetValue("ChangeDelta", out object value))
                    {
                        if (value is float floatValue)
                        {
                            healthChangeDelta = floatValue;
                        }
                        else if (value is double doubleValue)
                        {
                            healthChangeDelta = (float)doubleValue;
                        }
                        else if (value is int intValue)
                        {
                            healthChangeDelta = intValue;
                        }
                    }
                }
            }

            // 初始化生命值
            if (HealthCom != null)
            {
                initialHealth = HealthCom.maxHitPoints;
                lastHealth = HealthCom.hitPoints;
            }

            // 初始化阈值触发状态
            thresholdTriggered = new bool[] { false, false, false };
            Subscribe((int)GameHashes.HealthChanged, OnHealthChanged);
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.HealthChanged, OnHealthChanged);
            base.OnCleanUp();
        }

        public void OnHealthChanged(object data)
        {
            // 检查是否超时重置伤害
            if (lastDamageTime > 0 && Time.time - lastDamageTime > resetTime)
            {
                // 重置伤害计时器
                lastDamageTime = 0f;
            }

            if (HealthCom == null || initialHealth <= 0)
            {
                return;
            }

            float currentHealth = HealthCom.hitPoints;
            float healthLost = lastHealth - currentHealth;

            // 检查是否是生命值恢复
            if (healthLost <= 0)
            {
                // 立即重置伤害计时器
                lastDamageTime = 0f;
                lastHealth = currentHealth;
                return;
            }

            // 更新伤害时间
            lastDamageTime = Time.time;

            // 计算当前生命值比例
            float currentHealthRatio = currentHealth / initialHealth;

            // 检查固定阈值（70%、40%、10%）
            for (int i = 0; i < fixedThresholds.Length; i++)
            {
                if (!thresholdTriggered[i] && currentHealthRatio <= fixedThresholds[i])
                {
                    thresholdTriggered[i] = true;
                    // 触发固定阈值对应的技能
                        var combatManager = gameObject.GetComponent<MutanterCombatManager>();
                    // 使用战斗管理器执行被动技能，优先级设为高
                    combatManager?.QueueSkill(Skill.name, 50, null, 0);
                    lastHealth = currentHealth;
                        return;
                    }
                }

                // 检查30%伤害阈值
                float healthLostRatio = healthLost / initialHealth;
                if (healthLostRatio >= healthThreshold)
                {
                    var combatManager = gameObject.GetComponent<MutanterCombatManager>();
                // 使用战斗管理器执行被动技能，优先级设为高
                combatManager?.QueueSkill(Skill.name, 50, null, 0);
            }

            // 更新上次生命值
            lastHealth = currentHealth;
        }
    }
}