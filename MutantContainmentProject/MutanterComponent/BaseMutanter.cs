using Klei.AI;
using MutantContainmentProject.Buildings;
using System.Collections.Generic;
using TbbLib.UI;
using TUNING;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    public enum MutanterDangerLevel
    {
        ZAYIN = 1,//ZAYIN级（低危）
        TETH = 2,//TETH级（低危）
        HE = 3,//HE级（中危）
        WAW = 4,//WAW级（高危）
        ALEPH = 5//ALEPH级（灾难级）

    }
    public class BaseMutanter
    {
        public static GameObject ExtendMutanterToDangerLevel(GameObject template, MutanterDangerLevel dangerLevel)
        {
            //TODO 收容modifiers
            //TODO 收容监控
            //TODO 攻击行为
            template.AddOrGetDef<MutanterSecurableMonitor.Def>();

            return template;
        }
        public static GameObject ExtendMutanterMove(GameObject template, string NavGridName = "WalkerNavGrid1x1", NavType navType = NavType.Floor, int max_probing_radius = 32, float moveSpeed = 2f)
        {
            //需要各个畸变体自己设置 移动方式
            KPrefabID component = template.GetComponent<KPrefabID>();
            component.AddTag(GameTags.Creatures.Walker, false);
            component.prefabInitFn += delegate (GameObject inst)
            {
                inst.GetAttributes().Add(Db.Get().Attributes.MaxUnderwaterTravelCost);
            };

            Navigator navigator = template.AddOrGet<Navigator>();
            navigator.NavGridName = NavGridName;
            navigator.CurrentNavType = NavType.Floor;
            navigator.defaultSpeed = moveSpeed;
            navigator.updateProber = true;
            navigator.maxProbeRadiusX = max_probing_radius;
            navigator.maxProbeRadiusY = max_probing_radius;
            navigator.sceneLayer = Grid.SceneLayer.Creatures;

            return template;
        }
        public static GameObject ExtendToBaseMutanter(GameObject template)
        {
            var emotionMonitor = template.AddOrGetDef<EmotionMonitor.Def>();
            emotionMonitor.considerDecor = true;
            emotionMonitor.considerPlantFactors = true;
            emotionMonitor.considerEnvironmentalFactors = true;

            template.AddOrGetDef<MutanterStateMachine.Def>();

            return template;
        }
        public static GameObject ExtendTraitsToBaseMutanter(GameObject template, string[] traits)
        {
            Modifiers modifiers = template.AddOrGet<Modifiers>();
            if (traits != null)
            {
                foreach (var trait in traits)
                {
                    modifiers.initialTraits.Add(trait);
                }
            }

            modifiers.initialAmounts.Add(Db.Get().Amounts.HitPoints.Id);
            modifiers.initialAmounts.Add(Db.Get().Amounts.CritterTemperature.Id);

            return template;
        }
        public static GameObject ExtendThreatToBaseMutanter(GameObject template,bool considerDecor = true)
        {
            //template.AddOrGetDef<ThreatMonitor.Def>().fleethresholdState = Health.HealthState.Dead;
            //template.AddWeapon(1f, 1f, AttackProperties.DamageType.Standard, AttackProperties.TargetType.Single, 1, 0f);
            template.AddOrGet<MutanterAttackBehaviors>();
            var EmotionMonitorDef = template.AddOrGetDef<EmotionMonitor.Def>();
            if(considerDecor){ 
                template.AddOrGetDef<CreatureDecorMonitor.Def>();
                EmotionMonitorDef.considerDecor = true;
            }
            template.AddOrGetDef<MutanterStateMachine.Def>();
            template.AddOrGet<TbbRangeVisualizer>();

            return template;
        }
        public static GameObject ExtendEntityToBasicCreature(bool isWarmBlooded, GameObject template, string anim_filename, string build_filename = null, string symbol_override_prefix = null, FactionManager.FactionID faction = FactionManager.FactionID.Prey, float warningLowTemperature = 283.15f, float warningHighTemperature = 293.15f, float lethalLowTemperature = 243.15f, float lethalHighTemperature = 343.15f)
        {
            List<KAnimFile> list = new List<KAnimFile>();
            KAnimFile kAnimFile = ((anim_filename != null) ? Assets.GetAnim(anim_filename) : null);
            KAnimFile kAnimFile2 = ((build_filename != null) ? Assets.GetAnim(build_filename) : null);
            list.Add(kAnimFile2);
            list.Add(kAnimFile);
            KBatchedAnimController component = template.GetComponent<KBatchedAnimController>();
            component.isMovable = true;
            if (kAnimFile2 != null)
            {
                component.AnimFiles = list.ToArray();
            }

            template.AddOrGet<KPrefabID>().AddTag(GameTags.Creature);
            template.AddOrGet<KPrefabID>().AddTag(MutanterTags.Mutanter);

            Pickupable pickupable = template.AddOrGet<Pickupable>();
            int sortOrder = -1;
            string name = template.PrefabID().Name;
            if (TUNING.CREATURES.SORTING.CRITTER_ORDER.ContainsKey(name))
            {
                sortOrder = TUNING.CREATURES.SORTING.CRITTER_ORDER[name];
            }

            pickupable.sortOrder = sortOrder;
            template.AddOrGet<Clearable>().isClearable = false;
            template.AddOrGet<Traits>();
            //收容逻辑：暴动后收容逻辑，被攻击、生命扣减为0，封包
            template.AddOrGet<Health>().isCritter = true;
            template.AddOrGet<RangedAttackable>();
            EntityTemplates.CreateAndRegisterBaggedCreature(template, true, false, false);
            template.AddOrGetDef<DefeatStates.Def>();


            template.AddOrGet<CharacterOverlay>();
            template.AddOrGet<FactionAlignment>().Alignment = faction;
            template.AddOrGet<Prioritizable>();
            template.AddOrGet<Effects>();
            template.AddOrGetDef<CritterEmoteMonitor.Def>();//TODO 可能需要针对畸变体修正
            template.AddOrGetDef<CreatureDebugGoToMonitor.Def>();
            template.AddOrGetDef<CreatureThoughtGraph.Def>();
            template.AddOrGetDef<AnimInterruptMonitor.Def>();
            template.AddOrGet<AnimEventHandler>();
            SymbolOverrideController symbol_override_controller = SymbolOverrideControllerUtil.AddToPrefab(template);
            if (symbol_override_prefix != null && kAnimFile != null)
            {
                symbol_override_controller.ApplySymbolOverridesByAffix((kAnimFile2 == null) ? kAnimFile : kAnimFile2, symbol_override_prefix);
            }

            CritterTemperatureMonitor.Def def = template.AddOrGetDef<CritterTemperatureMonitor.Def>();
            def.temperatureHotDeadly = lethalHighTemperature;
            def.temperatureHotUncomfortable = warningHighTemperature;
            def.temperatureColdDeadly = lethalLowTemperature;
            def.temperatureColdUncomfortable = warningLowTemperature;
            template.GetComponent<PrimaryElement>().Temperature = def.GetIdealTemperature();

            if (isWarmBlooded)
            {
                string properName = template.GetProperName();
                template.UpdateComponentRequirement<SimTemperatureTransfer>(required: false);
                CreatureSimTemperatureTransfer creatureSimTemperatureTransfer = template.AddOrGet<CreatureSimTemperatureTransfer>();
                creatureSimTemperatureTransfer.temperatureAttributeName = "MutanterTemperature";
                creatureSimTemperatureTransfer.SurfaceArea = 17.5f;
                creatureSimTemperatureTransfer.Thickness = 0.025f;
                creatureSimTemperatureTransfer.GroundTransferScale = 0f;
                creatureSimTemperatureTransfer.skinThickness = 0.025f;
                creatureSimTemperatureTransfer.skinThicknessAttributeModifierName = properName;
                WarmBlooded warmBlooded = template.AddOrGet<WarmBlooded>();
                warmBlooded.TemperatureAmountName = "MutanterTemperature";
                warmBlooded.complexity = WarmBlooded.ComplexityType.SimpleHeatProduction;
                warmBlooded.IdealTemperature = def.GetIdealTemperature();
                warmBlooded.BaseGenerationKW = 10f;
                warmBlooded.BaseTemperatureModifierDescription = properName;
            }


            template.GetComponent<KPrefabID>().prefabSpawnFn += delegate (GameObject inst)
            {
                inst.GetComponent<KBatchedAnimController>().SetSymbolVisiblity("snapto_pivot", is_visible: false);
            };
            return template;
        }

        public static GameObject BaseGameObject(string id, string name, string desc, string anim_file, string symbol_override_prefix, float warnLowTemp, float warnHighTemp, float lethalLowTemp, float lethalHighTemp)
        {

            float mass = 50f;
            EffectorValues tier = DECOR.BONUS.TIER0;
            KAnimFile anim = Assets.GetAnim(anim_file);
            string initialAnim = "idle_loop";

            Grid.SceneLayer sceneLayer = Grid.SceneLayer.Creatures;
            int width = 1;
            int height = 1;
            EffectorValues decor = tier;

            float defaultTemperature = (warnLowTemp + warnHighTemp) / 2f;
            GameObject gameObject = EntityTemplates.CreatePlacedEntity(id, name, desc, mass, anim, initialAnim, sceneLayer, width, height, decor, default(EffectorValues), SimHashes.Creature, null, defaultTemperature);

            ExtendEntityToBasicCreature(false, gameObject, anim_file, "chameleo_build_kanim", null, FactionManager.FactionID.Pest, warnLowTemp, warnHighTemp, lethalLowTemp, lethalHighTemp);
            if (!string.IsNullOrEmpty(symbol_override_prefix))
            {
                gameObject.AddOrGet<SymbolOverrideController>().ApplySymbolOverridesByAffix(Assets.GetAnim(anim_file), symbol_override_prefix, null, 0);
            }
            Pickupable pickupable = gameObject.AddOrGet<Pickupable>();
            int sortOrder = TUNING.CREATURES.SORTING.CRITTER_ORDER["PrehistoricPacu"];
            pickupable.sortOrder = sortOrder;

            gameObject.AddOrGetDef<CreatureFallMonitor.Def>();
            gameObject.AddOrGet<LoopingSounds>();

            ChoreTable.Builder chore_table = new ChoreTable.Builder()
                .Add(new DeathStates.Def(), true, -1).Add(new AnimInterruptStates.Def(), true, -1)
                .Add(new BaggedStates.Def(), true, -1)
                .Add(new FallStates.Def(), true, -1).Add(new StunnedStates.Def(), true, -1)
                .Add(new DrowningStates.Def(), true, -1).Add(new DebugGoToStates.Def(), true, -1)
                .Add(new AttackStates.Def("eat_pre", "eat_pst", null), false, -1).PushInterruptGroup()
                .Add(new FixedCaptureStates.Def(), true, -1).Add(new RanchedStates.Def(), false, -1)
                .Add(new EatStates.Def(), true, -1)
                .Add(new PlayAnimsStates.Def(GameTags.Creatures.Poop, false, "poop", global::STRINGS.CREATURES.STATUSITEMS.EXPELLING_SOLID.NAME, global::STRINGS.CREATURES.STATUSITEMS.EXPELLING_SOLID.TOOLTIP), true, -1)
                .Add(new CritterEmoteStates.Def(Assets.GetAnim("chameleo_emotes_kanim")), true, -1).PopInterruptGroup()
                .Add(new CreatureSleepStates.Def(), true, -1).Add(new IdleStates.Def(), true, -1);
            //AddMutanterBrain(gameObject, chore_table, MutanterTags.Mutanters.Species.SCP173, symbol_override_prefix);
            EntityTemplates.AddCreatureBrain(gameObject, chore_table, MutanterTags.Mutanters.Species.SCP173, symbol_override_prefix);
            return gameObject;
        }
        public static void AddMutanterBrain(GameObject prefab, ChoreTable.Builder chore_table, Tag species, string symbol_prefix)
        {
            MutanterBrain brain = prefab.AddOrGet<MutanterBrain>();
            brain.species = species;
            brain.symbolPrefix = symbol_prefix;

            ChoreConsumer chore_consumer = prefab.AddOrGet<ChoreConsumer>();
            chore_consumer.choreTable = chore_table.CreateTable();
            KPrefabID kPrefabID = prefab.AddOrGet<KPrefabID>();
            kPrefabID.AddTag(MutanterTags.MutanterBrain);
            kPrefabID.instantiateFn += delegate (GameObject go)
            {
                go.GetComponent<ChoreConsumer>().choreTable = chore_consumer.choreTable;
            };
            kPrefabID.prefabSpawnFn += delegate (GameObject go)
            {
                Game.BrainScheduler.PrioritizeBrain(go.GetComponent<MutanterBrain>());
            };
        }
    }
}
