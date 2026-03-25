using System.Collections.Generic;
using static MutantContainmentProject.MutanterComponent.MutanterSkillComponent;

namespace MutantContainmentProject.MutanterComponent
{
    public class FixerWhiteLaserSweepEffect : IExtraAnimationEffect
    {
        private FixerWhiteLaserSweepController laser;

        public FixerWhiteLaserSweepEffect(FixerWhiteLaserSweepController laser)
        {
            this.laser = laser;
        }

        public void Activate()
        {
            laser?.ActivateLaserSweep();
        }

        public void Deactivate()
        {
            laser?.DeactivateLaserSweep();
        }
        public List<KPrefabID> GetAttackTargets()
        {
            return laser?.GetAttackTargets() ?? new List<KPrefabID>();
        }
    }
    public class FixerWhiteLaserSweepController : FixerWhiteLaserController
    {
        public void ActivateLaserSweep()
        {
            base.ActivateLaser();
            base.StartRotation();
        }
        public void DeactivateLaserSweep()
        {
            base.DeactivateLaser();
        }
        public List<KPrefabID> GetAttackTarget()
        {
            return base.GetAttackTargets();
        }
    }
}