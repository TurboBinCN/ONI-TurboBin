using System.Collections.Generic;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent.Triggers
{
    public interface ISkillTrigger : IPassiveSkillTrigger
    {
        int SelectSkill(GameObject caster, GameObject target, List<MutanterSkillComponent.SkillData> skills);
        void OnTriggerActivated(GameObject caster, MutanterSkillComponent.SkillData skill);
    }
}

