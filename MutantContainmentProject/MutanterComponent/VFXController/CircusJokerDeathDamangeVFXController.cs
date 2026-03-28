using System.Collections.Generic;

namespace MutantContainmentProject.MutanterComponent.VFXController
{
    [VFXAttribute("CircusJokerDeathDamangeVFX")]
    public class CircusJokerDeathDamangeVFXController : KMonoBehaviour, IVFXController
    {
        public void Activate()
        {
        }

        public void Deactivate()
        {
        }

        public List<KPrefabID> GetAttackTargets()
        {
            //TODO
            return new List<KPrefabID>();
        }
    }
}
