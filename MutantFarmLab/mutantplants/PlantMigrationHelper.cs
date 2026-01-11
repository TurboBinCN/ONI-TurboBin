using HarmonyLib;
using Klei.AI;
using PeterHan.PLib.Core;
using System;
using System.Reflection;
using UnityEngine;

namespace MutantFarmLab.mutantplants
{
    public static class PlantMigrationHelper
    {
        /// <summary>
        /// 将一个已经种植的植株从其当前的 PlantablePlot 迁移到一个新的 PlantablePlot。
        /// </summary>
        /// <param name="plant">要迁移的植株 GameObject</param>
        /// <param name="newPlot">目标 PlantablePlot</param>
        /// <returns>迁移是否成功</returns>
        public static bool MigratePlantToPlot(GameObject plant, PlantablePlot newPlot)
        {
            if (plant == null || newPlot == null)
            {
                PUtil.LogWarning("Plant or new plot is null.");
                return false;
            }

            // 1. 检查植株是否已经挂载了一个 ReceptacleMonitor
            ReceptacleMonitor receptacleMonitor = plant.GetComponent<ReceptacleMonitor>();
            if (receptacleMonitor == null)
            {
                PUtil.LogWarning("The plant does not have a ReceptacleMonitor component.");
                return false;
            }

            // 2. 获取当前关联的旧 Plot
            SingleEntityReceptacle oldReceptacle = receptacleMonitor.GetReceptacle();
            if (oldReceptacle == null) PUtil.LogDebug("The plant is not currently associated with any plot. ");

            // 3. 确保旧的 receptacle 是一个 PlantablePlot
            PlantablePlot oldPlot = oldReceptacle as PlantablePlot;
            if (oldPlot == null)PUtil.LogWarning("The plant's current receptacle is not a PlantablePlot.");

            // 4. 开始迁移流程
            try
            {
                // --- 解除与旧 Plot 的关系 ---
                if(oldReceptacle != null || oldPlot != null){
                    //因为已经移除，所以这里先不执行
                    //ClearPlotWithoutDestroyingPlant(oldPlot); // 这会将 oldPlot.occupyingObject 设为 null
                }
                // --- 建立与新 Plot 的关系 ---
                // c. 调用新 Plot 的 ReplacePlant 方法，这是最安全的方式
                //    ReplacePlant 内部会调用 ForceDeposit -> OnDepositObject -> ConfigureOccupyingObject
                //    这个流程会完整地重建植株与新 Plot 的双向绑定
                InvokeMethod(newPlot, "RegisterWithPlant",new object[] { plant });


                PUtil.LogDebug($"[双头株] NewPlot active[{newPlot.gameObject.activeSelf}].");
                var fertilizationSMI = plant.GetSMI<FertilizationMonitor.Instance>();
                //肥料SMI
                //TODO 灌溉SMI
                if (fertilizationSMI == null)
                {
                    ScheduleParams parameters = new();
                    parameters.PlantObj = plant;
                    parameters.NewPlotObj = newPlot.gameObject;
                    GameScheduler.Instance.Schedule("RecoverIPlot", 0.02f, delayCall, parameters);
                    PUtil.LogWarning($"[双头株] [{plant.name}] FertilizationMonitor.Instance 还没有实例化");
                    return false;
                }
                fertilizationSMI.SetStorage(newPlot.gameObject.GetComponent<Storage>());

                // d. 手动重置 ManualDeliveryKG 的状态 (保险起见)
                //    确保任何灌溉或施肥相关的 ManualDeliveryKG 都处于活跃状态
                var manualDeliveryKgs = plant.GetComponents<ManualDeliveryKG>();
                foreach (var deliveryKG in manualDeliveryKgs)
                {
                    if (deliveryKG != null)
                    {
                        // 使用反射设置 pauseManaging 为 false
                        deliveryKG.SetStorage(newPlot.gameObject.GetComponent<Storage>());
                        deliveryKG.allowPause = true;
                        deliveryKG.Pause(false, "Restart");
                        deliveryKG.enabled = true;
                        PUtil.LogDebug($"[双头株] 重启 ManualDeliveryKG for {deliveryKG.RequestedItemTag} on plant:[{plant.name}].");
                    }
                }
                plant.Trigger(1309017699, newPlot.gameObject.GetComponent<Storage>());
                PUtil.LogDebug($"[双头株] 植株:[{plant.name}] 完成迁移[{oldPlot?.name}]TO[{newPlot.name}].");

                return true;
            }
            catch (Exception e)
            {
                PUtil.LogError($"Failed to migrate plant: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }
        public  class ScheduleParams
        {
            public  GameObject PlantObj;
            public  GameObject NewPlotObj;
        }
        private static void delayCall(object data)
        {
            PUtil.LogDebug($"[双头株] 迁移 Delay Call");
            var paramsObj = (ScheduleParams)data;

            var fertilizationSMI = paramsObj.PlantObj.GetSMI<FertilizationMonitor.Instance>();
            if (fertilizationSMI != null)
            {
                //如果delay后仍然没有实例化说明只需要灌溉
                //TODO 处理灌溉
                fertilizationSMI.SetStorage(paramsObj.NewPlotObj.GetComponent<Storage>());

                PUtil.LogDebug($"[双头株] 延迟迁移 设置[{paramsObj.PlantObj?.name}] Storage ");
            }
        }

        private static void ClearPlotWithoutDestroyingPlant(SingleEntityReceptacle targetReceptacle)
        {
            var currentPlant = targetReceptacle?.Occupant;
            if (currentPlant == null)
            {
                PUtil.LogDebug("[DualHead] 种植盆已为空");
                return;
            }

            // 1. 解绑 Assignable
            if (currentPlant.TryGetComponent<Assignable>(out var assignable))
                assignable.Unassign();

            // 2. 取消 Uproot 标记
            if (currentPlant.TryGetComponent<Uprootable>(out var uprootable))
            {
                uprootable.ForceCancelUproot();
                SetField(uprootable, "isMarkedForUproot", false);
                SetField(uprootable, "chore", null);
            }

            // 3. 移出植株（不销毁）
            //currentPlant.transform.SetParent(null);


            // 4. 清空 receptacle 内部状态
            var receptacle = targetReceptacle;
            SetField(receptacle, "occupyingObject", null);
            SetField(receptacle, "occupyObjectRef", new Ref<KSelectable>());
            SetField(receptacle, "activeRequest", null);
            SetField(receptacle, "autoReplaceEntity", false);
            SetField(receptacle, "requestedEntityTag", Tag.Invalid);
            SetField(receptacle, "requestedEntityAdditionalFilterTag", Tag.Invalid);

            // 5. 清空 PlantablePlot 的 plantRef
            ClearPlantRef(targetReceptacle as PlantablePlot);

            // 6. 调用内部清理方法
            InvokeMethod(receptacle, "UnsubscribeFromOccupant");
            InvokeMethod(receptacle, "UpdateActive");

            PUtil.LogDebug($"[DualHead] 已移出植株 '{currentPlant.name}' 并清空 receptacle");
        }
        private static void ClearPlantRef(PlantablePlot _targetPlot)
        {
            var field = typeof(PlantablePlot).GetField("plantRef", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                var plantRef = (Ref<KPrefabID>)field.GetValue(_targetPlot);
                if (plantRef == null)
                {
                    plantRef = new Ref<KPrefabID>();
                    field.SetValue(_targetPlot, plantRef);
                }
                plantRef.Set(null);
                PUtil.LogDebug("[DualHead] plantRef 已设为 null");
            }
        }
        private static void SetField(object obj, string name, object value)
        {
            if (obj == null) return;
            var field = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null)
                field.SetValue(obj, value);
        }
        public static object GetField(object obj, string name)
        {
            if (obj == null) return null;
            var field = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null)
                return field.GetValue(obj);
            return null;
        }
        private static void InvokeMethod(object obj, string name, params object[] args)
        {
            if (obj == null) return;
            var types = args == null ? Type.EmptyTypes : Array.ConvertAll(args, a => a?.GetType() ?? typeof(object));
            var method = obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, types, null);
            method?.Invoke(obj, args);
        }
    }
}
