using TBB.He.TbbLib.Debuger;

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
                .ToggleStatusItem(MutanterStatusItems.Instance.Incapacitated)
                .ToggleTag(MutanterTags.Incapacitated)
                .Exit((smi) => smi.gameObject.GetComponent<KPrefabID>().RemoveTag(MutanterTags.Incapacitated));

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
                        });

            stable// --- 稳定状态 ---
                .ToggleStatusItem(MutanterStatusItems.Instance.Idle)
                .Update("CheckSanityForStability", (smi, dt) =>
                {
                    if (smi.EmotionSMI != null && smi.EmotionSMI.INSANITYValue <= smi.def.sanityThresholdToAgitate)
                    {
                        smi.GoTo(smi.sm.agitated);
                    }
                }, UpdateRate.SIM_1000ms);

            agitated// --- 焦躁状态 ---
                .ToggleStatusItem(MutanterStatusItems.Instance.Agitated)
                .Enter((smi) =>
                {
                    // 可能播放焦躁动画或音效
                    // Debug.Log($"[MutanterStateMachine] {smi.master.name} is agitated!");
                })
                .Update("CheckSanityForAgitation", (smi, dt) =>
                {
                    if (smi.EmotionSMI != null && smi.EmotionSMI.INSANITYValue <= smi.def.sanityThresholdToHostile)
                    {
                        smi.GoTo(hostile);
                    }
                    else if (smi.EmotionSMI != null && smi.EmotionSMI.INSANITYValue > smi.def.sanityThresholdToAgitate)
                    {
                        smi.GoTo(stable);
                    }
                }, UpdateRate.SIM_1000ms);
            hostile// --- 敌对状态 ---
                .ToggleStatusItem(MutanterStatusItems.Instance.Hostile)
                .Enter((smi) =>
                {
                    // 启动敌对AI行为，寻找目标等
                    // Debug.Log($"[MutanterStateMachine] {smi.master.name} is now hostile!");
                })
                .Update("CheckSanityForHostility", (smi, dt) =>
                {
                    if (smi.EmotionSMI != null && smi.EmotionSMI.INSANITYValue > smi.def.sanityThresholdToAgitate)
                    {
                        smi.GoTo(agitated);
                    }
                    else if (smi.EmotionSMI != null && smi.EmotionSMI.INSANITYValue <= smi.def.sanityThresholdToAttack)
                    {
                        smi.GoTo(attackStates);
                    }
                }, UpdateRate.SIM_1000ms);

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
                })
                .OnAnimQueueComplete(attackStates.loop);

            // 攻击循环状态
            attackStates.loop
                .Enter(smi =>
                {
                    TbbDebuger.LogDebug($"[MutanterStateMachine] Entering AttackStates.loop");
                })
                .Update((smi, dt) => ExecuteAttack(smi, dt), UpdateRate.SIM_1000ms)//攻击循环状态中执行攻击逻辑
                .ToggleStatusItem(MutanterStatusItems.Instance.AttackLoop)
                .Transition(attackStates.pst, smi => smi.EmotionSMI != null && smi.EmotionSMI.INSANITYValue >= smi.def.sanityThresholdToStable);

            // 攻击后状态
            attackStates.pst
                .Enter(smi =>
                {
                    TbbDebuger.LogDebug($"[MutanterStateMachine] Entering AttackStates.pst");
                })
                .OnAnimQueueComplete(stable);
        }
        public class StatesInstance : GameInstance
        {
            private EmotionMonitor.StatesInstance _emotionSMI;
            private MutanterChaseMonitor.StatesInstance _chaseMonitorSMI;

            public StatesInstance(IStateMachineTarget master, Def def) : base(master, def)
            {
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
            public MutanterChaseMonitor.StatesInstance ChaseMonitorSMI
            {
                get
                {
                    if (_chaseMonitorSMI == null)
                    {
                        _chaseMonitorSMI = master.gameObject.GetSMI<MutanterChaseMonitor.StatesInstance>();
                    }
                    return _chaseMonitorSMI;
                }
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
            var attackBehaviors = smi.master.gameObject.GetComponent<MutanterAttackBehaviors>();
            if (attackBehaviors != null && smi.EmotionSMI != null)
            {
                float insanityValue = smi.EmotionSMI.INSANITYValue;
                var threaters = smi.EmotionSMI.GetThreaters();
                
                if (threaters != null && threaters.Count > 0)
                {
                    foreach (var threater in threaters)
                    {
                        if (threater != null && threater.gameObject != null)
                        {
                            // 播放攻击动画
                            smi.master.gameObject.GetComponent<KBatchedAnimController>().Play("attack_once", KAnim.PlayMode.Once);
                            attackBehaviors.TryExecuteAttack(threater.gameObject, insanityValue);
                        }
                    }
                }
            }
        }
    }
}
