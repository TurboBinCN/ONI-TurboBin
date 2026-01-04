using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.PatchManager;
using UnityEngine;

namespace MutantFarmLab
{
    public class MutantFarmLabMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);

            PUtil.InitLibrary();
            new PPatchManager(harmony).RegisterPatchClass(typeof(MutantFarmLabMod));

            //建筑注册
            TbbBuilding.Initialize(mod, harmony)
                .ToAdvanced()
                //变异试验台
                .PlanAndTech(TbbTypes.PlanMenuCategory.Stations, TbbTypes.PlanMenuSubcategory.Decor, TbbTypes.Technology.Food.Bioengineering)
                .AddBuilding(MutantFarmLabConfig.ID)
                //辐射灯带
                .PlanAndTech(TbbTypes.PlanMenuCategory.Radiation, TbbTypes.PlanMenuSubcategory.Producers, TbbTypes.Technology.RadiationTechnologies.MaterialsScienceResearch)
                .AddBuilding(CustomRadiationLightConfig.ID)
                //种植砖辐射配件
                .PlanAndTech(TbbTypes.PlanMenuCategory.Radiation, TbbTypes.PlanMenuSubcategory.Producers, TbbTypes.Technology.RadiationTechnologies.MaterialsScienceResearch)
                .AddBuilding(RadiationParticleAdapterConfig.ID)
                //辐射种植砖
                .PlanAndTech(TbbTypes.PlanType.Food, TbbTypes.PlanMenuSubcategory.Farming, TbbTypes.Technology.Food.FoodRepurposing)
                .AddBuilding(RadiationFarmTileConfig.ID);


            //语言本地化
            TbbLocalization.Initialize(mod, harmony)
                .RegisterLoad(typeof(STRINGS))
                .RegisterAddStrings(typeof(STRINGS.BUILDINGS))
                .RegisterAddStrings(typeof(STRINGS.UI));
        }
    }
}
