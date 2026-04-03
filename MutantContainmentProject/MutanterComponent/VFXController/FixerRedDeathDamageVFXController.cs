using UnityEngine;

namespace MutantContainmentProject.MutanterComponent.VFXController
{
    [VFXAttribute("FixerRedDeathDamageVFX")]
    public class FixerRedDeathDamageVFXController : LaserBeamVFXController, IVFXController
    {
        public new void Activate(GameObject target = null)
        {
            base.ActiveParticle();
            base.StartBeamRotation();
        }
    }
}
