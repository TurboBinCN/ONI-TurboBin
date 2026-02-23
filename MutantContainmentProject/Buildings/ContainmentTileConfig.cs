using STRINGS;
using TBB.He.TbbLib.Utils;
using TUNING;
using UnityEngine;

namespace MutantContainmentProject.Buildings
{
    public class ContainmentTileConfig : IBuildingConfig
    {
        public static readonly string ID = "ContainmentTile";
        public static readonly int BlockTileConnectorID = Hash.SDBMLower("tiles_solid_tops");
        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(ID, 1, 1, "containment_tile_kanim", 100, 10f, TUNING.BUILDINGS.CONSTRUCTION_MASS_KG.TIER3,
              new string[]{
                SimHashes.Unobtanium.ToString()
            }, 1600f, BuildLocationRule.Tile, TUNING.BUILDINGS.DECOR.BONUS.TIER0, NOISE_POLLUTION.NONE, 0.2f);

            BuildingTemplates.CreateFoundationTileDef(buildingDef);
            buildingDef.Floodable = false;
            buildingDef.Overheatable = false;
            buildingDef.Entombable = false;
            buildingDef.UseStructureTemperature = false;
            buildingDef.AudioCategory = "Metal";
            buildingDef.AudioSize = "small";
            buildingDef.BaseTimeUntilRepair = -1f;
            buildingDef.SceneLayer = Grid.SceneLayer.TileMain;
            buildingDef.isKAnimTile = true;

            buildingDef.BlockTileAtlas = TbbAssetsUtils.Instance.LoadTextureAtlas("containment_tile_solid", "tiles_solid");
            buildingDef.BlockTilePlaceAtlas = Assets.GetTextureAtlas("tiles_solid_place");
            buildingDef.BlockTileMaterial = Assets.GetMaterial("tiles_solid");
            buildingDef.DecorBlockTileInfo = Assets.GetBlockTileDecorInfo("tiles_solid_tops_info");
            buildingDef.DecorPlaceBlockTileInfo = Assets.GetBlockTileDecorInfo("tiles_solid_tops_place_info");

            buildingDef.AddSearchTerms(SEARCH_TERMS.TILE);
            buildingDef.DragBuild = true;

            return buildingDef;
        }
        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            GeneratedBuildings.MakeBuildingAlwaysOperational(go);
            BuildingConfigManager.Instance.IgnoreDefaultKComponent(typeof(RequiresFoundation), prefab_tag);
            SimCellOccupier simCellOccupier = go.AddOrGet<SimCellOccupier>();
            simCellOccupier.doReplaceElement = true;
            simCellOccupier.strengthMultiplier = 1.5f;
            simCellOccupier.movementSpeedMultiplier = DUPLICANTSTATS.MOVEMENT_MODIFIERS.BONUS_2;
            simCellOccupier.notifyOnMelt = true;

            go.AddOrGet<KAnimGridTileVisualizer>().blockTileConnectorID = BlockTileConnectorID;
            go.AddOrGet<BuildingHP>().destroyOnDamaged = true;
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            GeneratedBuildings.RemoveLoopingSounds(go);
            go.GetComponent<KPrefabID>().AddTag(GameTags.FloorTiles, false);
            go.GetComponent<KPrefabID>().AddTag(MutanterComponent.MutanterTags.MutanterBuildings, false);
        }

        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            base.DoPostConfigureUnderConstruction(go);
            go.AddOrGet<KAnimGridTileVisualizer>();
        }


    }
}
