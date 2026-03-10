using System.Collections.Generic;
using TUNING;
using UnityEngine;

namespace MutantContainmentProject.Buildings
{
    public class GlobalErosionPlanelConfig : IBuildingConfig
    {
        public const string ID = "GlobalErosionPlanel";

        public void OnSpawn(GameObject inst)
        {
        }

        public override BuildingDef CreateBuildingDef()
        {
            string anim = "global_erosion_panel_kanim";
            int hitpoints = 250;
            float construction_time = 120f;
            float[] tier = TUNING.BUILDINGS.CONSTRUCTION_MASS_KG.TIER0;
            string[] refined_METALS = MATERIALS.REFINED_METALS;
            float melting_point = 3200f;
            BuildLocationRule build_location_rule = BuildLocationRule.OnFloor;
            EffectorValues tier2 = NOISE_POLLUTION.NOISY.TIER0;
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(ID, 1, 4, anim, hitpoints, construction_time, tier, refined_METALS, melting_point, build_location_rule, TUNING.BUILDINGS.DECOR.BONUS.TIER2, tier2, 0.2f);

            buildingDef.ExhaustKilowattsWhenActive = 0f;
            buildingDef.SelfHeatKilowattsWhenActive = 0f;
            buildingDef.Floodable = false;
            buildingDef.Entombable = true;
            buildingDef.Overheatable = false;
            buildingDef.AudioCategory = "Metal";
            buildingDef.AudioSize = "medium";
            buildingDef.SceneLayer = Grid.SceneLayer.BuildingBack;
            buildingDef.ForegroundLayer = Grid.SceneLayer.BuildingFront;
            buildingDef.ShowInBuildMenu = true;
            return buildingDef;
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<Demolishable>();
            go.AddOrGet<GlobalErosionPlanel>();
        }
    }
}