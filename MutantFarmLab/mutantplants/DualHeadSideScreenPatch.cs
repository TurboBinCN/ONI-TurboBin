using HarmonyLib;
using PeterHan.PLib.Core;
using UnityEngine;

namespace MutantFarmLab.patches
{
    [HarmonyPatch(typeof(PlanterSideScreen))]
    public static class DualHeadSideScreen_Patch
    {
        private static DualHeadSideScreen _sideScreen;

        [HarmonyPatch(nameof(PlanterSideScreen.SetTarget))]
        [HarmonyPostfix]
        public static void OnPlanterSideScreenOpen(PlanterSideScreen __instance, GameObject target)
        {
            if (_sideScreen == null)
            {
                GameObject extObj = new GameObject("DualHeadSideScreen_Instance");
                _sideScreen = extObj.AddComponent<DualHeadSideScreen>();
            }
            _sideScreen.Init(target, __instance.gameObject);

            // ✅ 新增：延迟刷新，保证UI加载完成
            __instance.StartCoroutine(DelayRefresh(_sideScreen));
        }

        // ✅ 协程延迟刷新，解决UI加载时序问题
        private static System.Collections.IEnumerator DelayRefresh(DualHeadSideScreen screen)
        {
            yield return new WaitForEndOfFrame();
            screen.Refresh();
        }
    }
}