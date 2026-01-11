//using HarmonyLib;
//using System.Reflection;
//using UnityEngine;
//using System.Linq;
//using Klei.AI; // Add this for LINQ operations

//// --- 反射辅助类，用于访问私有字段 ---
//public static class FertilizationMonitorReflectionHelper
//{
//    private static FieldInfo _storageFieldInfo = null; // Changed name to be more generic

//    public static FieldInfo GetStorageFieldInfo()
//    {
//        if (_storageFieldInfo == null)
//        {
//            // Find all fields of type Storage in the Instance class
//            var fields = typeof(FertilizationMonitor.Instance).GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
//                                                              .Where(f => f.FieldType == typeof(Storage));
//            if (fields.Any())
//            {
//                // Assume the first one is the one we want. If there are multiple, you might need to refine this logic.
//                _storageFieldInfo = fields.First();
//                Debug.Log($"[FertMonitor Reflection Helper] Successfully found Storage field '{_storageFieldInfo.Name}' using reflection.");
//            }
//            else
//            {
//                Debug.LogError("[FertMonitor Reflection Helper] Could not find any private/public field of type 'Storage' in FertilizationMonitor.Instance using reflection.");
//            }
//        }
//        return _storageFieldInfo;
//    }

//    public static Storage GetStorage(FertilizationMonitor.Instance instance)
//    {
//        var field = GetStorageFieldInfo();
//        if (field != null)
//        {
//            return field.GetValue(instance) as Storage;
//        }
//        return null;
//    }
//}

//// 补丁类 1: 监控 Instance 构造函数 (使用反射查找的字段)
//[HarmonyPatch]
//public static class FertilizationMonitorInstanceCtorPatch
//{
//    static MethodBase TargetMethod()
//    {
//        var outerType = typeof(FertilizationMonitor);
//        var innerType = AccessTools.Inner(outerType, "Instance"); // Use AccessTools.Inner
//        if (innerType != null)
//        {
//            var ctor = innerType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
//                                                null, new System.Type[] { typeof(IStateMachineTarget), typeof(FertilizationMonitor.Def) }, null);
//            if (ctor != null)
//            {
//                return ctor;
//            }
//            else
//            {
//                Debug.LogError("[FertMonitor Ctor Patch] Could not find FertilizationMonitor.Instance constructor with expected signature (IStateMachineTarget, FertilizationMonitor.Def)");
//                var ctors = innerType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
//                Debug.Log($"[FertMonitor Ctor Patch] Found {ctors.Length} constructors on Instance class.");
//                foreach (var c in ctors)
//                {
//                    Debug.Log($"[FertMonitor Ctor Patch] Constructor candidate: {c}");
//                }
//            }
//        }
//        else
//        {
//            Debug.LogError("[FertMonitor Ctor Patch] Could not find nested class FertilizationMonitor+Instance");
//        }
//        return null;
//    }

//    [HarmonyPostfix]
//    public static void Postfix(FertilizationMonitor.Instance __instance, IStateMachineTarget master, FertilizationMonitor.Def def)
//    {
//        var storageRef = FertilizationMonitorReflectionHelper.GetStorage(__instance);
//        Debug.Log($"[FertMonitor Ctor] {__instance.gameObject.name} ({__instance.gameObject.PrefabID()}) FertilizationMonitor Instance CONSTRUCTED. Initial storage field (via reflection): {(object)storageRef?.name ?? "NULL"}. Initial state: {__instance.GetCurrentState()?.name ?? "UNKNOWN_STATE"}");
//    }
//}

//// 补丁类 2: 监控 UpdateFertilization 方法 (使用反射查找的字段)
//[HarmonyPatch(typeof(FertilizationMonitor.Instance), "UpdateFertilization")]
//public static class FertilizationMonitorUpdateFertPatch
//{
//    [HarmonyPrefix]
//    public static bool Prefix(FertilizationMonitor.Instance __instance, float dt)
//    {
//        var storageRef = FertilizationMonitorReflectionHelper.GetStorage(__instance);

//        Debug.Log($"[FertMonitor UpdateFert] {__instance.gameObject.name} ({__instance.gameObject.PrefabID()}) UpdateFertilization called with dt={dt:F4}. Storage field (via reflection): {(object)storageRef?.name ?? "NULL"}. Current state: {__instance.GetCurrentState()?.name ?? "UNKNOWN_STATE"}");

//        if (storageRef == null)
//        {
//            Debug.Log($"[FertMonitor UpdateFert] {__instance.gameObject.name}: storage (via reflection) is NULL inside UpdateFertilization. Returning early.");
//            return true; // Let original method execute (it will return early anyway)
//        }

//        var def = __instance.def;
//        if (def?.consumedElements == null || def.consumedElements.Length == 0)
//        {
//            Debug.Log($"[FertMonitor UpdateFert] {__instance.gameObject.name}: No consumedElements defined in Def.");
//            return true;
//        }

//        var items = storageRef.items;
//        Debug.Log($"[FertMonitor UpdateFert] {__instance.gameObject.name}: Storage (via reflection) contains {items.Count} items:");
//        foreach (var item in items)
//        {
//            if (item != null)
//            {
//                var primaryElement = item.GetComponent<PrimaryElement>();
//                Debug.Log($"  - Item: {item.name} (Prefab: {item.PrefabID()}), Tag: {primaryElement.ElementID}, Mass: {primaryElement.Mass}");
//            }
//            else
//            {
//                Debug.Log($"  - Item: NULL OBJECT in storage!");
//            }
//        }

//        Debug.Log($"[FertMonitor UpdateFert] {__instance.gameObject.name}: Plant requires {def.consumedElements.Length} types of fertilizer:");
//        foreach (var consumeInfo in def.consumedElements)
//        {
//            Debug.Log($"  - Required Tag: {consumeInfo.tag}, Base Rate: {consumeInfo.massConsumptionRate}");
//        }

//        // 获取属性 - 修正API调用
//        var choreConsumer = __instance.GetComponent<ChoreConsumer>();
//        if (choreConsumer == null)
//        {
//            Debug.LogWarning($"[FertMonitor UpdateFert] {__instance.gameObject.name}: No ChoreConsumer component found on the monitor's target. Cannot get FertilizerUsageMod attribute. Assuming default value (1.0f).");
//        }
//        else
//        {
//            // 在较新的ONI版本中，可能需要通过其他方式获取，或者该方法不存在。
//            // 我们尝试通过 Db.Get().Attributes 获取 AttributeInstance 并计算
//            try
//            {
//                var attributes = choreConsumer.GetAttributes();
//                var fertilizerUsageMod = attributes.Get(Db.Get().PlantAttributes.FertilizerUsageMod);
//                if (fertilizerUsageMod != null)
//                {
//                    var totalValue = fertilizerUsageMod.GetTotalValue();
//                    Debug.Log($"[FertMonitor UpdateFert] {__instance.gameObject.name}: FertilizerUsageMod Total Value: {totalValue}");
//                }
//                else
//                {
//                    Debug.LogWarning($"[FertMonitor UpdateFert] {__instance.gameObject.name}: Could not find FertilizerUsageMod attribute via choreConsumer.attributes.");
//                }
//            }
//            catch (System.Exception e)
//            {
//                Debug.LogWarning($"[FertMonitor UpdateFert] {__instance.gameObject.name}: Error accessing attributes: {e.Message}. Assuming default value (1.0f).");
//            }
//        }


//        bool hasCorrectFert = true;
//        bool hasIncorrectFert = false;
//        float total_available_mass_debug = 0f;

//        for (int i = 0; i < def.consumedElements.Length; i++)
//        {
//            var consumeInfo = def.consumedElements[i];
//            // Use 1.0f if choreConsumer was null or attribute access failed, otherwise use the calculated value
//            float modValue = 1.0f;
//            if (choreConsumer != null)
//            {
//                try
//                {
//                    var attributes = choreConsumer.GetAttributes();
//                    var fertilizerUsageMod = attributes.Get(Db.Get().PlantAttributes.FertilizerUsageMod);
//                    if (fertilizerUsageMod != null)
//                    {
//                        modValue = fertilizerUsageMod.GetTotalValue();
//                    }
//                }
//                catch
//                {
//                    // If getting attribute fails, keep modValue as 1.0f
//                }
//            }

//            float requiredRatePerDt = consumeInfo.massConsumptionRate * modValue * dt;
//            float availableMass = 0f;

//            Debug.Log($"[FertMonitor UpdateFert] {__instance.gameObject.name}: Checking requirement [{i}]: Tag={consumeInfo.tag}, ReqRatePerDt={requiredRatePerDt:F4} (Rate * Mod * Dt)");

//            foreach (var item in items)
//            {
//                if (item != null && item.HasTag(consumeInfo.tag))
//                {
//                    var mass = item.GetComponent<PrimaryElement>().Mass;
//                    availableMass += mass;
//                    Debug.Log($"    -> Found matching item: {item.name}, Mass added: {mass}, Current Available: {availableMass}");
//                }
//                else if (item != null && item.HasTag(def.wrongFertilizerTestTag))
//                {
//                    Debug.Log($"    -> Found WRONG fertilizer item: {item.name}");
//                    hasIncorrectFert = true;
//                }
//            }

//            total_available_mass_debug = availableMass;
//            Debug.Log($"[FertMonitor UpdateFert] {__instance.gameObject.name}: Requirement [{i}] - Available Mass: {availableMass:F4}, Required: {requiredRatePerDt:F4}");

//            if (availableMass < requiredRatePerDt)
//            {
//                Debug.Log($"[FertMonitor UpdateFert] {__instance.gameObject.name}: Requirement [{i}] - FAILED. Available < Required.");
//                hasCorrectFert = false;
//                break;
//            }
//            else
//            {
//                Debug.Log($"[FertMonitor UpdateFert] {__instance.gameObject.name}: Requirement [{i}] - PASSED. Available >= Required.");
//            }
//        }

//        Debug.Log($"[FertMonitor UpdateFert] {__instance.gameObject.name}: Calculated hasCorrectFert: {hasCorrectFert}, hasIncorrectFert: {hasIncorrectFert}");
//        Debug.Log($"[FertMonitor UpdateFert] {__instance.gameObject.name} UpdateFertilization END ---");

//        // Let the original method run to set the parameters
//        return true;
//    }
//}

//// 补丁类 3: 监控 SetStorage 方法 (修正参数类型和 obj.name 访问)
//[HarmonyPatch(typeof(FertilizationMonitor.Instance), "SetStorage")]
//public static class FertilizationMonitorSetStoragePatch
//{
//    [HarmonyPostfix]
//    public static void Postfix(FertilizationMonitor.Instance __instance, object obj)
//    {
//        // 使用反射获取当前私有字段值
//        var currentStorageRef = FertilizationMonitorReflectionHelper.GetStorage(__instance);
//        // 安全地获取 obj 的信息
//        string objName = "NULL";
//        string objType = "NULL";
//        if (obj != null)
//        {
//            objName = (obj as KMonoBehaviour)?.name ?? obj.ToString(); // Try to get name from MonoBehaviour base, otherwise use ToString()
//            objType = obj.GetType().Name;
//        }

//        // 安全地获取 currentStorageRef 的信息
//        string currentStorageName = currentStorageRef?.name ?? "NULL";

//        Debug.Log($"[FertMonitor SetStorage] {__instance.gameObject.name} ({__instance.gameObject.PrefabID()}) SetStorage called. Parameter obj: {objName} ({objType}). Storage field (via reflection) now: {currentStorageName}. Sm.fertilizerStorage: {(object)__instance.sm.fertilizerStorage.Get(__instance)?.name ?? "NULL"}. Current state: {__instance.GetCurrentState()?.name ?? "UNKNOWN_STATE"}");
//    }
//}

//// --- 注意：已移除 FertilizationMonitorOnStorageChangePatch 和 FertilizationMonitorStateChangePatch ---