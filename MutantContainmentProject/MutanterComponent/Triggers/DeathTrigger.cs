using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent.Triggers
{
    [SkillTrigger("DeathTrigger", 10, true)]
    public class DeathTrigger : KMonoBehaviour, IPassiveSkillTrigger
    {
        public string TriggerName => "DeathTrigger";
        public int Priority => 10;
        public bool IsPassive => true;
        public MutanterSkillComponent.SkillData Skill { get; set; }
        private bool hasTriggeredDeathDamage = false;


        private Health health;
        private Health HealthInstance => health ??= GetComponent<Health>();
        protected override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.HealthChanged, OnHitPointsChanged);
        }
        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.HealthChanged, OnHitPointsChanged);
            base.OnCleanUp();
        }
        private void OnHitPointsChanged(object data)
        {
            if (hasTriggeredDeathDamage) return;

            // 检查生命值是否为0且已被击败
            if (HealthInstance != null && HealthInstance.hitPoints <= 0f)
            {
                hasTriggeredDeathDamage = true;

                // 延迟触发 DeathMonitor 的死亡状态，确保 MutanterStateMachine 状态转换完成
                // 加长延时到 0.3 秒，确保状态转换和动画清理完成
                TbbDebuger.LogDebug($"[FixerRedDeathDamage] Scheduling DeathMonitor trigger with 0.3s delay for {gameObject.name}");
                GameScheduler.Instance.Schedule("TriggerDeathMonitor", 0.3f, (_) => TriggerDeathMonitor());
            }
        }
        private void TriggerDeathMonitor()
        {
            // 触发 DeathMonitor 的死亡状态
            var deathMonitor = gameObject.GetSMI<DeathMonitor.Instance>();
            if (deathMonitor != null)
            {
                // 使用通用死亡类型
                deathMonitor.Kill(Db.Get().Deaths.Generic);
                var combatManager = gameObject.GetComponent<MutanterCombatManager>();
                // 使用战斗管理器执行被动技能，优先级设为最高
                combatManager?.QueueSkill(Skill.name, 100);
                TbbDebuger.LogDebug($"[FixerRedDeathDamage] Triggered DeathMonitor for {gameObject.name}");
            }
        }
    }
}
