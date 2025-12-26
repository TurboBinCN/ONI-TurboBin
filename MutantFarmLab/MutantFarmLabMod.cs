using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.UI;
using System.Xml.Linq;

namespace MutantFarmLab
{
    public class MutantFarmLab : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new PPatchManager(harmony).RegisterPatchClass(typeof(MutantFarmLab));
            //语言本地化
            TbbLocalization.Initialize(mod, harmony)
                .RegisterLoad(typeof(STRINGS))
                .RegisterAddStrings(typeof(STRINGS.BUILDINGS))
                .RegisterAddStrings(typeof(STRINGS.UI.UISIDESCREENS));
            //建筑注册
            TbbBuilding.Initialize(mod,harmony)
                .ToAdvanced()
                .PlanAndTech(TbbTypes.PlanType.Equipment, "rs_transfer_port", "LiquidPiping")
                .AddBuilding(MutantFarmLabConfig.ID);
        }
        [PLibMethod(RunAt.OnDetailsScreenInit)]
        private static void OnDetailsScreenInit()
        {
            Debug.Log($"[MutantFarmLab]:Run OnDetailsScreenInit");
            PUIUtils.AddSideScreenContent<MutantFarmLabSideScreen>();
        }
    }
}
