using HarmonyLib;
using Klei.AI;
using MutantContainmentProject.Skills;
using MutantContainmentProject.Mutanters;
using MutantContainmentProject.MutanterComponent;
using UnityEngine;
using TBB.He.TbbLib.Debuger;
using Random = UnityEngine.Random;

namespace MutantContainmentProject.Duplicants
{
    [HarmonyPatch]
    public class DuplicantsAttribute_Patch
    {
        [HarmonyPatch(typeof(MinionConfig), "OnSpawn")]
        [HarmonyPostfix]
        public static void AddAttackMutanterWeapon(GameObject go)
        {
            TbbDebuger.LogDebug("AddAttackMutanterWeapon called for: " + go.name);
            
            // 为所有小人添加AttackMutanterWeapon组件
            if (!go.GetComponent<AttackMutanterWeapon>())
            {
                go.AddComponent<AttackMutanterWeapon>();
                TbbDebuger.LogDebug("Added AttackMutanterWeapon to: " + go.name);
            }
            else
            {
                TbbDebuger.LogDebug("AttackMutanterWeapon already exists on: " + go.name);
            }
        }
        
        [HarmonyPatch(typeof(AttackableBase), "GetDamageMultiplier")]
        [HarmonyPrefix]
        public static bool ModifyDamageMultiplier(AttackableBase __instance, ref float __result)
        {
            TbbDebuger.LogDebug("ModifyDamageMultiplier called for: " + __instance.gameObject.name);
            
            // 检查目标是否为畸变体
            if (__instance.gameObject.HasTag(MutanterTags.Mutanter))
            {
                TbbDebuger.LogDebug("Target is a mutanter: " + __instance.gameObject.name);
                
                // 对于畸变体，使用我们的自定义伤害计算
                StandardWorker worker = __instance.worker as StandardWorker;
                if (worker != null)
                {
                    TbbDebuger.LogDebug("Worker found: " + worker.name);
                    
                    AttackMutanterWeapon weapon = worker.gameObject.GetComponent<AttackMutanterWeapon>();
                    if (weapon != null)
                    {
                        TbbDebuger.LogDebug("AttackMutanterWeapon found on worker: " + worker.name);
                        
                        // 计算基础伤害
                        float baseDamage = Random.Range(5f, 10f); // 默认基础伤害范围
                        // 计算最终伤害
                        float finalDamage = weapon.CalculateMutanterDamage(__instance.gameObject);
                        // 将最终伤害转换为倍数
                        __result = finalDamage / baseDamage;
                        
                        TbbDebuger.LogDebug("Calculated damage multiplier: " + __result + " (finalDamage: " + finalDamage + ", baseDamage: " + baseDamage + ")");
                        
                        return false; // 跳过原方法的执行
                    }
                    else
                    {
                        TbbDebuger.LogDebug("AttackMutanterWeapon not found on worker: " + worker.name);
                    }
                }
                else
                {
                    TbbDebuger.LogDebug("No worker found for target: " + __instance.gameObject.name);
                }
            }
            else
            {
                TbbDebuger.LogDebug("Target is not a mutanter, using original damage calculation");
            }
            
            // 对于非畸变体，使用原方法的计算结果
            return true;
        }
    }
}
