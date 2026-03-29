using System.Collections.Generic;

namespace MutantContainmentProject.MutanterComponent.VFXController
{
    [VFXAttribute("FixerWhiteLaserSweepVFX")]
    public class FixerWhiteLaserSweepVFXController : FixerWhiteLaserVFXController, IVFXController
    {
        public void Activate()
        {
            base.ActivateLaser();
            base.StartRotation();
        }
    }
}