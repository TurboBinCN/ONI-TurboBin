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
            CombatManager.QueueSkillExecution(Skill.name, 100);
        }
    }
}
