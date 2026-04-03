using System.Collections.Generic;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent.VFXController
{
    public interface IVFXController
    {
        void Activate(GameObject target = null);
        void Deactivate();
        List<KPrefabID> GetAttackTargets();
        void UpdateLOD(float distance);
        void SetLODLevel(int level);
        int GetCurrentLODLevel();
    }
}
