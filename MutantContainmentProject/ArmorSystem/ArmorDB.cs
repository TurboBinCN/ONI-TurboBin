using System.Collections.Generic;
using Database;

namespace MutantContainmentProject.ArmorSystem
{
    public class ArmorDB
    {
        private static ArmorDB _instance;
        public static ArmorDB Instance
        {
            get
            {
                _instance ??= new ArmorDB();
                return _instance;
            }
        }

        private Dictionary<string, ArmorPiece> armorPieces = new();
        private Dictionary<string, ArmorSet> armorSets = new();

        private ArmorDB()
        {
        }

        // 初始化防具数据
        public void InitializeArmors()
        {
            // 这里可以添加默认的防具和套装
            // 示例：添加一套基础防具
            AddArmorPiece(new ArmorPiece("top_black_suit", "西装", ArmorType.Suit, 0.8f, 1.0f, 1.0f, 1.0f));
            AddArmorPiece(new ArmorPiece("plants_black_suit", "西装裤", ArmorType.Plants, 0.7f, 1.0f, 1.0f, 1.0f));
            AddArmorPiece(new ArmorPiece("GlovesBasicWhite", "基础白色手套", ArmorType.Gloves, 1.0f, 1.0f, 1.0f, 1.0f));
            AddArmorPiece(new ArmorPiece("shoes_black_suit", "皮鞋", ArmorType.Shoes, 1.0f, 1.0f, 1.0f, 1.0f));

            // 添加基础套装
            AddArmorSet(new ArmorSet(
                "outfit_black_suit",
                "西装",
                new string[] { "top_black_suit", "plants_black_suit", "GlovesBasicWhite", "shoes_black_suit" },
                0.9f, 1.0f, 1.0f, 1.0f
            ));

        }

        public void AddArmorPiece(ArmorPiece armorPiece)
        {
            if (!armorPieces.ContainsKey(armorPiece.Id))
            {
                armorPieces.Add(armorPiece.Id, armorPiece);
            }
        }

        public void AddArmorSet(ArmorSet armorSet)
        {
            if (!armorSets.ContainsKey(armorSet.Id))
            {
                armorSets.Add(armorSet.Id, armorSet);
            }
        }

        public ArmorPiece GetArmorPiece(string id)
        {
            armorPieces.TryGetValue(id, out ArmorPiece armorPiece);
            return armorPiece;
        }

        public ArmorSet GetArmorSet(string id)
        {
            armorSets.TryGetValue(id, out ArmorSet armorSet);
            return armorSet;
        }

        public List<ArmorPiece> GetAllArmorPieces()
        {
            return new List<ArmorPiece>(armorPieces.Values);
        }

        public List<ArmorSet> GetAllArmorSets()
        {
            return new List<ArmorSet>(armorSets.Values);
        }

        // 根据服装ID获取对应的防具（直接使用服装ID作为防具ID）
        public ArmorPiece GetArmorByClothingId(string clothingId)
        {
            return GetArmorPiece(clothingId);
        }

        // 根据服装列表获取对应的防具列表
        public List<ArmorPiece> GetArmorsByClothingIds(string[] clothingIds)
        {
            List<ArmorPiece> armors = new List<ArmorPiece>();
            foreach (string clothingId in clothingIds)
            {
                ArmorPiece armor = GetArmorByClothingId(clothingId);
                if (armor != null)
                {
                    armors.Add(armor);
                }
            }
            return armors;
        }

        // 获取服装对应的防具类型
        public ArmorType GetArmorTypeByClothingId(string clothingId)
        {
            ArmorPiece armor = GetArmorByClothingId(clothingId);
            return armor != null ? armor.Type : ArmorType.Suit; // 默认返回Suit类型
        }
    }
}
