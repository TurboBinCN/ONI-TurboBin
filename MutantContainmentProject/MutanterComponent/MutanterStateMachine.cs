using TBB.He.TbbLib.Debuger;

namespace MutantContainmentProject.MutanterComponent
{
    /**
        1. 畸变体状态机(MutanterStateMachine)
        功能: 作为畸变体行为的顶层控制器，管理其在不同行为模式间的切换。
        主要状态:
        Incapacitated(瘫痪) : 畸变体无法行动。
        Sealed(封印) : 在完美收容下，行为被抑制。
        Stable(稳定) : 在正常收容下，表现平静或执行低威胁行为。
        Agitated(焦躁) : 收容出现问题时，开始表现出攻击性或不安。
        Hostile(敌对) : 收容失效或达到特定条件时，进入全面攻击模式。
        SpecialAction(特殊行动) : 执行与其背景故事或特性相关的独特行为。
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
        public override void InitializeStates(out BaseState default_state)
        {
            default_state = stable;

            TbbDebuger.LogDebug($"MutanterStatusItems.Instance: [{MutanterStatusItems.Instance}]");
            incapacitated// --- 瘫痪状态 ---
                .Enter(smi => { TbbDebuger.LogDebug($"MutanterStatusItems.Instance.Incapacitated.id:[{MutanterStatusItems.Instance.Incapacitated?.Id}]"); })
                //.ToggleStatusItem(MutanterStatusItems.Instance.Incapacitated)
                .ToggleTag(MutanterTags.Incapacitated)
                .Exit((smi) => smi.gameObject.GetComponent<KPrefabID>().RemoveTag(MutanterTags.Incapacitated));

            _sealed// --- 封印状态 ---
                .Enter(smi => { TbbDebuger.LogDebug($"MutanterStatusItems.Instance.Sealed.id:[{MutanterStatusItems.Instance.Sealed?.Id}]"); })
                //.ToggleStatusItem(MutanterStatusItems.Instance.Sealed)
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
                    else if (smi.EmotionSMI != null && smi.EmotionSMI.INSANITYValue >= smi.def.sanityThresholdToSeal)
                    {
                        smi.GoTo(smi.sm._sealed);
                    }
                });

            agitated// --- 焦躁状态 ---
                .Enter(smi => { TbbDebuger.LogDebug($"MutanterStatusItems.Instance.Agitated.id:[{MutanterStatusItems.Instance.Agitated?.Id}]"); })
                //.ToggleStatusItem(MutanterStatusItems.Instance.Agitated) // 示例状态项
                .Enter((smi) =>
                {
                    // 可能播放焦躁动画或音效
                    // Debug.Log($"[MutanterStateMachine] {smi.master.name} is agitated!");
                })
                .Update("CheckSanityForAgitation", (smi, dt) =>
                {
                    if (smi.EmotionSMI != null && smi.EmotionSMI.INSANITYValue <= smi.def.sanityThresholdToHostile)
                    {
                        smi.GoTo(smi.sm.hostile);
                    }
                    else if (smi.EmotionSMI != null && smi.EmotionSMI.INSANITYValue > smi.def.sanityThresholdToAgitate)
                    {
                        smi.GoTo(smi.sm.stable);
                    }
                });
            hostile// --- 敌对状态 ---
                .Enter(smi => { TbbDebuger.LogDebug($"MutanterStatusItems.Instance.Hostile.id:[{MutanterStatusItems.Instance.Hostile?.Id}]"); })
                //.ToggleStatusItem(MutanterStatusItems.Instance.Hostile) // 示例状态项
                .Enter((smi) =>
                {
                    // 启动敌对AI行为，寻找目标等
                    // Debug.Log($"[MutanterStateMachine] {smi.master.name} is now hostile!");
                })
                .Update("CheckSanityForHostility", (smi, dt) =>
                {
                    if (smi.EmotionSMI != null && smi.EmotionSMI.INSANITYValue > smi.def.sanityThresholdToAgitate)
                    {
                        smi.GoTo(smi.sm.agitated);
                    }
                });

            specialAction// --- 特殊行动状态 ---
                .Enter(smi => { TbbDebuger.LogDebug($"MutanterStatusItems.Instance.SpecialAction.id:[{MutanterStatusItems.Instance.SpecialAction?.Id}]"); })
                //.ToggleStatusItem(MutanterStatusItems.Instance.SpecialAction) // 示例状态项
                .Enter((smi) =>
                {
                    // 启动特殊行为，例如觅食、自我修复、释放能量波等
                    // 可以在这里调度一个结束此状态的方法
                    //smi.ScheduleOnce((_) => smi.GoTo(smi.sm.stable), smi.def.durationOfSpecialAction);
                });
        }
        public class StatesInstance : GameInstance
        {
            public EmotionMonitor.StatesInstance EmotionSMI;
            public StatesInstance(IStateMachineTarget master, Def def) : base(master, def)
            {
                EmotionSMI = master.gameObject.GetSMI<EmotionMonitor.StatesInstance>();
            }
        }
        public class Def : BaseDef
        {
            public float sanityThresholdToAgitate = 0;
            public float sanityThresholdToSeal = 1;
            public float sanityThresholdToHostile = 0;
        }

    }
}
