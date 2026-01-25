using Klei.AI;
using PeterHan.PLib.Core;
using System;
using UnityEngine;

namespace MutantFarmLab.mutantplants
{
    public class PlantMigrationHelper2
    {
        public static void MigratePlant(GameObject plant, PlantablePlot targetPlot)
        {
            if (plant == null || targetPlot == null)
            {
                PUtil.LogWarning("[双头株]Source or Target plot is null.");
                return;
            }
            //---情况 1：植物没有种植槽，直接移植
            GameObject sourcePlantObject = plant;
            var sourcePlantablePlot = sourcePlantObject.GetComponent<ReceptacleMonitor>()?.GetReceptacle();
            if (sourcePlantablePlot == null)
            {
                PUtil.LogWarning("[双头株]植株没有种植槽，直接放入目标种植槽.");
                targetPlot.ReplacePlant(sourcePlantObject, keepPlantablePlotStorage);
                return;
            }
            var marker = sourcePlantablePlot.gameObject.GetComponent<DualHeadReceptacleMarker>();
            //--情况 2： 植株有种植槽，重建植株
            // --- 步骤 1: 获取植物的核心信息 ---
            KPrefabID plantKpid = sourcePlantObject.GetComponent<KPrefabID>();
            if (plantKpid == null)
            {
                PUtil.LogWarning("[双头株]Source plant object does not have KPrefabID component.");
                return;
            }
            string plantId = plantKpid.PrefabTag.Name; // 例如 "PrickleFlower"
            PUtil.LogDebug($"[双头株]Identified plant ID: {plantId}");

            // --- 步骤 2: 重建植株 ---
            GameObject rebuildedPlantObject = GameUtil.KInstantiate(Assets.GetPrefab(plantId), Grid.SceneLayer.BuildingBack, null, 0);
            rebuildedPlantObject.transform.SetPosition(targetPlot.transform.GetPosition());

            // 获取种子组件并设置突变 (如果原植物有突变)
            PUtil.LogDebug($"[双头株]复制变异");
            MutantPlant component = sourcePlantObject.GetComponent<MutantPlant>();
            MutantPlant component2 = rebuildedPlantObject.GetComponent<MutantPlant>();
            if (component != null && rebuildedPlantObject != null)
            {
                component.CopyMutationsTo(component2);
                PlantSubSpeciesCatalog.Instance.IdentifySubSpecies(component2.SubSpeciesID);
            }
            rebuildedPlantObject.SetActive(true);

            PUtil.LogDebug($"[双头株]复制生长状态");
            Growing component3 = sourcePlantObject.GetComponent<Growing>();
            Growing component4 = rebuildedPlantObject.GetComponent<Growing>();
            if (component3 != null && component4 != null)
            {
                float num = component3.PercentGrown();
                if (useGrowthTimeRatio)
                {
                    AmountInstance amountInstance = component3.GetAmounts().Get(Db.Get().Amounts.Maturity);
                    AmountInstance amountInstance2 = component4.GetAmounts().Get(Db.Get().Amounts.Maturity);
                    float num2 = amountInstance.GetMax() / amountInstance2.GetMax();
                    num = Mathf.Clamp01(num * num2);
                }
                component4.OverrideMaturityLevel(num);
            }
            try
            {
                PUtil.LogDebug($"复制PrimaryElement/Effects");
                PrimaryElement component5 = rebuildedPlantObject.GetComponent<PrimaryElement>();
                PrimaryElement component6 = sourcePlantObject.GetComponent<PrimaryElement>();
                component5.Temperature = component6.Temperature;
                component5.AddDisease(component6.DiseaseIdx, component6.DiseaseCount, "TransformedPlant");
                rebuildedPlantObject.GetComponent<Effects>().CopyEffects(sourcePlantObject.GetComponent<Effects>());
            }
            catch (Exception ex) { PUtil.LogWarning($"{ex.Message}\n{ex.StackTrace}"); }

            PUtil.LogDebug($"[双头株]复制HarvestDesignatable");
            HarvestDesignatable component7 = sourcePlantObject.GetComponent<HarvestDesignatable>();
            HarvestDesignatable component8 = rebuildedPlantObject.GetComponent<HarvestDesignatable>();
            if (component7 != null && component8 != null)
            {
                component8.SetHarvestWhenReady(component7.HarvestWhenReady);
            }

            PUtil.LogDebug($"[双头株]Prioritizable");
            Prioritizable component9 = sourcePlantObject.GetComponent<Prioritizable>();
            Prioritizable component10 = rebuildedPlantObject.GetComponent<Prioritizable>();
            if (component9 != null && component10 != null)
            {
                component10.SetMasterPriority(component9.GetMasterPriority());
            }
            if (rebuildedPlantObject == null)
            {
                PUtil.LogError("[双头株]Failed to instantiate the standard seed object.");
                return;
            }
            PUtil.LogDebug($"[双头株]Instantiated standard seed object: {rebuildedPlantObject.name}");

            // --- 步骤 3: 销毁源植物 ---
            if (sourcePlantObject != null){
                PUtil.LogDebug("[双头株]执行拔除植物");
                Util.KDestroyGameObject(sourcePlantObject);
                TbbHarmonyExtension.InvokeMethod(sourcePlantObject.GetComponent<ReceptacleMonitor>().GetReceptacle(), "ClearOccupant",new object[] { });
            }
            // --- 步骤 4: 强制放入目标种植槽 ---
            PUtil.LogDebug("[双头株]调用 ReplacePlant 放入种植槽.");
            targetPlot.gameObject.SetActive(true);
            targetPlot.ReplacePlant(rebuildedPlantObject, keepPlantablePlotStorage);
            var dualHeadPlantComponent = rebuildedPlantObject.AddOrGet<DualHeadPlantComponent>();
            dualHeadPlantComponent.RootPlotGameObject = targetPlot.gameObject;
            marker.primaryPlant = rebuildedPlantObject;
            //sourcePlot.ReplacePlant(standardSeedObject, keepPlantablePlotStorage);//原地重种没问题
        }

        private static bool useGrowthTimeRatio = true;
        private static bool keepPlantablePlotStorage = true;
    }
}
