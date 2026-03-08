using Klei.AI;
using MutantContainmentProject.MutanterEffect;
using STRINGS;
using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    /**
     * 畸变体建筑破坏组件 (MutanterBuildingDestruction)
     * 功能: 当畸变体未被收容时，持续破坏周围的建筑
     * 工作机制:
     * 1. 检测周围的建筑（参考 EmotionMonitor 的 SpaceProbe 方法）
     * 2. 当畸变体没有 MUTANTER_CONTAINED_EFFECT 效果时，对周围建筑造成伤害
     * 3. 每 7.5 秒对每个建筑造成 1 点伤害
     * 4. 当建筑生命值归零时，破坏建筑并归还材料
     */
    public class MutanterBuildingDestruction : GameStateMachine<MutanterBuildingDestruction, MutanterBuildingDestruction.StatesInstance, IStateMachineTarget, MutanterBuildingDestruction.Def>
    {
        // --- 状态定义 ---
        public State idle;            // 默认状态，不破坏建筑
        public State destroying;      // 破坏建筑的状态

        public override void InitializeStates(out BaseState default_state)
        {
            default_state = idle;

            idle
                .Enter(smi =>
                {
                    smi.ResetDamageTimers();
                })
                .UpdateTransition(destroying, (smi, dt) =>
                {
                    return smi.ShouldStartDestruction();
                }, UpdateRate.SIM_1000ms);

            destroying
                .Enter(smi =>
                {
                    smi.ResetDamageTimers();
                })
                .Update("DestroyBuildings", (smi, dt) =>
                {
                    smi.DestroyNearbyBuildings(dt);
                    if (!smi.ShouldContinueDestruction())
                    {
                        smi.GoTo(idle);
                    }
                }, UpdateRate.SIM_4000ms);
        }

        public class StatesInstance : GameInstance
        {
            private Dictionary<Building, float> buildingDamageTimers = new Dictionary<Building, float>();
            private List<KPrefabID> buildings = new List<KPrefabID>();
            private EmotionMonitor.StatesInstance _emotionSMI;
            private object buildingsLock = new object();

            public StatesInstance(IStateMachineTarget master, Def def) : base(master, def)
            {
                ResetDamageTimers();
            }
            
            public EmotionMonitor.StatesInstance EmotionSMI
            {
                get
                {
                    if (_emotionSMI == null)
                    {
                        _emotionSMI = master.gameObject.GetSMI<EmotionMonitor.StatesInstance>();
                    }
                    return _emotionSMI;
                }
            }

            public void ResetDamageTimers()
            {
                buildingDamageTimers.Clear();
            }

            public bool ShouldStartDestruction()
            {
                // 检查是否应该开始破坏建筑
                // 当畸变体没有 MUTANTER_CONTAINED_EFFECT 效果时开始破坏
                var effects = master.gameObject.GetComponent<Effects>();
                return effects == null || !effects.HasEffect(MutanterEffects.MUTANTER_CONTAINED_EFFECT);
            }

            public bool ShouldContinueDestruction()
            {
                // 检查是否应该继续破坏建筑
                return ShouldStartDestruction();
            }

            public void DestroyNearbyBuildings(float dt)
            {
                // 获取周围的建筑
                if (EmotionSMI != null)
                {
                    buildings = EmotionSMI.GetBuildings();
                }
                
                // 对每个建筑造成伤害
                if (buildings != null)
                {
                    foreach (var buildingPrefab in buildings)
                    {
                        if (buildingPrefab == null) continue;

                        Building building = buildingPrefab.GetComponent<Building>();
                        if (building == null) continue;

                        // 跳过收容瓷砖建筑
                        if (buildingPrefab.HasTag(MutanterTags.MutanterBuildings))
                        {
                            continue;
                        }

                        // 检查建筑是否有生命值组件
                        BuildingHP buildingHP = building.GetComponent<BuildingHP>();
                        if (buildingHP == null) continue;

                        // 更新伤害计时器
                        if (!buildingDamageTimers.ContainsKey(building))
                        {
                            buildingDamageTimers[building] = 0f;
                        }

                        buildingDamageTimers[building] += dt;

                        // 每 7.5 秒造成 1 点伤害
                        if (buildingDamageTimers[building] >= def.damageInterval)
                        {
                            buildingDamageTimers[building] -= def.damageInterval;
                            ApplyDamageToBuilding(building, buildingHP);
                        }
                    }
                }
            }

            private void ApplyDamageToBuilding(Building building, BuildingHP buildingHP)
    {
        // 对建筑造成伤害
        building.BoxingTrigger<BuildingHP.DamageSourceInfo>(-794517298, new BuildingHP.DamageSourceInfo()
        {
            damage = def.damagePerInterval,
            source = STRINGS.MUTANTERS.STATUSITEMS.BUILDINGDESTRUCTION.SOURCE,
            popString = STRINGS.MUTANTERS.STATUSITEMS.BUILDINGDESTRUCTION.POP_STRING
        });

        // 为建筑添加伤害状态项
        KSelectable selectable = building.GetComponent<KSelectable>();
        if (selectable != null)
        {
            // 检查是否已经添加了状态项
            if (!selectable.HasStatusItem(MutanterStatusItems.Instance.BuildingDestruction))
            {
                selectable.AddStatusItem(MutanterStatusItems.Instance.BuildingDestruction, this);
            }
        }

        // 检查建筑是否被破坏
        if (buildingHP.HitPoints <= 0)
        {
            // 破坏建筑并归还材料
            DestroyBuilding(building);
        }
    }

            private void DestroyBuilding(Building building)
            {
                if (building == null) return;

                // 移除建筑的伤害状态项
                KSelectable selectable = building.GetComponent<KSelectable>();
                if (selectable != null)
                {
                    selectable.RemoveStatusItem(MutanterStatusItems.Instance.BuildingDestruction);
                }

                // 归还建筑材料
                Deconstructable deconstructable = building.GetComponent<Deconstructable>();
                if (deconstructable != null)
                {
                    deconstructable.ForceDestroyAndGetMaterials();
                }
                else
                {
                    // 没有 Deconstructable 组件时的备用处理
                    BuildingDef def = building.Def;
                    if (def != null)
                    {
                        PrimaryElement primaryElement = building.GetComponent<PrimaryElement>();
                        if (primaryElement != null)
                        {
                            float temperature = primaryElement.Temperature;
                            byte diseaseIdx = primaryElement.DiseaseIdx;
                            int diseaseCount = primaryElement.DiseaseCount;

                            // 生成材料
                            float[] masses = def.Mass;
                            Tag[] constructionElements = new Tag[masses.Length];
                            
                            // 尝试获取建筑的 constructionElements
                            if (building.TryGetComponent<Deconstructable>(out Deconstructable tempDeconstructable) && tempDeconstructable.constructionElements != null && tempDeconstructable.constructionElements.Length > 0)
                            {
                                constructionElements = tempDeconstructable.constructionElements;
                            }
                            else
                            {
                                // 使用建筑的主要元素作为材料
                                for (int i = 0; i < masses.Length; i++)
                                {
                                    constructionElements[i] = primaryElement.Element.tag;
                                }
                            }

                            // 生成材料实体
                            for (int i = 0; i < masses.Length; i++)
                            {
                                float mass = masses[i];
                                Tag materialTag = constructionElements[i];

                                // 生成材料
                                Element element = ElementLoader.GetElement(materialTag);
                                if (element != null)
                                {
                                    Vector3 position = building.transform.position;
                                    int cell = Grid.PosToCell(position);
                                    Vector3 spawnPos = Grid.CellToPosCBC(cell, Grid.SceneLayer.Ore);
                                    GameObject materialObject = element.substance.SpawnResource(spawnPos, mass, temperature, diseaseIdx, diseaseCount);
                                    if (materialObject != null)
                                    {
                                        // 添加掉落效果
                                        if (GameComps.Fallers.Has(materialObject))
                                        {
                                            GameComps.Fallers.Remove(materialObject);
                                        }
                                        GameComps.Fallers.Add(materialObject, new Vector2(UnityEngine.Random.Range(-1f, 1f) * 0.5f, 4f));
                                    }
                                }
                            }
                        }
                    }
                }

                // 破坏建筑
                Util.KDestroyGameObject(building.gameObject);

                // 从计时器中移除
                if (buildingDamageTimers.ContainsKey(building))
                {
                    buildingDamageTimers.Remove(building);
                }
            }
        }

        public class Def : BaseDef
        {
            // 伤害间隔（秒）
            public float damageInterval = 7.5f;

            // 每次伤害的点数
            public int damagePerInterval = 3;
        }
    }
}
