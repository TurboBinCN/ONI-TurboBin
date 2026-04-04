namespace MutantContainmentProject.ArmorSystem
{
    public enum ArmorType
    {
        Suit,
        Plants,
        Gloves,
        Shoes
    }

    public class ArmorPiece
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public ArmorType Type { get; set; }
        public float PhysicalResistance { get; set; } // 物理抗性
        public float PsychologicalResistance { get; set; } // 精神抗性
        public float ErosionResistance { get; set; } // 侵蚀抗性
        public float SoulResistance { get; set; } // 灵魂抗性
        public string SpecialAbility { get; set; } // 特殊能力

        public ArmorPiece(string id, string name, ArmorType type, float physicalResistance, float psychologicalResistance, float erosionResistance, float soulResistance, string specialAbility = "")
        {
            Id = id;
            Name = name;
            Type = type;
            PhysicalResistance = physicalResistance;
            PsychologicalResistance = psychologicalResistance;
            ErosionResistance = erosionResistance;
            SoulResistance = soulResistance;
            SpecialAbility = specialAbility;
        }
    }
}
