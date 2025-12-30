using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.UI;
using System;
using System.Xml.Linq;
using UnityEngine;

namespace MutantFarmLab
{
    public class MutantFarmLab : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);

            //// ✅ ========== 核心代码：Unity全局异常拦截（一行根治） ==========
            //Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.None); // 关闭异常堆栈打印
            //AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            //{
            //    var ex = e.ExceptionObject as Exception;
            //    if (ex != null && ex is InvalidCastException
            //        && ex.StackTrace.Contains("Database.BuildingStatusItems")
            //        && ex.StackTrace.Contains("StatusItem.ResolveString"))
            //    {
            //        // 吞噬目标异常，不抛出、不打印日志
            //        return;
            //    }
            //    // 其他异常正常抛出（不影响模组调试）
            //    Debug.LogError($"[其他异常]{ex?.Message}\n{ex?.StackTrace}");
            //};

            //Debug.Log("[你的模组名] ✅ 全局异常拦截已生效，点击建筑将无InvalidCastException报错！");


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
        //[PLibMethod(RunAt.OnDetailsScreenInit)]
        //private static void OnDetailsScreenInit()
        //{
        //    Debug.Log($"[MutantFarmLab]:Run OnDetailsScreenInit");
        //    PUIUtils.AddSideScreenContent<MutantFarmLabSideScreen>();
        //}
    }
}
