using TUNING;
using UnityEngine;

namespace MutantFarmLab
{
    public class MutantFarmLabConfig : IBuildingConfig
    {
        public static string ID = "MutantFarmLab";
        public static string TagName = "MutantFarmLab";
        public static float Deliverycapacity = 5f;
        public static float ParticleConsumeAmount = 100f;
        public static float MutationDuration = 40f;
        public override BuildingDef CreateBuildingDef()
        {
            int width = 7;
            int height = 3;
            string anim = "genetic_analysisstation_kanim";
            int hitpoints = 30;
            float construction_time = 30f;
            float[] tier = BUILDINGS.CONSTRUCTION_MASS_KG.TIER4;
            string[] all_METALS = MATERIALS.ALL_METALS;
            float melting_point = 1600f;
            BuildLocationRule build_location_rule = BuildLocationRule.OnFloor;
            EffectorValues tier2 = NOISE_POLLUTION.NOISY.TIER3;
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(ID, width, height, anim, hitpoints, construction_time, tier, all_METALS, melting_point, build_location_rule, BUILDINGS.DECOR.NONE, tier2, 0.2f);
            BuildingTemplates.CreateElectricalBuildingDef(buildingDef);
            buildingDef.AudioCategory = "Metal";
            buildingDef.AudioSize = "large";
            buildingDef.EnergyConsumptionWhenActive = 480f;
            buildingDef.ExhaustKilowattsWhenActive = 0.5f;
            buildingDef.SelfHeatKilowattsWhenActive = 4f;
            buildingDef.UseHighEnergyParticleInputPort = true;
            buildingDef.HighEnergyParticleInputOffset = new CellOffset(0, 2);


            
            buildingDef.Deprecated = !DlcManager.FeaturePlantMutationsEnabled();
            buildingDef.RequiredSkillPerkID = Db.Get().SkillPerks.CanIdentifyMutantSeeds.Id;
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            go.GetComponent<KPrefabID>().AddTag(RoomConstraints.ConstraintTags.ScienceBuilding, false);
            go.AddOrGet<BuildingComplete>().isManuallyOperated = true;
            go.AddOrGetDef<MutantFarmLabStates.Def>();
            go.AddOrGet<MutantFarmLabWorkable>();
            Prioritizable.AddRef(go);
            go.AddOrGet<DropAllWorkable>();
            go.AddOrGetDef<PoweredActiveController.Def>();

            HighEnergyParticleStorage highEnergyParticleStorage = go.AddOrGet<HighEnergyParticleStorage>();
            highEnergyParticleStorage.capacity = 2000f;
            highEnergyParticleStorage.autoStore = true;
            highEnergyParticleStorage.PORT_ID = "HEP_STORAGE";
            highEnergyParticleStorage.showCapacityStatusItem = true;

            KPrefabID kPrefabID = go.GetComponent<KPrefabID>();
            if (kPrefabID == null)
                kPrefabID = go.AddComponent<KPrefabID>();
            kPrefabID.AddTag(TagManager.Create(TagName));

            // 你的原有FlatTagFilterable代码（保留不变）
            var filterable = go.AddOrGet<FlatTagFilterable>();
            filterable.headerText = STRINGS.UI.UISIDESCREENS.MUTANTFARMLAB.FILTER_CATEGORY;
            filterable.displayOnlyDiscoveredTags = true;

            var treeFilterable = go.AddOrGet<TreeFilterable>();
            treeFilterable.storageToFilterTag = GameTags.Seed;
            treeFilterable.dropIncorrectOnFilterChange = false;
            treeFilterable.filterByStorageCategoriesOnSpawn = false;
            treeFilterable.autoSelectStoredOnLoad = false;
            treeFilterable.uiHeight = TreeFilterable.UISideScreenHeight.Short;

            Storage storage = go.AddOrGet<Storage>();
            storage.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
            storage.storageID = GameTags.Seed;

        }

        public override void DoPostConfigureComplete(GameObject go)
        {
        }
        public override void ConfigurePost(BuildingDef def)
        {

        }

        public override string[] GetRequiredDlcIds()
        {
            return DlcManager.EXPANSION1;
        }
        public static ManualDeliveryKG AddSeedMDKG(GameObject go, Tag seedTag, float capacity, float refillRate = 0.75f, bool enable = false)
        {
            ManualDeliveryKG mdkg = go.AddComponent<ManualDeliveryKG>();
            mdkg.RequestedItemTag = seedTag; // ✅ 核心：每个MDKG绑定单独的种子Tag
            mdkg.capacity = capacity;        // 该种子的仓储容量（你的allValidTags对应容量）
            mdkg.refillMass = refillRate * capacity; // 补货阈值（75%容量时触发配送）
            mdkg.choreTypeIDHash = Db.Get().ChoreTypes.MachineFetch.IdHash;
            mdkg.operationalRequirement = Operational.State.Functional; // 建筑可用才激活
            mdkg.allowPause = false;          // 允许暂停，适配筛选取消
            mdkg.enabled = enable;            // 默认禁用，勾选后激活
            return mdkg;
        }
    }
}
