using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.PatchManager;

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
                .PlanAndTech(TbbTypes.PlanType.Equipment, "rs_transfer_port", "LiquidPiping")
                .AddBuilding(MutantFarmLabConfig.ID)
                .PlanAndTech(TbbTypes.PlanType.Equipment, "rs_transfer_port", "LiquidPiping")
                .AddBuilding(CustomRadiationLightConfig.ID)
                .PlanAndTech(TbbTypes.PlanType.Equipment, "rs_transfer_port", "LiquidPiping")
                .AddBuilding(DualLayerRadiationFarmConfig.ID)
                .PlanAndTech(TbbTypes.PlanType.Equipment, "rs_transfer_port", "LiquidPiping")
                .AddBuilding(TestRaConfig.ID)
                .PlanAndTech(TbbTypes.PlanType.Equipment, "rs_transfer_port", "LiquidPiping")
                .AddBuilding(RadiationParticleAdapterConfig.ID);

            //语言本地化
            TbbLocalization.Initialize(mod, harmony)
                .RegisterLoad(typeof(STRINGS))
                .RegisterAddStrings(typeof(STRINGS.BUILDINGS))
                .RegisterAddStrings(typeof(STRINGS.UI));
        }
    }
}
