using System.Collections.Generic;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent.VFXController
{
    [VFXAttribute("CircusJokerDeathDamangeVFX")]
    public class CircusJokerDeathDamangeVFXController : KMonoBehaviour, IVFXController
    {
        private int currentLODLevel = 0;

        public void Activate(GameObject target = null)
        {
        }

        public void Deactivate()
        {
        }

        public List<KPrefabID> GetAttackTargets()
        {
            //TODO
            return new List<KPrefabID>();
        }

        public void UpdateLOD(float distance)
        {
            int newLODLevel = 0;
            if (distance > 10f)
            {
                newLODLevel = 2; // 低细节
            }
            else if (distance > 5f)
            {
                newLODLevel = 1; // 中等细节
            }
            else
            {
                newLODLevel = 0; // 高细节
            }

            if (newLODLevel != currentLODLevel)
            {
                SetLODLevel(newLODLevel);
            }
        }

        public void SetLODLevel(int level)
        {
            currentLODLevel = level;
            // 这里可以添加具体的LOD实现
        }

        public int GetCurrentLODLevel()
        {
            return currentLODLevel;
        }
    }
}
