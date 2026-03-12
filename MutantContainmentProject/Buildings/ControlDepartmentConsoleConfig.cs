using System.Collections.Generic;
using TUNING;
using UnityEngine;

namespace MutantContainmentProject.Buildings
{
    public class ControlDepartmentConsoleConfig : IBuildingConfig
    {
        public const string ID = "ControlDepartmentConsole";
        public const string OUTPUT_LOGIC_PORT_ID = "CONTROL_STATUS_PORT";

        public override BuildingDef CreateBuildingDef()
        {
            float[] tieR4 = TUNING.BUILDINGS.CONSTRUCTION_MASS_KG.TIER4;
            string[] refinedMetals = TUNING.MATERIALS.REFINED_METALS;
            EffectorValues none1 = NOISE_POLLUTION.NONE;
            EffectorValues none2 = TUNING.BUILDINGS.DECOR.NONE;
            EffectorValues noise = none1;
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(ID, 3, 3, "geoTuner_kanim", 30, 120f, tieR4, refinedMetals, 2400f, BuildLocationRule.OnFloor, none2, noise);
            buildingDef.Floodable = true;
            buildingDef.Entombable = true;
            buildingDef.Overheatable = false;
            buildingDef.ObjectLayer = ObjectLayer.Building;
            buildingDef.SceneLayer = Grid.SceneLayer.Building;
            buildingDef.AudioCategory = "Metal";
            buildingDef.AudioSize = "medium";
            buildingDef.PermittedRotations = PermittedRotations.FlipH;
            buildingDef.UseStructureTemperature = true;
            buildingDef.LogicOutputPorts = new List<LogicPorts.Port>()
            {
                LogicPorts.Port.OutputPort((HashedString)OUTPUT_LOGIC_PORT_ID, new CellOffset(-1, 1), (string)STRINGS.BUILDINGS.PREFABS.CONTROLDEPARTMENTCONSOLE.LOGIC_PORT, (string)STRINGS.BUILDINGS.PREFABS.CONTROLDEPARTMENTCONSOLE.LOGIC_PORT_ACTIVE, (string)STRINGS.BUILDINGS.PREFABS.CONTROLDEPARTMENTCONSOLE.LOGIC_PORT_INACTIVE)
            };
            buildingDef.RequiresPowerInput = true;
            buildingDef.ViewMode = OverlayModes.Power.ID;
            buildingDef.EnergyConsumptionWhenActive = 240f;
            buildingDef.ExhaustKilowattsWhenActive = 0.5f;
            buildingDef.SelfHeatKilowattsWhenActive = 4f;
            return buildingDef;
        }

        public Tag REQUIRED_MATERIAL = GameTags.RefinedMetal;
        public const float MATERIAL_QUANTITY = 100f;

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            go.GetComponent<KPrefabID>().AddTag(RoomConstraints.ConstraintTags.ScienceBuilding);
            Storage storage = go.AddOrGet<Storage>();
            storage.capacityKg = MATERIAL_QUANTITY;
            List<Storage.StoredItemModifier> modifiers = new List<Storage.StoredItemModifier>()
            {
                Storage.StoredItemModifier.Hide,
                Storage.StoredItemModifier.Seal,
                Storage.StoredItemModifier.Insulate,
                Storage.StoredItemModifier.Preserve
            };
            storage.SetDefaultStoredItemModifiers(modifiers);
            storage.storageFilters = new List<Tag>() { REQUIRED_MATERIAL };
            ManualDeliveryKG manualDeliveryKg = go.AddOrGet<ManualDeliveryKG>();
            manualDeliveryKg.choreTypeIDHash = Db.Get().ChoreTypes.ResearchFetch.IdHash;
            manualDeliveryKg.capacity = MATERIAL_QUANTITY;
            manualDeliveryKg.refillMass = MATERIAL_QUANTITY;
            manualDeliveryKg.MinimumMass = MATERIAL_QUANTITY;
            manualDeliveryKg.RequestedItemTag = REQUIRED_MATERIAL;
            manualDeliveryKg.SetStorage(storage);
            go.AddOrGet<ControlDepartmentConsoleWorkable>();
            go.AddOrGet<CopyBuildingSettings>();
            ControlDepartmentConsole.Def def = go.AddOrGetDef<ControlDepartmentConsole.Def>();
            def.OUTPUT_LOGIC_PORT_ID = OUTPUT_LOGIC_PORT_ID;
            def.requiredMaterial = REQUIRED_MATERIAL;
            def.materialQuantity = MATERIAL_QUANTITY;
            RoomTracker roomTracker = go.AddOrGet<RoomTracker>();
            roomTracker.requiredRoomType = MutantContainmentProject.Room.DepartmentRoom.ROOMTYPE_ID;
            roomTracker.requirement = RoomTracker.Requirement.Required;
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
        }
    }
}