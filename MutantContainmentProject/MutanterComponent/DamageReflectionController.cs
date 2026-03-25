using HarmonyLib;
using System.Collections.Generic;
using TBB.He.TbbLib.Utils;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    public class DamageReflectionController : KMonoBehaviour
    {
        public float LastDamageReceived { get; private set; }
        public GameObject LastDamageSource { get; private set; }
        private MutanterCombatManager combatManager;
        private bool isSkillStart = false;

        private MutanterCombatManager CombatManager => combatManager ??= GetComponent<MutanterCombatManager>();

        protected override void OnSpawn()
        {
            base.OnSpawn();
        }

        protected override void OnCleanUp()
        {
            base.OnCleanUp();
        }

        public void HandleDamageReflection(float damage)
        {
            if (LastDamageSource != null && LastDamageSource != gameObject)
            {
                CombatManager?.TryExecuteSkill(GetType().Name, damage, false);
            }
        }

        public List<KPrefabID> GetAttackTargets()
        {
            List<KPrefabID> targets = new List<KPrefabID>();
            if (LastDamageSource != null)
            {
                var kPrefabID = LastDamageSource.GetComponent<KPrefabID>();
                if (kPrefabID != null)
                {
                    targets.Add(kPrefabID);
                }
            }
            return targets;
        }

        public void ActiveDamage()
        {
            isSkillStart = true;
        }

        public void DeactivateDamage()
        {
            isSkillStart = false;
        }

        // Harmony patch 类 - 拦截 Hit 的 DeliverHit 方法（推荐）
        [HarmonyPatch(typeof(Hit))]
        [HarmonyPatch("DeliverHit")]
        public static class Hit_DeliverHit_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(Hit __instance)
            {
                AttackProperties properties = (AttackProperties)TbbHarmonyExtension.GetField(__instance, "properties");
                GameObject target = (GameObject)TbbHarmonyExtension.GetField(__instance, "target");

                if (properties != null && target != null)
                {
                    var attacker = properties.attacker?.gameObject;

                    DamageReflectionController reflectionController = target.GetComponent<DamageReflectionController>();
                    if (reflectionController != null && reflectionController.isSkillStart)
                    {
                        // 计算伤害值
                        var health = target.GetComponent<Health>();
                        if (health != null)
                        {
                            float damage = UnityEngine.Random.Range(properties.base_damage_min, properties.base_damage_max);
                            damage *= (1f + target.GetComponent<AttackableBase>().GetDamageMultiplier());

                            // 存储伤害值和来源
                            reflectionController.LastDamageReceived = damage;
                            reflectionController.LastDamageSource = attacker;

                            // 处理伤害反射
                            reflectionController.HandleDamageReflection(damage);

                            return false; // 取消原始伤害
                        }
                    }
                }

                // 返回 true 继续执行原始方法，返回 false 拦截伤害
                return true;
            }
        }

        // Harmony patch 类 - 拦截 Health 的 Damage 方法（作为备用）
        [HarmonyPatch(typeof(Health))]
        [HarmonyPatch("Damage")]
        public static class Health_Damage_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(Health __instance, float amount)
            {
                DamageReflectionController reflectionController = __instance.gameObject.GetComponent<DamageReflectionController>();
                if (reflectionController != null && reflectionController.isSkillStart)
                {
                    reflectionController.LastDamageReceived = amount;

                    return false; // 取消原始伤害
                }

                // 返回 true 继续执行原始方法，返回 false 拦截伤害
                return true;
            }
        }
    }
}