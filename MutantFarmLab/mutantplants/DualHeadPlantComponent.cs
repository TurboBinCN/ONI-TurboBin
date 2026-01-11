// DualHeadPlantComponent.cs
using HarmonyLib;
using Klei.AI;
using MutantFarmLab.tbbLibs;
using PeterHan.PLib.Core;
using UnityEngine;

namespace MutantFarmLab.mutantplants
{
    public class DualHeadReceptacleMarker : KMonoBehaviour,ISaveLoadable
    {
        public GameObject primaryPlant;
    }
    public class DualHeadPlantComponent : KMonoBehaviour, ISaveLoadable
    {
        //双头株状态 下相关属性
        public bool dualHead = false;
        public DualHeadPlantComponent twin;
        private DualHeadReceptacleMarker _marker;

        public GameObject RootPlotGameObject;
        public GameObject iPlotGameObject;

        private GameObject _PlantI;

        private bool IsTargetFarmTile(GameObject targetObj)
        {
            KPrefabID prefabId = targetObj.GetComponent<KPrefabID>();
            if (prefabId != null && prefabId.HasTag(GameTags.FarmTiles)) return true;
            if (targetObj.name.Contains("FarmTile")) return true;
            return targetObj.name.Contains("Hydroponic");
        }
        private bool isDulHeadMutantPlant(GameObject plant)
        {
            return TryGetComponent(out MutantPlant mp) &&
                             mp.MutationIDs?.Contains(PlantMutationRegister.DUAL_HEAD_MUT_ID) == true;
        }
        protected override void OnSpawn()
        {
            base.OnSpawn();

            _PlantI = gameObject;
            //读档重建数据
            if(!isDulHeadMutantPlant(_PlantI)) return;

            //TODO 子株也是双头株变异需要判断
            //母株逻辑
            int centerCell = Grid.PosToCell(_PlantI); // 自身中心格子（判定基准）
            if (!Grid.IsValidCell(centerCell)) return;

            CellOffset[] checkOffsets = new[]
            {
                CellOffset.none,   // 自身
                new CellOffset(0, 1),   // 上格
                new CellOffset(0, -1) // 下格
            };

            foreach (var offset in checkOffsets)
            {
                //计算「自身中心±上下左右1格」的目标格子ID
                int targetCell = Grid.OffsetCell(centerCell, offset);

                //目标格子无效 → 直接跳过
                if (!Grid.IsValidCell(targetCell)) continue;

                //TODO 需要判断是否是自己占据的FarmTile格子

                // 自身中心±1格 + 是自己占据的格子 → 检测种植砖
                GameObject farmTileObj = Grid.Objects[targetCell, (int)ObjectLayer.FoundationTile];
                if (farmTileObj == null) continue;

                if (IsTargetFarmTile(farmTileObj))
                {
                    RootPlotGameObject = farmTileObj;
                    PUtil.LogDebug($"[双头株]Plant:[{_PlantI.name}] cell:[{targetCell}] | 种植砖={farmTileObj.name}");
                    break;
                }
            }
            if (RootPlotGameObject == null) return;
            _marker = RootPlotGameObject?.GetComponent<DualHeadReceptacleMarker>();
            _marker.primaryPlant = _PlantI;

            //母株迁移Plot
            var plantablePlotGO = PlantablePlotGameObject.GetGameObject(RootPlotGameObject);
            if (plantablePlotGO != null)
            {
                plantablePlotGO.SetActive(true);
                var plot = plantablePlotGO.AddOrGet<PlantablePlot>();
                iPlotGameObject = plot.gameObject;

                plot.InitializeComponent();
                _PlantI.transform.SetParent(plot.transform);
                PlantMigrationHelper.MigratePlantToPlot(_PlantI, plot);

                PUtil.LogDebug($"[双头株] 读档重建 母株迁移Plot :[{plot.gameObject.name}]");
            }

            //重建子株并绑定
            var twinPlant = RootPlotGameObject.GetComponent<PlantablePlot>().Occupant;
            if (twinPlant == null) return;
            var twinPlantCom = twinPlant.GetComponent<DualHeadPlantComponent>();
            twinPlantCom.RootPlotGameObject = RootPlotGameObject;
            twinPlantCom.iPlotGameObject = RootPlotGameObject;
            twinPlantCom._marker = _marker;
            //更好的方案effect

            SetTwin(twinPlant.GetComponent<DualHeadPlantComponent>());
            
            SetDualHead(true);

            //重建Effect
            ApplyDualHeadBonuses(_PlantI, twinPlant);

            if (_marker == null || !dualHead) PUtil.LogDebug($"[双头株] 重建完成 Plant [{_PlantI.name}] twin:[{twin.gameObject.name}] _markerPrimary:[{_marker.primaryPlant.name}] dualHead:[{dualHead}]");
        }
        public void StartDualHead()
        {
            PUtil.LogDebug($"[双头株] StartDualHead");
            if (RootPlotGameObject == null) return;
            //母株 设定
            if (TryGetComponent(out MutantPlant mp) &&
                             mp.MutationIDs?.Contains(PlantMutationRegister.DUAL_HEAD_MUT_ID) == true)
            {
                // === 主株逻辑 ===
                if (_marker.primaryPlant == null) _marker.primaryPlant = gameObject;//为何丢失主株信息?
            }
            //子株 设定
            if (_marker != null && _marker.primaryPlant != _PlantI)
            {
                //子株创建，为母株创建PlantablePlot
                var plantablePlotGO = PlantablePlotGameObject.GetGameObject(RootPlotGameObject);
                if (plantablePlotGO != null)
                {
                    plantablePlotGO.SetActive(true);
                    var plot = plantablePlotGO.AddOrGet<PlantablePlot>();
                    iPlotGameObject = plot.gameObject;
                    plot.InitializeComponent();

                    _marker.primaryPlant.transform.SetParent(plot.transform);
                    PlantMigrationHelper.MigratePlantToPlot(_marker.primaryPlant, plot);
                }
                // === 子株逻辑 ===
                // 找到主株，尝试配对
                var primaryComp = _marker.primaryPlant.GetComponent<DualHeadPlantComponent>();
                if (primaryComp != null)
                {
                    PUtil.LogDebug($"[双头株] 开始双向配对与应用双头增益");
                    // 双向配对
                    SetTwin(primaryComp);

                    // 应用双头增益（由子株触发）
                    ApplyDualHeadBonuses(_marker.primaryPlant, gameObject);
                        
                    SetDualHead(true);
                }
            }
        }
        private void ApplyDualHeadBonuses(GameObject primary, GameObject secondary)
        {
            BreakSymbiosis(primary);
            BreakSymbiosis(secondary);
            EstablishSymbiosis(primary, secondary);
        }

        protected override void OnCleanUp()
        {
            if( _marker != null && _marker.primaryPlant == _PlantI) { 
                _marker.primaryPlant = null;
                //母株unactive Plot
                PlantablePlotGameObject.setActive(RootPlotGameObject, false);
            }
            if (dualHead) {

                //清理关联引用与增益
                var dulHeadPlantCom = _PlantI.GetComponent<DualHeadPlantComponent>();
                BreakSymbiosis(dulHeadPlantCom?.twin.gameObject);
                dulHeadPlantCom.twin = null;

                //断开配对
                Unpair();
                SetDualHead(false);
            }
            base.OnCleanUp();
        }
        private bool SetDualHead(bool flag = false)
        {
            // 检查 twin 是否有效
            if (twin == null)
            {
                dualHead = false;
                return false;
            }

            // 获取标记组件
            if (_marker == null || _marker.primaryPlant == null)
            {
                dualHead = false;
                return false;
            }

            // 获取主植物的 DualHead 组件
            var primaryPlantDualHeadCom = _marker.primaryPlant.GetComponent<DualHeadPlantComponent>();
            if (primaryPlantDualHeadCom == null)
            {
                dualHead = false;
                return false;
            }

            // 同步设置 dualHead 状态
            dualHead = flag;
            primaryPlantDualHeadCom.dualHead = flag;

            return true;
        }
        void BreakSymbiosis(GameObject plant)
        {
            Effects effectsComp = plant?.GetComponent<Effects>();
            if (effectsComp != null && effectsComp.HasEffect(MutantEffects.DUAL_HEAD_SYMBIOSIS)){
                effectsComp.Remove(MutantEffects.DUAL_HEAD_SYMBIOSIS);
                var controller = plant.GetComponent<DualHeadSymbiosisEffectController>();
                controller?.RemoveEffect();
            }
        }
        void EstablishSymbiosis(GameObject plantA, GameObject plantB)
        {
            Effects effectsComp = plantA.AddOrGet<Effects>();
            if (effectsComp != null && !effectsComp.HasEffect(MutantEffects.DUAL_HEAD_SYMBIOSIS))
            {
                effectsComp.Add(MutantEffects.DUAL_HEAD_SYMBIOSIS, true);
                var controller = plantA.AddOrGet<DualHeadSymbiosisEffectController>();
                controller.twin = plantB;
                controller.ApplyEffect();
            }
            effectsComp = plantB.AddOrGet<Effects>();
            if (effectsComp != null && !effectsComp.HasEffect(MutantEffects.DUAL_HEAD_SYMBIOSIS))
            {
                effectsComp.Add(MutantEffects.DUAL_HEAD_SYMBIOSIS, true);
                var controller = plantB.AddOrGet<DualHeadSymbiosisEffectController>();
                controller.twin = plantB;
                controller.ApplyEffect();
            }
        }
        public void SetTwin(DualHeadPlantComponent p)
        {
            if (twin == p) return;
            Unpair();
            twin = p;
            if (p != null && p.twin != this)
            {
                p.twin = this;
            }
        }

        public void Unpair()
        {
            if (twin != null)
            {
                // 断开双向引用
                if (twin.twin == this)
                {
                    twin.twin = null;
                }
                twin = null;
            }
        }
    }
    [HarmonyPatch(typeof(KPrefabID), "OnSpawn")]
    public static class EnsureComponentOnSpawn
    {
        [HarmonyPostfix]
        public static void Postfix(KPrefabID __instance)
        {
            if (__instance.HasTag(GameTags.Plant))
            {
                __instance.gameObject.AddOrGet<DualHeadPlantComponent>();
            }
        }
    }
}