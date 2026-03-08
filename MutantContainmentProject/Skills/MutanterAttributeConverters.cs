using Database;
using UnityEngine;

namespace MutantContainmentProject.Skills
{
    public class MutanterAttributeConverters
    {
        public static string AttributeWorkingSpeedConverterID = "WorkingSpeedConverter";
        public static string AttributeAttackDamageConverterID = "AttackDamageConverter";

        public static void RegisterAttributeConverters(AttributeConverters __instance)
        {
            Debug.Log("[MutanterAttributeConverters] RegisterAttributeConverters called");
            
            // 获取属性
            var workingSpeedAttribute = Db.Get().Attributes.Get(MutanterAttributes.AttributeWorkingSpeedID);
            var attackDamageAttribute = Db.Get().Attributes.Get(MutanterAttributes.AttributeAttackDamageID);

            Debug.Log("[MutanterAttributeConverters] workingSpeedAttribute: " + (workingSpeedAttribute != null ? workingSpeedAttribute.Id : "null"));
            Debug.Log("[MutanterAttributeConverters] attackDamageAttribute: " + (attackDamageAttribute != null ? attackDamageAttribute.Id : "null"));

            // 创建百分比格式化器
            var percentFormatter = new ToPercentAttributeFormatter(1f);

            // 创建工作速度转换器
            if (workingSpeedAttribute != null)
            {
                Debug.Log("[MutanterAttributeConverters] Creating WorkingSpeedConverter");
                __instance.Create(
                    AttributeWorkingSpeedConverterID,
                    "Working Speed",
                    "Increases containment work speed",
                    workingSpeedAttribute,
                    0.1f, // 每级增加10%工作速度
                    0.0f,
                    percentFormatter
                );
                Debug.Log("[MutanterAttributeConverters] WorkingSpeedConverter created");
            }
            else
            {
                Debug.Log("[MutanterAttributeConverters] workingSpeedAttribute is null, skipping WorkingSpeedConverter creation");
            }

            // 创建攻击伤害转换器
            if (attackDamageAttribute != null)
            {
                Debug.Log("[MutanterAttributeConverters] Creating AttackDamageConverter");
                __instance.Create(
                    AttributeAttackDamageConverterID,
                    "Attack Damage",
                    "Increases damage against mutanters",
                    attackDamageAttribute,
                    0.1f, // 每级增加10%攻击伤害
                    0.0f,
                    percentFormatter
                );
                Debug.Log("[MutanterAttributeConverters] AttackDamageConverter created");
            }
            else
            {
                Debug.Log("[MutanterAttributeConverters] attackDamageAttribute is null, skipping AttackDamageConverter creation");
            }
        }
    }
}