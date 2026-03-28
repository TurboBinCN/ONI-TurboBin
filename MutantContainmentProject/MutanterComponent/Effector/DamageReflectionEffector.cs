using HarmonyLib;
using TBB.He.TbbLib.Debuger;
using TBB.He.TbbLib.Utils;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent.Effector
{
    [SkillEffector("DamageReflection", 10)]
    public class DamageReflectionEffector : KMonoBehaviour, ISkillEffector
    {
        public string EffectorName => "DamageReflection";

        public int Priority => 10;

        public bool Active { get; set; }

        private MutanterAttackSystem attackSystem;
        public MutanterAttackSystem AttackSystem => attackSystem ??= GetComponent<MutanterAttackSystem>();

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
        protected override void OnSpawn()
        {
            base.OnSpawn();
            
            Active = false;
            MutantContainmentProjectMod.MutantContainmentProject.ModHarmony.Patch(typeof(Hit), "DeliverHit",
                prefix: new HarmonyMethod(typeof(DamageReflectionEffector), nameof(Hit_DeliverHit_Patch_Prefix2)) { priority = HarmonyLib.Priority.High });
        }
        public static bool Hit_DeliverHit_Patch_Prefix2(Hit __instance)
        {
            AttackProperties properties = (AttackProperties)TbbHarmonyExtension.GetField(__instance, "properties");
            GameObject target = (GameObject)TbbHarmonyExtension.GetField(__instance, "target");
                
            if (properties != null && target != null)
            {
                var attacker = properties.attacker?.gameObject;

                var effector = target.GetComponent<DamageReflectionEffector>();
                    
                if (effector != null && effector.Active)
                {
                    // 计算伤害值
                    var health = target.GetComponent<Health>();
                        
                    if (health != null)
                    {
                        float damage = Random.Range(properties.base_damage_min, properties.base_damage_max);

                        damage *= (1f + target.GetComponent<AttackableBase>().GetDamageMultiplier());
                        effector.AttackSystem?.TryExecuteAttack(attacker, damage, MutanterTags.PhysicalAttack);
                    }
                }
            }

            // 返回 true 继续执行原始方法，返回 false 拦截伤害
            return true;
        }
    }
}
