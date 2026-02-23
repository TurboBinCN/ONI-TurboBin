using HarmonyLib;
using Klei.AI;
using UnityEngine;
using MutantContainmentProject.Mutanters;
using MutantContainmentProject.MutanterComponent;
using static GameTags;
using Random = UnityEngine.Random;
using TBB.He.TbbLib.Debuger;

namespace MutantContainmentProject.Duplicants
{
    [AddComponentMenu("KMonoBehaviour/scripts/AttackMutanterWeapon")]
    public class AttackMutanterWeapon : KMonoBehaviour
    {
        [MyCmpReq]
        private FactionAlignment alignment;
        [MyCmpReq]
        private Weapon weapon;
        [MyCmpReq]
        private StandardWorker worker;
        
        public float CalculateMutanterDamage(GameObject target)
        {
            TbbDebuger.LogDebug("CalculateMutanterDamage called for target: " + target.name);
            
            if (!IsMutanter(target))
            {
                TbbDebuger.LogDebug("Target is not a mutanter, returning 0 damage");
                return 0f;
            }
            
            float baseDamage = CalculateBaseDamage();
            float damageMultiplier = CalculateDamageMultiplier();
            float finalDamage = baseDamage * damageMultiplier;
            
            TbbDebuger.LogDebug("Calculated damage: base=" + baseDamage + ", multiplier=" + damageMultiplier + ", final=" + finalDamage);
            
            return finalDamage;
        }
        
        private bool IsMutanter(GameObject target)
        {
            // 检查目标是否为畸变体
            bool isMutanter = target.HasTag(MutanterTags.Mutanter);
            TbbDebuger.LogDebug("IsMutanter check for " + target.name + ": " + isMutanter);
            return isMutanter;
        }
        
        private float CalculateBaseDamage()
        {
            // 基础伤害计算
            float baseDamage;
            if (weapon != null && weapon.properties != null)
            {
                baseDamage = Random.Range(weapon.properties.base_damage_min, weapon.properties.base_damage_max);
                TbbDebuger.LogDebug("Calculated base damage from weapon: " + baseDamage);
            }
            else
            {
                baseDamage = 5f; // 默认基础伤害
                TbbDebuger.LogDebug("Using default base damage: " + baseDamage);
            }
            return baseDamage;
        }
        
        private float CalculateDamageMultiplier()
        {
            float multiplier = 1f;
            
            // 技能系统伤害加成
            // 从AttackDamage属性获取加成
            Attribute attribute = Db.Get().Attributes.TryGet("AttackDamage");
            if (attribute != null)
            {
                AttributeInstance instance = worker.GetAttributes().Get(attribute.Id);
                if (instance != null)
                {
                    float attributeValue = instance.GetTotalValue();
                    float attributeBonus = attributeValue * 0.1f;
                    multiplier += attributeBonus;
                    TbbDebuger.LogDebug("AttackDamage attribute value: " + attributeValue + ", bonus: " + attributeBonus);
                }
                else
                {
                    TbbDebuger.LogDebug("No AttackDamage attribute instance found for worker: " + worker.name);
                }
            }
            else
            {
                TbbDebuger.LogDebug("No AttackDamage attribute found in database");
            }
            
            // 后续武器系统的伤害加成
            // 这里可以扩展，例如根据装备的武器类型增加伤害
            
            float finalMultiplier = Mathf.Max(multiplier, 0.1f); // 最小伤害倍数为0.1
            TbbDebuger.LogDebug("Calculated damage multiplier: " + finalMultiplier);
            
            return finalMultiplier;
        }
        
        public void AttackMutanter(GameObject target)
        {
            TbbDebuger.LogDebug("AttackMutanter called for target: " + target.name);
            
            if (IsMutanter(target))
            {
                float damage = CalculateMutanterDamage(target);
                Health health = target.GetComponent<Health>();
                if (health != null)
                {
                    health.Damage(damage);
                    TbbDebuger.LogDebug("Dealt " + damage + " damage to " + target.name);
                }
                else
                {
                    TbbDebuger.LogDebug("No Health component found on target: " + target.name);
                }
            }
            else
            {
                TbbDebuger.LogDebug("Target is not a mutanter, skipping attack");
            }
        }
    }
}
