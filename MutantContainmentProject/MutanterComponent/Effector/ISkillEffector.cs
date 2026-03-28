using System.Collections.Generic;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent.Effector
{
    public interface ISkillEffector
    {
        string EffectorName { get; }
        public int Priority { get; }
        bool ApplyEffectorsBefore();
        bool ApplyEffectorAfter(GameObject target, MutanterSkillComponent.SkillData skillData);
    }
}
