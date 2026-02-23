using Database;

namespace MutantContainmentProject.Skills
{
    public class MutanterSkillPerks
    {
        public static readonly string IncreaseContainSpeedSmall = "IncreaseContainSpeedSmall";
        public static readonly string IncreaseContainSpeedMedium = "IncreaseContainSpeedMedium";
        public static readonly string IncreaseContainSpeedLarge = "IncreaseContainSpeedLarge";

        public static readonly string IncreaseWillPowerSmall = "IncreaseWillPowerSmall";
        public static readonly string IncreaseWillPowerMedium = "IncreaseWillPowerMedium";
        public static readonly string IncreaseWillPowerLarge = "IncreaseCWillPowerLarge";

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

        public static float ATTRIBUTE_BONUS = 3f;
        public static float HEALTH_BONUS_PER_LEVEL = 10f;
        public static void SkillPerkContain(SkillPerks __instance)
        {
            __instance.Add(new SkillAmountPerk(IncreaseHitPointsSmall, "HitPoints", HEALTH_BONUS_PER_LEVEL * ATTRIBUTE_BONUS, STRINGS.DUPLICANTS.ROLES.BAVERYI.NAME, false));

            __instance.Add(new SkillAmountPerk(IncreaseHitPointsMedium, "HitPoints", HEALTH_BONUS_PER_LEVEL * ATTRIBUTE_BONUS, STRINGS.DUPLICANTS.ROLES.BAVERYII.NAME, false));

            __instance.Add(new SkillAmountPerk(IncreaseHitPointsLarge, "HitPoints", HEALTH_BONUS_PER_LEVEL * ATTRIBUTE_BONUS, STRINGS.DUPLICANTS.ROLES.BAVERYIII.NAME, false));
        }
        public static void SkillPerkWillPower(SkillPerks __instance)
        {
            __instance.Add(new SkillAttributePerk(IncreaseWillPowerSmall, MutanterAttributes.AttributeWillPowerID, ATTRIBUTE_BONUS, STRINGS.DUPLICANTS.ROLES.WILLPOWERI.NAME, false));

            __instance.Add(new SkillAttributePerk(IncreaseWillPowerMedium, MutanterAttributes.AttributeWillPowerID, ATTRIBUTE_BONUS, STRINGS.DUPLICANTS.ROLES.WILLPOWERII.NAME, false));

            __instance.Add(new SkillAttributePerk(IncreaseWillPowerLarge, MutanterAttributes.AttributeWillPowerID, ATTRIBUTE_BONUS, STRINGS.DUPLICANTS.ROLES.WILLPOWERIII.NAME, false));
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
    }
}
