using UnityEngine;

namespace MutantContainmentProject.MutanterComponent.Effector
{
    [SkillEffector("SCP049HealEffector", 10)]
    public class SCP049HealEffector : KMonoBehaviour, ISkillEffector
    {
        public string EffectorName => "SCP049HealEffector";

        public int Priority => 10;

        public bool ApplyEffectorAfter(GameObject target, MutanterSkillComponent.SkillData skillData)
        {
            var controller = gameObject.GetComponent<SCP049Controller>();
            controller?.PerformFlawedRecovery();
            return true;
        }

        public bool ApplyEffectorsBefore()
        {
            return true;
        }
    }
}