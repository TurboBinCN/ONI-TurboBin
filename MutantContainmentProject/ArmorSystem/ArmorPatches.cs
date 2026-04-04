using Database;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MutantContainmentProject.ArmorSystem
{
    // 监听服装切换事件的补丁
    [HarmonyPatch(typeof(WearableAccessorizer), "ApplyClothingItems")]
    public class WearableAccessorizerApplyClothingItemsPatch
    {
        public static void Postfix(WearableAccessorizer __instance, object outfitType, System.Collections.Generic.IEnumerable<Database.ClothingItemResource> items)
        {
            // 当小人切换服装时，更新对应的防具
            MinionIdentity minionIdentity = __instance.GetComponent<MinionIdentity>();
            if (minionIdentity != null && !minionIdentity.IsNullOrDestroyed())
            {
                Tag dupeTag = minionIdentity.GetComponent<KPrefabID>().PrefabID();
                ArmorManager.Instance.UpdateDupeArmorFromClothing(__instance.gameObject, dupeTag);
            }
        }
    }

    // 监听小人创建事件的补丁
    [HarmonyPatch(typeof(MinionIdentity), "OnSpawn")]
    public class MinionIdentityOnSpawnPatch
    {
        public static void Postfix(MinionIdentity __instance)
        {
            // 当小人创建时，初始化其防具
            if (__instance != null && !__instance.IsNullOrDestroyed())
            {
                Tag dupeTag = __instance.GetComponent<KPrefabID>().PrefabID();
                ArmorManager.Instance.UpdateDupeArmorFromClothing(__instance.gameObject, dupeTag);
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

    // 使用Harmony补丁修改DlcManager.IsDlcId方法
    [HarmonyPatch(typeof(DlcManager), "IsDlcId")]
    public class DlcManagerIsDlcIdPatch
    {
        public static void Postfix(string dlcId, ref bool __result)
        {
            // 检查是否是我们的自定义DLC ID
            if (dlcId == ArmorBlueprintProvider.MUTANT_CONTAINMENT_DLC_ID)
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
            if (dlcId == ArmorBlueprintProvider.MUTANT_CONTAINMENT_DLC_ID)
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
            if (dlcId == ArmorBlueprintProvider.MUTANT_CONTAINMENT_DLC_ID)
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
            if (dlcId == ArmorBlueprintProvider.MUTANT_CONTAINMENT_DLC_ID)
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
            if (dlcId == ArmorBlueprintProvider.MUTANT_CONTAINMENT_DLC_ID)
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
            if (dlcId == ArmorBlueprintProvider.MUTANT_CONTAINMENT_DLC_ID)
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
            if (dlcId == ArmorBlueprintProvider.MUTANT_CONTAINMENT_DLC_ID)
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

