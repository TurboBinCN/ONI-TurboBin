using System.Collections.Generic;

namespace MutantContainmentProject.MutanterComponent.VFXController
{
    [VFXAttribute("FixerWhiteLaserSweepVFX")]
    public class FixerWhiteLaserSweepVFXController : FixerWhiteLaserVFXController, IVFXController
    {
        public new void Activate()
        {
            base.ActivateLaser();
            base.StartRotation();
        }

        public List<KPrefabID> GetAttackTarget()
        {
            return base.GetAttackTargets();
        }
    }
}