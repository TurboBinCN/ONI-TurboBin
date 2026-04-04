using KSerialization;
using System.Collections.Generic;
using UnityEngine;

namespace MutantContainmentProject.ArmorSystem
{
    public class ArmorManager : KMonoBehaviour
    {
        private static ArmorManager _instance;
        [Serialize]
        private Dictionary<Tag, Dictionary<ArmorType, string>> dupeArmorEquipments = new Dictionary<Tag, Dictionary<ArmorType, string>>();

        public static ArmorManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<ArmorManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ArmorManager");
                        _instance = go.AddComponent<ArmorManager>();
                    }
                }
                return _instance;
            }
        }

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            _instance = this;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            // 初始化时找到所有小人穿着的蓝图服装
            InitializeDupeArmors();
        }

        // 初始化所有小人的防具
        private void InitializeDupeArmors()
        {
            // 找到所有小人
            foreach (MinionIdentity minion in FindObjectsOfType<MinionIdentity>())
            {
                if (minion != null && !minion.IsNullOrDestroyed())
                {
                    Tag dupeTag = minion.GetComponent<KPrefabID>().PrefabID();
                    UpdateDupeArmorFromClothing(minion.gameObject, dupeTag);
                }
            }
        }

        // 根据小人的服装更新防具
        public void UpdateDupeArmorFromClothing(GameObject minionInstance, Tag dupeTag)
        {
            WearableAccessorizer accessorizer = minionInstance.GetComponent<WearableAccessorizer>();
            if (accessorizer == null)
                return;

            // 获取小人穿着的服装
            string[] clothingIds = accessorizer.GetClothingItemsIds(ClothingOutfitUtility.OutfitType.Clothing);
            if (clothingIds == null || clothingIds.Length == 0)
                return;

            // 清空当前防具
            if (dupeArmorEquipments.ContainsKey(dupeTag))
            {
                dupeArmorEquipments[dupeTag].Clear();
            }
            else
            {
                dupeArmorEquipments[dupeTag] = new Dictionary<ArmorType, string>();
            }

            // 根据服装映射到对应的防具
            foreach (string clothingId in clothingIds)
            {
                ArmorPiece armor = ArmorDB.Instance.GetArmorByClothingId(clothingId);
                if (armor != null)
                {
                    dupeArmorEquipments[dupeTag][armor.Type] = armor.Id;
                }
            }
        }

        // 装备防具
        public void EquipArmor(Tag dupeTag, ArmorType armorType, string armorPieceId)
        {
            if (!dupeArmorEquipments.ContainsKey(dupeTag))
            {
                dupeArmorEquipments[dupeTag] = new Dictionary<ArmorType, string>();
            }
            dupeArmorEquipments[dupeTag][armorType] = armorPieceId;
        }

        // 卸下防具
        public void UnequipArmor(Tag dupeTag, ArmorType armorType)
        {
            if (dupeArmorEquipments.ContainsKey(dupeTag) && dupeArmorEquipments[dupeTag].ContainsKey(armorType))
            {
                dupeArmorEquipments[dupeTag].Remove(armorType);
            }
        }

        // 获取小人装备的防具
        public Dictionary<ArmorType, string> GetEquippedArmors(Tag dupeTag)
        {
            if (dupeArmorEquipments.TryGetValue(dupeTag, out Dictionary<ArmorType, string> armors))
            {
                return armors;
            }
            return new Dictionary<ArmorType, string>();
        }

        // 计算小人的综合抗性
        public ArmorResistance CalculateTotalResistance(Tag dupeTag)
        {
            Dictionary<ArmorType, string> equippedArmors = GetEquippedArmors(dupeTag);
            float physicalResistance = 1.0f;
            float psychologicalResistance = 1.0f;
            float erosionResistance = 1.0f;
            float soulResistance = 1.0f;

            // 计算单件防具的抗性
            foreach (var armorEntry in equippedArmors)
            {
                string armorPieceId = armorEntry.Value;
                ArmorPiece armorPiece = ArmorDB.Instance.GetArmorPiece(armorPieceId);
                if (armorPiece != null)
                {
                    physicalResistance *= armorPiece.PhysicalResistance;
                    psychologicalResistance *= armorPiece.PsychologicalResistance;
                    erosionResistance *= armorPiece.ErosionResistance;
                    soulResistance *= armorPiece.SoulResistance;
                }
            }

            // 检查是否装备了完整套装，应用套装加成
            foreach (ArmorSet armorSet in ArmorDB.Instance.GetAllArmorSets())
            {
                if (armorSet.IsFullSetEquipped(equippedArmors))
                {
                    physicalResistance *= armorSet.PhysicalResistanceBonus;
                    psychologicalResistance *= armorSet.PsychologicalResistanceBonus;
                    erosionResistance *= armorSet.ErosionResistanceBonus;
                    soulResistance *= armorSet.SoulResistanceBonus;
                    break; // 只应用一个套装的加成
                }
            }

            return new ArmorResistance(physicalResistance, psychologicalResistance, erosionResistance, soulResistance);
        }

        // 计算伤害减免
        public float CalculateDamageReduction(Tag dupeTag, string damageType, float baseDamage, int attackerLevel, int defenderLevel)
        {
            ArmorResistance resistance = CalculateTotalResistance(dupeTag);
            float resistanceValue = 1.0f;

            // 根据伤害类型选择对应的抗性
            switch (damageType)
            {
                case "PhysicalAttack":
                    resistanceValue = resistance.PhysicalResistance;
                    break;
                case "PsychologicalAttack":
                    resistanceValue = resistance.PsychologicalResistance;
                    break;
                case "ErosionAttack":
                    resistanceValue = resistance.ErosionResistance;
                    break;
                case "SoulAttack":
                    resistanceValue = resistance.SoulResistance;
                    break;
            }

            // 应用脑叶公司的增伤-减伤公式
            int levelDifference = defenderLevel - attackerLevel;
            float levelMultiplier = GetLevelMultiplier(levelDifference);

            // 计算最终伤害
            float finalDamage = baseDamage * levelMultiplier * resistanceValue;
            return finalDamage;
        }

        // 根据等级差获取伤害乘数
        private float GetLevelMultiplier(int levelDifference)
        {
            switch (levelDifference)
            {
                case -4:
                    return 2.0f; // 200%
                case -3:
                    return 1.5f; // 150%
                case -2:
                    return 1.2f; // 120%
                case -1:
                    return 1.0f; // 100%
                case 0:
                    return 1.0f; // 100%
                case 1:
                    return 0.8f; // 80%
                case 2:
                    return 0.7f; // 70%
                case 3:
                    return 0.6f; // 60%
                case 4:
                    return 0.4f; // 40%
                default:
                    return levelDifference < -4 ? 2.0f : 0.4f;
            }
        }
    }

    // 抗性结构体
    public struct ArmorResistance
    {
        public float PhysicalResistance { get; set; }
        public float PsychologicalResistance { get; set; }
        public float ErosionResistance { get; set; }
        public float SoulResistance { get; set; }

        public ArmorResistance(float physicalResistance, float psychologicalResistance, float erosionResistance, float soulResistance)
        {
            PhysicalResistance = physicalResistance;
            PsychologicalResistance = psychologicalResistance;
            ErosionResistance = erosionResistance;
            SoulResistance = soulResistance;
        }
    }
}
