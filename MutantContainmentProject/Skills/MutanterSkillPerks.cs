using Database;

namespace MutantContainmentProject.Skills
{
    public class MutanterSkillPerks
    {
        public static readonly string IncreaseContainSpeedSmall = "IncreaseContainSpeedSmall";
        public static readonly string IncreaseContainSpeedMedium = "IncreaseContainSpeedMedium";
        public static readonly string IncreaseContainSpeedLarge = "IncreaseContainSpeedLarge";

        //收容或安全限制操作成功率
        public static readonly string IncreaseSuccessRateLow = "IncreaseSuccessRateLow";
        public static readonly string IncreaseSuccessRateMedium = "IncreaseSuccessRateMedium";
        public static readonly string IncreaseSuccessRateHigh = "IncreaseSuccessRateHigh";
        //工作速度
        public static readonly string IncreaseWorkingSpeedSmall = "IncreaseWorkingSpeedSmall";
        public static readonly string IncreaseWorkingSpeedMedium = "IncreaseWorkingSpeedMedium";
        public static readonly string IncreaseWorkingSpeedLarge = "IncreaseWorkingSpeedLarge";

        //攻击伤害
        public static readonly string IncreaseAttackDamageSmall = "IncreaseAttackDamageSmall";
        public static readonly string IncreaseAttackDamageMedium = "IncreaseAttackDamageMedium";
        public static readonly string IncreaseAttackDamageLarge = "IncreaseAttackDamageLarge";

        //生命值
        public static readonly string IncreaseHitPointsSmall = "IncreaseHitPointsSmall";
        public static readonly string IncreaseHitPointsMedium = "IncreaseHitPointsMedium";
        public static readonly string IncreaseHitPointsLarge = "IncreaseHitPointsLarge";

        //精神抗性
        public static readonly string IncreaseMentalResistanceSmall = "IncreaseMentalResistanceSmall";
        public static readonly string IncreaseMentalResistanceMedium = "IncreaseMentalResistanceMedium";
        public static readonly string IncreaseMentalResistanceLarge = "IncreaseMentalResistanceLarge";

        public static float ATTRIBUTE_BONUS = 3f;
        public static float HEALTH_BONUS_PER_LEVEL = 10f;
        public static float MENTAL_RESISTANCE_BONUS = 1f;
        public static void SkillPerkContain(SkillPerks __instance)
        {
            __instance.Add(new SkillAmountPerk(IncreaseHitPointsSmall, "HitPoints", HEALTH_BONUS_PER_LEVEL * ATTRIBUTE_BONUS, STRINGS.DUPLICANTS.ROLES.BAVERYI.NAME, false));

            __instance.Add(new SkillAmountPerk(IncreaseHitPointsMedium, "HitPoints", HEALTH_BONUS_PER_LEVEL * ATTRIBUTE_BONUS, STRINGS.DUPLICANTS.ROLES.BAVERYII.NAME, false));

            __instance.Add(new SkillAmountPerk(IncreaseHitPointsLarge, "HitPoints", HEALTH_BONUS_PER_LEVEL * ATTRIBUTE_BONUS, STRINGS.DUPLICANTS.ROLES.BAVERYIII.NAME, false));
        }
        public static void SkillSuccessRatePower(SkillPerks __instance)
        {
            __instance.Add(new SkillAttributePerk(IncreaseSuccessRateLow, MutanterAttributes.AttributeSuccessRateID, ATTRIBUTE_BONUS, STRINGS.DUPLICANTS.ROLES.SUCCESSRATEI.NAME, false));

            __instance.Add(new SkillAttributePerk(IncreaseSuccessRateMedium, MutanterAttributes.AttributeSuccessRateID, ATTRIBUTE_BONUS, STRINGS.DUPLICANTS.ROLES.SUCCESSRATEII.NAME, false));

            __instance.Add(new SkillAttributePerk(IncreaseSuccessRateHigh, MutanterAttributes.AttributeSuccessRateID, ATTRIBUTE_BONUS, STRINGS.DUPLICANTS.ROLES.SUCCESSRATEIII.NAME, false));
        }
        public static void SkillWorkingSpeedPower(SkillPerks __instance)
        {
            __instance.Add(new SkillAttributePerk(IncreaseWorkingSpeedSmall, MutanterAttributes.AttributeWorkingSpeedID, ATTRIBUTE_BONUS, STRINGS.DUPLICANTS.ROLES.WORKINGSPEEDI.NAME, false));

            __instance.Add(new SkillAttributePerk(IncreaseWorkingSpeedMedium, MutanterAttributes.AttributeWorkingSpeedID, ATTRIBUTE_BONUS, STRINGS.DUPLICANTS.ROLES.WORKINGSPEEDII.NAME, false));

            __instance.Add(new SkillAttributePerk(IncreaseWorkingSpeedLarge, MutanterAttributes.AttributeWorkingSpeedID, ATTRIBUTE_BONUS, STRINGS.DUPLICANTS.ROLES.WORKINGSPEEDIII.NAME, false));
        }
        public static void SkillAttackDamagePower(SkillPerks __instance)
        {
            __instance.Add(new SkillAttributePerk(IncreaseAttackDamageSmall, MutanterAttributes.AttributeAttackDamageID, ATTRIBUTE_BONUS, STRINGS.DUPLICANTS.ROLES.ATTACKDAMAGEI.NAME, false));

            __instance.Add(new SkillAttributePerk(IncreaseAttackDamageMedium, MutanterAttributes.AttributeAttackDamageID, ATTRIBUTE_BONUS, STRINGS.DUPLICANTS.ROLES.ATTACKDAMAGEII.NAME, false));

            __instance.Add(new SkillAttributePerk(IncreaseAttackDamageLarge, MutanterAttributes.AttributeAttackDamageID, ATTRIBUTE_BONUS, STRINGS.DUPLICANTS.ROLES.ATTACKDAMAGEIII.NAME, false));
        }

        public static void SkillMentalResistancePower(SkillPerks __instance)
        {
            // 使用Caution属性作为精神抗性
            __instance.Add(new SkillAttributePerk(IncreaseMentalResistanceSmall, MutanterAttributes.AttributeMentalResistanceID, MENTAL_RESISTANCE_BONUS, STRINGS.DUPLICANTS.ROLES.MENTALRESISTANCEI.NAME, false));

            __instance.Add(new SkillAttributePerk(IncreaseMentalResistanceMedium, MutanterAttributes.AttributeMentalResistanceID, MENTAL_RESISTANCE_BONUS, STRINGS.DUPLICANTS.ROLES.MENTALRESISTANCEII.NAME, false));

            __instance.Add(new SkillAttributePerk(IncreaseMentalResistanceLarge, MutanterAttributes.AttributeMentalResistanceID, MENTAL_RESISTANCE_BONUS, STRINGS.DUPLICANTS.ROLES.MENTALRESISTANCEIII.NAME, false));
        }
    }
}
