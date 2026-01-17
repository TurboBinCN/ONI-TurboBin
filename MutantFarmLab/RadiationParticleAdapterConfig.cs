using STRINGS;
using System.Collections.Generic;
using TUNING;
using UnityEngine;
using BUILDINGS = TUNING.BUILDINGS;

namespace MutantFarmLab
{
    /// <summary>
    /// 辐射粒子适配配件【适配原生种植砖/水培砖】- 全报错修复版
    /// </summary>
    public class RadiationParticleAdapterConfig : IBuildingConfig
    {
        public const string ID = "RadiationParticleAdapter";
        public static string HEP_RQ_LOGIC_PORT_ID = "HEPT_PORT_RP";

        // 可配置参数
        private const float PARTICLE_STORAGE_CAP = 2000f;
        private const float RADIATION_EMIT_VALUE = 350f;

        public override BuildingDef CreateBuildingDef()
        {
            int width = 1;
            int height = 2;
            string anim = "radiation_tile_adapter_kanim";
            int hitpoints = 10;
            float constructionTime = 3f;
            float[] buildMass = BUILDINGS.CONSTRUCTION_MASS_KG.TIER0;
            string[] buildMaterials = MATERIALS.REFINED_METALS;
            float meltPoint = 1600f;
            BuildLocationRule buildRule = BuildLocationRule.Anywhere;

            EffectorValues noise = NOISE_POLLUTION.NONE;

            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                ID, width, height, anim, hitpoints, constructionTime,
                buildMass, buildMaterials, meltPoint, buildRule,
                BUILDINGS.DECOR.PENALTY.TIER0, noise, 0.2f);

            // 逻辑门类核心特性
            def.BlockTileIsTransparent = true;
            def.ViewMode = OverlayModes.Radiation.ID;
            //def.ObjectLayer = ObjectLayer.LogicGate;
            //def.SceneLayer = Grid.SceneLayer.LogicGates;
            def.ObjectLayer = ObjectLayer.Building;       // 核心：改用实体建筑层，粒子可被检测接收
            def.SceneLayer = Grid.SceneLayer.Building;
            def.Floodable = false;
            def.Overheatable = false;
            def.Entombable = false;
            def.AudioCategory = "Metal";
            def.AudioSize = "small";
            def.BaseTimeUntilRepair = -1f;
            def.PermittedRotations = PermittedRotations.FlipV;
            def.DragBuild = true;

            def.UseHighEnergyParticleInputPort = true;
            def.HighEnergyParticleInputOffset = new CellOffset(0, 0);
            def.LogicOutputPorts = new List<LogicPorts.Port>()
            {
                LogicPorts.Port.OutputPort(
                    HEP_RQ_LOGIC_PORT_ID, 
                    new CellOffset(0, 0),
                    STRINGS.BUILDINGS.PREFABS.RADIATIONPARTICLEADAPTER.LOGIC_PORT_NAME,
                    STRINGS.BUILDINGS.PREFABS.RADIATIONPARTICLEADAPTER.LOGIC_PORT_ACTIVE,
                    STRINGS.BUILDINGS.PREFABS.RADIATIONPARTICLEADAPTER.LOGIC_PORT_INACTIVE,
                    false, false)
            };

            def.AddSearchTerms(SEARCH_TERMS.AUTOMATION);
            def.AddSearchTerms(SEARCH_TERMS.FARM);

            GeneratedBuildings.RegisterWithOverlay(OverlayModes.Logic.HighlightItemIDs, ID);
            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            GeneratedBuildings.MakeBuildingAlwaysOperational(go);
            BuildingConfigManager.Instance.IgnoreDefaultKComponent(typeof(RequiresFoundation), prefab_tag);

            KPrefabID kPrefab = go.GetComponent<KPrefabID>();
            kPrefab.AddTag(GameTags.OverlayBehindConduits, false);
            // ✅ 修复4：移除不存在的GameTags.Automation

            // 挂载核心组件
            HighEnergyParticleStorage particleStorage = go.AddOrGet<HighEnergyParticleStorage>();
            particleStorage.capacity = PARTICLE_STORAGE_CAP;
            particleStorage.autoStore = true;
            particleStorage.PORT_ID = "HEP_STORAGE";
            particleStorage.showInUI = true;
            particleStorage.showCapacityStatusItem = true;
            particleStorage.showCapacityAsMainStatus = true;

            RadiationEmitter radiationEmitter = go.AddOrGet<RadiationEmitter>();
            radiationEmitter.emitType = RadiationEmitter.RadiationEmitterType.Constant;
            radiationEmitter.radiusProportionalToRads = false;
            radiationEmitter.emitRadiusX = 6;
            radiationEmitter.emitRadiusY = 1;
            radiationEmitter.emitRads = RADIATION_EMIT_VALUE;
            radiationEmitter.emitAngle = 90f;
            //radiationEmitter.emitDirection = 90f;
            //radiationEmitter.emissionOffset = new Vector3(0f, 2f, 0);

            RadiationParticleAdapterController controller = go.AddOrGet<RadiationParticleAdapterController>();
            controller.ParticleConsumeRate = 1f;
            controller.LowParticleThreshold = 200f;

            go.AddOrGet<CopyBuildingSettings>();

            go.AddOrGetDef<RadiationParticleAdapterStates.Def>();

        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            //go.GetComponent<RequireInputs>().requireConduitHasMass = false;
        }
    }
}