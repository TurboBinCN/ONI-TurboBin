using HarmonyLib;
using KMod;
using MutantFarmLab.mutantplants;
using MutantFarmLab.tbbLibs;

namespace MutantFarmLab
{
    public class MutantFarmLabMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);

            if (KPrefabID.NextUniqueID <= 0)
                KPrefabID.NextUniqueID = 1;

            TbbDebuger.GlobalLogLevel = TbbDebuger.LogLevel.None;
            harmony.PatchAll();

            ManualDeliveryKGPatch.Patch(harmony);

            TbbBuilding.Initialize(mod, harmony)
                .ToAdvanced()
                .PlanAndTech(TbbTypes.PlanMenuCategory.Stations, TbbTypes.PlanMenuSubcategory.Farming, TbbTypes.Technology.Food.Bioengineering)
                .AddBuilding(MutantFarmLabConfig.ID)
                .PlanAndTech(TbbTypes.PlanMenuCategory.Radiation, TbbTypes.PlanMenuSubcategory.Producers, TbbTypes.Technology.RadiationTechnologies.MaterialsScienceResearch)
                .AddBuilding(CustomRadiationLightConfig.ID)
                .PlanAndTech(TbbTypes.PlanMenuCategory.Radiation, TbbTypes.PlanMenuSubcategory.Producers, TbbTypes.Technology.RadiationTechnologies.MaterialsScienceResearch)
                .AddBuilding(RadiationParticleAdapterConfig.ID)
                .PlanAndTech(TbbTypes.PlanType.Food, TbbTypes.PlanMenuSubcategory.Farming, TbbTypes.Technology.Food.FoodRepurposing)
                .AddBuilding(RadiationFarmTileConfig.ID);

            TbbLocalization.Initialize(mod, harmony)
                .RegisterLoad(typeof(STRINGS))
                .RegisterAddStrings(typeof(STRINGS.BUILDINGS))
                .RegisterAddStrings(typeof(STRINGS.UI));
        }
    }
    [HarmonyPatch(typeof(Db), nameof(Db.Initialize))]
    public static class DB_INIT_PATCH
    {
        [HarmonyPostfix]
        public static void Db_Initialize_Postfix(Db __instance)
        {
            try
            {
                PlantMutationRegister.RegisterAllCustomMutations();
                FoodEffectRegister.RegisterAllEffects();
                MutantEffects.RegisterAllEffect();
            }
            catch (System.Exception e)
            {
                TbbDebuger.LogError($"注册失败：{e.Message}\n{e.StackTrace}");
            }
        }
    }
}