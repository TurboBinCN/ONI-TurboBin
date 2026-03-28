using System.Collections.Generic;
using static MutantContainmentProject.MutanterComponent.MutanterSkillComponent;

namespace MutantContainmentProject.MutanterComponent.Triggers
{
    public interface IPassiveSkillTrigger
    {
        string TriggerName { get; }
        public int Priority { get; }
        bool IsPassive { get; }

        SkillData Skill { get; set; }
    }
}
