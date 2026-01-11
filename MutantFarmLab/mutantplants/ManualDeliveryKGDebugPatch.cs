//using HarmonyLib;
//using System.Reflection;
//using PeterHan.PLib.Core; // 确保你有 PLib 并导入
//using System.Collections.Generic; // For Dictionary
//using UnityEngine; // For Mathf

//namespace MutantFarmLab.mutantplants
//{
//    [HarmonyPatch(typeof(ManualDeliveryKG))]
//    public static class ManualDeliveryKGDebugPatch
//    {
//        [HarmonyPatch(nameof(ManualDeliveryKG.Sim1000ms))]
//        [HarmonyPrefix]
//        public static void Sim1000ms_Prefix(ManualDeliveryKG __instance, float dt)
//        {
//            // 记录 Sim1000ms 被调用
//            PUtil.LogDebug($"[ManualDeliveryKG Debug] Sim1000ms called for {__instance.name}. Time: {dt}");
//        }

//        [HarmonyPatch(nameof(ManualDeliveryKG.UpdateDeliveryState))]
//        [HarmonyPrefix]
//        public static void UpdateDeliveryState_Prefix(ManualDeliveryKG __instance)
//        {
//            PUtil.LogDebug($"[ManualDeliveryKG Debug] UpdateDeliveryState called for {__instance.name}.");
//            // Access private fields using reflection or properties if available
//            var operationalField = typeof(ManualDeliveryKG).GetField("operational", BindingFlags.NonPublic | BindingFlags.Instance);
//            var storageField = typeof(ManualDeliveryKG).GetField("storage", BindingFlags.NonPublic | BindingFlags.Instance);
//            var operationalRequirementField = typeof(ManualDeliveryKG).GetField("operationalRequirement", BindingFlags.NonPublic | BindingFlags.Instance);
//            var pausedField = typeof(ManualDeliveryKG).GetField("paused", BindingFlags.NonPublic | BindingFlags.Instance);

//            var operational = operationalField?.GetValue(__instance) as Operational;
//            var storage = storageField?.GetValue(__instance) as Storage;
//            var operationalRequirement = (Operational.State)(operationalRequirementField?.GetValue(__instance) ?? Operational.State.None);
//            var paused = (bool)(pausedField?.GetValue(__instance) ?? false);

//            PUtil.LogDebug($"  - paused (via reflection): {paused}");
//            PUtil.LogDebug($"  - operationalRequirement (via reflection): {operationalRequirement}");
//            PUtil.LogDebug($"  - operational meets requirement (via reflection): {(operational != null ? operational.MeetsRequirements(operationalRequirement) : "operational is null")}");
//            PUtil.LogDebug($"  - storage (via reflection): {(storage != null ? storage.name : "null")}");
//            PUtil.LogDebug($"  - requestedItemTag (property): {__instance.RequestedItemTag}");
//            PUtil.LogDebug($"  - fetchList (property): {(__instance.DebugFetchList != null ? "exists and " + (__instance.DebugFetchList.IsComplete ? "complete" : "incomplete") : "null")}");
//            if (storage != null && __instance.RequestedItemTag.IsValid)
//            {
//                // Use the property MassStored which accesses private fields internally
//                float massStored = typeof(ManualDeliveryKG).GetProperty("MassStored", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(__instance) as float? ?? 0f;
//                var capacityProp = typeof(ManualDeliveryKG).GetProperty("Capacity", BindingFlags.Public | BindingFlags.Instance);
//                float capacity = (float)(capacityProp?.GetValue(__instance) ?? 0f);
//                var refillMassField = typeof(ManualDeliveryKG).GetField("refillMass", BindingFlags.NonPublic | BindingFlags.Instance);
//                float refillMass = (float)(refillMassField?.GetValue(__instance) ?? 0f);

//                PUtil.LogDebug($"  - MassStored (via reflection): {massStored}, Capacity (via reflection): {capacity}, RefillMass (via reflection): {refillMass}");
//            }
//        }

//        // Patch the method that actually contains the logic we want to trace
//        [HarmonyPatch("UpdateFetchList", MethodType.Normal)] // Explicitly specify MethodType for private methods if needed
//        [HarmonyPrefix]
//        public static void UpdateFetchList_Prefix(ManualDeliveryKG __instance)
//        {
//            PUtil.LogDebug($"[ManualDeliveryKG Debug] UpdateFetchList called for {__instance.name}.");

//            var pausedField = typeof(ManualDeliveryKG).GetField("paused", BindingFlags.NonPublic | BindingFlags.Instance);
//            var fetchListField = typeof(ManualDeliveryKG).GetField("fetchList", BindingFlags.NonPublic | BindingFlags.Instance);
//            var operationalField = typeof(ManualDeliveryKG).GetField("operational", BindingFlags.NonPublic | BindingFlags.Instance);
//            var operationalRequirementField = typeof(ManualDeliveryKG).GetField("operationalRequirement", BindingFlags.NonPublic | BindingFlags.Instance);
//            var storageField = typeof(ManualDeliveryKG).GetField("storage", BindingFlags.NonPublic | BindingFlags.Instance);
//            var refillMassField = typeof(ManualDeliveryKG).GetField("refillMass", BindingFlags.NonPublic | BindingFlags.Instance);

//            var paused = (bool)(pausedField?.GetValue(__instance) ?? false);
//            var fetchListInstance = fetchListField?.GetValue(__instance) as FetchList2;
//            var operational = operationalField?.GetValue(__instance) as Operational;
//            var operationalRequirement = (Operational.State)(operationalRequirementField?.GetValue(__instance) ?? Operational.State.None);
//            var storage = storageField?.GetValue(__instance) as Storage;

//            PUtil.LogDebug($"  - paused (via reflection): {paused}");
//            PUtil.LogDebug($"  - fetchList complete (via reflection): {(fetchListInstance != null ? fetchListInstance.IsComplete.ToString() : "null")}");
//            PUtil.LogDebug($"  - operational meets requirement (via reflection): {(operational != null ? operational.MeetsRequirements(operationalRequirement) : "operational is null")}");

//            if (storage != null && __instance.RequestedItemTag.IsValid)
//            {
//                float massStored = typeof(ManualDeliveryKG).GetProperty("MassStored", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(__instance) as float? ?? 0f;
//                float refillMass = (float)(refillMassField?.GetValue(__instance) ?? 0f);

//                PUtil.LogDebug($"  - MassStored (via reflection): {massStored}, RefillMass (via reflection): {refillMass}");
//            }
//        }

//        [HarmonyPatch("RequestDelivery", MethodType.Normal)] // Explicitly specify MethodType for private methods
//        [HarmonyPrefix]
//        public static void RequestDelivery_Prefix(ManualDeliveryKG __instance)
//        {
//            PUtil.LogDebug($"[ManualDeliveryKG Debug] RequestDelivery called for {__instance.name}.");

//            var fetchListField = typeof(ManualDeliveryKG).GetField("fetchList", BindingFlags.NonPublic | BindingFlags.Instance);
//            var storageField = typeof(ManualDeliveryKG).GetField("storage", BindingFlags.NonPublic | BindingFlags.Instance);
//            var capacityProp = typeof(ManualDeliveryKG).GetProperty("Capacity", BindingFlags.Public | BindingFlags.Instance);

//            var fetchListInstance = fetchListField?.GetValue(__instance) as FetchList2;
//            var storage = storageField?.GetValue(__instance) as Storage;
//            float capacity = (float)(capacityProp?.GetValue(__instance) ?? 0f);

//            PUtil.LogDebug($"  - fetchList already exists (via reflection): {(fetchListInstance != null)}");
//            if (storage != null && __instance.RequestedItemTag.IsValid)
//            {
//                float massStored = typeof(ManualDeliveryKG).GetProperty("MassStored", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(__instance) as float? ?? 0f;
//                PUtil.LogDebug($"  - MassStored (via reflection): {massStored}, Capacity (via reflection): {capacity}");
//            }
//        }

//        [HarmonyPatch("CreateFetchChore", MethodType.Normal)] // Explicitly specify MethodType for private methods
//        [HarmonyPrefix]
//        public static void CreateFetchChore_Prefix(ManualDeliveryKG __instance, float stored_mass)
//        {
//            PUtil.LogDebug($"[ManualDeliveryKG Debug] CreateFetchChore called for {__instance.name}.");
//            PUtil.LogDebug($"  - stored_mass argument: {stored_mass}");

//            var capacityField = typeof(ManualDeliveryKG).GetField("capacity", BindingFlags.NonPublic | BindingFlags.Instance);
//            var roundToIntField = typeof(ManualDeliveryKG).GetField("RoundFetchAmountToInt", BindingFlags.NonPublic | BindingFlags.Instance);

//            float capacity = (float)(capacityField?.GetValue(__instance) ?? 0f);
//            bool roundToInt = (bool)(roundToIntField?.GetValue(__instance) ?? false);

//            PUtil.LogDebug($"  - capacity (via reflection): {capacity}");
//            float num = capacity - stored_mass;
//            PUtil.LogDebug($"  - calculated amount needed: {num}");
//            float minPickable = PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
//            PUtil.LogDebug($"  - minimum pickable amount: {minPickable}");
//            float finalNum = Mathf.Max(minPickable, num); // Now Mathf should be accessible
//            PUtil.LogDebug($"  - final amount after Max check: {finalNum}");
//            if (roundToInt)
//            {
//                finalNum = (float)((int)finalNum);
//                PUtil.LogDebug($"  - rounded amount: {finalNum}");
//            }
//            if (finalNum < 0.1f)
//            {
//                PUtil.LogDebug($"  - Amount ({finalNum}) is less than 0.1f, returning without creating chore.");
//                // 如果在这里返回，说明 amount 太小了
//                return; // This return only affects the patch, not the original method
//            }
//            PUtil.LogDebug($"  - Proceeding to create chore with final amount: {finalNum}");
//        }

//        // For OnStorageChanged, we can patch the delegate method that calls it
//        [HarmonyPatch("OnStorageChanged", MethodType.Normal)]
//        [HarmonyPrefix]
//        public static void OnStorageChanged_Prefix(ManualDeliveryKG __instance, object data)
//        {
//            PUtil.LogDebug($"[ManualDeliveryKG Debug] OnStorageChanged (internal handler) called for {__instance.name}. Data: {data}");
//        }

//        // Alternatively, patch the subscription handler delegate if direct method patching fails
//        // But patching the actual method is usually preferred if possible.
//        // The MethodType.Normal should handle private methods in Harmony 2.x+
//    }
//}