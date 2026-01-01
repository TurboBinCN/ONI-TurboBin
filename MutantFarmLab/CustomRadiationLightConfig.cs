using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TUNING;
using UnityEngine;

namespace MutantFarmLab
{
    public class CustomRadiationLightConfig : IBuildingConfig
    {
        public const string ID = "CustomRadiationLight_LongBar";

        public override BuildingDef CreateBuildingDef()
        {
            int width = 3;
            int height = 1;
            string anim = "radiation_lamp_kanim";
            int hitpoints = 100;
            float construction_time = 3f;
            float[] tier = BUILDINGS.CONSTRUCTION_MASS_KG.TIER0;
            string[] all_METALS = MATERIALS.ALL_METALS;
            float melting_point = 9999f;
            BuildLocationRule build_location_rule = BuildLocationRule.Anywhere;
            EffectorValues tier2 = NOISE_POLLUTION.NOISY.TIER5;
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(ID, width, height, anim, hitpoints, construction_time, tier, all_METALS, melting_point, build_location_rule, BUILDINGS.DECOR.PENALTY.TIER2, tier2, 0.2f);
            buildingDef.ViewMode = OverlayModes.Radiation.ID;
            buildingDef.AudioCategory = "HollowMetal";
            buildingDef.AudioSize = "large";
            buildingDef.Floodable = false;
            buildingDef.Overheatable = false;
            buildingDef.PermittedRotations = PermittedRotations.FlipV; // 仅允许水平翻转

            buildingDef.UseHighEnergyParticleInputPort = true;
            buildingDef.HighEnergyParticleInputOffset = new CellOffset(1, 0);

            buildingDef.LogicOutputPorts = new List<LogicPorts.Port>
            {
                LogicPorts.Port.OutputPort("HEP_STORAGE_REQ", new CellOffset(1, 0), STRINGS.BUILDINGS.PREFABS.CUSTOMRADIATIONLIGHT.LOGIC_PORT_STORAGE, STRINGS.BUILDINGS.PREFABS.CUSTOMRADIATIONLIGHT.LOGIC_PORT_STORAGE_ACTIVE, STRINGS.BUILDINGS.PREFABS.CUSTOMRADIATIONLIGHT.LOGIC_PORT_STORAGE_INACTIVE, false, false)
            };
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            RadiationEmitter radiationEmitter = go.AddOrGet<RadiationEmitter>();
            radiationEmitter.emitType = RadiationEmitter.RadiationEmitterType.Constant;
            radiationEmitter.radiusProportionalToRads = false;
            radiationEmitter.emitRadiusX = 5;
            radiationEmitter.emitRadiusY = 5;
            radiationEmitter.emitRads = 300;
            radiationEmitter.emitAngle = 180f; // 向下90度扇形辐射
            radiationEmitter.emitDirection = 270f; // 辐射方向：向下（0=右，270=下）
            radiationEmitter.emissionOffset = new Vector3(1f, -0.5f, 0f); // 辐射偏移（长条中心向下）

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
            controller.consumerRate = 1f;
            controller.lowParticleThreshold = 200f; // 低粒子阈值（200单位触发信号）

            //go.AddOrGetDef<CustomRadiationLightSM.Def>();

        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGetDef<PoweredActiveController.Def>();
        }

        
    }

}
