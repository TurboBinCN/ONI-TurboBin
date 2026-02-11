using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MutantFarmLab.mutantplants
{
    public class DualHeadConduitConsumer
    {
        private static readonly Dictionary<ConduitConsumer, Storage> SecondaryStorageMap = new();
        [HarmonyPatch]
        public class DualHeadConduitConsumer_ConduitConsumer_Patch
        {
            [HarmonyTargetMethod]
            public static MethodBase TargetMethod_OnSpawn()
            {
                var type = typeof(ConduitConsumer);
                var method = AccessTools.DeclaredMethod(type, "OnSpawn");
                if (method == null)
                {
                    method = AccessTools.DeclaredMethod(typeof(KMonoBehaviour), "OnSpawn");
                }
                return method;
            }

            [HarmonyPostfix]
            public static void OnSpawn_Postfix(ConduitConsumer __instance)
            {
                if (__instance.gameObject.PrefabID() == RadiationFarmTileConfig.ID ||
                    __instance.gameObject.PrefabID() == HydroponicFarmConfig.ID)
                {
                    Transform plotTransform = __instance.transform.Find(PlantablePlotGameObject.storageName);
                    if (plotTransform != null)
                    {
                        Storage secondaryStorageInstance = plotTransform.GetComponent<Storage>();
                        if (secondaryStorageInstance != null)
                        {
                            lock (SecondaryStorageMap)
                            {
                                if (!SecondaryStorageMap.ContainsKey(__instance))
                                {
                                    SecondaryStorageMap.Add(__instance, secondaryStorageInstance);
                                }
                            }
                        }
                        else
                        {
                            // Debug.LogWarning($"ConduitConsumer on '{__instance.name}' found child '{RadiationFarmTileConfig.RadiationStorageName}' but no Storage component on it.");
                        }
                    }
                    else
                    {
                        // Debug.LogWarning($"ConduitConsumer on '{__instance.name}' could not find child named '{RadiationFarmTileConfig.RadiationStorageName}'.");
                    }
                }
            }
        }
        [HarmonyPatch]
        public class DualHeadConduitConsumer_ConduitConsumer_Consume_Patch
        {
            [HarmonyTargetMethod]
            public static MethodBase TargetMethod_Consume()
            {
                var type = typeof(ConduitConsumer);
                return AccessTools.Method(type, "Consume", new System.Type[] { typeof(float), typeof(ConduitFlow) });
            }
            [HarmonyPrefix]
            public static bool Consume_Prefix(
                ConduitConsumer __instance,
                float dt,
                ConduitFlow conduit_mgr
            )
            {
                Storage SecondaryStorage = null;

                lock (SecondaryStorageMap) // 加锁保证线程安全
                {
                    SecondaryStorageMap.TryGetValue(__instance, out SecondaryStorage);
                }
                if (SecondaryStorage != null)
                {
                    Storage primaryStorage = __instance.storage;
                    if (primaryStorage == null) return true;
                    // --- 计算总容量和目标分配 ---
                    float totalCapacityPerStorage = __instance.capacityKG; // 每个 Storage 的容量限制
                    float totalAvailableCapacity = totalCapacityPerStorage * 2; // 两个 Storage 总容量

                    //只处理液体
                    if (__instance.ConduitType != ConduitType.Liquid) return true;

                    int cell = (int)TbbHarmonyExtension.GetField(__instance, "utilityCell");
                    ConduitFlow.ConduitContents contents = conduit_mgr.GetContents(cell);
                    // --- 步骤 2: 模拟 Consume 的逻辑，计算本次可以获取的量 ---
                    float massToConsume = contents.mass;
                    massToConsume = Mathf.Min(massToConsume, dt * __instance.consumptionRate); // 考虑消耗速率


                    //处理吸收元素与错误元素
                    bool primaryStorageCanAbsorb = CanConduitElementAbsorb(__instance.storage, contents.element);
                    bool secondaryStorageCanAbsorb = CanConduitElementAbsorb(SecondaryStorage, contents.element);

                    float primaryDelta = primaryStorageCanAbsorb ? totalCapacityPerStorage - primaryStorage.GetMassAvailable(__instance.capacityTag) : 0;
                    float secondaryDelta = secondaryStorageCanAbsorb ? totalCapacityPerStorage - SecondaryStorage.GetMassAvailable(__instance.capacityTag) : 0;

                    float availableInPrimary = primaryDelta > 0 ? primaryDelta : 0;
                    float availableInSecondary = secondaryDelta > 0 ? secondaryDelta : 0;

                    float totalAvailableInStorages = availableInPrimary + availableInSecondary;

                    massToConsume = Mathf.Min(massToConsume, totalAvailableInStorages);
                    if (massToConsume > 0)
                    {
                        ConduitFlow.ConduitContents conduitContents = conduit_mgr.RemoveElement(cell, massToConsume);
                        float num2 = conduitContents.mass;
                        __instance.lastConsumedElement = conduitContents.element;

                        int disease_count = (int)((float)contents.diseaseCount * (num2 / contents.mass));

                        float massForPrimary = Mathf.Min(num2, availableInPrimary);

                        PrimaryElement primaryElement = null;
                        if (primaryStorageCanAbsorb)
                            primaryElement = primaryStorage.AddLiquid(contents.element, massForPrimary, contents.temperature, contents.diseaseIdx, disease_count, __instance.keepZeroMassObject, false);

                        float remainingForSecondary = availableInSecondary > 0 ? num2 - massForPrimary : 0;
                        if (secondaryStorageCanAbsorb)
                            primaryElement = SecondaryStorage.AddLiquid(contents.element, remainingForSecondary, contents.temperature, contents.diseaseIdx, disease_count, __instance.keepZeroMassObject, false);

                        //PUtil.LogDebug($"[DualHeadConduitConsume] massToConsume [{massToConsume}] massForPrimary:[{massForPrimary}] remainingForSecondary: [{remainingForSecondary}]");
                    }

                    return false; // 不执行原始的 Consume 方法
                }

                return true;
            }
            private static bool CanConduitElementAbsorb(Storage storage, SimHashes element)
            {
                bool flag = false;
                GameObject plant = storage.gameObject?.GetComponent<PlantablePlot>()?.Occupant;
                if (plant != null)
                {
                    IrrigationMonitor.Instance smi = plant.GetSMI<IrrigationMonitor.Instance>();
                    if (smi != null)
                    {
                        var consumed_infos = smi.def.consumedElements;
                        if (consumed_infos != null)
                        {
                            DumpIncorrectFertilizers(storage, consumed_infos, false);
                            foreach (PlantElementAbsorber.ConsumeInfo consumeInfo in consumed_infos)
                            {
                                if (element.CreateTag() == consumeInfo.tag)
                                {
                                    flag = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                return flag;
            }
            private static void DumpIncorrectFertilizers(Storage storage, PlantElementAbsorber.ConsumeInfo[] consumed_infos, bool validate_solids)
            {
                Vector3 position = storage.transform.position;
                List<GameObject> items = new();
                if (storage == null)
                {
                    return;
                }
                for (int i = storage.items.Count - 1; i >= 0; i--)
                {
                    GameObject gameObject = storage.items[i];
                    if (!(gameObject == null))
                    {
                        PrimaryElement component = gameObject.GetComponent<PrimaryElement>();
                        if (!(component == null) && !(gameObject.GetComponent<ElementChunk>() == null))
                        {
                            if (validate_solids)
                            {
                                if (!component.Element.IsSolid)
                                {
                                    continue;
                                }
                            }
                            else if (!component.Element.IsLiquid)
                            {
                                continue;
                            }
                            bool flag = false;

                            KPrefabID component2 = component.GetComponent<KPrefabID>();
                            if (consumed_infos != null)
                            {
                                foreach (PlantElementAbsorber.ConsumeInfo consumeInfo in consumed_infos)
                                {
                                    if (component2.HasTag(consumeInfo.tag))
                                    {
                                        flag = true;
                                        break;
                                    }
                                }
                            }
                            if (!flag) items.Add(gameObject);
                        }
                    }
                }
                foreach (GameObject item in items)
                {
                    storage.Drop(item, false);
                }
            }
        }
    }
}