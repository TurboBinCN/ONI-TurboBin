using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    /**
     * 4. 低温碎冰监控器 (MutanterColdIceMonitor)
     * 功能: 控制SCP-4762释放低温碎冰的行为，同时持续吸收周围热量
     * 主要状态:
     * idle: 默认状态，不释放低温碎冰
     * generating: 生成低温碎冰的状态，累积能量/温度
     * discharging: 释放低温碎冰的状态，生成低温碎冰实体
     * cooling: 持续吸收周围热量的状态
     */
    public class MutanterColdIceMonitor : GameStateMachine<MutanterColdIceMonitor, MutanterColdIceMonitor.StatesInstance, IStateMachineTarget, MutanterColdIceMonitor.Def>
    {
        // --- 状态定义 ---
        public State idle;        // 默认状态，不释放低温碎冰
        public State generating;  // 生成低温碎冰的状态
        public State discharging; // 释放低温碎冰的状态
        public State cooling;     // 持续吸收周围热量的状态

        public override void InitializeStates(out BaseState default_state)
        {
            default_state = cooling;

            generating
                .Enter(smi =>
                {
                    TbbDebuger.LogDebug($"[{smi.master.name}] 开始生成低温碎冰");
                })
                .Update("GenerateColdIce", (smi, dt) =>
                {
                    smi.AccumulateEnergy(dt);
                    if (smi.IsReadyToDischarge())
                    {
                        smi.GoTo(discharging);
                    }
                }, UpdateRate.SIM_1000ms)
                .Exit(smi =>
                {
                    TbbDebuger.LogDebug($"[{smi.master.name}] 停止生成低温碎冰");
                });

            discharging
                .Enter(smi =>
                {
                    TbbDebuger.LogDebug($"[{smi.master.name}] 开始释放低温碎冰");
                    smi.DischargeColdIce();
                    smi.UpdateLastDischargeTime();
                    smi.ResetGeneration();
                    smi.EmotionSMI.EasyEmotion(30f);
                    smi.GoTo(cooling);
                });

            cooling
                .Enter(smi =>
                {
                    TbbDebuger.LogDebug($"[{smi.master.name}] 开始吸收周围热量");
                })
                .Update("AbsorbHeat", (smi, dt) =>
                {
                    smi.AbsorbSurroundingHeat(dt);
                    if (smi.ShouldStartGeneration())
                    {
                        smi.GoTo(generating);
                    }
                }, UpdateRate.SIM_1000ms)
                .Exit(smi =>
                {
                    TbbDebuger.LogDebug($"[{smi.master.name}] 停止吸收周围热量");
                });
        }

        public class StatesInstance : GameInstance
        {
            public float energyAccumulated;
            private EmotionMonitor.StatesInstance _emtionSMI;
            public EmotionMonitor.StatesInstance EmotionSMI
            {
                get
                {
                    if (_emtionSMI == null) _emtionSMI = master.gameObject.GetSMI<EmotionMonitor.StatesInstance>();
                    return _emtionSMI;
                }
            }
            private float lastDischargeTime;

            public StatesInstance(IStateMachineTarget master, Def def) : base(master, def)
            {
                _emtionSMI = master.gameObject.GetSMI<EmotionMonitor.StatesInstance>();
                ResetGeneration();
                lastDischargeTime = -def.dischargeCooldown;
            }

            public bool IsDischargeOnCooldown()
            {
                return (float)GameClock.Instance.GetTime() - lastDischargeTime < def.dischargeCooldown;
            }

            public void ResetGeneration()
            {
                energyAccumulated = 0f;
            }

            public bool ShouldStartGeneration()
            {
                // 检查是否应该开始生成低温碎冰
                // 例如：基于情绪状态、时间等
                if (EmotionSMI != null)
                {
                    // 当理智值低于某个阈值时，开始生成低温碎冰
                    return EmotionSMI.INSANITYValue < def.insanityThresholdToStart;
                }
                return false;
            }

            public void AccumulateEnergy(float dt)
            {
                // 累积能量，基于时间和情绪状态
                float energyRate = def.baseEnergyRate;
                if (EmotionSMI != null)
                {
                    // 理智值越低，生成速率越快
                    float insanityFactor = 1f - (EmotionSMI.INSANITYValue / 100f);
                    energyRate *= (1f + insanityFactor * def.insanityEnergyMultiplier);
                }
                energyAccumulated += energyRate * dt;
                //TbbDebuger.LogDebug($"[{master.name}] 累积能量: {energyAccumulated:F2}");
            }

            public bool IsReadyToDischarge()
            {
                // 检查是否达到释放阈值且不在冷却中
                return energyAccumulated >= def.dischargeThreshold && !IsDischargeOnCooldown();
            }

            public void DischargeColdIce()
            {
                Element element = ElementLoader.FindElementByHash(def.coldIceElement);
                if (element != null)
                {
                    int cell = Grid.PosToCell(master.transform.GetPosition());
                    if (Grid.IsValidCell(cell))
                    {
                        int spawnCount = 10; // 将总量分成10份进行生成
                        for (int i = 0; i < spawnCount; i++)
                        {
                            var pos = master.transform.GetPosition() + new Vector3(i%2, i%2, 0.0f);
                            if (Grid.IsValidCell(Grid.PosToCell(pos)) && !Grid.IsSolidCell(Grid.PosToCell(pos))){
                                element.substance.SpawnResource(
                                    pos,
                                    def.coldIceAmount / spawnCount,
                                    def.coldIceTemperature,
                                    0,
                                    0,
                                    forceTemperature: true
                                );
                                TbbDebuger.LogDebug($"[{master.name}] 释放 {def.coldIceAmount / spawnCount} kg 的 {def.coldIceElement}");
                            }
                        }
                    }
                }
            }

            public void UpdateLastDischargeTime()
            {
                lastDischargeTime = (float)GameClock.Instance.GetTime();
            }

            public void AbsorbSurroundingHeat(float dt)
            {
                int cell = Grid.PosToCell(master.transform.GetPosition());
                float coolingRate = def.coolingRate * dt;
                
                // 遍历冷却范围内的所有单元格
                for (int y = def.minCoolingRange.y; y < def.maxCoolingRange.y; ++y)
                {
                    for (int x = def.minCoolingRange.x; x < def.maxCoolingRange.x; ++x)
                    {
                        CellOffset offset = new CellOffset(x, y);
                        int targetCell = Grid.OffsetCell(cell, offset);
                        
                        // 检查单元格是否有效且温度高于最低温度
                        if (Grid.IsValidCell(targetCell) && Grid.Temperature[targetCell] > def.minCooledTemperature)
                        {
                            // 计算可以吸收的热量
                            float currentTemp = Grid.Temperature[targetCell];
                            if (currentTemp > def.minCooledTemperature)
                            {
                                // 计算需要吸收的热量
                                float heatToAbsorb = Mathf.Min(coolingRate, (currentTemp - def.minCooledTemperature) * Grid.Element[targetCell].specificHeatCapacity * 1f);
                                
                                // 应用冷却效果
                                if (heatToAbsorb > 0)
                                {
                                    // 使用 SimMessages.ModifyEnergy 来移除热量，从而降低温度
                                    SimMessages.ModifyEnergy(targetCell, -heatToAbsorb, def.minCooledTemperature, SimMessages.EnergySourceID.LiquidCooledFan);
                                }
                            }
                        }
                    }
                }
            }
        }

        public class Def : BaseDef
        {
            // 基础能量生成速率 (单位: 能量/秒)
            public float baseEnergyRate = 8f;

            // 释放阈值 (单位: 能量)
            public float dischargeThreshold = 80f;

            // 每次释放的低温碎冰量 (单位: kg)
            public float coldIceAmount = 100f;

            // 低温碎冰元素类型
            public SimHashes coldIceElement = SimHashes.Ice;

            public float coldIceTemperature = 243.15f; // 低温碎冰的温度 (单位: K)

            // 开始生成的理智阈值
            public float insanityThresholdToStart = 60f;

            // 理智值对能量生成的乘数
            public float insanityEnergyMultiplier = 1.5f;

            // 释放冷却时间 (单位: 秒)
            public float dischargeCooldown = 240f;

            // 冷却相关参数
            public float minCooledTemperature = 173.15f; // 最低冷却温度 (单位: K)，-100摄氏度
            public float coolingRate = 588f; // 冷却速率 (单位: 焦耳/秒)
            public Vector2I minCoolingRange = new Vector2I(-1, -1); // 冷却范围最小值
            public Vector2I maxCoolingRange = new Vector2I(1, 1); // 冷却范围最大值
        }
    }
}