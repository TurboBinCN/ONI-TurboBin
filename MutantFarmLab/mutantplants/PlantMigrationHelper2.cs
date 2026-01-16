using HarmonyLib;
using MutantFarmLab.tbbLibs;
using PeterHan.PLib.Core;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static STRINGS.BUILDING.STATUSITEMS;

namespace MutantFarmLab.mutantplants
{
    public class PlantMigrationHelper2 : KMonoBehaviour
    {
        public void MigratePlant(PlantablePlot sourcePlot, PlantablePlot targetPlot)
        {
            if (sourcePlot == null || targetPlot == null)
            {
                PUtil.LogWarning("Source or Target plot is null.");
                return;
            }

            GameObject sourcePlantObject = sourcePlot.Occupant;
            if (sourcePlantObject == null)
            {
                PUtil.LogWarning("No plant object found in the source plot.");
                return;
            }

            // --- 步骤 1: 获取植物的核心信息 ---
            // 获取植物ID
            KPrefabID plantKpid = sourcePlantObject.GetComponent<KPrefabID>();
            if (plantKpid == null)
            {
                PUtil.LogWarning("Source plant object does not have KPrefabID component.");
                return;
            }
            string plantId = plantKpid.PrefabTag.Name; // 例如 "PrickleFlower"
            PUtil.LogDebug($"Identified plant ID: {plantId}");

            // 获取突变ID (如果有)
            Tag subSpeciesId = null;
            MutantPlant mutantPlant = sourcePlantObject.GetComponent<MutantPlant>();
            if (mutantPlant != null)
            {
                subSpeciesId = mutantPlant.GetSubSpeciesInfo().speciesID; // 例如 "PrickleFlower_DualHeadPlantMutation"
                PUtil.LogDebug($"Identified sub-species ID: {subSpeciesId}");
            }
            else
            {
                PUtil.LogDebug("Source plant is not a mutant.");
            }

            // 获取生长阶段 (可选，用于未来状态恢复)
            float currentGrowth = 0f;
            Growing growingComponent = sourcePlantObject.GetComponent<Growing>();
            if (growingComponent != null)
            {
                currentGrowth = growingComponent.PercentGrown();
                PUtil.LogDebug($"Identified current growth: {currentGrowth:P2}");
            }
            else
            {
                PUtil.LogDebug("Source plant does not have Growing component.");
            }

            Tag seedPrefabTag = sourcePlantObject.GetComponent<SeedProducer>().seedInfo.seedId; // 例如 "PrickleFlowerSeed"
            //Tag seedPrefabTag = sourcePlantObject.GetComponent<MutantPlant>().SubSpeciesID;
            

            // --- 步骤 3: 创建标准种子 ---
            GameObject seedPrefab = Assets.GetPrefab(seedPrefabTag);
            if (seedPrefab == null)
            {
                PUtil.LogError($"Could not find seed prefab for plant ID: {plantId}. Tag tried: {seedPrefabTag}");
                return; // 如果找不到种子，迁移失败
            }
            PUtil.LogDebug($"Found seed prefab: {seedPrefab.name} for tag: {seedPrefabTag}");

            // 实例化一个种子对象
            GameObject standardSeedObject = GameUtil.KInstantiate(seedPrefab, targetPlot.transform.GetPosition(), Grid.SceneLayer.Front); // 使用合适的层
            TbbDebuger.PrintGameObjectFullInfo(standardSeedObject);
            if (standardSeedObject == null)
            {
                PUtil.LogError("Failed to instantiate the standard seed object.");
                return;
            }
            PUtil.LogDebug($"Instantiated standard seed object: {standardSeedObject.name}");

            // 获取种子组件并设置突变 (如果原植物有突变)
            PlantableSeed seedComponent = standardSeedObject.GetComponent<PlantableSeed>();
            if (seedComponent == null)
            {
                PUtil.LogWarning("Instantiated seed object does not have PlantableSeed component.");
            }
            else
            {
                if (!string.IsNullOrEmpty(subSpeciesId.ToString()))
                {
                    int targetCell = Grid.PosToCell(targetPlot);
                    int plotInstanceId = targetPlot.GetInstanceID();
                    PlantMigrationData storedData = new PlantMigrationData
                    {
                        OriginalSubSpeciesId = subSpeciesId,
                        OriginalGrowth = currentGrowth
                    };
                    Instance.StoreMigrationData(plotInstanceId, storedData); // 需要在类中添加此方法和字段
                    PUtil.LogDebug($"Stored migration data for plot instance ID: {plotInstanceId}, SubSpecies: {subSpeciesId}, Growth: {currentGrowth:P2}");
                }

                var mutantPlantCom = sourcePlantObject.GetComponent<MutantPlant>();
                var mutantPlantCom2 = standardSeedObject.GetComponent<MutantPlant>();
                if (mutantPlantCom != null && mutantPlantCom2)
                {
                    mutantPlantCom.CopyMutationsTo(mutantPlantCom2);
                }
            }
            Util.KDestroyGameObject(sourcePlantObject);
            PUtil.LogDebug("Destroyed source plant object.");
            // --- 步骤 4: 强制放入目标种植槽 ---
            targetPlot.ForceDeposit(standardSeedObject); // 这会触发 OnDepositObject -> SpawnOccupyingObject
            PUtil.LogDebug("Called ForceDeposit on the standard seed object.");

            // --- 步骤 2: 销毁源植物 ---
            
        }

        // 添加存储和检索迁移数据的方法
        private Dictionary<int, PlantMigrationData> pendingMigrationData = new Dictionary<int, PlantMigrationData>();

        public void StoreMigrationData(int plotInstanceId, PlantMigrationData data)
        {
            pendingMigrationData[plotInstanceId] = data;
        }

        public PlantMigrationData RetrieveAndRemoveMigrationData(int plotInstanceId)
        {
            if (pendingMigrationData.TryGetValue(plotInstanceId, out PlantMigrationData data))
            {
                pendingMigrationData.Remove(plotInstanceId);
                return data;
            }
            return new();
        }

        // 获取单例实例 (如果需要)
        public static PlantMigrationHelper2 Instance { get; private set; }

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            Instance = this;
        }

        // --- 数据结构 ---
        public struct PlantMigrationData
        {
            public Tag OriginalSubSpeciesId;
            public float OriginalGrowth;
        }
    }
    //[HarmonyPatch(typeof(PlantablePlot), "SpawnOccupyingObject")]
    //public class PlantablePlot_SpawnOccupyingObject_Patch
    //{
    //    public static void Postfix(PlantablePlot __instance, GameObject depositedEntity, ref GameObject __result)
    //    {
    //        //PUtil.LogDebug($"----SpawnOccupyingObject Start----");
    //        //TbbDebuger.PrintGameObjectFullInfo(depositedEntity);
    //        //PUtil.LogDebug($"----__result----");
    //        //TbbDebuger.PrintGameObjectFullInfo(__result);
    //        //PUtil.LogDebug($"----SpawnOccupyingObject End----");
    //        try
    //        {
    //            KPrefabID component = __result.GetComponent<KPrefabID>();

    //            if (component == null)
    //            {
    //                PUtil.LogWarning($"PlantablePlot_SpawnOccupyingObject_Patch POST:name:[{__result.name}] & KPrefabID NULL");
    //            }
    //            else
    //            {
                    
    //                PUtil.LogDebug($"PlantablePlot_SpawnOccupyingObject_Patch POST: depositedEntity name:[{depositedEntity.name}] & ID:[{depositedEntity.GetComponent<KPrefabID>()?.InstanceID}]");
    //                PUtil.LogDebug($"PlantablePlot_SpawnOccupyingObject_Patch POST:name:[{__result.name}] & ID:[{component.InstanceID}]");
    //                PUtil.LogDebug($"PlantablePlot_SpawnOccupyingObject_Patch POST:name:[{component.GetComponent<KPrefabID>()?.InstanceID}]");
    //            }
    //        }
    //        catch (Exception e)
    //        {
    //            PUtil.LogWarning($"Error in PlantablePlot_SpawnOccupyingObject_Patch: {e.Message}\n{e.StackTrace}");
    //        }
    //        return;
    //    }
    //}
    //[HarmonyPatch(typeof(PlantablePlot), "SpawnOccupyingObject")] // Corrected class name
    //public class PlantablePlot_SpawnOccupyingObject_ApplyState_Patch
    //{
    //    // Postfix runs *after* the original method returns
    //    public static void Postfix(PlantablePlot __instance, GameObject depositedEntity, ref GameObject __result) // Access private field 'depositedObject' using ___ prefix
    //    {
    //        // 获取当前 Plot 的 InstanceID
    //        int plotInstanceId = __instance.GetInstanceID();
    //        PUtil.LogDebug($"Postfix: Checking migration data for plot instance ID: {plotInstanceId}");
    //        //TbbDebuger.PrintGameObjectFullInfo(depositedEntity);
    //        //TbbDebuger.PrintGameObjectFullInfo(__result);
    //        // 尝试从 MutantFarmLab 单例中获取存储的数据
    //        var migrationData = PlantMigrationHelper2.Instance?.RetrieveAndRemoveMigrationData(plotInstanceId);
    //        if (migrationData.HasValue)
    //        {
    //            var data = migrationData.Value;
    //            PUtil.LogDebug($"Found migration data for this plot: SubSpecies: {data.OriginalSubSpeciesId}, Growth: {data.OriginalGrowth:P2}");

    //            // 新实例化的植物对象
    //            GameObject newPlantedObject = __result; // __result 是 SpawnOccupyingObject 返回的新植物

    //            if (newPlantedObject != null)
    //            {
    //                // 应用生长阶段
    //                if (data.OriginalGrowth > 0f)
    //                {
    //                    Growing newGrowingComponent = newPlantedObject.GetComponent<Growing>();
    //                    if (newGrowingComponent != null)
    //                    {
    //                        newGrowingComponent.OverrideMaturityLevel(data.OriginalGrowth);
    //                        PUtil.LogDebug($"Applied growth level '{data.OriginalGrowth:P2}' to new plant '{newPlantedObject.name}'.");
    //                    }
    //                    else
    //                    {
    //                        PUtil.LogWarning($"New plant '{newPlantedObject.name}' does not have Growing component. Cannot apply growth level.");
    //                    }
    //                }
    //            }
    //            else
    //            {
    //                PUtil.LogWarning("New planted object returned by SpawnOccupyingObject is null.");
    //            }
    //        }
    //        else
    //        {
    //            // PUtil.LogInfo($"No migration data found for plot instance ID: {plotInstanceId}. This is normal for non-migrated plants.");
    //            // 注释掉这条日志，因为对于非迁移的常规种植，这会很频繁。
    //        }
    //    }
    //}
}
