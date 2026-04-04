using Database;
using System.Linq;

namespace MutantContainmentProject.ArmorSystem
{
    public class ArmorBlueprintProvider : BlueprintProvider
    {
        public const string MUTANT_CONTAINMENT_DLC_ID = "MUTANT_CONTAINMENT_DLC_ID";
        public override void SetupBlueprints()
        {
            // 注册防具相关的蓝图
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
                    // 根据防具类型映射到BlueprintProvider.ClothingType
                    BlueprintProvider.ClothingType clothingType = MapArmorTypeToClothingType(armorPiece.Type);
                    // 注册防具
                    AddClothing(clothingType, PermitRarity.Decent, armorPiece.Id, armorPiece.Id + "_kanim");
                }
            }

            // 注册所有套装
            foreach (ArmorSet armorSet in db.GetAllArmorSets())
            {
                // 检查该套装是否已经存在于系统中
                if (!IsOutfitAlreadyExists(armorSet.Id))
                {
                    // 注册套装
                    AddOutfit(BlueprintProvider.OutfitType.Clothing, armorSet.Id, armorSet.ArmorPieceIds.ToArray());
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


        private BlueprintProvider.ClothingType MapArmorTypeToClothingType(ArmorType armorType)
        {
            switch (armorType)
            {
                case ArmorType.Suit:
                    return BlueprintProvider.ClothingType.DupeTops;
                case ArmorType.Plants:
                    return BlueprintProvider.ClothingType.DupeBottoms;
                case ArmorType.Gloves:
                    return BlueprintProvider.ClothingType.DupeGloves;
                case ArmorType.Shoes:
                    return BlueprintProvider.ClothingType.DupeShoes;
                default:
                    return BlueprintProvider.ClothingType.DupeTops;
            }
        }

        public override string[] GetRequiredDlcIds()
        {
            return null; // 没有需要的DLC
        }

        public override string[] GetForbiddenDlcIds()
        {
            return null; // 没有禁止的DLC
        }
    }
}



