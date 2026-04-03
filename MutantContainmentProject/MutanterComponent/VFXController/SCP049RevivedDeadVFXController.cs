using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent.VFXController
{
    [VFXAttribute("SCP049RevivedDeadVFX")]
    public class SCP049RevivedDeadVFXController : MagicHealSpellVFXController, IVFXController
    {
        private List<KPrefabID> attackTargets = new();
        public void Activate(GameObject target = null)
        {
            var targets = GetComponent<SCP049Controller>()?.deadBodies;
            if (targets.Count > 0 && targets[0] != null)
            {
                FaceTarget(targets[0]);
                attackTargets.Add(targets[0].GetComponent<KPrefabID>());
                base.ActivateVFX(targets[0]);
            }
        }
        private void FaceTarget(GameObject target)
        {
            if (target == null) return;
            Vector3 targetPos = target.transform.position;
            GetComponent<Facing>()?.Face(targetPos);
        }
        public List<KPrefabID> GetAttackTargets()
        {
            return attackTargets;
        }

        public int GetCurrentLODLevel() { return 0; }

        public void SetLODLevel(int level)
        {
        }

        public void UpdateLOD(float distance)
        {
        }
    }
}
