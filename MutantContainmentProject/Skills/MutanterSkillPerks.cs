using Database;

namespace MutantContainmentProject.Skills
{
    public class MutanterSkillPerks
    {
        //生命值
        public static readonly string IncreaseHitPointsSmall = "IncreaseHitPointsSmall";
        public static readonly string IncreaseHitPointsMedium = "IncreaseHitPointsMedium";
        public static readonly string IncreaseHitPointsLarge = "IncreaseHitPointsLarge";

        //防御
        public static readonly string IncreaseDefenseSmall = "IncreaseDefenseSmall";
        public static readonly string IncreaseDefenseMedium = "IncreaseDefenseMedium";
        public static readonly string IncreaseDefenseLarge = "IncreaseDefenseLarge";

        //自律
        public static readonly string IncreaseDisciplineSmall = "IncreaseDisciplineSmall";
        public static readonly string IncreaseDisciplineMedium = "IncreaseDisciplineMedium";
        public static readonly string IncreaseDisciplineLarge = "IncreaseDisciplineLarge";

        //正义
        public static readonly string IncreaseRighteousnessSmall = "IncreaseRighteousnessSmall";
        public static readonly string IncreaseRighteousnessMedium = "IncreaseRighteousnessMedium";
        public static readonly string IncreaseRighteousnessLarge = "IncreaseRighteousnessLarge";

        //特殊技能
        public static readonly string CanMutanterBeAttacked = "CanMutanterBeAttacked";
        public static readonly string CanSecureMutanter = "CanSecureMutanter";

        public static float ATTRIBUTE_BONUS = 3f;
        public static float HEALTH_BONUS_PER_LEVEL = 10f;
        public static float DEFENSE_BONUS = 1f;
        public static void SkillPerkContain(SkillPerks __instance)
        {
            __instance.Add(new SkillAmountPerk(IncreaseHitPointsSmall, "HitPoints", HEALTH_BONUS_PER_LEVEL * ATTRIBUTE_BONUS, STRINGS.DUPLICANTS.ROLES.BAVERYI.NAME, false));

            __instance.Add(new SkillAmountPerk(IncreaseHitPointsMedium, "HitPoints", HEALTH_BONUS_PER_LEVEL * ATTRIBUTE_BONUS, STRINGS.DUPLICANTS.ROLES.BAVERYII.NAME, false));

            __instance.Add(new SkillAmountPerk(IncreaseHitPointsLarge, "HitPoints", HEALTH_BONUS_PER_LEVEL * ATTRIBUTE_BONUS, STRINGS.DUPLICANTS.ROLES.BAVERYIII.NAME, false));
        }
        public static void SkillDefensePower(SkillPerks __instance)
        {
            __instance.Add(new SkillAttributePerk(IncreaseDefenseSmall, MutanterAttributes.AttributeDefenseID, DEFENSE_BONUS, STRINGS.DUPLICANTS.ROLES.DEFENSEI.NAME, false));

            __instance.Add(new SkillAttributePerk(IncreaseDefenseMedium, MutanterAttributes.AttributeDefenseID, DEFENSE_BONUS, STRINGS.DUPLICANTS.ROLES.DEFENSEII.NAME, false));

            __instance.Add(new SkillAttributePerk(IncreaseDefenseLarge, MutanterAttributes.AttributeDefenseID, DEFENSE_BONUS, STRINGS.DUPLICANTS.ROLES.DEFENSEIII.NAME, false));
        }
        public static void SkillDisciplinePower(SkillPerks __instance)
        {
            __instance.Add(new SkillAttributePerk(IncreaseDisciplineSmall, MutanterAttributes.AttributeDisciplineID, ATTRIBUTE_BONUS, STRINGS.DUPLICANTS.ROLES.DISCIPLINEI.NAME, false));

            __instance.Add(new SkillAttributePerk(IncreaseDisciplineMedium, MutanterAttributes.AttributeDisciplineID, ATTRIBUTE_BONUS, STRINGS.DUPLICANTS.ROLES.DISCIPLINEII.NAME, false));

            __instance.Add(new SkillAttributePerk(IncreaseDisciplineLarge, MutanterAttributes.AttributeDisciplineID, ATTRIBUTE_BONUS, STRINGS.DUPLICANTS.ROLES.DISCIPLINEIII.NAME, false));

            __instance.Add(new SimpleSkillPerk(CanSecureMutanter, STRINGS.UI.ROLES_SCREEN.PERKS.CAN_SECURE_MUTANTER.DESCRIPTION));

        }
        public static void SkillRighteousnessPower(SkillPerks __instance)
        {
            __instance.Add(new SkillAttributePerk(IncreaseRighteousnessSmall, MutanterAttributes.AttributeRighteousnessID, ATTRIBUTE_BONUS, STRINGS.DUPLICANTS.ROLES.RIGHTEOUSNESSI.NAME, false));

            __instance.Add(new SkillAttributePerk(IncreaseRighteousnessMedium, MutanterAttributes.AttributeRighteousnessID, ATTRIBUTE_BONUS, STRINGS.DUPLICANTS.ROLES.RIGHTEOUSNESSII.NAME, false));

            __instance.Add(new SkillAttributePerk(IncreaseRighteousnessLarge, MutanterAttributes.AttributeRighteousnessID, ATTRIBUTE_BONUS, STRINGS.DUPLICANTS.ROLES.RIGHTEOUSNESSIII.NAME, false));

            __instance.Add(new SimpleSkillPerk(CanMutanterBeAttacked, STRINGS.UI.ROLES_SCREEN.PERKS.CAN_MUTANTER_BE_ATTACKED.DESCRIPTION));

        }
    }
}
