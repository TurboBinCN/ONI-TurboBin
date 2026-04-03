using System.Collections.Generic;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent.VFXController
{
    [VFXAttribute("FixerWhiteLaserSweepVFX")]
    public class FixerWhiteLaserSweepVFXController : FixerWhiteLaserVFXController, IVFXController
    {
        public void Activate(GameObject target = null)
        {
            base.ActivateLaser();
            base.StartRotation();
        }
    }
}