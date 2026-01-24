// DualHeadPlantComponent.cs
using HarmonyLib;
using Klei.AI;
using MutantFarmLab.tbbLibs;
using PeterHan.PLib.Core;
using System;
using UnityEngine;

namespace MutantFarmLab.mutantplants
{
    public class DualHeadReceptacleMarker : KMonoBehaviour,ISaveLoadable
    {
        [SerializeField]
        public GameObject primaryPlant;
    }
    /**
     * DualHeadPlantComponent为动态加载无法存储任何数据
     * 加载时机：植株创建与读档时
     */
    public class DualHeadPlantComponent : KMonoBehaviour, ISaveLoadable
    {
        //双头株状态下相关属性
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
        private bool isDulHeadMutantPlant()
        {
            return TryGetComponent(out MutantPlant mp) &&
                             mp.MutationIDs?.Contains(PlantMutationRegister.DUAL_HEAD_MUT_ID) == true;
        }
        protected override void OnSpawn()
        {
            base.OnSpawn();

            _PlantI = gameObject;

            //=情况1：双头株变异植株，并未开启第二株种植状态，植株未被转移过-->阻断;判断依据：自己占据FramTile，
            //=情况5：子株读档重建 与 情况1 相同逻辑 -->阻断
            //=情况4：子株创建时OnSpawn 由子株子株调用增益创建
            if (_PlantI.GetComponent<ReceptacleMonitor>()?.GetReceptacle()?.Occupant == _PlantI) return ;
            //=情况6：双头株变异植株，自己状态未知，判断为：读档重建数据
            if (RootPlotGameObject == null && isDulHeadMutantPlant()){

                PUtil.LogDebug($"[双头株]Plant:[{_PlantI.name}] 开始数据重建.");
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
            }

            if (RootPlotGameObject == null)
            {
                PUtil.LogDebug($"[双头株]Plant:[{_PlantI.name}] 未找到种植砖，结束");
                return;
            }
            _marker = RootPlotGameObject?.GetComponent<DualHeadReceptacleMarker>();
            
            if(_marker?.primaryPlant == null)
            {
                _marker.primaryPlant = _PlantI;
            }

            //子株占据Farmtile
            var twinPlant = RootPlotGameObject.GetComponent<PlantablePlot>().Occupant;
            //=情况2：双头株变异植株，开启第二株，并未种植
            //占据格子空或者的是自己 --> 母株重建结束
            if (twinPlant == null || twinPlant == _PlantI) return;

            //=情况3：子母株同时存在
            //子株逻辑：建立增益
            //PlantablePlotGameObject加载或许过晚导致没有plot
            PUtil.LogDebug($"[双头株] 母株：[{_PlantI.name}] [{_PlantI.GetComponent<ReceptacleMonitor>()?.GetReceptacle()?.gameObject?.name}] 子株: [{twinPlant.name}] [{twinPlant.GetComponent<ReceptacleMonitor>()?.GetReceptacle()?.gameObject?.name}]");
            //注释:DualHeadSideScreen ClickHandler中完成初次迁移
            //读档需要二次种植到PlantablePlot上，原因：farmtile上的子gameobject上的plantableplot不能在游戏载入中载入
            //确定有两株植物-->母株迁移Plot
            var plantablePlotGO = PlantablePlotGameObject.GetGameObject(RootPlotGameObject);
            if (plantablePlotGO != null)
            {
                plantablePlotGO.SetActive(true);
                var plot = plantablePlotGO.AddOrGet<PlantablePlot>();
                iPlotGameObject = plot.gameObject;

                plot.InitializeComponent();
                _PlantI.transform.SetParent(plot.transform);
                PlantMigrationHelper2.MigratePlant(_PlantI, plot);
                PUtil.LogDebug($"[双头株] 完成母株[{_PlantI.name}]迁移Plot:[{plot.gameObject.name}]");
            }

            //确定有两株植物-->重建子株并绑定
            var twinPlantCom = twinPlant.AddOrGet<DualHeadPlantComponent>();
            twinPlantCom.RootPlotGameObject = RootPlotGameObject;
            twinPlantCom.iPlotGameObject = RootPlotGameObject;
            twinPlantCom._marker = _marker;
            twinPlantCom.SetTwin(this);

            SetTwin(twinPlantCom);
            
            SetDualHead(true);

            //重建Effect
            ApplyDualHeadBonuses(_PlantI, twinPlant);

            PUtil.LogDebug($"[双头株] 完成绑定与Effect 母株[{_PlantI.name}] 子株:[{twin.gameObject.name}] 标记:[{_marker.primaryPlant.name}] dualHead:[{dualHead}]");
        }
        /**
         * 由子株调用
         */
        public void StartDualHead() {
            var twinPlant = _marker.primaryPlant;
            //确定有两株植物-->重建子株并绑定
            var twinPlantCom = twinPlant.AddOrGet<DualHeadPlantComponent>();
            twinPlantCom.RootPlotGameObject = RootPlotGameObject;
            twinPlantCom.iPlotGameObject = RootPlotGameObject;
            twinPlantCom._marker = _marker;
            twinPlantCom.SetTwin(this);

            SetTwin(twinPlantCom);

            SetDualHead(true);

            //重建Effect
            ApplyDualHeadBonuses(_PlantI, twinPlant);

            PUtil.LogDebug($"[双头株] 完成绑定与Effect 母株[{twinPlant.name}] 子株:[{_PlantI.name}] 标记:[{_marker.primaryPlant.name}] dualHead:[{dualHead}]");
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
                PUtil.LogDebug($"[双头株]Plant:[{_PlantI.name}]CleanUP 开始清理[{dulHeadPlantCom?.twin.gameObject.name}]共生状态.");
                BreakSymbiosis(dulHeadPlantCom?.twin.gameObject);

                //断开配对
                SetDualHead(false);
                Unpair();
            }
            base.OnCleanUp();
        }
        private bool SetDualHead(bool flag = false)
        {
            // 检查 twin 是否有效
            if (twin == null)dualHead = false;


            var twinPlantDualHeadCom = twin?.GetComponent<DualHeadPlantComponent>();

            // 同步设置 dualHead 状态
            if(twinPlantDualHeadCom!= null){
                dualHead = flag;
                twinPlantDualHeadCom.dualHead = flag;
            }

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

    /******************************************************************************
     * 补丁1：植株生成后执行【100%匹配源码】
     * 目标方法：PlantablePlot.ConfigureOccupyingObject(GameObject source)
     * 核心作用：1.关闭autoReplaceEntity，允许双株共存 2.给双头株挂载DHP组件 
     * 3.给第二株挂载DHCom组件
     *****************************************************************************/
    [HarmonyPatch(typeof(PlantablePlot), "ConfigureOccupyingObject")]
    public static class DualHeadPlotConfigPatch
    {
        [HarmonyPostfix]
        public static void Postfix(PlantablePlot __instance, GameObject newPlant)
        {
            if (!PlantMutationRegister.DUAL_HEAD_ENABLED) return;
            if (__instance == null || newPlant == null) return;
            var receptacleGo = __instance.gameObject;
            var marker = receptacleGo.AddOrGet<DualHeadReceptacleMarker>();

            // 情况1：新植物是双头变异株 → 成为第一株
            if (newPlant.TryGetComponent(out MutantPlant mutant)
                && mutant.MutationIDs?.Contains(PlantMutationRegister.DUAL_HEAD_MUT_ID) == true)
            {
                var dualHeadPlantCom = newPlant.AddOrGet<DualHeadPlantComponent>();
                dualHeadPlantCom.RootPlotGameObject = __instance.gameObject;

                marker.primaryPlant = newPlant;
                PUtil.LogDebug($"[双头株] 母株[{marker.primaryPlant.name}]种植配置");
                // 锁定 receptacle
                __instance.autoReplaceEntity = false;
            }
            // 情况2：receptacle 已有 Marker → 此次是第二株
            else if (marker != null && marker.primaryPlant != null)
            {
                var secondDHP = newPlant.AddOrGet<DualHeadPlantComponent>();
                secondDHP.RootPlotGameObject = __instance.gameObject;
                secondDHP.StartDualHead();

                PUtil.LogDebug($"[双头株] 子株种植配置 [子:{secondDHP.name} 母:{marker.primaryPlant.name}]");
            }
        }
    }
}