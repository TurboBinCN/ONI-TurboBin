using HarmonyLib;
using UnityEngine;
using System.Reflection;

namespace MutantFarmLab
{
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

        [HarmonyPostfix]
        public static void Postfix(BuildingDef __instance, GameObject source_go, int cell, Orientation orientation, global::ObjectLayer layer, global::ObjectLayer tile_layer, bool replace_tile, bool restrictToActiveWorld, ref bool __result)
        {
            if (__result == true || __instance == null)
                return;

            if (__instance.PrefabID == RadiationParticleAdapterConfig.ID)
            {
                if (CanPlaceOnFarmTile(__instance, cell, orientation))
                {
                    __result = true;
                }
            }
            else if (IsFarmTileBuilding(__instance))
            {
                if (CanPlaceOnAdapter(__instance, cell, orientation))
                {
                    __result = true;
                }
            }
        }

        private static bool IsFarmTileBuilding(BuildingDef def)
        {
            return def.PrefabID == "FarmTile" || def.PrefabID == "HydroponicFarm";
        }

        private static bool CanPlaceOnFarmTile(BuildingDef def, int cell, Orientation orientation)
        {
            for (int index = 0; index < def.PlacementOffsets.Length; ++index)
            {
                CellOffset rotatedCellOffset = Rotatable.GetRotatedCellOffset(def.PlacementOffsets[index], orientation);
                if (!Grid.IsCellOffsetValid(cell, rotatedCellOffset))
                    return false;

                int cell1 = Grid.OffsetCell(cell, rotatedCellOffset);

                if (!Grid.IsValidBuildingCell(cell1))
                    return false;

                if (Grid.Element[cell1].id == SimHashes.Unobtanium)
                    return false;

                GameObject objOnBuildingLayer = Grid.Objects[cell1, (int)global::ObjectLayer.Building];
                GameObject objOnFoundationLayer = Grid.Objects[cell1, (int)global::ObjectLayer.FoundationTile];

                bool isFarmTile = IsFarmTile(objOnBuildingLayer) || IsFarmTile(objOnFoundationLayer);

                if (!isFarmTile)
                {
                    if (objOnBuildingLayer != null)
                        return false;

                    if (objOnFoundationLayer != null)
                        return false;
                }
            }

            return true;
        }

        private static bool IsFarmTile(GameObject go)
        {
            if (go == null)
                return false;

            KPrefabID kPrefabID = go.GetComponent<KPrefabID>();
            if (kPrefabID != null)
            {
                if (kPrefabID.HasTag(GameTags.CodexCategories.FarmBuilding))
                    return true;
                if (kPrefabID.HasTag(GameTags.FarmTiles))
                    return true;
            }

            return go.name.Contains("FarmTile") || go.name.Contains("Hydroponic");
        }

        private static bool CanPlaceOnAdapter(BuildingDef def, int cell, Orientation orientation)
        {
            for (int index = 0; index < def.PlacementOffsets.Length; ++index)
            {
                CellOffset rotatedCellOffset = Rotatable.GetRotatedCellOffset(def.PlacementOffsets[index], orientation);
                if (!Grid.IsCellOffsetValid(cell, rotatedCellOffset))
                    return false;

                int cell1 = Grid.OffsetCell(cell, rotatedCellOffset);

                if (!Grid.IsValidBuildingCell(cell1))
                    return false;

                if (Grid.Element[cell1].id == SimHashes.Unobtanium)
                    return false;

                GameObject objOnBuildingLayer = Grid.Objects[cell1, (int)global::ObjectLayer.Building];
                GameObject objOnFoundationLayer = Grid.Objects[cell1, (int)global::ObjectLayer.FoundationTile];

                bool isAdapter = IsAdapter(objOnBuildingLayer) || IsAdapter(objOnFoundationLayer);

                if (!isAdapter)
                {
                    if (objOnBuildingLayer != null)
                        return false;

                    if (objOnFoundationLayer != null)
                        return false;
                }
            }

            return true;
        }

        private static bool IsAdapter(GameObject go)
        {
            if (go == null)
                return false;

            KPrefabID kPrefabID = go.GetComponent<KPrefabID>();
            if (kPrefabID != null && kPrefabID.PrefabTag == TagManager.Create(RadiationParticleAdapterConfig.ID))
                return true;

            return go.name.Contains("RadiationParticleAdapter");
        }
    }
}
