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

        //攻击速度
        public static readonly string IncreaseAttackSpeedSmall = "IncreaseAttackSpeedSmall";
        public static readonly string IncreaseAttackSpeedMedium = "IncreaseAttackSpeedMedium";
        public static readonly string IncreaseAttackSpeedLarge = "IncreaseAttackSpeedLarge";
        //移动速度
        public static readonly string IncreaseMovingSpeedSmall = "IncreaseMovingSpeedSmall";
        public static readonly string IncreaseMovingSpeedMedium = "IncreaseMovingSpeedMedium";
        public static readonly string IncreaseMovingSpeedLarge = "IncreaseMovingSpeedLarge";
        public static void SkillPerkContain(SkillPerks __instance)
        {
            __instance.Add(new SkillAttributePerk(IncreaseContainSpeedSmall, MutanterAttributes.AttributeBraveryID, (float)TUNING.ROLES.ATTRIBUTE_BONUS_FIRST, STRINGS.DUPLICANTS.ROLES.BAVERYI.NAME, false));

            __instance.Add(new SkillAttributePerk(IncreaseContainSpeedMedium, MutanterAttributes.AttributeBraveryID, (float)TUNING.ROLES.ATTRIBUTE_BONUS_SECOND, STRINGS.DUPLICANTS.ROLES.BAVERYII.NAME, false));

            __instance.Add(new SkillAttributePerk(IncreaseContainSpeedLarge, MutanterAttributes.AttributeBraveryID, (float)TUNING.ROLES.ATTRIBUTE_BONUS_THIRD, STRINGS.DUPLICANTS.ROLES.BAVERYIII.NAME, false));
        }
        public static void SkillPerkWillPower(SkillPerks __instance)
        {
            __instance.Add(new SkillAttributePerk(IncreaseWillPowerSmall, MutanterAttributes.AttributeWillPowerID, (float)TUNING.ROLES.ATTRIBUTE_BONUS_FIRST, STRINGS.DUPLICANTS.ROLES.WILLPOWERI.NAME, false));

            __instance.Add(new SkillAttributePerk(IncreaseWillPowerMedium, MutanterAttributes.AttributeWillPowerID, (float)TUNING.ROLES.ATTRIBUTE_BONUS_SECOND, STRINGS.DUPLICANTS.ROLES.WILLPOWERII.NAME, false));

            __instance.Add(new SkillAttributePerk(IncreaseWillPowerLarge, MutanterAttributes.AttributeWillPowerID, (float)TUNING.ROLES.ATTRIBUTE_BONUS_THIRD, STRINGS.DUPLICANTS.ROLES.WILLPOWERIII.NAME, false));
        }
        public static void SkillSuccessRatePower(SkillPerks __instance)
        {
            __instance.Add(new SkillAttributePerk(IncreaseSuccessRateLow, MutanterAttributes.AttributeSuccessRateID, (float)TUNING.ROLES.ATTRIBUTE_BONUS_FIRST, STRINGS.DUPLICANTS.ROLES.SUCCESSRATEI.NAME, false));

            __instance.Add(new SkillAttributePerk(IncreaseSuccessRateMedium, MutanterAttributes.AttributeSuccessRateID, (float)TUNING.ROLES.ATTRIBUTE_BONUS_SECOND, STRINGS.DUPLICANTS.ROLES.SUCCESSRATEII.NAME, false));

            __instance.Add(new SkillAttributePerk(IncreaseSuccessRateHigh, MutanterAttributes.AttributeSuccessRateID, (float)TUNING.ROLES.ATTRIBUTE_BONUS_THIRD, STRINGS.DUPLICANTS.ROLES.SUCCESSRATEIII.NAME, false));
        }
        public static void SkillWorkingSpeedPower(SkillPerks __instance)
        {
            __instance.Add(new SkillAttributePerk(IncreaseWorkingSpeedSmall, MutanterAttributes.AttributeWorkingSpeedID, (float)TUNING.ROLES.ATTRIBUTE_BONUS_FIRST, STRINGS.DUPLICANTS.ROLES.WORKINGSPEEDI.NAME, false));

            __instance.Add(new SkillAttributePerk(IncreaseWorkingSpeedMedium, MutanterAttributes.AttributeWorkingSpeedID, (float)TUNING.ROLES.ATTRIBUTE_BONUS_SECOND, STRINGS.DUPLICANTS.ROLES.WORKINGSPEEDII.NAME, false));

            __instance.Add(new SkillAttributePerk(IncreaseWorkingSpeedLarge, MutanterAttributes.AttributeWorkingSpeedID, (float)TUNING.ROLES.ATTRIBUTE_BONUS_THIRD, STRINGS.DUPLICANTS.ROLES.WORKINGSPEEDIII.NAME, false));
        }
        public static void SkillAttackSpeedPower(SkillPerks __instance)
        {
            __instance.Add(new SkillAttributePerk(IncreaseAttackSpeedSmall, MutanterAttributes.AttributeAttackSpeedID, (float)TUNING.ROLES.ATTRIBUTE_BONUS_FIRST, STRINGS.DUPLICANTS.ROLES.ATTACKPEEDI.NAME, false));

            __instance.Add(new SkillAttributePerk(IncreaseAttackSpeedMedium, MutanterAttributes.AttributeAttackSpeedID, (float)TUNING.ROLES.ATTRIBUTE_BONUS_SECOND, STRINGS.DUPLICANTS.ROLES.ATTACKPEEDII.NAME, false));

            __instance.Add(new SkillAttributePerk(IncreaseAttackSpeedLarge, MutanterAttributes.AttributeAttackSpeedID, (float)TUNING.ROLES.ATTRIBUTE_BONUS_THIRD, STRINGS.DUPLICANTS.ROLES.ATTACKPEEDIII.NAME, false));
        }
        public static void SkillMovingSpeedPower(SkillPerks __instance)
        {
            __instance.Add(new SkillAttributePerk(IncreaseMovingSpeedSmall, MutanterAttributes.AttributeMovingSpeedID, (float)TUNING.ROLES.ATTRIBUTE_BONUS_FIRST, STRINGS.DUPLICANTS.ROLES.MOVINGSPEEDI.NAME, false));

            __instance.Add(new SkillAttributePerk(IncreaseMovingSpeedMedium, MutanterAttributes.AttributeMovingSpeedID, (float)TUNING.ROLES.ATTRIBUTE_BONUS_SECOND, STRINGS.DUPLICANTS.ROLES.MOVINGSPEEDII.NAME, false));

            __instance.Add(new SkillAttributePerk(IncreaseMovingSpeedLarge, MutanterAttributes.AttributeMovingSpeedID, (float)TUNING.ROLES.ATTRIBUTE_BONUS_THIRD, STRINGS.DUPLICANTS.ROLES.MOVINGSPEEDIII.NAME, false));
        }
    }
}
