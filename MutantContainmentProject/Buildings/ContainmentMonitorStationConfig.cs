using MutantContainmentProject.MutanterComponent;
using MutantContainmentProject.Room;
using TUNING;
using UnityEngine;

namespace MutantContainmentProject.Buildings
{
    //畸变体监控站
    public class ContainmentMonitorStationConfig : IBuildingConfig
    {
        public static readonly string ID = "ContainmentMonitorStation";

        public override BuildingDef CreateBuildingDef()
        {
            string anim = "containment_monitor_station_kanim"; // Replace with actual animation file
            int hitpoints = 100;
            float construction_time = 60f;
            float[] tier = TUNING.BUILDINGS.CONSTRUCTION_MASS_KG.TIER3; // Adjust cost tier
            string[] all_METALS = MATERIALS.ALL_METALS;
            float melting_point = 1600f;
            BuildLocationRule build_location_rule = BuildLocationRule.OnFloor;
            EffectorValues tier2 = NOISE_POLLUTION.NOISY.TIER2; // Adjust noise
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(ID, 6, 4, anim, hitpoints, construction_time, tier, all_METALS, melting_point, build_location_rule, TUNING.BUILDINGS.DECOR.PENALTY.TIER2, tier2, 0.2f);
            buildingDef.RequiresPowerInput = true;
            buildingDef.EnergyConsumptionWhenActive = 120;
            buildingDef.ExhaustKilowattsWhenActive = 0f;
            buildingDef.SelfHeatKilowattsWhenActive = 1f;
            buildingDef.ViewMode = OverlayModes.Rooms.ID;
            buildingDef.AudioCategory = "Metal";
            buildingDef.AudioSize = "large";
            buildingDef.BaseTimeUntilRepair = 0f;
            buildingDef.SceneLayer = Grid.SceneLayer.BuildingBack;
            buildingDef.ConstructionOffsetFilter = BuildingDef.ConstructionOffsetFilter_OneDown;
            buildingDef.ForegroundLayer = Grid.SceneLayer.BuildingFront;
            buildingDef.Overheatable = false;
            buildingDef.PermittedRotations = PermittedRotations.FlipH; // Adjust if needed
            buildingDef.UtilityInputOffset = new CellOffset(0, 0);
            buildingDef.UtilityOutputOffset = new CellOffset(0, 0);
            buildingDef.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(new CellOffset(1, 0));

            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            go.AddOrGet<LoopingSounds>();
            go.GetComponent<KPrefabID>().AddTag(MutanterTags.MutanterBuildings, false);

            var roomTracker = go.AddOrGet<RoomTracker>();
            roomTracker.requiredRoomType = ContainmentCharmberRoom.ROOMTYPE_ID;
            roomTracker.requirement = RoomTracker.Requirement.Required;

            // Add Operational Controller for power/logic input
            go.AddOrGet<LogicOperationalController>();

            go.AddOrGetDef<ContainmentMonitor.Def>();
            go.AddOrGet<ContainmentMonitorWorkable>();
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            // Add other necessary components after basic configure
            Prioritizable.AddRef(go);
            go.AddOrGetDef<StorageController.Def>();
            // Add SkillPerkMissingComplainer if needed
            // go.AddOrGet<SkillPerkMissingComplainer>().requiredSkillPerk = Db.Get().SkillPerks.NewPerk.Id;
        }
    }
}
