using System.Collections.Generic;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent.Triggers
{
    public interface ISkillTrigger : IPassiveSkillTrigger
    {
        bool CheckCondition(GameObject caster, GameObject target = null);
        int SelectSkill(GameObject caster, GameObject target, List<MutanterSkillComponent.SkillData> skills);
        void OnTriggerActivated(GameObject caster, MutanterSkillComponent.SkillData skill);
    }
}

