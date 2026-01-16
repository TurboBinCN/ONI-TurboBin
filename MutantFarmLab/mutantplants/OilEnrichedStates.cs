using PeterHan.PLib.Core;
using STRINGS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MutantFarmLab.mutantplants
{
    public class OilEnrichedStates : StateMachineComponent<OilEnrichedStates.StatesInstance>
    {

        protected void DestroySelf(object callbackParam)
        {
            CreatureHelpers.DeselectCreature(base.gameObject);
            Util.KDestroyGameObject(base.gameObject);
        }


        protected override void OnSpawn()
        {
            base.OnSpawn();
            base.smi.StartSM();
        }


        protected override void OnCleanUp()
        {
            base.OnCleanUp();
        }


        protected override void OnPrefabInit()
        {
            base.Subscribe<OilEnrichedStates>(1309017699, OilEnrichedStates.OnReplantedDelegate);
            base.OnPrefabInit();
        }


        private void OnReplanted(object data = null)
        {
            this.SetConsumptionRate();
        }


        public void SetConsumptionRate()
        {
            if (this.receptacleMonitor.Replanted)
            {
                this.elementConsumer.consumptionRate = PlantMutationRegister.OIL_ENRICH_CARBONGAS_MOD;
                return;
            }
            this.elementConsumer.consumptionRate = PlantMutationRegister.OIL_ENRICH_CARBONGAS_MOD/4;
        }


        [MyCmpReq]
        private WiltCondition wiltCondition;
        [MyCmpReq]
        private ElementConsumer elementConsumer;
        [MyCmpReq]
        private ReceptacleMonitor receptacleMonitor;
        [MyCmpReq]
        private Growing growing;
        [MyCmpReq]
        private Storage swapStorage;


        private static readonly EventSystem.IntraObjectHandler<OilEnrichedStates> OnReplantedDelegate = new EventSystem.IntraObjectHandler<OilEnrichedStates>(delegate (OilEnrichedStates component, object data)
        {
            component.OnReplanted(data);
        });


        public class StatesInstance : GameStateMachine<States, StatesInstance, OilEnrichedStates, object>.GameInstance
        {
            private const float CONVERSION_RATIO_CO2_TO_OIL = 3.11666667f; // 1 单位 CO2 -> 1/3.11666667 单位 Oil
            private const int CO2_MASS_DIVISOR = 32; // 每次转化所需的 CO2 质量单位
            public StatesInstance(OilEnrichedStates master) : base(master)
            {
                Subscribe((int)GameHashes.OnStorageChange, OnStorageChangeHandler);
            }
            private void OnStorageChangeHandler(object obj)
            {
                // PUtil.LogDebug("[原油富集] onStorageChangeHandler");

                // 假设 master 是持有 Storage 组件的对象
                var storageComponent = master?.GetComponent<Storage>();
                if (storageComponent == null)
                {
                    PUtil.LogError($"[OilEnriched] Storage component is null in OnStorageChangeHandler for {master?.name}");
                    return;
                }

                var itemsInStorage = storageComponent.items;
                if (itemsInStorage == null || itemsInStorage.Count == 0)
                    return;

                // --- 1. 查找并计算理论上可以转化的CO2总量 ---
                // 同时记录每个物品及其可贡献的质量，以便后续扣除
                float totalPotentialCo2MassToConvert = 0f;
                List<(GameObject item, float massToRemoveFromThisItem)> potentialRemovals = new List<(GameObject, float)>();

                for (int i = 0; i < itemsInStorage.Count; i++)
                {
                    GameObject item = itemsInStorage[i];
                    if (item == null) continue;

                    var primaryElement = item.GetComponent<PrimaryElement>();
                    if (primaryElement != null && primaryElement.ElementID == SimHashes.CarbonDioxide)
                    {
                        float currentMass = primaryElement.Mass;
                        // 检查当前物品的质量是否满足转化条件（是 CO2_MASS_DIVISOR 的倍数）
                        int wholeUnits = (int)(currentMass / CO2_MASS_DIVISOR);
                        if (wholeUnits > 0)
                        {
                            float massToConvertThisItem = wholeUnits * CO2_MASS_DIVISOR;
                            totalPotentialCo2MassToConvert += massToConvertThisItem;

                            // 记录该物品及其理论上可贡献的质量
                            potentialRemovals.Add((item, massToConvertThisItem));
                        }
                    }
                }

                if (totalPotentialCo2MassToConvert <= 0)
                {
                    // 没有找到任何可以转化的CO2
                    return;
                }

                // --- 2. 根据存储容量计算实际可以转化的CO2质量 ---
                float potentialTotalOilMass = totalPotentialCo2MassToConvert / CONVERSION_RATIO_CO2_TO_OIL;
                float availableSpace = storageComponent.RemainingCapacity();

                // 计算受空间限制的实际能生成的原油质量
                float actualOilMassToGenerate = Mathf.Min(potentialTotalOilMass, availableSpace);
                // 反推实际需要消耗的CO2质量
                float actualCo2MassToConsume = actualOilMassToGenerate * CONVERSION_RATIO_CO2_TO_OIL;

                if (actualCo2MassToConsume <= 0)
                {
                    // 空间不足，无法生成任何原油，不消耗CO2
                    PUtil.LogDebug($"[原油富集变异] 存储空间已满，无法生成原油，不消耗CO2。");
                    return;
                }

                PUtil.LogDebug($"[原油富集变异] 理论可转化CO2: {totalPotentialCo2MassToConvert} kg, 实际转化CO2: {actualCo2MassToConsume} kg, 计划生成原油: {actualOilMassToGenerate} kg.");

                // --- 3. 执行CO2质量扣除 (按实际转化量) ---
                float remainingCo2ToConsume = actualCo2MassToConsume;
                foreach (var (item, potentialMassToRemove) in potentialRemovals)
                {
                    if (remainingCo2ToConsume <= 0) break; // 已经扣够了

                    if (item == null) continue;

                    var primaryElement = item.GetComponent<PrimaryElement>();
                    if (primaryElement != null)
                    {
                        // 决定从当前物品扣除多少质量
                        float massToRemoveFromCurrentItem = Mathf.Min(potentialMassToRemove, remainingCo2ToConsume);

                        // 扣除质量
                        primaryElement.Mass -= massToRemoveFromCurrentItem;
                        remainingCo2ToConsume -= massToRemoveFromCurrentItem;

                        // 触发物品更新事件，通知存储和其他监听者
                        item.Trigger((int)GameHashes.OnStorageChange, null);

                        // 如果物品质量变为0或接近0，通常会被自动清理
                        if (primaryElement.Mass <= 0)
                        {
                            // 可能需要手动 Drop，取决于 ONI 引擎行为
                            // storageComponent.Drop(item, true);
                        }
                    }
                }

                // 确保扣除量准确（理论上应该相等）
                if (Math.Abs(remainingCo2ToConsume) > float.Epsilon)
                {
                    PUtil.LogError($"[OilEnriched] 严重错误：计划扣除 {actualCo2MassToConsume} kg CO2，但只扣除了 {actualCo2MassToConsume - remainingCo2ToConsume} kg。");
                    // 尝试恢复逻辑，但这很复杂且容易出错，最好的办法是确保上面逻辑不出错
                    return; // 退出，避免进一步错误
                }

                // --- 4. 生成实际计算出的原油质量 ---
                if (actualOilMassToGenerate > 0)
                {
                    // --- 5. 获取原油预制件并实例化 ---
                    var oilPrefab = Assets.GetPrefab(SimHashes.CrudeOil.CreateTag());
                    if (oilPrefab == null)
                    {
                        PUtil.LogError($"[OilEnriched] Crude Oil prefab not found.");
                        // 注意：此时CO2已经扣除了，但无法生成原油，这是一个问题。可能需要回滚CO2扣除，但这增加了复杂性。
                        return;
                    }

                    // 获取植物本身的温度作为参考
                    var plantPE = master.GetComponent<PrimaryElement>();
                    float temperature = plantPE?.Temperature ?? 293.15f; // 默认室温

                    // --- 6. 循环生成原油物品 (实际计算出的数量) ---
                    float generatedOilMass = 0f;
                    while (generatedOilMass < actualOilMassToGenerate)
                    {
                        // 计算本次生成的质量 (不超过剩余需求)
                        float massThisIteration = actualOilMassToGenerate - generatedOilMass;

                        // 实例化原油物品
                        var oilInstance = GameUtil.KInstantiate(oilPrefab, master.transform.GetPosition(), Grid.SceneLayer.Ore, null, 0);
                        if (oilInstance != null)
                        {
                            oilInstance.SetActive(true);
                            var oilPE = oilInstance.GetComponent<PrimaryElement>();
                            if (oilPE != null)
                            {
                                oilPE.Mass = massThisIteration;
                                oilPE.Temperature = temperature; // 设置温度
                                                                 // 尝试将生成的原油存入存储
                                bool storedSuccessfully = storageComponent.Store(oilInstance, false, false, true, false);
                                if (storedSuccessfully)
                                {
                                    generatedOilMass += massThisIteration;
                                }
                                else
                                {
                                    // 如果存储失败，销毁生成的物品
                                    // 这里理论上不应该发生，因为我们已经在前面检查了可用空间
                                    PUtil.LogDebug($"[原油富集变异] 严重警告：理论空间充足但原油存储失败，销毁: {oilInstance.name}, Mass: {massThisIteration}");
                                    Util.KDestroyGameObject(oilInstance);
                                    // 尝试恢复CO2扣除，但这很复杂
                                    break; // 停止生成
                                }
                            }
                            else
                            {
                                PUtil.LogError($"[OilEnriched] Generated oil item missing PrimaryElement component: {oilInstance.name}");
                                Util.KDestroyGameObject(oilInstance); // 没有 PrimaryElement，销毁
                            }
                        }
                        else
                        {
                            PUtil.LogError($"[OilEnriched] Failed to instantiate Crude Oil prefab: {oilPrefab.name}");
                            break; // 实例化失败，停止循环
                        }
                    }

                    if (generatedOilMass > 0)
                    {
                        PUtil.LogDebug($"[原油富集变异] 成功生成 {generatedOilMass} kg 原油.");
                    }
                }
            }
        }


        public class States : GameStateMachine<States, StatesInstance, OilEnrichedStates>
        {

            public override void InitializeStates(out StateMachine.BaseState default_state)
            {
                default_state = this.grow;
                State state = this.dead;
                string name = CREATURES.STATUSITEMS.DEAD.NAME;
                string tooltip = CREATURES.STATUSITEMS.DEAD.TOOLTIP;
                string icon = "";
                StatusItem.IconType icon_type = StatusItem.IconType.Info;
                NotificationType notification_type = NotificationType.Neutral;
                bool allow_multiples = false;
                StatusItemCategory main = Db.Get().StatusItemCategories.Main;

                state.ToggleStatusItem(name, tooltip, icon, icon_type, notification_type, allow_multiples, default(HashedString), 129022, null, null, main).Enter(delegate (StatesInstance smi)
                {
                    GameUtil.KInstantiate(Assets.GetPrefab(EffectConfigs.PlantDeathId), smi.master.transform.GetPosition(), Grid.SceneLayer.FXFront, null, 0).SetActive(true);
                    smi.master.Trigger(1623392196, null);
                    smi.master.GetComponent<KBatchedAnimController>().StopAndClear();
                    UnityEngine.Object.Destroy(smi.master.GetComponent<KBatchedAnimController>());
                    smi.Schedule(0.5f, new Action<object>(smi.master.DestroySelf), null);
                });
                this.blocked_from_growing.ToggleStatusItem(Db.Get().MiscStatusItems.RegionIsBlocked, null).EventTransition(GameHashes.EntombedChanged, this.alive, (StatesInstance smi) => this.alive.ForceUpdateStatus(smi.master.gameObject)).EventTransition(GameHashes.TooColdWarning, this.alive, (StatesInstance smi) => this.alive.ForceUpdateStatus(smi.master.gameObject)).EventTransition(GameHashes.TooHotWarning, this.alive, (StatesInstance smi) => this.alive.ForceUpdateStatus(smi.master.gameObject)).TagTransition(GameTags.Uprooted, this.dead, false);
                this.grow.Enter(delegate (StatesInstance smi)
                {
                    if (smi.master.receptacleMonitor.HasReceptacle() && !this.alive.ForceUpdateStatus(smi.master.gameObject))
                    {
                        smi.GoTo(this.blocked_from_growing);
                        return;
                    }
                    smi.GoTo(this.alive);
                });
                this.alive.InitializeStates(this.masterTarget, this.dead).DefaultState(this.alive.growing).Enter(delegate (StatesInstance smi)
                {
                    smi.master.SetConsumptionRate();
                });
                this.alive.growing.EventTransition(GameHashes.Wilt, this.alive.wilting, (StatesInstance smi) => smi.master.wiltCondition.IsWilting()).Enter(delegate (StatesInstance smi)
                {
                    smi.master.elementConsumer.EnableConsumption(true);
                }).Exit(delegate (StatesInstance smi)
                {
                    smi.master.elementConsumer.EnableConsumption(false);
                }).EventTransition(GameHashes.Grow, this.alive.fullygrown, (StatesInstance smi) => smi.master.growing.IsGrown());
                this.alive.fullygrown.EventTransition(GameHashes.Wilt, this.alive.wilting, (StatesInstance smi) => smi.master.wiltCondition.IsWilting()).EventTransition(GameHashes.HarvestComplete, this.alive.growing, null);
                this.alive.wilting.EventTransition(GameHashes.WiltRecover, this.alive.growing, (StatesInstance smi) => !smi.master.wiltCondition.IsWilting());
            }


            public State grow;


            public State blocked_from_growing;


            public States.AliveStates alive;


            public State dead;


            public class AliveStates : PlantAliveSubState
            {

                public State growing;


                public State fullygrown;


                public State wilting;
            }
        }
    }

}
