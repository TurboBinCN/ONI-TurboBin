using KSerialization;
using PeterHan.PLib.Core;
using STRINGS;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using TemplateClasses;
using TUNING;
using UnityEngine;
using static KMod.Testing;

namespace MutantFarmLab
{
    // 双层辐射种植砖
    public class RadiationFarmTileConfig : IBuildingConfig
    {
        public const string ID = "RadiationFarmTile";
        public static readonly Tag EnergySource = SimHashes.UraniumOre.CreateTag();
        public static readonly string RadiationStorageName = "UraniumOreStorage";
        public static readonly Tag RadiationPlotTag = TagManager.Create("RadiationPlot");
        private GameObject RadiationPlot;
        public override BuildingDef CreateBuildingDef()
        {
            int width = 1;
            int height = 1;
            string anim = "radiation_farm_tile_kanim";
            int hitpoints = 100;
            float construction_time = 10f;
            float[] tier = TUNING.BUILDINGS.CONSTRUCTION_MASS_KG.TIER2;
            string[] all_METALS = MATERIALS.ALL_METALS;
            float melting_point = 1600f;
            BuildLocationRule build_location_rule = BuildLocationRule.Tile;
            EffectorValues none = NOISE_POLLUTION.NONE;
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(ID, width, height, anim, hitpoints, construction_time, tier, all_METALS, melting_point, build_location_rule, TUNING.BUILDINGS.DECOR.PENALTY.TIER0, none, 0.2f);
            BuildingTemplates.CreateFoundationTileDef(buildingDef);

            buildingDef.AudioCategory = "HollowMetal";
            buildingDef.AudioSize = "small";
            buildingDef.Floodable = false;
            buildingDef.Overheatable = false;
            buildingDef.Entombable = false;
            buildingDef.UseStructureTemperature = false;
            buildingDef.BaseTimeUntilRepair = -1f;
            buildingDef.PermittedRotations = PermittedRotations.FlipV;

            //辐射
            buildingDef.ViewMode = OverlayModes.Radiation.ID;


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
            //==种植==
            BuildingTemplates.CreateDefaultStorage(go, true).SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
            go.GetComponent<KPrefabID>().AddTag(GameTags.CodexCategories.FarmBuilding, false);
            SimCellOccupier simCellOccupier = go.AddOrGet<SimCellOccupier>();
            simCellOccupier.doReplaceElement = true;
            simCellOccupier.notifyOnMelt = true;

            go.AddOrGet<TileTemperature>();
            ConduitConsumer conduitConsumer = go.AddOrGet<ConduitConsumer>();
            conduitConsumer.conduitType = ConduitType.Liquid;
            conduitConsumer.consumptionRate = 1f;
            conduitConsumer.capacityKG = 5f;
            conduitConsumer.capacityTag = GameTags.Liquid;
            conduitConsumer.wrongElementResult = ConduitConsumer.WrongElementResult.Dump;
            conduitConsumer.forceAlwaysSatisfied = true;

            PlantablePlot plantablePlot = go.AddOrGet<PlantablePlot>();
            plantablePlot.AddDepositTag(GameTags.CropSeed);
            plantablePlot.AddDepositTag(GameTags.WaterSeed);
            plantablePlot.occupyingObjectRelativePosition.y = 1f;
            plantablePlot.SetFertilizationFlags(true, true);

            go.AddOrGet<CopyBuildingSettings>().copyGroupTag = GameTags.Farm;

            go.AddOrGet<PlanterBox>();
            go.AddOrGet<AnimTileable>();
            go.AddOrGet<DropAllWorkable>();

            //==辐射==
            //--辐射子体--
            RadiationPlot = new GameObject("RadiationPlot");
            RadiationPlot.transform.SetParent(go.transform, false);
            RadiationPlot.transform.localPosition = Vector3.zero;

            KPrefabID kPrefabID = RadiationPlot.AddOrGet<KPrefabID>();
            kPrefabID.PrefabTag = RadiationPlotTag;
            kPrefabID.InstanceID = KPrefabID.GetUniqueID();
            PUtil.LogDebug($"RadiationPlot InstanceID:[{kPrefabID.InstanceID}]");
            kPrefabID.AddTag(GameTags.StorageLocker, false);
            KPrefabIDTracker.Get().Register(kPrefabID);

            KSelectable kSelectable = RadiationPlot.AddOrGet<KSelectable>();
            kSelectable.SetName("RadiationPlot"); // 设置UI显示名称
            kSelectable.IsSelectable = true;

            KBatchedAnimController animController = RadiationPlot.AddOrGet<KBatchedAnimController>();
            animController.AnimFiles = new KAnimFile[] { Assets.GetAnim("farmtilehydroponicrotating_kanim") }; // 自定义GO可空，不影响功能

            RadiationEmitter radiationEmitter = go.AddOrGet<RadiationEmitter>();
            radiationEmitter.emitType = RadiationEmitter.RadiationEmitterType.Constant;
            radiationEmitter.radiusProportionalToRads = false;
            //辐射区域闪烁跟X/Y相关
            radiationEmitter.emitRadiusX = 6;
            radiationEmitter.emitRadiusY = 1;
            radiationEmitter.emitRads = 300;
            radiationEmitter.emitAngle = 90f;
            radiationEmitter.emitDirection = 90f;
            radiationEmitter.emissionOffset = new Vector3(0f, 1f, 0f);

            Storage uraniumStorage = RadiationPlot.AddComponent<Storage>();
            uraniumStorage.name = RadiationStorageName;
            uraniumStorage.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
            uraniumStorage.capacityKg = 100f;
            uraniumStorage.storageFilters = new List<Tag>() { EnergySource };
            uraniumStorage.allowItemRemoval = true; // 允许小人搬运铀矿
            uraniumStorage.showInUI = true; // UI面板显示铀矿储量
            uraniumStorage.showUnreachableStatus = true;
            uraniumStorage.SetOffsetTable(OffsetGroups.InvertedStandardTable);

            ManualDeliveryKG manualDeliveryKG = RadiationPlot.AddOrGet<ManualDeliveryKG>();
            manualDeliveryKG.SetStorage(uraniumStorage);
            manualDeliveryKG.choreTypeIDHash = Db.Get().ChoreTypes.MachineFetch.IdHash;
            manualDeliveryKG.RequestedItemTag = EnergySource;
            manualDeliveryKG.capacity = 100f;
            manualDeliveryKG.MinimumMass = 50f;
            manualDeliveryKG.FillToMinimumMass = true;
            manualDeliveryKG.RoundFetchAmountToInt = false;
            manualDeliveryKG.allowPause = true;
            manualDeliveryKG.ShowStatusItem = true;
            manualDeliveryKG.enabled = true;

            RadiationPlot.AddOrGet<RadiationPlotStorageHolder>();

            go.AddOrGet<RadiationFarmTile>();
            go.AddOrGetDef<RadiationFarmTileStates.Def>();

            RadiationPlot.SetActive(true);
            go.AddOrGet<RadiationPlotStorageSaver>();
            Prioritizable.AddRef(go);
        }

        // 建筑配置完成后初始化
        public override void DoPostConfigureComplete(GameObject go)
        {
            FarmTileConfig.SetUpFarmPlotTags(go);

            go.GetComponent<KPrefabID>().AddTag(GameTags.FarmTiles, false);
            go.GetComponent<RequireInputs>().requireConduitHasMass = false;

        }
    }
    public class RadiationPlotStorageHolder : KMonoBehaviour, ISaveLoadable
    {
        [MyCmpGet] private Storage storage;

        protected override void OnPrefabInit() { base.OnPrefabInit(); }

        protected override void OnSpawn() { base.OnSpawn(); }
    }
    [SerializationConfig(MemberSerialization.OptIn)]
    public class RadiationPlotStorageSaver : KMonoBehaviour, ISaveLoadable
    {
        // 只保存物品的 prefab tag（字符串），这是 ONI 存档的标准做法
        [Serialize]
        private List<GameObject> savedItems = new();

        private Storage uraniumStorage;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            PUtil.LogDebug($"RadiationPlotStorageSaver OnSpawn");
            // 在 OnSpawn 中绑定子对象的 Storage
            var plot = transform.Find(RadiationFarmTileConfig.RadiationStorageName);
            if (plot != null)
            {
                uraniumStorage = plot.GetComponent<Storage>();
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            PUtil.LogDebug($"RadiationPlotStorageSaver Serialize");
            // 清空旧数据（防止重复）
            savedItems.Clear();
            savedItems.AddRange(uraniumStorage.items);

        }

        public void Deserialize(IReader reader)
        {
            // 延迟到下一帧恢复（确保 RadiationPlot 已存在）
            //GameScheduler.Instance.Schedule("RestoreRadiationItems", 1f, RestoreItems, null);
            RestoreItems();
        }

        private void RestoreItems()
        {
            PUtil.LogDebug($"RadiationPlotStorageSaver RestoreItems");
            if (uraniumStorage == null)
            {
                // 如果 OnSpawn 还没跑，尝试再找一次
                var plot = transform.Find(RadiationFarmTileConfig.RadiationStorageName);
                if (plot != null)
                {
                    uraniumStorage = plot.GetComponent<Storage>();
                }
            }

            if (uraniumStorage == null) return;

            foreach (var item in savedItems)
            {
                uraniumStorage.Store(item);
            }
            savedItems.Clear(); // 可选：清空以节省内存
        }
    }
}