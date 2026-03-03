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
    }
}
