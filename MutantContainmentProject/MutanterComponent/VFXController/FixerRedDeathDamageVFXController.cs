namespace MutantContainmentProject.MutanterComponent.VFXController
{
    [VFXAttribute("FixerRedDeathDamageVFX")]
    public class FixerRedDeathDamageVFXController : LaserBeamVFXController, IVFXController
    {
        public new void Activate()
        {
            base.ActiveParticle();
            base.StartBeamRotation();
        }
    }
}
