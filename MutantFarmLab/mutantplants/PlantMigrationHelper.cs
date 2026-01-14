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
                PUtil.LogWarning($"[双头株]植株[{plant.name}] 缺失ReceptacleMonitor组件.");
                return false;
            }

            // 2. 获取当前关联的旧 Plot
            PlantablePlot oldReceptacle = receptacleMonitor.GetReceptacle();
            if (oldReceptacle == null) PUtil.LogDebug($"[双头株] 植株[{plant.name}]已经没有PlantablePlot");

            // 4. 开始迁移流程
            try
            {
                // --- 解除与旧 Plot 的关系 ---
                if(oldReceptacle != null ){
                    //因为已经移除，所以这里先不执行
                    //ClearPlotWithoutDestroyingPlant(oldPlot);
                }
                //清理plant的旧的订阅
                

                // --- 建立与新 Plot 的关系 ---
                InvokeMethod(newPlot, "RegisterWithPlant",new object[] { plant });

                Components.PlantablePlots.Add(newPlot.gameObject.GetMyWorldId(), newPlot);
                PUtil.LogDebug($"[双头株] 迁移 [{plant?.name}] 注册新Plot[{newPlot?.name}] ");
                //newPlot.gameObject.Trigger(-1820564715);//OccupantValidChanged
                //plant.Unsubscribe(1969584890);//ObjectDestroyed 
                //肥料SMI
                var fertilizationSMI = plant.GetSMI<FertilizationMonitor.Instance>();
                if (fertilizationSMI != null)
                {
                    fertilizationSMI.SetStorage(newPlot.gameObject.GetComponent<Storage>());
                    PUtil.LogDebug($"[双头株] 迁移 [{plant?.name}] 肥料Storage ");
                }
                else
                {
                    ScheduleParams parameters = new();
                    parameters.PlantObj = plant;
                    parameters.NewPlotObj = newPlot.gameObject;
                    GameScheduler.Instance.Schedule("RecoverIPlot", 0.02f, delayCall, parameters);
                    PUtil.LogWarning($"[双头株] [{plant.name}] FertilizationMonitor.Instance 还没有实例化");
                }
                    
                // 灌溉SMI
                var irrigationSMI = plant.GetSMI<IrrigationMonitor.Instance>();
                if(irrigationSMI != null)
                {
                    irrigationSMI.SetStorage(newPlot.gameObject.GetComponent<Storage>());
                    PUtil.LogDebug($"[双头株] 迁移 [{plant?.name}] 灌溉Storage ");
                }

                PUtil.LogDebug($"[双头株] 植株:[{plant.name}] 完成迁移[{oldReceptacle?.name}]TO[{newPlot.name}].");

                return true;
            }
            catch (Exception e)
            {
                PUtil.LogError($"[双头株] 迁移失败: {e.Message}\n{e.StackTrace}");
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
        public static void ClearPlotWithoutDestroyingPlant(PlantablePlot targetReceptacle)
        {
            var currentPlant = targetReceptacle?.Occupant;
            if (currentPlant == null)
            {
                PUtil.LogDebug("[双头株] 种植盆已为空");
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
            ClearPlantRef(targetReceptacle);

            // 6. 调用内部清理方法
            InvokeMethod(receptacle, "UnsubscribeFromOccupant");
            InvokeMethod(receptacle, "UpdateActive");

            PUtil.LogDebug($"[双头株] 已移出植株 '{currentPlant.name}' 并清空 receptacle");
        }
        private static void ClearPlantRef(PlantablePlot targetPlot)
        {
            var field = typeof(PlantablePlot).GetField("plantRef", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                var plantRef = (Ref<KPrefabID>)field.GetValue(targetPlot);
                if (plantRef == null)
                {
                    plantRef = new Ref<KPrefabID>();
                    field.SetValue(targetPlot, plantRef);
                }
                plantRef.Set(null);
                PUtil.LogDebug("[双头株] plantRef 已设为 null");
            }
        }
        public static void ResetPlotToPlantableState(PlantablePlot targetPlot, Operational plotOperational)
        {
            targetPlot?.SetPreview(Tag.Invalid, false);

            if (plotOperational != null && !plotOperational.IsOperational)
                plotOperational.SetActive(true, false);

            InvokeMethod(targetPlot, "UpdateActive");
            InvokeUpdateStatusItem(targetPlot);

            //targetPlot.gameObject.Trigger(-1820564715);//OccupantValidChanged
            //targetPlot.gameObject.Unsubscribe(-1697596308);//OnStorageChange
            //targetPlot.gameObject.Unsubscribe(-1820564715);//OccupantValidChanged
        }
        private static void InvokeUpdateStatusItem(object obj)
        {
            if (obj == null) return;

            var noParam = obj.GetType().GetMethod("UpdateStatusItem", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, Type.EmptyTypes, null);
            if (noParam != null)
            {
                noParam.Invoke(obj, null);
                return;
            }

            var withParam = obj.GetType().GetMethod("UpdateStatusItem", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new[] { typeof(KSelectable) }, null);
            if (withParam != null)
            {
                var selectable = obj as KSelectable ?? ((Component)obj).GetComponent<KSelectable>();
                withParam.Invoke(obj, new object[] { selectable });
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
