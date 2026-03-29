namespace MutantContainmentProject.MutanterComponent.VFXController
{
    [VFXAttribute("FixerWhiteStraightLaserVFX")]
    public class FixerWhiteStraightLaserVFXController : FixerWhiteLaserVFXController, IVFXController
    {
        public void Activate()
        {
            base.ActivateStraightLaser();
        }
    }
}
