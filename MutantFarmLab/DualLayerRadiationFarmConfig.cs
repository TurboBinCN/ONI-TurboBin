using STRINGS;
using System.Collections.Generic;
using TUNING;
using UnityEngine;
using BUILDINGS = TUNING.BUILDINGS;

namespace MutantFarmLab
{
    // 双层辐射种植砖
    public class DualLayerRadiationFarmConfig : IBuildingConfig
    {
        public const string ID = "DualLayerRadiationFarm";
        
        public override BuildingDef CreateBuildingDef()
        {
            int width = 1;
            int height = 2;
            string anim = "radiation_collector_kanim";
            int hitpoints = 100;
            float construction_time = 3f;
            float[] tier = BUILDINGS.CONSTRUCTION_MASS_KG.TIER0;
            string[] all_METALS = MATERIALS.ALL_METALS;
            float melting_point = 9999f;
            BuildLocationRule build_location_rule = BuildLocationRule.Anywhere;
            EffectorValues tier2 = NOISE_POLLUTION.NOISY.TIER5;
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(ID, width, height, anim, hitpoints, construction_time, tier, all_METALS, melting_point, build_location_rule, BUILDINGS.DECOR.PENALTY.TIER2, tier2, 0.2f);
            buildingDef.AudioCategory = "HollowMetal";
            buildingDef.AudioSize = "large";
            buildingDef.Floodable = false;
            buildingDef.Overheatable = false;
            buildingDef.PermittedRotations = PermittedRotations.FlipV;

            //辐射
            buildingDef.ViewMode = OverlayModes.Radiation.ID;
            buildingDef.UseHighEnergyParticleInputPort = true;
            buildingDef.HighEnergyParticleInputOffset = new CellOffset(0, 0);

            buildingDef.LogicOutputPorts = new List<LogicPorts.Port>
            {
                LogicPorts.Port.OutputPort("HEP_STORAGE_REQ2", new CellOffset(0, 0), STRINGS.BUILDINGS.PREFABS.CUSTOMRADIATIONLIGHT.LOGIC_PORT_STORAGE, STRINGS.BUILDINGS.PREFABS.CUSTOMRADIATIONLIGHT.LOGIC_PORT_STORAGE_ACTIVE, STRINGS.BUILDINGS.PREFABS.CUSTOMRADIATIONLIGHT.LOGIC_PORT_STORAGE_INACTIVE, false, false)
            };

            //种植
            buildingDef.ObjectLayer = ObjectLayer.Building;
            buildingDef.BaseTimeUntilRepair = -1f;
            buildingDef.SceneLayer = Grid.SceneLayer.TileMain;
            buildingDef.ConstructionOffsetFilter = BuildingDef.ConstructionOffsetFilter_OneDown;
            buildingDef.InputConduitType = ConduitType.Liquid;
            buildingDef.UtilityInputOffset = new CellOffset(0, 0);

            buildingDef.AddSearchTerms(SEARCH_TERMS.FARM);
            buildingDef.AddSearchTerms(SEARCH_TERMS.FOOD);
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            //==辐射==
            RadiationEmitter radiationEmitter = go.AddOrGet<RadiationEmitter>();
            radiationEmitter.emitType = RadiationEmitter.RadiationEmitterType.Constant;
            radiationEmitter.radiusProportionalToRads = false;
            //辐射区域闪烁跟X/Y相关
            radiationEmitter.emitRadiusX = 6;
            radiationEmitter.emitRadiusY = 1;
            radiationEmitter.emitRads = 300;
            radiationEmitter.emitAngle = 90f; 
            radiationEmitter.emitDirection = 90f; 
            radiationEmitter.emissionOffset = new Vector3(0f, 2f, 0f);

            // 添加粒子存储组件（容量2000单位，参考HighEnergyParticleStorage逻辑）
            HighEnergyParticleStorage particleStorage = go.AddOrGet<HighEnergyParticleStorage>();
            particleStorage.capacity = 2000f;
            particleStorage.autoStore = true;
            particleStorage.PORT_ID = "HEP_STORAGE";
            particleStorage.showInUI = true;
            particleStorage.showCapacityStatusItem = true;
            particleStorage.showCapacityAsMainStatus = true;

            CustomRadiationLight controller = go.AddOrGet<CustomRadiationLight>();
            controller.radiationEmitter = radiationEmitter;
            controller.consumerRate = 0.5f;
            controller.lowParticleThreshold = 200f; // 低粒子阈值（200单位触发信号）

            //==种植==
            go.GetComponent<KPrefabID>().AddTag(GameTags.CodexCategories.FarmBuilding, false);
            SimCellOccupier simCellOccupier = go.AddOrGet<SimCellOccupier>();
            simCellOccupier.doReplaceElement = true;
            simCellOccupier.notifyOnMelt = true;
            //simCellOccupier. = true;  // 强制标记：占据双层所有单元格，无遗漏
            //simCellOccupier.isSolid = true;       // 标记为建筑固体，底层不会生成可挖掘矿石

            go.AddOrGet<TileTemperature>();
            ConduitConsumer conduitConsumer = go.AddOrGet<ConduitConsumer>();
            conduitConsumer.conduitType = ConduitType.Liquid;
            conduitConsumer.consumptionRate = 1f;
            conduitConsumer.capacityKG = 5f;
            conduitConsumer.capacityTag = GameTags.Liquid;
            conduitConsumer.wrongElementResult = ConduitConsumer.WrongElementResult.Dump;

            go.AddOrGet<Storage>();
            PlantablePlot plantablePlot = go.AddOrGet<PlantablePlot>();
            plantablePlot.AddDepositTag(GameTags.CropSeed);
            plantablePlot.AddDepositTag(GameTags.WaterSeed);
            plantablePlot.occupyingObjectRelativePosition.y = 1f;
            plantablePlot.SetFertilizationFlags(true, true);

            go.AddOrGet<CopyBuildingSettings>().copyGroupTag = GameTags.Farm;
            BuildingTemplates.CreateDefaultStorage(go, false).SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
            go.AddOrGet<PlanterBox>();
            go.AddOrGet<AnimTileable>();
            go.AddOrGet<DropAllWorkable>();

            Prioritizable.AddRef(go);
        }

        // 建筑配置完成后初始化
        public override void DoPostConfigureComplete(GameObject go)
        {
            FarmTileConfig.SetUpFarmPlotTags(go);

            go.GetComponent<KPrefabID>().AddTag(GameTags.FarmTiles, false);
            go.GetComponent<RequireInputs>().requireConduitHasMass = false;
            // ✅ 补充：双层建筑必备，阻止底层生成可挖掘矿石
            KPrefabID prefabID = go.GetComponent<KPrefabID>();
            //prefabID.AddTag(GameTags.build, false);
            prefabID.AddTag(GameTags.Solid, false); // 标记为建筑，而非可挖掘固体

        }

    }

    

    
}
