namespace MutantContainmentProject.MutanterComponent
{
    public static class MutanterTags
    {
        public static readonly Tag Mutanter = TagManager.Create("Mutanter");
        public static readonly Tag MutanterBuildings = TagManager.Create("MutanterBuildings");
        public static readonly Tag ShouldBeSeCured = TagManager.Create("ShouldBeSeCured");

        public static readonly Tag Incapacitated = TagManager.Create("Incapacitated");
        public static readonly Tag MutanterBrain = TagManager.Create("MutanterBrain");
        //攻击类Tag
        public static readonly Tag PhysicalAttack = TagManager.Create("PhysicalAttack");
        public static readonly Tag PsychologicalAttack = TagManager.Create("PsychologicalAttack");
        public static readonly Tag ErosionAttack = TagManager.Create("ErosionAttack");
        public static readonly Tag SoulAttack = TagManager.Create("SoulAttack");
        public static class Mutanters
        {
            public static class Species
            {
                public static readonly Tag Mutanter = TagManager.Create("Mutanter_Species", STRINGS.CREATURES.FAMILY_PLURAL.MUTANTER_SPECIES);
                public static readonly Tag SCP173 = TagManager.Create("SCP173", STRINGS.CREATURES.FAMILY_PLURAL.MUTANTER_SCP173);
                public static readonly Tag DawnConfusion = TagManager.Create("DawnConfusion", STRINGS.CREATURES.FAMILY_PLURAL.MUTANTER_DAWN_CONFUSION);
                public static readonly Tag BugDusk = TagManager.Create("BugDusk", STRINGS.CREATURES.FAMILY_PLURAL.MUTANTER_BUG_DUSK);
                public static readonly Tag CosmicBug = TagManager.Create("CosmicBug", STRINGS.CREATURES.FAMILY_PLURAL.MUTANTER_COSMIC_BUG);
                public static readonly Tag CircusJoker = TagManager.Create("CircusJoker", STRINGS.CREATURES.FAMILY_PLURAL.MUTANTER_CIRCUS_JOKER);
            }
        }
    }
}
