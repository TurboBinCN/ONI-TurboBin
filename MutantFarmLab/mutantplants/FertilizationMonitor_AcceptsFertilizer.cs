//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace MutantFarmLab.mutantplants
//{
//    // --- 假设你有一个 Harmony Patch 类 ---
//    using HarmonyLib;
//    using PeterHan.PLib.Core;
//    using System.Reflection;
//    using UnityEngine;

//    [HarmonyPatch]
//    public class FertilizationMonitor_AcceptsFertilizer_DebugPatch
//    {
//        // 定位到 FertilizationMonitor.Instance.AcceptsFertilizer 方法
//        [HarmonyTargetMethod]
//        public static MethodBase TargetMethod()
//        {
//            // 注意：这里的类型名和方法名需要与实际游戏代码匹配
//            // 假设 FertilizationMonitor.Instance 类名为 FertilizationMonitor+Instance
//            var instanceType = typeof(FertilizationMonitor).GetNestedType("Instance", BindingFlags.Public | BindingFlags.NonPublic);
//            if (instanceType != null)
//            {
//                return AccessTools.Method(instanceType, "AcceptsFertilizer");
//            }
//            else
//            {
//                // 如果嵌套类型查找失败，尝试其他方式或抛出异常
//                Debug.LogError("Could not find FertilizationMonitor+Instance type.");
//                return null;
//            }
//        }

//        // 使用 Prefix 替换原方法
//        [HarmonyPrefix]
//        public static bool Prefix(FertilizationMonitor.Instance __instance)
//        {
//            // --- 开始调试逻辑 ---

//            // 1. 获取存储对象 (GameObject)
//            GameObject storageGameObject = __instance.sm.fertilizerStorage.Get(__instance);

//            // 记录存储对象的信息
//            string storageName = storageGameObject != null ? storageGameObject.name : "NULL";
//            PUtil.LogDebug($"[FertilizationMonito AcceptsFertilizer] Instance: {__instance.gameObject.name}, Storage Object: {storageName}");

//            // 2. 尝试从该 GameObject 上获取 PlantablePlot 组件
//            PlantablePlot component = storageGameObject?.GetComponent<PlantablePlot>();

//            // 记录组件获取结果
//            string componentInfo = component != null ? $"Found on {component.gameObject.name}" : "NOT FOUND";
//            PUtil.LogDebug($"[FertilizationMonito AcceptsFertilizer] PlantablePlot Component: {componentInfo}");

//            // 3. 检查组件是否存在且其 AcceptsFertilizer 属性
//            bool acceptsFertResult = false;
//            if (component != null)
//            {
//                bool acceptsValue = component.AcceptsFertilizer;
//                PUtil.LogDebug($"[FertilizationMonito AcceptsFertilizer] PlantablePlot.AcceptsFertilizer Value: {acceptsValue}");
//                acceptsFertResult = acceptsValue;
//            }
//            else
//            {
//                PUtil.LogDebug($"[FertilizationMonito AcceptsFertilizer] Cannot check AcceptsFertilizer, PlantablePlot component is NULL.");
//            }

//            // 记录最终返回值
//            PUtil.LogDebug($"[FertilizationMonito AcceptsFertilizer] Final AcceptsFertilizer Result: {acceptsFertResult}");

//            // --- 结束调试逻辑 ---

//            // 返回计算出的结果，这模拟了原方法的行为
//            // 由于原方法是 virtual，且我们使用 Prefix，
//            // 我们需要在这里完成所有逻辑并返回结果，不继续执行原方法。
//            return acceptsFertResult;
//        }
//    }
//    // --- End of Patch Class ---

//    // --- 在你的 Mod 主类的 OnLoad 或类似初始化方法中应用 Patch ---
//    // harmonyInstance.PatchAll(typeof(FertilizationMonitor_AcceptsFertilizer_DebugPatch));
//}
