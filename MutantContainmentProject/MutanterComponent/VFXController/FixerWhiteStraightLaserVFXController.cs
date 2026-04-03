using UnityEngine;

namespace MutantContainmentProject.MutanterComponent.VFXController
{
    [VFXAttribute("FixerWhiteStraightLaserVFX")]
    public class FixerWhiteStraightLaserVFXController : FixerWhiteLaserVFXController, IVFXController
    {
        public void Activate(GameObject target = null)
        {
            base.ActivateStraightLaser();
        }
    }
}
