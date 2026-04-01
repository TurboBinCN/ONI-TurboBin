using Database;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MutantContainmentProject.Suits
{
    public class BlackSuitBlueprintProvider : BlueprintProvider
    {
        // 自定义DLC ID
        public const string MUTANT_CONTAINMENT_DLC_ID = "MUTANT_CONTAINMENT_DLC_ID";

        public override void SetupBlueprints()
        {
            // 检查是否已经添加过，避免重复添加
            if (!IsClothingAlreadyAdded("top_black_suit"))
            {
                // 添加black_suit的服装项目 - 参考velour_black风格
                AddClothing(ClothingType.DupeTops, PermitRarity.Decent, "top_black_suit", "top_black_suit_kanim");
            }
            if (!IsClothingAlreadyAdded("shoes_black_suit"))
            {
                AddClothing(ClothingType.DupeShoes, PermitRarity.Decent, "shoes_black_suit", "shoes_black_suit_kanim");
            }
            if (!IsClothingAlreadyAdded("plants_black_suit"))
            {
                AddClothing(ClothingType.DupeBottoms, PermitRarity.Decent, "plants_black_suit", "plants_black_suit_kanim");
            }
            if (!IsOutfitAlreadyAdded("outfit_black_suit"))
            {
                // 添加black_suit套装 - 参考velour_black风格
                AddOutfit(OutfitType.Clothing, "outfit_black_suit", new string[] { "top_black_suit", "plants_black_suit", "shoes_black_suit", "GlovesBasicWhite" });
            }
        }

        // 检查服装是否已经添加
        private bool IsClothingAlreadyAdded(string permitId)
        {
            return blueprintCollection.clothingItems.Any(item => item.id == permitId);
        }

        // 检查套装是否已经添加
        private bool IsOutfitAlreadyAdded(string outfitId)
        {
            return blueprintCollection.outfits.Any(outfit => outfit.Id == outfitId);
        }

        public override string[] GetRequiredDlcIds()
        {
            // 返回自定义DLC ID来显示斜纹效果
            return new string[] { MUTANT_CONTAINMENT_DLC_ID };
        }

        public override string[] GetForbiddenDlcIds()
        {
            return null; // 没有禁止的DLC
        }

    }

    // 使用Harmony补丁在游戏启动时添加black_suit服装
    [HarmonyPatch(typeof(Blueprints), "Get")]
    public class BlueprintsGetPatch
    {
        public static void Postfix(Blueprints __result)
        {
            // 检查是否已经添加过black_suit相关蓝图
            bool hasBlackSuit = __result.all.clothingItems.Any(item =>
                item.id == "top_black_suit" || item.id == "shoes_black_suit" || item.id == "plants_black_suit");

            if (!hasBlackSuit)
            {
                // 创建并添加BlackSuitBlueprintProvider
                var provider = new BlackSuitBlueprintProvider();
                __result.all.AddBlueprintsFrom(provider);
            }
        }
    }

    // 使用Harmony补丁添加收容所工作服子分类
    [HarmonyPatch(typeof(InventoryOrganization), "GenerateTopLevelCategories")]
    public class InventoryOrganizationGenerateTopLevelCategoriesPatch
    {
        public static void Postfix()
        {
            // 添加收容所工作服子分类到上衣分类
            var categoryIdToSubcategoryIdsMap = typeof(InventoryOrganization).GetField("categoryIdToSubcategoryIdsMap", BindingFlags.Static | BindingFlags.NonPublic);
            if (categoryIdToSubcategoryIdsMap != null)
            {
                var map = categoryIdToSubcategoryIdsMap.GetValue(null) as Dictionary<string, List<string>>;
                if (map != null && map.ContainsKey("CLOTHING_TOPS"))
                {
                    if (!map["CLOTHING_TOPS"].Contains("CLOTHING_TOPS_CONTAINMENT_SUIT"))
                    {
                        map["CLOTHING_TOPS"].Add("CLOTHING_TOPS_CONTAINMENT_SUIT");
                    }
                }
            }
        }
    }

    // 使用Harmony补丁为收容所工作服子分类添加服装项目
    [HarmonyPatch(typeof(InventoryOrganization), "GenerateSubcategories")]
    public class InventoryOrganizationGenerateSubcategoriesPatch
    {
        public static void Postfix()
        {
            // 添加收容所工作服子分类
            var addSubcategoryMethod = typeof(InventoryOrganization).GetMethod("AddSubcategory", BindingFlags.Static | BindingFlags.NonPublic);
            if (addSubcategoryMethod != null)
            {
                // 为收容所工作服子分类添加black_suit服装
                object[] parameters = new object[] {
                    "CLOTHING_TOPS_CONTAINMENT_SUIT",
                    Assets.GetSprite((HashedString) "icon_inventory_tops"),
                    600, // 排序键
                    new string[] { "top_black_suit", "GlovesBasicWhite", "shoes_black_suit", "plants_black_suit" }
                };
                addSubcategoryMethod.Invoke(null, parameters);
            }
        }
    }

    // 使用Harmony补丁修改子分类名称显示
    [HarmonyPatch(typeof(InventoryOrganization), "GetSubcategoryName")]
    public class InventoryOrganizationGetSubcategoryNamePatch
    {
        public static void Postfix(string subcategoryId, ref string __result)
        {
            // 检查是否是我们的收容所工作服子分类
            if (subcategoryId == "CLOTHING_TOPS_CONTAINMENT_SUIT")
            {
                // 返回收容所工作服作为子分类名称
                __result = STRINGS.BLUEPRINTS.CATEGORY.CONTAINMENT_SUIT.NAME;
            }
        }
    }



    // 使用Harmony补丁修改DlcManager.IsDlcId方法
    [HarmonyPatch(typeof(DlcManager), "IsDlcId")]
    public class DlcManagerIsDlcIdPatch
    {
        public static void Postfix(string dlcId, ref bool __result)
        {
            // 检查是否是我们的自定义DLC ID
            if (dlcId == BlackSuitBlueprintProvider.MUTANT_CONTAINMENT_DLC_ID)
            {
                __result = true;
            }
        }
    }

    // 使用Harmony补丁修改DlcManager.GetDlcBannerSprite方法
    [HarmonyPatch(typeof(DlcManager), "GetDlcBannerSprite")]
    public class DlcManagerGetDlcBannerSpritePatch
    {
        public static void Postfix(string dlcId, ref string __result)
        {
            // 为我们的自定义DLC ID返回正确的精灵
            if (dlcId == BlackSuitBlueprintProvider.MUTANT_CONTAINMENT_DLC_ID)
            {
                __result = "cosmetics_banner";
            }
        }
    }

    // 使用Harmony补丁修改DlcManager.GetDlcBannerColor方法
    [HarmonyPatch(typeof(DlcManager), "GetDlcBannerColor")]
    public class DlcManagerGetDlcBannerColorPatch
    {
        public static void Postfix(string dlcId, ref Color __result)
        {
            // 为我们的自定义DLC ID返回正确的颜色
            if (dlcId == BlackSuitBlueprintProvider.MUTANT_CONTAINMENT_DLC_ID)
            {
                __result = new Color(0.8f, 0.2f, 0.2f); // 红色斜纹
            }
        }
    }

    // 使用Harmony补丁修改DlcManager.GetDlcTitle方法
    [HarmonyPatch(typeof(DlcManager), "GetDlcTitle")]
    public class DlcManagerGetDlcTitlePatch
    {
        public static void Postfix(string dlcId, ref string __result)
        {
            // 为我们的自定义DLC ID返回正确的标题
            if (dlcId == BlackSuitBlueprintProvider.MUTANT_CONTAINMENT_DLC_ID)
            {
                __result = $"<i><color=#{new Color(0.8f, 0.2f, 0.2f).ToHexString()}>{STRINGS.BLUEPRINTS.CATEGORY.CONTAINMENT_SUIT.NAME}</color></i>";
            }
        }
    }

    // 使用Harmony补丁修改DlcManager.GetDlcBanner方法
    [HarmonyPatch(typeof(DlcManager), "GetDlcBanner")]
    public class DlcManagerGetDlcBannerPatch
    {
        public static void Postfix(string dlcId, ref string __result)
        {
            // 为我们的自定义DLC ID返回正确的banner
            if (dlcId == BlackSuitBlueprintProvider.MUTANT_CONTAINMENT_DLC_ID)
            {
                __result = "cosmetics_banner";
            }
        }
    }

    // 使用Harmony补丁修改DlcManager.CheckPlatformSubscription方法
    [HarmonyPatch(typeof(DlcManager), "CheckPlatformSubscription")]
    public class DlcManagerCheckPlatformSubscriptionPatch
    {
        public static void Postfix(string dlcId, ref bool __result)
        {
            // 让我们的自定义DLC ID始终被视为已订阅
            if (dlcId == BlackSuitBlueprintProvider.MUTANT_CONTAINMENT_DLC_ID)
            {
                __result = true;
            }
        }
    }

    // 使用Harmony补丁修改DlcManager.CheckForDLCFileInstallation方法
    [HarmonyPatch(typeof(DlcManager), "CheckForDLCFileInstallation")]
    public class DlcManagerCheckForDLCFileInstallationPatch
    {
        public static void Postfix(string dlcId, ref bool __result)
        {
            // 让我们的自定义DLC ID始终被视为已安装
            if (dlcId == BlackSuitBlueprintProvider.MUTANT_CONTAINMENT_DLC_ID)
            {
                __result = true;
            }
        }
    }

    // 使用Harmony补丁修改PermitItems.IsPermitUnlocked方法，让我们的自定义服装默认解锁
    [HarmonyPatch(typeof(PermitItems), "IsPermitUnlocked")]
    public class PermitItemsIsPermitUnlockedPatch
    {
        public static void Postfix(PermitResource permit, ref bool __result)
        {
            // 检查是否是我们的自定义服装
            if (permit != null && (permit.Id == "top_black_suit" || permit.Id == "shoes_black_suit" || permit.Id == "plants_black_suit"))
            {
                // 默认解锁我们的自定义服装
                __result = true;
                //TODO 使用Game.Instance.unlock 来根据进程解锁
            }
        }
    }

    // 使用Harmony补丁修改PermitItems.GetOwnedCount方法，让我们的自定义服装返回拥有数量为1
    [HarmonyPatch(typeof(PermitItems), "GetOwnedCount")]
    public class PermitItemsGetOwnedCountPatch
    {
        public static void Postfix(PermitResource permit, ref int __result)
        {
            // 检查是否是我们的自定义服装
            if (permit != null && (permit.Id == "top_black_suit" || permit.Id == "shoes_black_suit" || permit.Id == "plants_black_suit"))
            {
                // 让我们的自定义服装返回拥有数量为1
                __result = 1;
            }
        }
    }
}