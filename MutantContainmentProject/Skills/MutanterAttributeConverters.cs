using Database;
using UnityEngine;

namespace MutantContainmentProject.Skills
{
    public class MutanterAttributeConverters
    {
        public static string AttributeAttackDamageConverterID = "AttackDamageConverter";
        public static string AttributeAttackSpeedConverterID = "AttackSpeedConverter";
        public static string AttributePhysicalDefenseConverterID = "PhysicalDefenseConverter";
        public static string AttributeMentalDefenseConverterID = "MentalDefenseConverter";
        public static string AttributeContainmentSpeedConverterID = "ContainmentSpeedConverter";
        public static string AttributeSafetyMeasureSuccessRateConverterID = "SafetyMeasureSuccessRateConverter";

        public static void RegisterAttributeConverters(AttributeConverters __instance)
        {
            // 获取属性
            var disciplineAttribute = Db.Get().Attributes.Get(MutanterAttributes.AttributeDisciplineID);
            var righteousnessAttribute = Db.Get().Attributes.Get(MutanterAttributes.AttributeRighteousnessID);
            var defenseAttribute = Db.Get().Attributes.Get(MutanterAttributes.AttributeDefenseID);

            // 创建百分比格式化器
            var percentFormatter = new ToPercentAttributeFormatter(1f);

            // 创建物理防御转换器
            if (defenseAttribute != null)
            {
                __instance.Create(
                    AttributePhysicalDefenseConverterID,
                    STRINGS.SKILLS.PHYSICAL_DEFENSE,
                    "Reduces physical damage taken",
                    defenseAttribute,
                    0.05f, // 每级减少5%物理伤害
                    0.0f,
                    percentFormatter
                );
            }

            // 创建精神防御转换器
            if (defenseAttribute != null)
            {
                __instance.Create(
                    AttributeMentalDefenseConverterID,
                    STRINGS.SKILLS.MENTAL_DEFENSE,
                    "Reduces mental damage taken",
                    defenseAttribute,
                    0.07f, // 每级减少7%精神伤害
                    0.0f,
                    percentFormatter
                );
            }

            // 创建收容速度转换器
            if (disciplineAttribute != null)
            {
                __instance.Create(
                    AttributeContainmentSpeedConverterID,
                    STRINGS.SKILLS.CONTAINMENT_SPEED,
                    "Increases containment work speed",
                    disciplineAttribute,
                    0.06f, // 每级增加6%收容速度
                    0.0f,
                    percentFormatter
                );
            }

            // 创建安全措施成功率转换器
            if (disciplineAttribute != null)
            {
                __instance.Create(
                    AttributeSafetyMeasureSuccessRateConverterID,
                    STRINGS.SKILLS.SAFETY_MEASURE_SUCCESS_RATE,
                    "Increases safety measure success rate",
                    disciplineAttribute,
                    0.08f, // 每级增加8%安全措施成功率
                    0.0f,
                    percentFormatter
                );
            }

            // 创建攻击伤害转换器
            if (righteousnessAttribute != null)
            {
                __instance.Create(
                    AttributeAttackDamageConverterID,
                    STRINGS.SKILLS.ATTACK_DAMAGE,
                    "Increases damage against mutanters",
                    righteousnessAttribute,
                    0.1f, // 每级增加10%攻击伤害（保持与原来相同的百分比）
                    0.0f,
                    percentFormatter // 使用百分比格式化器
                );
            }

            // 创建攻击速度转换器
            if (righteousnessAttribute != null)
            {
                __instance.Create(
                    AttributeAttackSpeedConverterID,
                    STRINGS.SKILLS.ATTACK_SPEED,
                    "Increases attack speed against mutanters",
                    righteousnessAttribute,
                    0.08f, // 每级增加8%攻击速度
                    0.0f,
                    percentFormatter
                );
            }
        }
    }
}