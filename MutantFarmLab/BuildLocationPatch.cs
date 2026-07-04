using HarmonyLib;
using UnityEngine;
using System.Reflection;

namespace MutantFarmLab
{
    [HarmonyPatch]
    public static class BuildLocationPatch_IsValidPlaceLocation
    {
        private static MethodInfo TargetMethod()
        {
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
        public static bool Prefix(BuildingDef __instance, GameObject source_go, int cell, Orientation orientation, bool replace_tile, out string fail_reason, ref bool __result)
        {
            fail_reason = null;
            if (__instance != null && __instance.PrefabID == RadiationParticleAdapterConfig.ID)
            {
                __result = IsAdapterCanPlaceOnFarmTile(__instance, source_go, cell, orientation, replace_tile, out fail_reason);
                return false;
            }
            return true;
        }

        private static bool IsAdapterCanPlaceOnFarmTile(
            BuildingDef def,
            GameObject source_go,
            int cell,
            Orientation orientation,
            bool replace_tile,
            out string fail_reason)
        {
            fail_reason = null;

            string originalFailReason;
            bool originalResult = def.IsValidPlaceLocation(source_go, cell, orientation, replace_tile, out originalFailReason);

            if (originalResult)
                return true;

            for (int index = 0; index < def.PlacementOffsets.Length; ++index)
            {
                CellOffset rotatedCellOffset = Rotatable.GetRotatedCellOffset(def.PlacementOffsets[index], orientation);
                int cell1 = Grid.OffsetCell(cell, rotatedCellOffset);
                
                GameObject objOnTileLayer = Grid.Objects[cell1, (int)global::ObjectLayer.FoundationTile];
                if (objOnTileLayer != null && IsFarmTile(objOnTileLayer))
                    return true;

                GameObject objOnBuildingLayer = Grid.Objects[cell1, (int)global::ObjectLayer.Building];
                if (objOnBuildingLayer != null && IsFarmTile(objOnBuildingLayer))
                    return true;
            }

            return false;
        }

        private static bool IsFarmTile(GameObject go)
        {
            if (go == null)
                return false;
            
            KPrefabID kPrefabID = go.GetComponent<KPrefabID>();
            if (kPrefabID != null && kPrefabID.HasTag(GameTags.CodexCategories.FarmBuilding))
                return true;
            
            return go.name.Contains("FarmTile") || go.name.Contains("Hydroponic");
        }
    }

    [HarmonyPatch]
    public static class BuildLocationPatch_IsValidPlaceLocation_Restrict
    {
        private static MethodInfo TargetMethod()
        {
            return typeof(BuildingDef).GetMethod(
                name: "IsValidPlaceLocation",
                bindingAttr: BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: new[] {
                    typeof(GameObject),
                    typeof(int),
                    typeof(Orientation),
                    typeof(bool),
                    typeof(string).MakeByRefType(),
                    typeof(bool)
                },
                modifiers: null
            );
        }

        [HarmonyPrefix]
        public static bool Prefix(BuildingDef __instance, GameObject source_go, int cell, Orientation orientation, bool replace_tile, out string fail_reason, bool restrictToActiveWorld, ref bool __result)
        {
            fail_reason = null;
            if (__instance != null && __instance.PrefabID == RadiationParticleAdapterConfig.ID)
            {
                __result = IsAdapterCanPlaceOnFarmTile(__instance, source_go, cell, orientation, replace_tile, out fail_reason, restrictToActiveWorld);
                return false;
            }
            return true;
        }

        private static bool IsAdapterCanPlaceOnFarmTile(
            BuildingDef def,
            GameObject source_go,
            int cell,
            Orientation orientation,
            bool replace_tile,
            out string fail_reason,
            bool restrictToActiveWorld)
        {
            fail_reason = null;

            string originalFailReason;
            bool originalResult = def.IsValidPlaceLocation(source_go, cell, orientation, replace_tile, out originalFailReason, restrictToActiveWorld);

            if (originalResult)
                return true;

            for (int index = 0; index < def.PlacementOffsets.Length; ++index)
            {
                CellOffset rotatedCellOffset = Rotatable.GetRotatedCellOffset(def.PlacementOffsets[index], orientation);
                int cell1 = Grid.OffsetCell(cell, rotatedCellOffset);
                
                GameObject objOnTileLayer = Grid.Objects[cell1, (int)global::ObjectLayer.FoundationTile];
                if (objOnTileLayer != null && IsFarmTile(objOnTileLayer))
                    return true;

                GameObject objOnBuildingLayer = Grid.Objects[cell1, (int)global::ObjectLayer.Building];
                if (objOnBuildingLayer != null && IsFarmTile(objOnBuildingLayer))
                    return true;
            }

            return false;
        }

        private static bool IsFarmTile(GameObject go)
        {
            if (go == null)
                return false;
            
            KPrefabID kPrefabID = go.GetComponent<KPrefabID>();
            if (kPrefabID != null && kPrefabID.HasTag(GameTags.CodexCategories.FarmBuilding))
                return true;
            
            return go.name.Contains("FarmTile") || go.name.Contains("Hydroponic");
        }
    }

    [HarmonyPatch]
    public static class BuildLocationPatch_IsValidBuildLocation
    {
        private static MethodInfo TargetMethod()
        {
            return typeof(BuildingDef).GetMethod(
                name: "IsValidBuildLocation",
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
        public static bool Prefix(BuildingDef __instance, GameObject source_go, int cell, Orientation orientation, bool replace_tile, out string fail_reason, ref bool __result)
        {
            fail_reason = null;
            if (__instance != null && __instance.PrefabID == RadiationParticleAdapterConfig.ID)
            {
                __result = IsAdapterCanPlaceOnFarmTile(__instance, source_go, cell, orientation, replace_tile, out fail_reason);
                return false;
            }
            return true;
        }

        private static bool IsAdapterCanPlaceOnFarmTile(
            BuildingDef def,
            GameObject source_go,
            int cell,
            Orientation orientation,
            bool replace_tile,
            out string fail_reason)
        {
            fail_reason = null;

            string originalFailReason;
            bool originalResult = def.IsValidBuildLocation(source_go, cell, orientation, replace_tile, out originalFailReason);

            if (originalResult)
                return true;

            for (int index = 0; index < def.PlacementOffsets.Length; ++index)
            {
                CellOffset rotatedCellOffset = Rotatable.GetRotatedCellOffset(def.PlacementOffsets[index], orientation);
                int cell1 = Grid.OffsetCell(cell, rotatedCellOffset);
                
                GameObject objOnTileLayer = Grid.Objects[cell1, (int)global::ObjectLayer.FoundationTile];
                if (objOnTileLayer != null && IsFarmTile(objOnTileLayer))
                    return true;

                GameObject objOnBuildingLayer = Grid.Objects[cell1, (int)global::ObjectLayer.Building];
                if (objOnBuildingLayer != null && IsFarmTile(objOnBuildingLayer))
                    return true;
            }

            return false;
        }

        private static bool IsFarmTile(GameObject go)
        {
            if (go == null)
                return false;
            
            KPrefabID kPrefabID = go.GetComponent<KPrefabID>();
            if (kPrefabID != null && kPrefabID.HasTag(GameTags.CodexCategories.FarmBuilding))
                return true;
            
            return go.name.Contains("FarmTile") || go.name.Contains("Hydroponic");
        }
    }

    [HarmonyPatch]
    public static class BuildLocationPatch_IsAreaClear
    {
        private static MethodInfo TargetMethod()
        {
            return typeof(BuildingDef).GetMethod(
                name: "IsAreaClear",
                bindingAttr: BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: new[] {
                    typeof(GameObject),
                    typeof(int),
                    typeof(Orientation),
                    typeof(global::ObjectLayer),
                    typeof(global::ObjectLayer),
                    typeof(bool),
                    typeof(bool),
                    typeof(string).MakeByRefType(),
                    typeof(bool)
                },
                modifiers: null
            );
        }

        [HarmonyPrefix]
        public static bool Prefix(BuildingDef __instance, GameObject source_go, int cell, Orientation orientation, global::ObjectLayer layer, global::ObjectLayer tile_layer, bool replace_tile, bool restrictToActiveWorld, out string fail_reason, bool permitUproots, ref bool __result)
        {
            fail_reason = null;
            if (__instance != null && __instance.PrefabID == RadiationParticleAdapterConfig.ID)
            {
                __result = IsAdapterCanPlaceOnFarmTile(__instance, source_go, cell, orientation, layer, tile_layer, replace_tile, restrictToActiveWorld, out fail_reason, permitUproots);
                return false;
            }
            return true;
        }

        private static bool IsAdapterCanPlaceOnFarmTile(
            BuildingDef def,
            GameObject source_go,
            int cell,
            Orientation orientation,
            global::ObjectLayer layer,
            global::ObjectLayer tile_layer,
            bool replace_tile,
            bool restrictToActiveWorld,
            out string fail_reason,
            bool permitUproots)
        {
            fail_reason = null;

            MethodInfo originalMethod = typeof(BuildingDef).GetMethod(
                name: "IsAreaClear",
                bindingAttr: BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: new[] {
                    typeof(GameObject),
                    typeof(int),
                    typeof(Orientation),
                    typeof(global::ObjectLayer),
                    typeof(global::ObjectLayer),
                    typeof(bool),
                    typeof(bool),
                    typeof(string).MakeByRefType(),
                    typeof(bool)
                },
                modifiers: null
            );

            string originalFailReason = null;
            object[] parameters = { source_go, cell, orientation, layer, tile_layer, replace_tile, restrictToActiveWorld, originalFailReason, permitUproots };
            bool originalResult = (bool)originalMethod.Invoke(def, parameters);
            originalFailReason = (string)parameters[7];

            if (originalResult)
                return true;

            for (int index = 0; index < def.PlacementOffsets.Length; ++index)
            {
                CellOffset rotatedCellOffset = Rotatable.GetRotatedCellOffset(def.PlacementOffsets[index], orientation);
                int cell1 = Grid.OffsetCell(cell, rotatedCellOffset);
                
                GameObject objOnTileLayer = Grid.Objects[cell1, (int)global::ObjectLayer.FoundationTile];
                if (objOnTileLayer != null && IsFarmTile(objOnTileLayer))
                    return true;

                GameObject objOnBuildingLayer = Grid.Objects[cell1, (int)global::ObjectLayer.Building];
                if (objOnBuildingLayer != null && IsFarmTile(objOnBuildingLayer))
                    return true;
            }

            return false;
        }

        private static bool IsFarmTile(GameObject go)
        {
            if (go == null)
                return false;
            
            KPrefabID kPrefabID = go.GetComponent<KPrefabID>();
            if (kPrefabID != null && kPrefabID.HasTag(GameTags.CodexCategories.FarmBuilding))
                return true;
            
            return go.name.Contains("FarmTile") || go.name.Contains("Hydroponic");
        }
    }
}
