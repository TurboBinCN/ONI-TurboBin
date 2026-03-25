using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;
using static MutantContainmentProject.MutanterComponent.MutanterSkillComponent;

namespace MutantContainmentProject.MutanterComponent
{
    public class FixerRedDeathDamageEffect : IExtraAnimationEffect
    {
        private FixerRedDeathDamageController deathDamageController;

        public FixerRedDeathDamageEffect(FixerRedDeathDamageController controller)
        {
            this.deathDamageController = controller;
        }

        public void Activate()
        {
            deathDamageController?.ActivateDeathDamage();
        }

        public void Deactivate()
        {
            deathDamageController?.DeactivateDeathDamage();
        }
        public List<KPrefabID> GetAttackTargets()
        {
            return deathDamageController?.GetAttackTargets() ?? new List<KPrefabID>();
        }
    }
    public class FixerRedDeathDamageController : LaserBeamController
    {
        private bool hasTriggeredDeathDamage = false;
        private Health health;
        private Health HealthInstance => health ??= GetComponent<Health>();

        public void ActivateDeathDamage()
        {
            base.ActiveParticle();
            base.StartBeamRotation();
            TbbDebuger.LogDebug($"[FixerRedDeathDamage] 激活红色收尾人死亡攻击 {gameObject.name}");
        }
        public void DeactivateDeathDamage()
        {
            base.DeactiveParticle();
            TbbDebuger.LogDebug($"[FixerRedDeathDamage] 取消红色收尾人死亡攻击 {gameObject.name}");
        }
        public List<KPrefabID> GetAttackTargets()
        {
            return base.GetAttackTargets();
        }
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
                var combat_manager = gameObject.GetComponent<MutanterCombatManager>();
                if (combat_manager != null)
                {
                    combat_manager.TryExecuteSkill(typeof(FixerRedDeathDamageController).Name);
                }
                TbbDebuger.LogDebug($"[FixerRedDeathDamage] Triggered DeathMonitor for {gameObject.name}");
            }
        }
    }
}
