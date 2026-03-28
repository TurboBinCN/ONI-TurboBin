using HarmonyLib;
using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;
using TBB.He.TbbLib.Module;
using TBB.He.TbbLib.Utils;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent.Effector
{
    [SkillEffector("FrozenHitPoints", 10)]
    public class FrozenHitPointsEffector : KMonoBehaviour, ISkillEffector
    {
        public string EffectorName => "FrozenHitPoints";

        public int Priority => 10;
        public bool Active { get; set; }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Active = false;
            MutantContainmentProjectMod.MutantContainmentProject.ModHarmony.Patch(typeof(Hit), "DeliverHit",
                prefix: new HarmonyMethod(typeof(FrozenHitPointsEffector), nameof(Hit_DeliverHit_Patch_Prefix)) { priority = HarmonyLib.Priority.Last });
        }
        protected override void OnCleanUp()
        {
            base.OnCleanUp();
        }

        public bool ApplyEffectorAfter(GameObject target, MutanterSkillComponent.SkillData skillData)
        {
            Active = false;
            return true;
        }

        public bool ApplyEffectorsBefore()
        {
            Active = true;
            return true;
        }
        public static bool Hit_DeliverHit_Patch_Prefix(Hit __instance)
        {
            AttackProperties properties = (AttackProperties)TbbHarmonyExtension.GetField(__instance, "properties");
            GameObject target = (GameObject)TbbHarmonyExtension.GetField(__instance, "target");

            if (properties != null && target != null)
            {
                var attacker = properties.attacker?.gameObject;

                var effector = target.GetComponent<FrozenHitPointsEffector>();
                if (effector != null && effector.Active) return false;
            }
            return true;
        }
    }
}
