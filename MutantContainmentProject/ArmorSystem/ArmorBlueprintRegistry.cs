using Database;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using TBB.He.TbbLib.Debuger;

namespace MutantContainmentProject.ArmorSystem
{
    [HarmonyPatch(typeof(Blueprints), "Get")]
    public class ArmorBlueprintRegistry
    {
        private static bool Initialized = false;
        public static void Postfix(Blueprints __result)
        {
            if (!Initialized)
            {
                TbbDebuger.LogDebug("注册防具");
                // 初始化ArmorDB
                ArmorDB db = ArmorDB.Instance;
                db.InitializeArmors();

                // 注册ArmorBlueprintProvider
                var provider = new ArmorBlueprintProvider();
                __result.all.AddBlueprintsFrom(provider);

                SettingArmors(ArmorBlueprintProvider.ArmorPieceIds);

                Initialized = true;
            }
        }

        // 将所有防具添加到收容所工作服子分类
        private static void SettingArmors(List<string> armorPieceIds)
        {
            // 添加到收容所工作服子分类
            AddToContainmentSubcategory(armorPieceIds);
            //设置数量
        }

        // 添加防具到收容所工作服子分类
        private static void AddToContainmentSubcategory(List<string> armorPieceIds)
        {
            // 获取AddSubcategory方法
            var addSubcategoryMethod = typeof(InventoryOrganization).GetMethod("AddSubcategory", BindingFlags.Static | BindingFlags.NonPublic);
            if (addSubcategoryMethod != null)
            {
                // 为收容所工作服子分类添加防具
                object[] parameters = new object[] {
                    "CLOTHING_TOPS_CONTAINMENT_SUIT",
                    Assets.GetSprite((HashedString) "icon_inventory_tops"),
                    600, // 排序键
                    armorPieceIds.ToArray()
                };
                addSubcategoryMethod.Invoke(null, parameters);
            }
        }
        [HarmonyPatch(typeof(PermitItems), "GetOwnedCount")]
        public class PermitItemsGetOwnedCountPatch
        {
            public static void Postfix(PermitResource permit, ref int __result)
            {
                if (permit != null && ArmorBlueprintProvider.ArmorPieceIds.Contains(permit.Id))
                {
                    __result = 1;
                }
            }
        }
    }
}
