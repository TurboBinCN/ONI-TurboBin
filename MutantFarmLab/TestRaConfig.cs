using System;
using System.Collections.Generic;
using TUNING;
using UnityEngine;

namespace MutantFarmLab
{

    public class TestRaConfig : IBuildingConfig
    {
        // Token: 0x0600024A RID: 586 RVA: 0x00010090 File Offset: 0x0000E290
        public override BuildingDef CreateBuildingDef()
        {
            int width = 3;
            int height = 1;
            string anim = "dev_generator_kanim";
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
            buildingDef.DebugOnly = true;

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
            go.AddTag(GameTags.DevBuilding);
            RadiationEmitter radiationEmitter = go.AddOrGet<RadiationEmitter>();
            radiationEmitter.emitType = RadiationEmitter.RadiationEmitterType.Constant;
            radiationEmitter.radiusProportionalToRads = false;
            radiationEmitter.emitRadiusX = 5;
            radiationEmitter.emitRadiusY = 5;
            radiationEmitter.emitRads = 2400f / ((float)radiationEmitter.emitRadiusX / 6f);
            radiationEmitter.emitAngle = 180;
            radiationEmitter.emitDirection = 270f;
            radiationEmitter.emissionOffset = new Vector3(1f,0,0);

            HighEnergyParticleStorage particleStorage = go.AddOrGet<HighEnergyParticleStorage>();
            particleStorage.capacity = 2000f;
            particleStorage.autoStore = true;
            particleStorage.PORT_ID = "HEP_STORAGE";
            particleStorage.showInUI = true;
            particleStorage.showCapacityStatusItem = true;
            particleStorage.showCapacityAsMainStatus = true;

            TestRA testRa = go.AddOrGet<TestRA>();
            testRa.lowParticleThreshold = 200;
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGetDef<PoweredActiveController.Def>();
        }

        public const string ID = "TestRa";
    }



}
