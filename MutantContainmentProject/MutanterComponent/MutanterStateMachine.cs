using Klei.AI;
using MutantContainmentProject.MutanterEffect;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    /**
        畸变体状态机(MutanterStateMachine)
    **/
    public class MutanterStateMachine : GameStateMachine<MutanterStateMachine, MutanterStateMachine.StatesInstance, IStateMachineTarget, MutanterStateMachine.Def>
    {
        // --- 状态定义 ---
        public State incapacitated; // (瘫痪): 畸变体无法行动。
        public State stable;        // (稳定): 在正常收容下，表现平静或执行低威胁行为。
        public State _sealed;        // (封印): 在完美收容下，行为被抑制。
        public State agitated;      // (焦躁): 收容出现问题时，开始表现出攻击性或不安。
        public State hostile;       // (敌对): 收容失效或达到特定条件时，进入全面攻击模式。
        public State specialAction; // (特殊行动): 执行与其背景故事或特性相关的独特行为
        public AttackStates attackStates; // (攻击状态组): 管理攻击相关的状态

        public class AttackStates : State
        {
            public State pre; // 攻击前状态
            public State loop; // 攻击循环状态
            public State pst; // 攻击后状态
        }

        public override void InitializeStates(out BaseState default_state)
        {
            default_state = stable;

            TbbDebuger.LogDebug($"MutanterStatusItems.Instance: [{MutanterStatusItems.Instance}]");
            incapacitated// --- 瘫痪状态 ---
                .Enter(smi =>
                {
                    TbbDebuger.LogDebug($"[MutanterStateMachine] Entering incapacitated state for {smi.master.gameObject.name}");
                    smi.StopIdleStates();
                    // 清理动画队列，确保死亡动画不会被打断
                    if (smi.AnimController != null)
                    {
                        smi.AnimController.ClearQueue();
                        TbbDebuger.LogDebug($"[MutanterStateMachine] Cleared animation queue in incapacitated state for {smi.master.gameObject.name}");
                    }
                })
                .ToggleStatusItem(MutanterStatusItems.Instance.Incapacitated)
                .ToggleTag(MutanterTags.Incapacitated)
                .EventTransition(GameHashes.HealthChanged, stable, (smi => (smi.HealthInstance.State == Health.HealthState.Alright)))
                .Exit((smi) =>
                {
                    TbbDebuger.LogDebug($"[MutanterStateMachine] Exiting incapacitated state for {smi.master.gameObject.name}");
                    smi.gameObject.GetComponent<KPrefabID>().RemoveTag(MutanterTags.Incapacitated);
                    smi.ContinueIdleStates();
                });

            _sealed// --- 封印状态 ---
                .ToggleStatusItem(MutanterStatusItems.Instance.Sealed)
                .Enter((smi) =>
                    {
                        // 可能的封印动画或视觉效果
                        // Debug.Log($"[MutanterStateMachine] {smi.master.name} is sealed.");
                    })
                .Exit((smi) =>
                        {
                            // 解除封印时的清理工作
                        })
                .Transition(incapacitated, smi => smi.HealthInstance != null && smi.HealthInstance.hitPoints <= 10f);

            stable// --- 稳定状态 ---
                  //.ToggleStatusItem(MutanterStatusItems.Instance.Idle)
                .Update("CheckSanityForStability", (smi, dt) =>
                {
                    if (!smi.IsContained && smi.EmotionSMI != null && smi.EmotionSMI.INSANITYValue <= smi.def.sanityThresholdToAgitate)
                    {
                        smi.GoTo(smi.sm.agitated);
                    }
                }, UpdateRate.SIM_1000ms)
                .Transition(incapacitated, smi => smi.HealthInstance != null && smi.HealthInstance.hitPoints <= 10f);

            agitated// --- 焦躁状态 ---
                .ToggleStatusItem(MutanterStatusItems.Instance.Agitated)
                .Enter((smi) =>
                {
                    // 可能播放焦躁动画或音效
                    // Debug.Log($"[MutanterStateMachine] {smi.master.name} is agitated!");
                })
                .Update("CheckSanityForAgitation", (smi, dt) =>
                {
                    if (smi.IsContained)
                    {
                        smi.GoTo(stable);
                    }
                    else if (smi.EmotionSMI != null && smi.EmotionSMI.INSANITYValue <= smi.def.sanityThresholdToHostile)
                    {
                        smi.GoTo(hostile);
                    }
                    else if (smi.EmotionSMI != null && smi.EmotionSMI.INSANITYValue > smi.def.sanityThresholdToAgitate + 5) // 添加滞后效果
                    {
                        smi.GoTo(stable);
                    }
                }, UpdateRate.SIM_1000ms)
                .Transition(incapacitated, smi => smi.HealthInstance != null && smi.HealthInstance.hitPoints <= 10f);
            hostile// --- 敌对状态 ---
                .ToggleStatusItem(MutanterStatusItems.Instance.Hostile)
                .Enter((smi) =>
                {
                    // 启动敌对AI行为，寻找目标等
                    // Debug.Log($"[MutanterStateMachine] {smi.master.name} is now hostile!");
                })
                .Update("CheckSanityForHostility", (smi, dt) =>
                {
                    if (smi.IsContained)
                    {
                        smi.GoTo(stable);
                    }
                    else if (smi.EmotionSMI != null && smi.EmotionSMI.INSANITYValue > smi.def.sanityThresholdToAgitate + 5) // 添加滞后效果
                    {
                        smi.GoTo(agitated);
                    }
                    else if (smi.EmotionSMI != null && smi.EmotionSMI.INSANITYValue <= smi.def.sanityThresholdToAttack)
                    {
                        smi.GoTo(attackStates);
                    }
                }, UpdateRate.SIM_1000ms)
                .Transition(incapacitated, smi => smi.HealthInstance != null && smi.HealthInstance.hitPoints <= 10f);

            // --- 攻击状态组实现 ---
            attackStates
                .DefaultState(attackStates.pre)
                .Enter(smi =>
                {
                    TbbDebuger.LogDebug($"[MutanterStateMachine] Entering AttackStates");
                })
                .Exit(smi =>
                {
                    TbbDebuger.LogDebug($"[MutanterStateMachine] Exiting AttackStates");
                });

            // 攻击前状态
            attackStates.pre
                .Enter(smi =>
                {
                    TbbDebuger.LogDebug($"[MutanterStateMachine] Entering AttackStates.pre");
                    if (smi.IdleSmi != null)
                    {
                        TbbDebuger.LogDebug($"[MutanterStateMachine] 进入 AttackStates.pre, 停止空闲状态");
                        smi.StopIdleStates();
                    }
                })
                .ScheduleGoTo(1f, attackStates.loop)
                .Transition(incapacitated, smi => smi.HealthInstance != null && smi.HealthInstance.hitPoints <= 10f);

            // 攻击循环状态
            attackStates.loop
                .Enter(smi =>
                {
                    TbbDebuger.LogDebug($"[MutanterStateMachine] Entering AttackStates.loop");
                })
                .Update((smi, dt) => ExecuteAttack(smi, dt), UpdateRate.SIM_1000ms)//攻击循环状态中执行攻击逻辑
                .ToggleStatusItem(MutanterStatusItems.Instance.AttackLoop)
                .Transition(attackStates.pst, smi => smi.EmotionSMI != null && smi.EmotionSMI.INSANITYValue > smi.def.sanityThresholdToAttack)
                .Transition(incapacitated, smi => smi.HealthInstance != null && smi.HealthInstance.hitPoints <= 10f);

            // 攻击后状态
            attackStates.pst
                .Enter(smi =>
                {
                    TbbDebuger.LogDebug($"[MutanterStateMachine] Entering AttackStates.pst");
                    if (smi.IdleSmi != null)
                    {
                        TbbDebuger.LogDebug($"[MutanterStateMachine] 退出 AttackStates.pst, 继续空闲状态");
                        smi.ContinueIdleStates();
                    }
                })
                .ScheduleGoTo(1f, stable)
                .Transition(incapacitated, smi => smi.HealthInstance != null && smi.HealthInstance.hitPoints <= 10f);
        }
        public class StatesInstance : GameInstance
        {
            private EmotionMonitor.StatesInstance _emotionSMI;
            public EmotionMonitor.StatesInstance EmotionSMI => _emotionSMI ??= master.gameObject.GetSMI<EmotionMonitor.StatesInstance>();

            private MutanterChaseMonitor.StatesInstance _chaseMonitorSMI;
            public MutanterChaseMonitor.StatesInstance ChaseMonitorSMI => _chaseMonitorSMI ??= master.gameObject.GetSMI<MutanterChaseMonitor.StatesInstance>();

            private IdleStates.Instance _idleSmi;
            public IdleStates.Instance IdleSmi => _idleSmi ??= master.gameObject.GetSMI<IdleStates.Instance>();
            private bool _isContained = false;
            public bool IsContained { get => _isContained; }
            private MutanterAttackSystem _attackSystem;
            public MutanterAttackSystem AttackSystem => _attackSystem ??= master.gameObject.GetComponent<MutanterAttackSystem>();

            private KBatchedAnimController _animController;
            public KBatchedAnimController AnimController => _animController ??= master.gameObject.GetComponent<KBatchedAnimController>();

            private Health health;
            public Health HealthInstance => health ??= master.gameObject.GetComponent<Health>();
            public StatesInstance(IStateMachineTarget master, Def def) : base(master, def)
            {
                // 初始化时检查当前的收容状态
                Effects effects = gameObject.GetComponent<Effects>();
                if (effects != null && effects.HasEffect(MutanterEffects.MUTANTER_CONTAINED_EFFECT))
                {
                    _isContained = true;
                    TbbDebuger.LogDebug($"[MutanterStateMachine] {gameObject.name} initialized with containment effect, IsContained = true");
                }

                // 订阅事件来更新IsContained属性
                Subscribe((int)MutanterGameHashes.MutanterContained, OnContained);
                Subscribe((int)MutanterGameHashes.MutanterBreachContained, OnBreachContained);
            }

            private void OnContained(object data)
            {
                GameObject mutanterObj = data as GameObject;
                if (mutanterObj == gameObject)
                {
                    _isContained = true;
                    TbbDebuger.LogDebug($"[MutanterStateMachine] {gameObject.name} received MutanterContained event, IsContained = true");
                }
            }

            private void OnBreachContained(object data)
            {
                GameObject mutanterObj = data as GameObject;
                if (mutanterObj == gameObject)
                {
                    _isContained = false;
                    TbbDebuger.LogDebug($"[MutanterStateMachine] {gameObject.name} received MutanterBreachContained event, IsContained = false");
                }
            }

            protected override void OnCleanUp()
            {
                Unsubscribe((int)MutanterGameHashes.MutanterContained, OnContained);
                Unsubscribe((int)MutanterGameHashes.MutanterBreachContained, OnBreachContained);
                base.OnCleanUp();
            }
            public void StopIdleStates()
            {
                if (IdleSmi == null) return;
                IdleSmi.GoTo((StateMachine.BaseState)IdleSmi.sm.root);
                IdleSmi.GetComponent<Navigator>().Stop();
            }

            public void ContinueIdleStates()
            {
                if (IdleSmi == null) return;
                IdleSmi.GoTo(IdleSmi.sm.GetDefaultState());
            }
        }
        public class Def : BaseDef
        {
            public float sanityThresholdToAgitate = 60;
            public float sanityThresholdToHostile = 40;
            public float sanityThresholdToAttack = 30;
            public float sanityThresholdToStable = 70;
            public int threatenRange = 10;
        }
        // 执行攻击逻辑
        private void ExecuteAttack(StatesInstance smi, float dt)
        {
            int threatCount = 0;
            // 检查生命值，确保只有在生命值大于0时才执行攻击
            if (smi.HealthInstance != null && smi.HealthInstance.hitPoints <= 0f)
            {
                return;
            }

            // 获取战斗管理器
            var combatManager = smi.master.gameObject.GetComponent<MutanterCombatManager>();
            
            if (combatManager != null && smi.EmotionSMI != null)
            {
                var threaters = smi.EmotionSMI.GetThreaters();
                TbbDebuger.LogDebug($"[MutanterStateMachine] 攻击目标: {threaters.Count} 个");

                if (threaters != null && threaters.Count > 0)
                {
                    foreach (var threater in threaters)
                    {
                        if (threater != null && threater.gameObject != null
                        && threater.gameObject.GetComponent<Health>().hitPoints > 0f)
                        {
                            threatCount++;
                            // 使用战斗管理器执行攻击
                            TbbDebuger.LogDebug($"[MutanterStateMachine] 攻击目标: {threater.gameObject.name}");
                            combatManager.TryExecuteAttack(threater.gameObject);
                        }
                    }
                }
                if (threatCount == 0)
                {
                    //smi.EmotionSMI.ExpelThreat();
                }
            }
        }
    }
}
