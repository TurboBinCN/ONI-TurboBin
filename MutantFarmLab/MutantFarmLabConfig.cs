using PeterHan.PLib.Core;
using PeterHan.PLib.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TUNING;
using UnityEngine;
using static STRINGS.COLONY_ACHIEVEMENTS.ACTIVATEGEOTHERMALPLANT;

namespace MutantFarmLab
{
    public class MutantFarmLabConfig : IBuildingConfig
    {
        public static string ID = "MutantFarmLab";
        public static string TagName = "MutantFarmLab";
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
            go.AddOrGet<MutantFarmLabWorkable>().finishedSeedDropOffset = new Vector3(-3f, 1.5f, 0f);
            Prioritizable.AddRef(go);
            go.AddOrGet<DropAllWorkable>();
            go.AddOrGetDef<PoweredActiveController.Def>();
            Storage storage = go.AddOrGet<Storage>();
            ManualDeliveryKG manualDeliveryKG = go.AddOrGet<ManualDeliveryKG>();
            manualDeliveryKG.SetStorage(storage);
            manualDeliveryKG.choreTypeIDHash = Db.Get().ChoreTypes.MachineFetch.IdHash;
            manualDeliveryKG.RequestedItemTag = GameTags.Seed;
            manualDeliveryKG.refillMass = 1.1f;
            manualDeliveryKG.MinimumMass = 1f;
            manualDeliveryKG.capacity = 5f;
            HighEnergyParticleStorage highEnergyParticleStorage = go.AddOrGet<HighEnergyParticleStorage>();
            highEnergyParticleStorage.capacity = 2000f;
            highEnergyParticleStorage.autoStore = true;
            highEnergyParticleStorage.PORT_ID = "HEP_STORAGE";
            highEnergyParticleStorage.showCapacityStatusItem = true;

            KPrefabID kPrefabID = go.GetComponent<KPrefabID>();
            if (kPrefabID == null)
                kPrefabID = go.AddComponent<KPrefabID>();
            kPrefabID.AddTag(TagManager.Create(TagName));

        }

        public override void DoPostConfigureComplete(GameObject go)
        {
        }
        public override void ConfigurePost(BuildingDef def)
        {
            List<Tag> list = new List<Tag>();
            foreach (GameObject gameObject in Assets.GetPrefabsWithTag(GameTags.CropSeed))
            {
                if (gameObject.GetComponent<MutantPlant>() != null)
                {
                    list.Add(gameObject.PrefabID());
                }
            }
            def.BuildingComplete.GetComponent<Storage>().storageFilters = list;
        }
        public override string[] GetRequiredDlcIds()
        {
            return DlcManager.EXPANSION1;
        }
    }
}
