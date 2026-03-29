using System.Collections.Generic;

namespace MutantContainmentProject.MutanterComponent.VFXController
{
    public interface IVFXController
    {
        void Activate();
        void Deactivate();
        List<KPrefabID> GetAttackTargets();
        void UpdateLOD(float distance);
        void SetLODLevel(int level);
        int GetCurrentLODLevel();
    }
}
