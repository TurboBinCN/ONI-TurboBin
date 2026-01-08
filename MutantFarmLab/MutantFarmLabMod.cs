using HarmonyLib;
using KMod;
using MutantFarmLab.mutantplants;
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

            // 确保 KPrefabID 从 1 开始分配
            if (KPrefabID.NextUniqueID <= 0)
                KPrefabID.NextUniqueID = 1;
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
    /// <summary>
    /// 指明要钩住的原生方法：Database.Db.Initialize()
    /// </summary>
    [HarmonyPatch(typeof(Db), nameof(Db.Initialize))]
    public static class DB_INIT_PATCH {
        [HarmonyPostfix] // ✅ 关键：Postfix → 在原生方法执行【之后】运行
        public static void Db_Initialize_Postfix(Db __instance)
        {
            PUtil.LogDebug("==============原生Db.Initialize()执行完毕！===============");

            try
            {
                PlantMutationRegister.RegisterAllCustomMutations();
                FoodEffectRegister.RegisterAllEffects();
                MutantEffects.RegisterAllEffect();

                PUtil.LogDebug("============== 自定义变异+物品 注册全部成功！===============");
            }
            catch (System.Exception e)
            {
                PUtil.LogError($"注册失败：{e.Message}\n{e.StackTrace}");
            }
        }
    }
}
