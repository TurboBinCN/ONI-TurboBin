using Database;
using System.Collections.Generic;
using System.Linq;

namespace MutantContainmentProject.ArmorSystem
{
    public class ArmorBlueprintProvider : BlueprintProvider
    {
        public const string MUTANT_CONTAINMENT_DLC_ID = "MUTANT_CONTAINMENT_DLC_ID";
        private static List<string> armorPieceIds = new();
        public static List<string> ArmorPieceIds => armorPieceIds;
        public override void SetupBlueprints()
        {
            RegisterArmors();
        }

        private void RegisterArmors()
        {
            // 从ArmorDB读取防具数据并注册到蓝图系统
            RegisterArmorsFromDB(ArmorDB.Instance);
        }

        private void RegisterArmorsFromDB(ArmorDB db)
        {
            // 注册所有防具
            foreach (ArmorPiece armorPiece in db.GetAllArmorPieces())
            {
                // 检查该防具是否已经存在于系统中
                if (!IsClothingAlreadyExists(armorPiece.Id))
                {
                    ClothingType clothingType = MapArmorTypeToClothingType(armorPiece.Type);
                    AddClothing(clothingType, PermitRarity.Decent, armorPiece.Id, armorPiece.Id + "_kanim");
                    armorPieceIds.Add(armorPiece.Id);
                }
            }

            // 注册所有套装
            foreach (ArmorSet armorSet in db.GetAllArmorSets())
            {
                // 检查该套装是否已经存在于系统中
                if (!IsOutfitAlreadyExists(armorSet.Id))
                {
                    // 注册套装
                    AddOutfit(OutfitType.Clothing, armorSet.Id, armorSet.ArmorPieceIds.ToArray());
                }
            }
        }

        // 检查服装是否已经存在于系统中
        private bool IsClothingAlreadyExists(string clothingId)
        {
            return blueprintCollection.clothingItems.Any(item => item.id == clothingId);
        }

        // 检查套装是否已经存在于系统中
        private bool IsOutfitAlreadyExists(string outfitId)
        {
            return blueprintCollection.outfits.Any(outfit => outfit.Id == outfitId);
        }


        private ClothingType MapArmorTypeToClothingType(ArmorType armorType)
        {
            switch (armorType)
            {
                case ArmorType.Suit:
                    return ClothingType.DupeTops;
                case ArmorType.Plants:
                    return ClothingType.DupeBottoms;
                case ArmorType.Gloves:
                    return ClothingType.DupeGloves;
                case ArmorType.Shoes:
                    return ClothingType.DupeShoes;
                default:
                    return ClothingType.DupeTops;
            }
        }

        public override string[] GetRequiredDlcIds()
        {
            return new string[] { MUTANT_CONTAINMENT_DLC_ID };
        }

        public override string[] GetForbiddenDlcIds()
        {
            return null;
        }
    }
}



