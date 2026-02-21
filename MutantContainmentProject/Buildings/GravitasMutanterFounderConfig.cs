using TUNING;
using UnityEngine;
using static MutantContainmentProject.STRINGS;

namespace MutantContainmentProject.Buildings
{
    internal class GravitasMutanterFounderConfig : IBuildingConfig
    {
        public override BuildingDef CreateBuildingDef()
        {
            int width = 3;
            int height = 4;
            string anim = "gravitas_critter_manipulator_kanim";
            int hitpoints = 250;
            float construction_time = 120f;
            float[] tier = TUNING.BUILDINGS.CONSTRUCTION_MASS_KG.TIER5;
            string[] refined_METALS = MATERIALS.REFINED_METALS;
            float melting_point = 3200f;
            BuildLocationRule build_location_rule = BuildLocationRule.OnFloor;
            EffectorValues tier2 = NOISE_POLLUTION.NOISY.TIER5;
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(ID, width, height, anim, hitpoints, construction_time, tier, refined_METALS, melting_point, build_location_rule, TUNING.BUILDINGS.DECOR.BONUS.TIER2, tier2, 0.2f);

            buildingDef.ExhaustKilowattsWhenActive = 0f;
            buildingDef.SelfHeatKilowattsWhenActive = 0f;
            buildingDef.Floodable = false;
            buildingDef.Entombable = true;
            buildingDef.Overheatable = false;
            buildingDef.AudioCategory = "Metal";
            buildingDef.AudioSize = "medium";
            buildingDef.ForegroundLayer = Grid.SceneLayer.Ground;
            buildingDef.ShowInBuildMenu = false;
            return buildingDef;
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.GetComponent<KPrefabID>().AddTag(RoomConstraints.ConstraintTags.IndustrialMachinery, false);
            PrimaryElement component = go.GetComponent<PrimaryElement>();
            component.SetElement(SimHashes.Steel, true);
            component.Temperature = 294.15f;
            BuildingTemplates.ExtendBuildingToGravitas(go);

            go.AddComponent<Storage>();
            Activatable activatable = go.AddComponent<Activatable>();
            activatable.synchronizeAnims = false;
            activatable.overrideAnims = new KAnimFile[] { Assets.GetAnim("anim_use_remote_kanim") };
            activatable.SetWorkTime(30f);

            var def = go.AddOrGetDef<GravitasMutanterFounder.Def>();
            def.pickupOffset = new CellOffset(-1, 0);
            def.dropOffset = new CellOffset(1, 0);
            def.numSpeciesToUnlockMorphMode = 1;
            //def.workingDuration = 15f;
            //def.cooldownDuration = 540f;

            MakeBaseSolid.Def def2 = go.AddOrGetDef<MakeBaseSolid.Def>();
            def2.solidOffsets = new CellOffset[4];
            for (int i = 0; i < 4; i++)
            {
                def2.solidOffsets[i] = new CellOffset(0, i);
            }
            go.GetComponent<KPrefabID>().prefabInitFn += delegate (GameObject game_object)
            {
                game_object.GetComponent<Activatable>().SetOffsets(OffsetGroups.LeftOrRight);
            };
        }

        public const string ID = "GravitasMutanterFounder";

        public const string CODEX_ENTRY_ID = "STORYTRAITMUTANTERFOUNDER";

        public const string INITIAL_LORE_UNLOCK_ID = "story_trait_mutanter_founder_initial";

        public const string COMPLETED_LORE_UNLOCK_ID = "story_trait_mutanter_founder_complete";

        private const int HEIGHT = 4;
        public static Option<string> GetNameForSpeciesTag(Tag species)
        {
            StringEntry entry;
            if (!Strings.TryGet("STRINGS.CREATURES.FAMILY_PLURAL." + species.ToString().ToUpper(), out entry))
            {
                return Option.None;
            }
            return Option.Some<string>(entry);
        }
        public static Option<string> GetDescriptionForSpeciesTag(Tag species)
        {
            StringEntry entry;
            if (!Strings.TryGet("STRINGS.CODEX.STORY_TRAITS.MUTANTER_FOUNDER.SPECIES_ENTRIES." + species.ToString().ToUpper().Replace("SPECIES", ""), out entry))
            {
                return Option.None;
            }
            return Option.Some<string>(entry);
        }
        public static string GetBodyContent(string name, string desc)
        {
            return "<size=125%><b>" + name + "</b></size><line-height=150%>\n</line-height>" + desc;
        }
        public static Option<string> GetBodyContentForSpeciesTag(Tag species)
        {
            Option<string> nameForSpeciesTag = GetNameForSpeciesTag(species);
            Option<string> descriptionForSpeciesTag = GetDescriptionForSpeciesTag(species);
            if (nameForSpeciesTag.HasValue && descriptionForSpeciesTag.HasValue)
            {
                return GetBodyContent(nameForSpeciesTag.Value, descriptionForSpeciesTag.Value);
            }
            return Option.None;
        }
        public static string GetBodyContentForUnknownSpecies()
        {
            return GetBodyContent(CODEX.STORY_TRAITS.MUTANTER_FOUNDER.SPECIES_ENTRIES.UNKNOWN_TITLE, CODEX.STORY_TRAITS.MUTANTER_FOUNDER.SPECIES_ENTRIES.UNKNOWN);
        }
        public static class LORE_UNLOCK_ID
        {
            public static string For(Tag species)
            {
                return "story_trait_mutanter_founder_" + species.ToString().ToLower();
            }
        }
    }
}
