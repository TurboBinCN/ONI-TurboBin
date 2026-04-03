using TBB.He.TbbLib.Debuger;
namespace MutantContainmentProject.MutanterComponent.Triggers
{
    [SkillTrigger("CyclicCheckTrigger", 20, true)]
    public class CyclicCheckTrigger : KMonoBehaviour, IPassiveSkillTrigger, ISim1000ms
    {
        public string TriggerName => "CyclicCheckTrigger";

        public int Priority => 20;

        public bool IsPassive => true;

        private MutanterCombatManager combatManager;
        private MutanterCombatManager CombatManager => combatManager ??= gameObject.GetComponent<MutanterCombatManager>();
        public MutanterSkillComponent.SkillData Skill { get; set; }

        public void Sim1000ms(float dt)
        {
            if (Skill.triggers.Count <= 0) return;
            foreach (var trigger in Skill.triggers)
            {
                if(trigger.triggerName != TriggerName) continue;
                foreach (var (key, method) in trigger.conditionCallbackMethods)
                {
                    if (method(gameObject))
                    {
                        CombatManager.QueueSkill(Skill.name, 100);
                    }
                }
            }
        }
    }
}
