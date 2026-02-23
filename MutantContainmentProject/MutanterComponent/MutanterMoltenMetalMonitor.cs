using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    /**
     * 3. 熔融金属监控器 (MutanterMoltenMetalMonitor)
     * 功能: 控制畸变体产生熔融金属的行为
     * 主要状态:
     * idle: 默认状态，不产生熔融金属
     * generating: 生成熔融金属的状态，累积能量/温度
     * discharging: 释放熔融金属的状态，生成熔融金属实体
     */
    public class MutanterMoltenMetalMonitor : GameStateMachine<MutanterMoltenMetalMonitor, MutanterMoltenMetalMonitor.StatesInstance, IStateMachineTarget, MutanterMoltenMetalMonitor.Def>
    {
        // --- 状态定义 ---
        public State idle;        // 默认状态，不产生熔融金属
        public State generating;  // 生成熔融金属的状态
        public State discharging; // 释放熔融金属的状态

        public override void InitializeStates(out BaseState default_state)
        {
            default_state = idle;

            idle
                .Enter(smi =>
                {
                    smi.ResetGeneration();
                })
                .Update("CheckGenerationConditions", (smi, dt) =>
                {
                    if (smi.ShouldStartGeneration())
                    {
                        smi.GoTo(generating);
                    }
                }, UpdateRate.SIM_1000ms);

            generating
                .Enter(smi =>
                {
                    TbbDebuger.LogDebug($"[{smi.master.name}] 开始生成熔融金属");
                })
                .Update("GenerateMoltenMetal", (smi, dt) =>
                {
                    smi.AccumulateEnergy(dt);
                    if (smi.IsReadyToDischarge())
                    {
                        smi.GoTo(discharging);
                    }
                }, UpdateRate.SIM_1000ms)
                .Exit(smi =>
                {
                    TbbDebuger.LogDebug($"[{smi.master.name}] 停止生成熔融金属");
                });

            discharging
                .Enter(smi =>
                {
                    TbbDebuger.LogDebug($"[{smi.master.name}] 开始释放熔融金属");
                    smi.DischargeMoltenMetal();
                    smi.UpdateLastDischargeTime();
                    smi.ResetGeneration();
                    smi.GoTo(idle);
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
                // 检查是否应该开始生成熔融金属
                // 例如：基于情绪状态、时间等
                if (EmotionSMI != null)
                {
                    // 当理智值低于某个阈值时，开始生成熔融金属
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
                TbbDebuger.LogDebug($"[{master.name}] 累积能量: {energyAccumulated:F2}");
            }

            public bool IsReadyToDischarge()
            {
                // 检查是否达到释放阈值且不在冷却中
                return energyAccumulated >= def.dischargeThreshold && !IsDischargeOnCooldown();
            }

            public void DischargeMoltenMetal()
            {
                // 直接使用 SimMessages.AddRemoveSubstance 一次性添加熔融金属
                Element element = ElementLoader.FindElementByHash(def.moltenMetalElement);
                if (element != null)
                {
                    int cell = Grid.PosToCell(master.transform.GetPosition());
                    if (Grid.IsValidCell(cell))
                    {
                        // 使用与 ElementEmitter 相同的实现方式
                        if (element.IsGas || element.IsLiquid)
                        {
                            SimMessages.AddRemoveSubstance(
                                cell,
                                def.moltenMetalElement,
                                CellEventLogger.Instance.ElementConsumerSimUpdate,
                                def.moltenMetalAmount,
                                def.moltenMetalTemperature,
                                byte.MaxValue,
                                0
                            );
                        }
                        else if (element.IsSolid)
                        {
                            // 对于固体，使用 SpawnResource
                            element.substance.SpawnResource(
                                master.transform.GetPosition() + new Vector3(0.0f, 0.5f, 0.0f),
                                def.moltenMetalAmount,
                                def.moltenMetalTemperature,
                                byte.MaxValue,
                                0,
                                forceTemperature: true
                            );
                        }
                        PopFXManager.Instance.SpawnFX(
                            PopFXManager.Instance.sprite_Resource,
                            element.name,
                            master.gameObject.transform
                        );
                        TbbDebuger.LogDebug($"[{master.name}] 一次性喷发了 {def.moltenMetalAmount} kg 的 {def.moltenMetalElement}");
                    }
                }
            }

            public void UpdateLastDischargeTime()
            {
                lastDischargeTime = (float)GameClock.Instance.GetTime();
            }
        }

        public class Def : BaseDef
        {
            // 基础能量生成速率 (单位: 能量/秒)
            public float baseEnergyRate = 10f;

            // 释放阈值 (单位: 能量)
            public float dischargeThreshold = 100f;

            // 每次释放的熔融金属量 (单位: kg)
            public float moltenMetalAmount = 1000f;

            // 熔融金属元素类型
            public SimHashes moltenMetalElement = SimHashes.MoltenIron;

            public float moltenMetalTemperature = 1973.15f; // 熔融金属的温度 (单位: K)

            // 开始生成的理智阈值
            public float insanityThresholdToStart = 50f;

            // 理智值对能量生成的乘数
            public float insanityEnergyMultiplier = 2f;

            // 喷发冷却时间 (单位: 秒)
            public float dischargeCooldown = 300f;
        }
    }
}
