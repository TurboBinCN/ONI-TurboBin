// DualHeadPlantComponent.cs
using HarmonyLib;
using Klei.AI;
using MutantFarmLab.tbbLibs;
using UnityEngine;
using System.Collections.Generic;

namespace MutantFarmLab.mutantplants
{
    public class DualHeadReceptacleMarker : KMonoBehaviour, ISaveLoadable
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
            if (prefabId != null && prefabId.HasTag(GameTags.CodexCategories.FarmBuilding)) return true;
            if (targetObj.name.Contains("FarmTile")) return true;
            if (targetObj.name.Contains("BackwallFarm")) return true;
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
            bool GameLoad = false;

            //===母株： 需要找到自己RootPlotGameObject
            if (RootPlotGameObject == null)
            {
                RootPlotGameObject = _PlantI.GetComponent<ReceptacleMonitor>()?.GetReceptacle()?.gameObject;//占据Farmtile:母株(没有开启第二种植槽)/子株
                GameLoad = true;
                
                // 检查receptacle是否有效且有DualHeadReceptacleMarker
                if (RootPlotGameObject != null)
                {
                    var marker = RootPlotGameObject.GetComponent<DualHeadReceptacleMarker>();
                    if (marker == null)
                    {
                        // 尝试从父对象查找marker
                        marker = RootPlotGameObject.transform.parent?.GetComponent<DualHeadReceptacleMarker>();
                        if (marker != null)
                        {
                            RootPlotGameObject = marker.gameObject;
                        }
                    }
                    if (marker != null)
                    {
                    }
                    else
                    {
                        RootPlotGameObject = null;
                    }
                }
            }
            
            if (RootPlotGameObject == null && isDulHeadMutantPlant())
            {
                int centerCell = Grid.PosToCell(_PlantI);
                if (!Grid.IsValidCell(centerCell)) return;
                
                int plantX = Grid.CellToXY(centerCell).x;
                int plantY = Grid.CellToXY(centerCell).y;
                TbbDebuger.LogDebug($"[双头株]Plant:[{_PlantI.name}] 植物中心cell={centerCell}, XY=({plantX},{plantY}), 世界坐标={_PlantI.transform.GetPosition()}");

                CellOffset[] checkOffsets = new[]
                {
                    CellOffset.none,
                    new CellOffset(0, 1),
                    new CellOffset(0, -1),
                    new CellOffset(1, 0),
                    new CellOffset(-1, 0),
                    new CellOffset(1, 1),
                    new CellOffset(1, -1),
                    new CellOffset(-1, 1),
                    new CellOffset(-1, -1)
                };

                List<GameObject> candidateFarmTiles = new List<GameObject>();
                
                foreach (var offset in checkOffsets)
                {
                    int targetCell = Grid.OffsetCell(centerCell, offset);
                    if (!Grid.IsValidCell(targetCell)) continue;

                    GameObject farmTileObj = Grid.Objects[targetCell, (int)ObjectLayer.FoundationTile];
                    if (farmTileObj == null)
                    {
                        farmTileObj = Grid.Objects[targetCell, (int)ObjectLayer.Backwall];
                    }
                    if (farmTileObj == null) continue;

                    if (IsTargetFarmTile(farmTileObj))
                    {
                        var building = farmTileObj.GetComponent<Building>();
                        if (building != null && building.Def != null)
                        {
                            int buildingWidth = building.Def.WidthInCells;
                            int buildingHeight = building.Def.HeightInCells;
                            int buildingCell = Grid.PosToCell(farmTileObj);
                            
                            int buildingX = Grid.CellToXY(buildingCell).x;
                            int buildingY = Grid.CellToXY(buildingCell).y;
                            
                            int minX = buildingX;
                            int minY = buildingY;
                            int maxX = minX + buildingWidth - 1;
                            int maxY = minY + buildingHeight - 1;
                            
                            // 输出建筑占据的所有cell坐标
                            string occupiedCells = "";
                            for (int x = minX; x <= maxX; x++)
                            {
                                for (int y = minY; y <= maxY; y++)
                                {
                                    int cell = Grid.XYToCell(x, y);
                                    occupiedCells += $"({x},{y})={cell}, ";
                                }
                            }
                            
                            // 检查原生PlantablePlot的Occupant
                            var nativePlot = farmTileObj.GetComponent<PlantablePlot>();
                            string occupantName = nativePlot?.Occupant?.name ?? "null";
                            
                            bool isInRange = plantX >= minX && plantX <= maxX && plantY >= minY && plantY <= maxY;
                            
                            TbbDebuger.LogDebug($"[双头株]Plant:[{_PlantI.name}] 搜索到建筑={farmTileObj.name}, 锚点cell={buildingCell}, XY=({buildingX},{buildingY}), 尺寸={buildingWidth}x{buildingHeight}, 占据格子: {occupiedCells}");
                            TbbDebuger.LogDebug($"[双头株]Plant:[{_PlantI.name}]   植物位置=({plantX},{plantY}), 是否在范围内={isInRange}, 原生Plot.Occupant={occupantName}");
                            
                            if (isInRange)
                            {
                                candidateFarmTiles.Add(farmTileObj);
                            }
                            else
                            {
                            }
                        }
                        else
                        {
                            candidateFarmTiles.Add(farmTileObj);
                            TbbDebuger.LogDebug($"[双头株]Plant:[{_PlantI.name}] 候选种植砖={farmTileObj.name} (无Building组件)");
                        }
                    }
                }
                
                if (candidateFarmTiles.Count > 0)
                {
                    
                    if (candidateFarmTiles.Count == 1)
                    {
                        RootPlotGameObject = candidateFarmTiles[0];
                    }
                    else
                    {
                        GameObject bestMatch = null;
                        float minDistance = float.MaxValue;
                        
                        foreach (var farmTileObj in candidateFarmTiles)
                        {
                            var building = farmTileObj.GetComponent<Building>();
                            if (building != null && building.Def != null)
                            {
                                int buildingCell = Grid.PosToCell(farmTileObj);
                                int buildingWidth = building.Def.WidthInCells;
                                int buildingHeight = building.Def.HeightInCells;
                                
                                int buildingCenterX = Grid.CellToXY(buildingCell).x + (buildingWidth - 1) / 2;
                                int buildingCenterY = Grid.CellToXY(buildingCell).y + (buildingHeight - 1) / 2;
                                
                                float distance = Mathf.Abs(buildingCenterX - plantX) + Mathf.Abs(buildingCenterY - plantY);
                                
                                if (distance < minDistance)
                                {
                                    minDistance = distance;
                                    bestMatch = farmTileObj;
                                }
                                
                                TbbDebuger.LogDebug($"[双头株]Plant:[{_PlantI.name}] 候选={farmTileObj.name}, 建筑中心=({buildingCenterX},{buildingCenterY}), 植物中心=({plantX},{plantY}), 距离={distance}");
                            }
                        }
                        
                        if (bestMatch != null)
                        {
                            RootPlotGameObject = bestMatch;
                        }
                        else
                        {
                            RootPlotGameObject = candidateFarmTiles[0];
                        }
                    }
                }
            }
            if (RootPlotGameObject == null)
            {
                return;
            }
            _marker = RootPlotGameObject.GetComponent<DualHeadReceptacleMarker>();
            if (_marker == null)
            {
                return;
            }

            if (_marker.primaryPlant == null)
            {
                _marker.primaryPlant = _PlantI;
            }

            var plantablePlot = RootPlotGameObject.GetComponent<PlantablePlot>();
            if (plantablePlot == null)
            {
                return;
            }
            var OccupantPlant = plantablePlot.Occupant;

            TbbDebuger.LogDebug($"[双头株] 母株：[{_PlantI.name}] [{_PlantI.GetComponent<ReceptacleMonitor>()?.GetReceptacle()?.gameObject?.name}] OccupantPlant株: [{OccupantPlant?.name}] [{OccupantPlant?.GetComponent<ReceptacleMonitor>()?.GetReceptacle()?.gameObject?.name}]");

            //===读档时迁移操作，判断：读档 && 有双株 时机：母株重建时
            //注释:DualHeadSideScreen ClickHandler中完成初次迁移
            //读档需要二次种植到PlantablePlot上，原因：farmtile上的子gameobject上的plantableplot不能在游戏载入中载入
            //确定有两株植物-->母株迁移Plot
            if (GameLoad && _marker.primaryPlant == _PlantI)
            {
                var plantablePlotGO = PlantablePlotGameObject.GetGameObject(RootPlotGameObject);
                if (plantablePlotGO != null)
                {
                    plantablePlotGO.SetActive(true);
                    var plot = plantablePlotGO.AddOrGet<PlantablePlot>();
                    iPlotGameObject = plot.gameObject;

                    var currentReceptacle = _PlantI.GetComponent<ReceptacleMonitor>()?.GetReceptacle();
                    if (currentReceptacle != null && currentReceptacle.gameObject == plot.gameObject)
                    {
                    }
                    else
                    {
                        var originalPlot = RootPlotGameObject.GetComponent<PlantablePlot>();
                    if (originalPlot != null)
                    {
                        var building = RootPlotGameObject.GetComponent<Building>();
                        // 如果当前对象没有Building组件，或者Building尺寸为1x1但原始偏移不为(0,1,0)，尝试从父对象查找
                        if ((building == null || building.Def == null || (building.Def.WidthInCells == 1 && building.Def.HeightInCells == 1 && originalPlot.occupyingObjectRelativePosition != new Vector3(0f, 1f, 0f))) 
                            && RootPlotGameObject.transform.parent != null)
                        {
                            building = RootPlotGameObject.transform.parent.GetComponent<Building>();
                        }
                        
                        if (building != null && building.Def != null)
                        {
                            Vector3 centerOffset = new Vector3((building.Def.WidthInCells - 1) * 0.5f, (building.Def.HeightInCells - 1) * 0.5f, 0f);
                            plot.occupyingObjectRelativePosition = originalPlot.occupyingObjectRelativePosition - centerOffset;
                        }
                        else
                        {
                            plot.occupyingObjectRelativePosition = originalPlot.occupyingObjectRelativePosition;
                        }
                    }

                        plot.InitializeComponent();
                        PlantMigrationHelper2.MigratePlant(_PlantI, plot);
                    }
                }
            }
            //===绑定双株，设置增益，判断： 有双株 && 没有开启 双头株增益
            //确定有两株植物-->重建子株并绑定
            if (_marker.primaryPlant != null && !dualHead)
            {
                // 检查是否存在另一株植物（可能在不同种植槽）
                GameObject twinPlant = null;

                // 情况1：原生种植槽中有其他植物
                if (OccupantPlant != null && OccupantPlant != _PlantI)
                {
                    twinPlant = OccupantPlant;
                }
                // 情况2：主植物标记指向其他植物
                else if (_marker.primaryPlant != _PlantI)
                {
                    twinPlant = _marker.primaryPlant;
                }
                // 情况3：检查额外种植槽中是否有植物
                else
                {
                    var plantablePlotGO = PlantablePlotGameObject.GetGameObject(RootPlotGameObject);
                    if (plantablePlotGO != null)
                    {
                        var extraPlot = plantablePlotGO.GetComponent<PlantablePlot>();
                        if (extraPlot != null && extraPlot.Occupant != null && extraPlot.Occupant != _PlantI)
                        {
                            twinPlant = extraPlot.Occupant;
                        }
                    }
                }

                // 如果找到另一株植物，执行绑定
                if (twinPlant != null)
                {
                    var twinPlantCom = twinPlant.AddOrGet<DualHeadPlantComponent>();
                    if (twinPlantCom != twinPlant)
                    {
                        twinPlantCom.RootPlotGameObject = RootPlotGameObject;
                        twinPlantCom.iPlotGameObject = RootPlotGameObject;
                        twinPlantCom._marker = _marker;
                    }
                    twinPlantCom.SetTwin(this);

                    SetTwin(twinPlantCom);
                    SetDualHead(true);
                    ApplyDualHeadBonuses(_PlantI, twinPlant);

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
            if (_marker != null && _marker.primaryPlant == _PlantI)
            {
                _marker.primaryPlant = null;
                PlantablePlotGameObject.setActive(RootPlotGameObject, false);
            }
            
            // 同时清理主建筑上的marker（如果存在）
            if (RootPlotGameObject != null)
            {
                GameObject mainBuilding = RootPlotGameObject;
                if (RootPlotGameObject.transform.parent != null)
                {
                    mainBuilding = RootPlotGameObject.transform.parent.gameObject;
                }
                var mainMarker = mainBuilding.GetComponent<DualHeadReceptacleMarker>();
                if (mainMarker != null && mainMarker.primaryPlant == _PlantI)
                {
                    mainMarker.primaryPlant = null;
                }
            }

            if (dualHead)
            {
                var dulHeadPlantCom = _PlantI.GetComponent<DualHeadPlantComponent>();
                BreakSymbiosis(dulHeadPlantCom?.twin?.gameObject);

                SetDualHead(false);
                Unpair();
            }
            base.OnCleanUp();
        }
        private bool SetDualHead(bool flag = false)
        {
            // 检查 twin 是否有效
            if (twin == null) dualHead = false;


            var twinPlantDualHeadCom = twin?.GetComponent<DualHeadPlantComponent>();

            // 同步设置 dualHead 状态
            if (twinPlantDualHeadCom != null)
            {
                dualHead = flag;
                twinPlantDualHeadCom.dualHead = flag;
            }

            return true;
        }
        void BreakSymbiosis(GameObject plant)
        {
            Effects effectsComp = plant?.GetComponent<Effects>();
            if (effectsComp != null && effectsComp.HasEffect(MutantEffects.DUAL_HEAD_SYMBIOSIS))
            {
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
                controller.twin = plantA;
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
            
            // 查找marker：先从当前种植槽查找，如果没有则从父对象查找
            var plotGo = __instance.gameObject;
            var marker = plotGo.GetComponent<DualHeadReceptacleMarker>();
            GameObject mainBuilding = plotGo;
            
            if (marker == null && plotGo.transform.parent != null)
            {
                marker = plotGo.transform.parent.GetComponent<DualHeadReceptacleMarker>();
                if (marker != null)
                {
                    mainBuilding = plotGo.transform.parent.gameObject;
                }
            }
            
            // 如果还是没有，尝试从主建筑查找
            if (marker == null && plotGo.transform.parent != null)
            {
                foreach (Transform child in plotGo.transform.parent)
                {
                    if (child.GetComponent<Building>() != null)
                    {
                        marker = child.GetComponent<DualHeadReceptacleMarker>();
                        if (marker != null)
                        {
                            mainBuilding = child.gameObject;
                            break;
                        }
                    }
                }
            }
            
            // 如果还是没有，添加到主建筑上
            if (marker == null)
            {
                if (plotGo.GetComponent<Building>() != null)
                {
                    marker = plotGo.AddOrGet<DualHeadReceptacleMarker>();
                    mainBuilding = plotGo;
                }
                else if (plotGo.transform.parent != null && plotGo.transform.parent.GetComponent<Building>() != null)
                {
                    marker = plotGo.transform.parent.gameObject.AddOrGet<DualHeadReceptacleMarker>();
                    mainBuilding = plotGo.transform.parent.gameObject;
                }
                else
                {
                    marker = plotGo.AddOrGet<DualHeadReceptacleMarker>();
                    mainBuilding = plotGo;
                }
            }
            
            
            // 情况1：新植物是双头变异株 → 成为第一株
            if (newPlant.TryGetComponent(out MutantPlant mutant)
                && mutant.MutationIDs?.Contains(PlantMutationRegister.DUAL_HEAD_MUT_ID) == true)
            {
                var dualHeadPlantCom = newPlant.AddOrGet<DualHeadPlantComponent>();
                dualHeadPlantCom.RootPlotGameObject = mainBuilding;

                marker.primaryPlant = newPlant;
                // 锁定 receptacle
                __instance.autoReplaceEntity = false;
            }
            // 情况2：receptacle 已有 Marker → 此次是第二株
            else if (marker != null && marker.primaryPlant != null)
            {
                var secondDHP = newPlant.AddOrGet<DualHeadPlantComponent>();
                secondDHP.RootPlotGameObject = mainBuilding;

            }
        }
    }
}