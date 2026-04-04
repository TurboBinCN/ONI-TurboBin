using System.Collections.Generic;

namespace MutantContainmentProject.ArmorSystem
{
    public class ArmorSet
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> ArmorPieceIds { get; set; }
        public float PhysicalResistanceBonus { get; set; } // 物理抗性加成
        public float PsychologicalResistanceBonus { get; set; } // 精神抗性加成
        public float ErosionResistanceBonus { get; set; } // 侵蚀抗性加成
        public float SoulResistanceBonus { get; set; } // 灵魂抗性加成
        public string SetBonusAbility { get; set; } // 套装加成能力

        public ArmorSet(string id, string name, string[] armorPieceIds, float physicalResistanceBonus, float psychologicalResistanceBonus, float erosionResistanceBonus, float soulResistanceBonus, string setBonusAbility = "")
        {
            Id = id;
            Name = name;
            ArmorPieceIds = new List<string>(armorPieceIds);
            PhysicalResistanceBonus = physicalResistanceBonus;
            PsychologicalResistanceBonus = psychologicalResistanceBonus;
            ErosionResistanceBonus = erosionResistanceBonus;
            SoulResistanceBonus = soulResistanceBonus;
            SetBonusAbility = setBonusAbility;
        }

        // 检查是否装备了完整套装
        public bool IsFullSetEquipped(Dictionary<ArmorType, string> equippedArmors)
        {
            foreach (string armorPieceId in ArmorPieceIds)
            {
                ArmorPiece armorPiece = ArmorDB.Instance.GetArmorPiece(armorPieceId);
                if (armorPiece != null && !equippedArmors.ContainsValue(armorPieceId))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
