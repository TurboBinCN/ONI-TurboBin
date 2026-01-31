using HarmonyLib;
using UnityEngine;
using System.Reflection;
using PeterHan.PLib.Core;

namespace MutantFarmLab
{
    /// <summary>
    /// 修正版建造校验补丁
    /// </summary>
    [HarmonyPatch]
    public static class BuildLocationPatch
    {
        private static MethodInfo TargetMethod()
        {
            // 匹配：public bool IsValidPlaceLocation(GameObject, int, Orientation, bool, out string)
            return typeof(BuildingDef).GetMethod(
                name: "IsValidPlaceLocation",
                bindingAttr: BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: new[] {
                    typeof(GameObject),
                    typeof(int),
                    typeof(Orientation),
                    typeof(bool),
                    typeof(string).MakeByRefType()
                },
                modifiers: null
            );
        }

        [HarmonyPrefix]
        public static bool Prefix(BuildingDef __instance, GameObject source_go, int cell, Orientation orientation, bool replace_tile, out string fail_reason,ref bool __result)
        {
            fail_reason = null;
            // 仅过滤我们的辐射粒子配件，其他建筑完全不受影响
            if (__instance != null && __instance.PrefabID == RadiationParticleAdapterConfig.ID)
            {
                __result = true; // 强制返回【可放置】，红色禁止直接消失
                return false;    // 跳过原生校验逻辑，完全接管判定
            }
            if (__instance != null && (__instance.PrefabID == FarmTileConfig.ID || __instance.PrefabID == HydroponicFarmConfig.ID))
            {
                int cell_below = Grid.CellBelow(cell);
                if (Grid.IsValidCell(cell_below)) { 
                    GameObject gameObject = Grid.Objects[cell_below, (int)ObjectLayer.Building];
                    if ((gameObject?.PrefabID() == RadiationParticleAdapterConfig.ID) == true){
                        __result = true; // 强制返回【可放置】，红色禁止直接消失
                        return false;    // 跳过原生校验逻辑，完全接管判定
                    }
                }
            }
            return true; // 其他建筑执行原生校验，无副作用
        }
    }
}