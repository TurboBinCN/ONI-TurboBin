using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

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

                // 将所有防具添加到收容所工作服子分类
                AddArmorsToContainmentSubcategory();

                Initialized = true;
            }
        }

        // 将所有防具添加到收容所工作服子分类
        private static void AddArmorsToContainmentSubcategory()
        {
            // 获取所有防具ID
            List<string> armorIds = new List<string>();
            foreach (ArmorPiece armorPiece in ArmorDB.Instance.GetAllArmorPieces())
            {
                armorIds.Add(armorPiece.Id);
            }

            // 添加到收容所工作服子分类
            AddToContainmentSubcategory(armorIds.ToArray());
        }

        // 添加防具到收容所工作服子分类
        private static void AddToContainmentSubcategory(string[] armorIds)
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
                    armorIds
                };
                addSubcategoryMethod.Invoke(null, parameters);
            }
        }
    }
}
