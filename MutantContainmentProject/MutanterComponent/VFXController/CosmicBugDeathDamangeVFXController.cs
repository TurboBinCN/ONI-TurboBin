using System.Collections.Generic;

namespace MutantContainmentProject.MutanterComponent.VFXController
{
    [VFXAttribute("CosmicBugDeathDamangeVFX")]
    public class CosmicBugDeathDamangeVFXController : KMonoBehaviour, IVFXController
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
