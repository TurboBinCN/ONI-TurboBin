using UnityEngine;

namespace MutantContainmentProject.MutanterComponent.Effector
{
    [SkillEffector("SCP049ReviveEffector", 10)]
    public class SCP049ReviveEffector : KMonoBehaviour, ISkillEffector
    {
        public string EffectorName => "SCP049ReviveEffector";

        public int Priority => 10;

        public bool ApplyEffectorAfter(GameObject target, MutanterSkillComponent.SkillData skillData)
        {
            var controller = gameObject.GetComponent<SCP049Controller>();
            controller?.PerformRevivedZombie();
            return true;
        }

        public bool ApplyEffectorsBefore()
        {
            return true;
        }
    }
}