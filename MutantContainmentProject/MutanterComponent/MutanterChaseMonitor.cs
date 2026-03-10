using Klei.AI;
using MutantContainmentProject.MutanterEffect;
using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    public class MutanterChaseMonitor : GameStateMachine<MutanterChaseMonitor, MutanterChaseMonitor.StatesInstance, IStateMachineTarget, MutanterChaseMonitor.Def>
    {
        public class Def : BaseDef
        {
        }

        public State idle;
        public State chasing;

        public override void InitializeStates(out BaseState default_state)
        {
            default_state = root;

            root
                .Enter(smi => smi.StopChase())
                .Transition(chasing, smi => !smi.IsContained && smi.storedThreaters.Count > 0);

            chasing
                .Enter(smi => smi.StartChase())
                .Update((smi, dt) => smi.UpdateChase(), UpdateRate.SIM_200ms)
                .Exit(smi => smi.StopChase())
                .Transition(root, smi => smi.IsContained || smi.storedThreaters.Count == 0);
        }

        public class StatesInstance : GameInstance
        {
            public EmotionMonitor.StatesInstance emotionMonitorSMI;
            public MutanterStateMachine.StatesInstance mutanterStateMachineSMI;
            private MutanterAttackSystem _attackSystem;

            private MutanterAttackSystem attackSystem
            {
                get
                {
                    if (_attackSystem == null)
                    {
                        _attackSystem = master.gameObject.GetComponent<MutanterAttackSystem>();
                    }
                    return _attackSystem;
                }
            }
            public Effects effects;
            public Navigator navigator;
            public List<KPrefabID> storedThreaters = new();
            public KPrefabID currentTarget;
            public int currentTargetIndex = 0;

            private bool _isContained = false;
            public bool IsContained { get => _isContained; }

            public StatesInstance(IStateMachineTarget master, Def def) : base(master, def)
            {
                emotionMonitorSMI = master.gameObject.GetSMI<EmotionMonitor.StatesInstance>();
                mutanterStateMachineSMI = master.gameObject.GetSMI<MutanterStateMachine.StatesInstance>();
                effects = master.GetComponent<Effects>();
                navigator = master.GetComponent<Navigator>();

                // 初始化时检查当前的收容状态
                if (effects != null && effects.HasEffect(MutanterEffects.MUTANTER_CONTAINED_EFFECT))
                {
                    _isContained = true;
                    TbbDebuger.LogDebug($"[MutanterChaseMonitor] {gameObject.name} initialized with containment effect, IsContained = true");
                }

                // 使用Klei原生事件系统订阅事件
                Subscribe((int)MutanterGameHashes.MutanterContained, OnContained);
                Subscribe((int)MutanterGameHashes.MutanterBreachContained, OnBreachContained);
            }

            private void OnContained(object data)
            {
                GameObject mutanterObj = data as GameObject;
                if (mutanterObj == gameObject)
                {
                    _isContained = true;
                    StopChase();
                    TbbDebuger.LogDebug($"[MutanterChaseMonitor] {gameObject.name} received MutanterContained event, stopping chase");
                }
            }

            private void OnBreachContained(object data)
            {
                GameObject mutanterObj = data as GameObject;
                if (mutanterObj == gameObject)
                {
                    _isContained = false;
                    TbbDebuger.LogDebug($"[MutanterChaseMonitor] {gameObject.name} received MutanterBreachContained event");
                }
            }

            protected override void OnCleanUp()
            {
                // 使用Klei原生事件系统取消订阅
                Unsubscribe((int)MutanterGameHashes.MutanterContained, OnContained);
                Unsubscribe((int)MutanterGameHashes.MutanterBreachContained, OnBreachContained);
                base.OnCleanUp();
            }

            public void CheckChaseCondition()
            {
                // 检查是否有MUTANTER_CONTAINED_EFFECT，如果有，不执行追击
                if (effects != null && effects.HasEffect(MutanterEffects.MUTANTER_CONTAINED_EFFECT))
                {
                    return;
                }

                // 更新存储的Threaters队列
                UpdateStoredThreaters();
            }

            public void UpdateStoredThreaters()
            {
                if (emotionMonitorSMI == null)
                {
                    storedThreaters.Clear();
                    return;
                }

                List<KPrefabID> currentThreaters = emotionMonitorSMI.GetThreaters();

                // 移除无效的目标
                storedThreaters.RemoveAll(target => !IsTargetValid(target));

                // 添加新的目标
                foreach (var target in currentThreaters)
                {
                    if (!storedThreaters.Contains(target) && IsTargetValid(target))
                    {
                        storedThreaters.Add(target);
                    }
                }
            }

            public void StartChase()
            {
                if (IsContained)
                {
                    return;
                }

                currentTargetIndex = 0;

                if (effects != null && !effects.HasEffect(MutanterEffects.MUTANTER_CHASE_EFFECT))
                {
                    effects.Add(MutanterEffects.MUTANTER_CHASE_EFFECT, true);
                }

                if (emotionMonitorSMI != null && mutanterStateMachineSMI != null)
                {
                    emotionMonitorSMI.INSANITYValue = mutanterStateMachineSMI.def.sanityThresholdToAttack;
                }

                UpdateStoredThreaters();
                SetNextTarget();
            }

            public void UpdateChase()
            {
                if (IsContained)
                {
                    GoTo(sm.root);
                    return;
                }

                if (!IsTargetValid(currentTarget))
                {
                    SetNextTarget();
                    return;
                }

                if (navigator != null && currentTarget != null && currentTarget.gameObject != null)
                {
                    int targetCell = Grid.PosToCell(currentTarget.gameObject);

                    if (navigator.GetNavigationCost(targetCell) > 50)
                    {
                        navigator.Stop();
                        navigator.GoTo(targetCell);
                    }
                }

                // 尝试攻击目标
                if (attackSystem != null)
                {
                    attackSystem.TryExecuteAttack(currentTarget.gameObject);

                    // 检查目标是否被击败
                    if (IsTargetDefeated(currentTarget))
                    {
                        SetNextTarget();
                    }
                }
            }

            public void SetNextTarget()
            {
                if (IsContained)
                {
                    GoTo(sm.root);
                    return;
                }

                UpdateStoredThreaters();

                if (storedThreaters.Count == 0)
                {
                    GoTo(sm.root);
                    return;
                }

                currentTargetIndex = (currentTargetIndex + 1) % storedThreaters.Count;
                currentTarget = storedThreaters[currentTargetIndex];

                if (!IsTargetValid(currentTarget))
                {
                    SetNextTarget();
                }
            }

            public bool IsTargetValid(KPrefabID target)
            {
                if (target == null || target.gameObject == null)
                {
                    return false;
                }

                // 检查目标是否可达
                if (!IsTargetReachable(target))
                {
                    return false;
                }

                return true;
            }

            public bool IsTargetReachable(KPrefabID target)
            {
                if (target == null || target.gameObject == null || navigator == null)
                {
                    return false;
                }

                int targetCell = Grid.PosToCell(target.gameObject);
                return navigator.CanReach(targetCell);
            }

            public bool IsTargetDefeated(KPrefabID target)
            {
                if (target == null || target.gameObject == null)
                {
                    return true;
                }

                // 检查目标压力值是否满值
                StressMonitor.Instance stressMonitorSMI = target.gameObject.GetSMI<StressMonitor.Instance>();
                if (stressMonitorSMI != null && stressMonitorSMI.HasHadEnough())
                {
                    return true;
                }

                // 检查目标生命值是否归0
                Health health = target.gameObject.GetComponent<Health>();
                if (health != null && health.hitPoints <= 0)
                {
                    return true;
                }

                return false;
            }

            public void StopChase()
            {
                currentTarget = null;
                currentTargetIndex = 0;

                // 停止Navigator移动
                if (navigator != null)
                {
                    navigator.Stop();
                }

                if (effects != null && effects.HasEffect(MutanterEffects.MUTANTER_CHASE_EFFECT))
                {
                    effects.Remove(MutanterEffects.MUTANTER_CHASE_EFFECT);
                }

                // 将EmotionMonitor的理智值修改到稳定的理智值，触发状态流转到stable
                if (emotionMonitorSMI != null && mutanterStateMachineSMI != null)
                {
                    emotionMonitorSMI.INSANITYValue = mutanterStateMachineSMI.def.sanityThresholdToStable;
                }
            }
        }
    }
}
