using Klei.AI;

namespace MutantContainmentProject.MutanterEffect
{
    public class MutanterEffects
    {
        public static readonly string MUTANTER_CONTAINED_EFFECT = "MutanterContained";
        public static readonly string MUTANTER_WILLED_EFFECT = "MutanterWilled";
        public static readonly string MUTANTER_CHASE_EFFECT = "MutanterChase";
        public static readonly string MUTANTER_ATTACK_RESTRICTED_EFFECT = "MutanterAttackRestricted";
        public static readonly string MUTANTER_ATTACK_ENHANCED_EFFECT = "MutanterAttackEnhanced";
        public static readonly string SCP939_AMNESIA_EFFECT = "SCP939Amnesia";
        public static readonly string MUTANTER_CONTROL_SPEED_EFFECT = "MutanterControlSpeed";
        public static readonly string MUTANTER_CONTROL_SUPPRESSION_EFFECT = "MutanterControlSuppression";
        public static readonly string WHITE_MIST_SLOW_EFFECT = "WhiteMistSlow";

        public static readonly float MUTANTER_CTROL_SPEED_BOOST_DURATION = 1800f;

        public static void MutanterContainedEffect()
        {
            Effect mutanterContainedEffect = new(
                id: MUTANTER_CONTAINED_EFFECT,
                name: STRINGS.EFFECTS.MUTANTER_CONTAINED_EFFECT.NAME,
                description: STRINGS.EFFECTS.MUTANTER_CONTAINED_EFFECT.DESCRIPTION,
                duration: 3 * 600f,
                show_in_ui: true,
                trigger_floating_text: true,
                is_bad: false
            );
            Db.Get().effects.Add(mutanterContainedEffect);
        }
        public static void MutanterWilledEffect()
        {
            Effect mutanterWilledEffect = new(
                id: MUTANTER_WILLED_EFFECT,
                name: STRINGS.EFFECTS.MUTANTER_WILLED_EFFECT.NAME,
                description: STRINGS.EFFECTS.MUTANTER_WILLED_EFFECT.DESCRIPTION,
                duration: 10f,
                show_in_ui: true,
                trigger_floating_text: true,
                is_bad: false
            );
            Db.Get().effects.Add(mutanterWilledEffect);
        }
        public static void MutanterChaseEffect()
        {
            Effect mutanterChaseEffect = new(
                id: MUTANTER_CHASE_EFFECT,
                name: STRINGS.EFFECTS.MUTANTER_CHASE_EFFECT.NAME,
                description: STRINGS.EFFECTS.MUTANTER_CHASE_EFFECT.DESCRIPTION,
                duration: 3 * 600f,
                show_in_ui: true,
                trigger_floating_text: true,
                is_bad: true
            );
            Db.Get().effects.Add(mutanterChaseEffect);
        }
        public static void MutanterAttackRestrictedEffect()
        {
            Effect mutanterAttackRestrictedEffect = new(
                id: MUTANTER_ATTACK_RESTRICTED_EFFECT,
                name: STRINGS.EFFECTS.MUTANTER_ATTACK_RESTRICTED_EFFECT.NAME,
                description: STRINGS.EFFECTS.MUTANTER_ATTACK_RESTRICTED_EFFECT.DESCRIPTION,
                duration: 3 * 600f,
                show_in_ui: true,
                trigger_floating_text: true,
                is_bad: true
            );
            Db.Get().effects.Add(mutanterAttackRestrictedEffect);
        }
        public static void MutanterAttackEnhancedEffect()
        {
            Effect mutanterAttackEnhancedEffect = new(
                id: MUTANTER_ATTACK_ENHANCED_EFFECT,
                name: STRINGS.EFFECTS.MUTANTER_ATTACK_ENHANCED_EFFECT.NAME,
                description: STRINGS.EFFECTS.MUTANTER_ATTACK_ENHANCED_EFFECT.DESCRIPTION,
                duration: 60f,
                show_in_ui: true,
                trigger_floating_text: true,
                is_bad: false
            );
            Db.Get().effects.Add(mutanterAttackEnhancedEffect);
        }
        public static void SCP939AmnesiaEffect()
        {
            Effect scp939AmnesiaEffect = new(
                id: SCP939_AMNESIA_EFFECT,
                name: STRINGS.EFFECTS.SCP939_AMNESIA_EFFECT.NAME,
                description: STRINGS.EFFECTS.SCP939_AMNESIA_EFFECT.DESCRIPTION,
                duration: 1800f, // 30分钟
                show_in_ui: true,
                trigger_floating_text: true,
                is_bad: true
            );
            // 添加效果修改器，降低体力恢复效率
            //scp939AmnesiaEffect.Add(new AttributeModifier(Db.Get().Attributes.StaminaDelta.Id, -0.5f, "SCP-939 Amnesia"));
            Db.Get().effects.Add(scp939AmnesiaEffect);
        }
        public static void MutanterControlSpeedEffect()
        {
            Effect mutanterControlSpeedEffect = new(
                id: MUTANTER_CONTROL_SPEED_EFFECT,
                name: STRINGS.EFFECTS.MUTANTER_CONTROL_SPEED_EFFECT.NAME,
                description: STRINGS.EFFECTS.MUTANTER_CONTROL_SPEED_EFFECT.DESCRIPTION,
                duration: MUTANTER_CTROL_SPEED_BOOST_DURATION, // 持续效果
                show_in_ui: true,
                trigger_floating_text: false,
                is_bad: false
            );
            // 添加移动速度修改器
            mutanterControlSpeedEffect.Add(new AttributeModifier(Db.Get().Attributes.Athletics.Id, 1.2f, "Control Station Speed Boost",true));
            Db.Get().effects.Add(mutanterControlSpeedEffect);
        }
        public static void MutanterControlSuppressionEffect()
        {
            Effect mutanterControlSuppressionEffect = new(
                id: MUTANTER_CONTROL_SUPPRESSION_EFFECT,
                name: STRINGS.EFFECTS.MUTANTER_CONTROL_SUPPRESSION_EFFECT.NAME,
                description: STRINGS.EFFECTS.MUTANTER_CONTROL_SUPPRESSION_EFFECT.DESCRIPTION,
                duration: MUTANTER_CTROL_SPEED_BOOST_DURATION, // 与控制站速度效果相同的持续时间
                show_in_ui: true,
                trigger_floating_text: true,
                is_bad: false
            );
            Db.Get().effects.Add(mutanterControlSuppressionEffect);
        }
        public static void WhiteMistSlowEffect()
        {
            Effect whiteMistSlowEffect = new(
                id: WHITE_MIST_SLOW_EFFECT,
                name: STRINGS.EFFECTS.WHITE_MIST_SLOW_EFFECT.NAME,
                description: STRINGS.EFFECTS.WHITE_MIST_SLOW_EFFECT.DESCRIPTION,
                duration: 5f,
                show_in_ui: true,
                trigger_floating_text: true,
                is_bad: true
            );
            // 添加移动速度修改器，降低50%移动速度
            whiteMistSlowEffect.Add(new AttributeModifier(Db.Get().Attributes.Athletics.Id, -0.5f, "White Mist Slow", true));
            Db.Get().effects.Add(whiteMistSlowEffect);
        }
    }
}
