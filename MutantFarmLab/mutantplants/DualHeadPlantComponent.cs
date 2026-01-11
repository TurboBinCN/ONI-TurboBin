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
        [SerializeField]
        public GameObject primaryPlant;
    }
    public class DualHeadPlantComponent : KMonoBehaviour, ISaveLoadable
    {
        [SerializeField]
        public DualHeadPlantComponent twin;

        [SerializeField]
        public bool dualHead = false;
        [SerializeField]
        public GameObject RootPlotGameObject;
        [SerializeField]
        public GameObject iPlotGameObject;

        private DualHeadReceptacleMarker _marker;
        private GameObject _PlantI;

        private bool IsTargetFarmTile(GameObject targetObj)
        {
            KPrefabID prefabId = targetObj.GetComponent<KPrefabID>();
            if (prefabId != null && prefabId.HasTag(GameTags.FarmTiles)) return true;
            if (targetObj.name.Contains("FarmTile")) return true;
            return targetObj.name.Contains("Hydroponic");
        }
        protected override void OnSpawn()
        {
            base.OnSpawn();

            _PlantI = gameObject;
            //读档重建数据
            var mutantPlantCom = _PlantI.GetComponent<MutantPlant>();
            if (mutantPlantCom == null || mutantPlantCom.MutationIDs == null ||!mutantPlantCom.MutationIDs.Contains(PlantMutationRegister.DUAL_HEAD_MUT_ID)) return;
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
                // 1. 计算「自身中心±上下左右1格」的目标格子ID
                int targetCell = Grid.OffsetCell(centerCell, offset);

                // ❗ 过滤1：目标格子无效 → 直接跳过
                if (!Grid.IsValidCell(targetCell)) continue;

                // ❗ 核心判定（严格匹配规则）：该格子是否被【当前Adapter自己】占据
                //GameObject cellObj = Grid.Objects[targetCell, (int)ObjectLayer.Plants];
                //bool isSelfOccupyCell = cellObj != null && cellObj == this.gameObject;
                //if (!isSelfOccupyCell) continue; // 不是自己占据的格子 → 直接跳过

                // ✅ 满足所有规则：自身中心±1格 + 是自己占据的格子 → 检测种植砖
                GameObject farmTileObj = Grid.Objects[targetCell, (int)ObjectLayer.FoundationTile];
                if (farmTileObj == null) continue;

                // ✅ 精准判定种植砖，命中即绑定
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
            var sub = RootPlotGameObject.transform.Find(PlantablePlotGameObject.storageName);
            if (sub != null)
            {
                var plot = sub.gameObject.AddOrGet<PlantablePlot>();
                iPlotGameObject = plot.gameObject;
                plot.InitializeComponent();
                gameObject.transform.SetParent(plot.transform);
                PlantMigrationHelper.MigratePlantToPlot(gameObject, plot);

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

            // 应用双头增益（由子株触发）
            //TODO 重建Effect
            ApplyDualHeadBonuses(_PlantI, twinPlant);

            if (_marker == null || !dualHead) PUtil.LogDebug($"[双头株] 重建完成 Plant [{_PlantI.name}] twin:[{twin.gameObject.name}] _markerPrimary:[{_marker.primaryPlant.name}] dualHead:[{dualHead}]");
        }
        public void StartDualHead()
        {
            PUtil.LogDebug($"[双头株] StartDualHead");
            if (RootPlotGameObject == null) return;
            var marker = RootPlotGameObject.AddOrGet<DualHeadReceptacleMarker>();
            //母株 设定
            if (TryGetComponent(out MutantPlant mp) &&
                             mp.MutationIDs?.Contains(PlantMutationRegister.DUAL_HEAD_MUT_ID) == true)
            {
                // === 主株逻辑 ===
                if (marker.primaryPlant == null) marker.primaryPlant = gameObject;//为何丢失主株信息?
            }
            //子株 设定
            if (marker != null && marker.primaryPlant != gameObject)
            {
                //子株创建，为母株创建PlantablePlot
                TbbDebuger.PrintGameObjectFullInfo(RootPlotGameObject);
                var sub = RootPlotGameObject.transform.Find(PlantablePlotGameObject.storageName);
                if (sub != null)
                {
                    var plot = sub.gameObject.AddOrGet<PlantablePlot>();
                    iPlotGameObject = plot.gameObject;
                    plot.InitializeComponent();
                    marker.primaryPlant.transform.SetParent(plot.transform);
                    PlantMigrationHelper.MigratePlantToPlot(marker.primaryPlant, plot);

                    PUtil.LogDebug($"[双头株] SubGameObject");
                    TbbDebuger.PrintGameObjectFullInfo(sub.gameObject);
                    PUtil.LogDebug($"[双头株] 母株迁移Plot :[{plot.gameObject.name}]");
                    PUtil.LogDebug($"[双头株] 迁移后PlantablePlot");
                    TbbDebuger.PrintGameObjectFullInfo(RootPlotGameObject);
                }
                // === 子株逻辑 ===
                // 找到主株，尝试配对
                var primaryComp = marker.primaryPlant.GetComponent<DualHeadPlantComponent>();
                if (primaryComp != null)
                {
                    PUtil.LogDebug($"[双头株] 开始双向配对与应用双头增益");
                    // 双向配对
                    SetTwin(primaryComp);

                    // 应用双头增益（由子株触发）
                    ApplyDualHeadBonuses(marker.primaryPlant, gameObject);
                        
                    SetDualHead(true);
                    PUtil.LogDebug($"PrimaryPlant:[{marker.primaryPlant.name}] [{marker.primaryPlant.GetComponent<DualHeadPlantComponent>().dualHead}] childPlant:[{gameObject.name}] [{dualHead}] ");
                }
            }
        }
        private void ApplyDualHeadBonuses(GameObject primary, GameObject secondary)
        {
            BreakSymbiosis(primary);
            EstablishSymbiosis(primary, secondary);
        }

        protected override void OnCleanUp()
        {
            if (dualHead) { 
                // 如果是主株，清理 receptacle 上的 marker 引用
                if (TryGetComponent(out MutantPlant mp) &&
                    mp.MutationIDs?.Contains(PlantMutationRegister.DUAL_HEAD_MUT_ID) == true)
                {
                    var receptacleGo = transform.parent?.gameObject;
                    if (receptacleGo != null)
                    {
                        var marker = receptacleGo.GetComponent<DualHeadReceptacleMarker>();
                        if (marker != null && marker.primaryPlant == gameObject)
                        {
                            marker.primaryPlant = null;
                            // 不 Destroy(marker)，保留组件避免反复 Add/Remove
                        }
                    }
                }
                //清理增益
                BreakSymbiosis(gameObject);
                Unpair();
            }
            //清理plantableplot
            //if(plot.gameObject.name == SubGameObjectWithStorage.gameObjectName)
            //{
            //    //TODO 掉出Storage中的东西以及种子
            //    Object.Destroy(plot.gameObject);
            //    gameObject.transform.SetParent(null, false);
            //}
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
            var marker = RootPlotGameObject.GetComponent<DualHeadReceptacleMarker>();
            if (marker == null || marker.primaryPlant == null)
            {
                dualHead = false;
                return false;
            }

            // 获取主植物的 DualHead 组件
            var primaryPlantDualHeadCom = marker.primaryPlant.GetComponent<DualHeadPlantComponent>();
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
            Effects effectsComp = plant.GetComponent<Effects>();
            if (effectsComp != null && effectsComp.HasEffect(MutantEffects.DUAL_HEAD_SYMBIOSIS)){
                effectsComp.Remove(MutantEffects.DUAL_HEAD_SYMBIOSIS);
                var controller = plant.GetComponent<DualHeadSymbiosisEffectController>();
                controller?.RemoveEffect();
            }

            effectsComp = plant.GetComponent<DualHeadPlantComponent>()?.twin?.gameObject.GetComponent<Effects>();
            if (effectsComp != null && effectsComp.HasEffect(MutantEffects.DUAL_HEAD_SYMBIOSIS))
            {
                effectsComp.Remove(MutantEffects.DUAL_HEAD_SYMBIOSIS);
                var controller = plant.GetComponent<DualHeadPlantComponent>().twin?.gameObject.GetComponent<DualHeadSymbiosisEffectController>();
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