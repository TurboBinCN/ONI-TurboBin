using System.Collections.Generic;

namespace MutantContainmentProject.MutanterComponent.VFXController
{
    public interface IVFXController
    {
        void Activate();
        void Deactivate();
        List<KPrefabID> GetAttackTargets();
    }
}
