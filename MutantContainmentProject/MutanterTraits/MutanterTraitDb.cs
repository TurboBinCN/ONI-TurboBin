using Klei.AI;

namespace MutantContainmentProject.MutanterTraits
{
    /**
     * 3. 特性系统 (MutanterTraits)
        功能: 定义每个畸变体的独特属性，是行为多样性的根本来源。
        特性类型:
        被动特性: 如“光敏”（遇光理智下降）、“群体感应”（附近同类越多越强）、“再生”、“免疫物理伤害”。
        主动特性: 如“精神污染”（降低附近员工理智）、“变形”（改变外观或能力）、“召唤”（召唤幻影或实体）。
        触发特性: 在特定条件下激活，如“濒死狂暴”（生命值低于X%时攻击大幅提升）、“恐惧之源”（附近员工恐慌时获得增益）。
        作用: MutanterTraits 的输出直接影响 EmotionMonitor 的计算、MutanterStateMachine 的行为选项以及 AttackStates 的具体表现。
     */
    public class MutanterTraitDb
    {
        private static readonly string MutanterTraitsGroupID = "MutanterTraitsGroup";
        public static readonly string MutanterPsychological = "Psychological";
        public static readonly string MutanterPhysical = "physical";

        public static void PsychologicalTrait(){
            TraitUtil.CreateNamedTrait(MutanterPsychological, STRINGS.TRAITS.PSYCHOLOGICAL.NAME, STRINGS.TRAITS.PSYCHOLOGICAL.DESC, false);
        }
        public static void PhysicalTrait()
        {
            TraitUtil.CreateNamedTrait(MutanterPhysical, STRINGS.TRAITS.PSYCHOLOGICAL.NAME, STRINGS.TRAITS.PSYCHOLOGICAL.DESC, false);
        }
        /*
         *             var lightSensitive = new MutanterTraitDef("LIGHT_SENSITIVE", "光敏", "暴露在光线下时理智持续下降。", TraitType.Passive);

            // Example: Create "Regeneration" trait
            var regeneration = new MutanterTraitDef("REGENERATION", "再生", "每秒恢复少量生命值。", TraitType.Passive);

            // Example: Create "Berserker" trait (Triggered)
            var berserker = new MutanterTraitDef("BERSERKER", "濒死狂暴", "生命值低于30%时，攻击力大幅增加。", TraitType.Triggered);

            // Example: Create "Mind Polluter" trait (Passive)
            var mindPolluter = new MutanterTraitDef("MIND_POLLUTER", "精神污染", "持续降低附近员工的理智。", TraitType.Passive);

            // Example: Create "Damage Immunity" trait (Passive)
            var damageImmune = new MutanterTraitDef("DAMAGE_IMMUNITY", "免疫物理伤害", "完全免疫物理伤害。", TraitType.Passive);
         */
    }
}
