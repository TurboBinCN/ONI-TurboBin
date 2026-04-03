using Klei.AI;
using MutantContainmentProject.MutanterEffect;
using System.Linq;
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
            public ChasingStates chasing; // 追逐目标状态
            public State attacking; // 执行攻击状态
            public State recovering; // 攻击后恢复状态
            public State cooldown; // 技能冷却状态
            public State retreating; // 撤退逃跑状态
            public State staggering; // 受击硬直状态
        }
        public class ChasingStates : State
        {
            public State scanning; // 目标扫描状态
            public State chasing;

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
                .DefaultState(attackStates.chasing)
                .Enter(smi =>
                {
                    TbbDebuger.LogDebug($"[MutanterStateMachine] 进入 AttackStates");
                    if (smi.IdleSmi != null)
                    {
                        TbbDebuger.LogDebug($"[MutanterStateMachine] 进入 AttackStates, 停止空闲状态");
                        smi.StopIdleStates();
                    }
                })
                .Exit(smi =>
                {
                    TbbDebuger.LogDebug($"[MutanterStateMachine] 退出 AttackStates");
                    if (smi.IdleSmi != null)
                    {
                        TbbDebuger.LogDebug($"[MutanterStateMachine] 退出 AttackStates, 继续空闲状态");
                        smi.ContinueIdleStates();
                    }
                });
            attackStates.chasing
                .DefaultState(attackStates.chasing.scanning)
                .ToggleStatusItem(MutanterStatusItems.Instance.Chasing)
                .Exit(smi => { smi.gameObject.GetComponent<KSelectable>()?.RemoveStatusItem(MutanterStatusItems.Instance.Chasing); });

            attackStates.chasing.scanning// 游荡扫描目标状态
                .Enter(smi =>
                {
                    TbbDebuger.LogDebug($"[MutanterStateMachine] 进入 AttackStates.chasing.scanning");
                    smi.MoveToNewCell();
                })
                .Update((smi, dt) =>
                {
                    TbbDebuger.LogDebug($"[MutanterStateMachine] AttackStates.chasing.scanning Update IsStateMachineLocked：{smi.IsStateMachineLocked}");
                    if (smi.IsStateMachineLocked) return;
                    var targets = smi.EmotionSMI?.GetThreaters();
                    if (targets != null && targets.Count > 0)
                    {
                        if (!targets.Any(target => target?.GetComponent<Health>()?.hitPoints > 0)) return;
                        smi.NavigatorInstance?.Stop();
                        GameScheduler.Instance.Schedule("StartChasing", 0.1f, (_) =>
                        {
                            smi.GoTo(attackStates.chasing.chasing);
                        });
                    }
                }, UpdateRate.SIM_1000ms)
                .EventTransition(GameHashes.DestinationReached, attackStates.chasing.scanning, smi => !smi.IsStateMachineLocked)
                .EventTransition(GameHashes.NavigationFailed, attackStates.chasing.scanning, smi => !smi.IsStateMachineLocked)
                .Transition(hostile, smi => smi.EmotionSMI != null && smi.EmotionSMI.INSANITYValue > smi.def.sanityThresholdToAttack)
                .Transition(incapacitated, smi => smi.HealthInstance != null && smi.HealthInstance.hitPoints <= 10f)
                .Transition(attackStates.staggering, smi => smi.IsStaggered);

            attackStates.chasing.chasing// 追逐目标状态
                .Enter(smi =>
                {
                    TbbDebuger.LogDebug($"[MutanterStateMachine] 进入 AttackStates.chasing.chasing");
                })
                .Update((smi, dt) =>
                {
                    // 状态机锁定时不执行状态转换
                    if (smi.IsStateMachineLocked)
                    {
                        return;
                    }

                    // 追逐目标逻辑
                    var targets = smi.EmotionSMI?.GetThreaters();
                    if (targets == null || targets.Count == 0 || targets.Any(target=>target?.GetComponent<Health>()?.hitPoints <= 0))
                    {
                        smi.GoTo(attackStates.chasing.scanning);
                        return;
                    }

                    // 检查生命值，决定是否撤退
                    if (smi.HealthInstance != null && smi.HealthInstance.hitPoints < smi.HealthInstance.maxHitPoints * 0.2f)
                    {
                        smi.GoTo(attackStates.retreating);
                        return;
                    }

                    OccupyArea occupyArea = smi.gameObject.GetComponent<OccupyArea>();
                    foreach (var target in targets)
                    {
                        try
                        {
                            if (target == null || target.gameObject == null)
                            {
                                continue;
                            }

                            float distance = Mathf.Abs(target.gameObject.transform.position.x - smi.gameObject.transform.position.x);
                            if (distance < 4f)
                            {
                                if (smi.CombatManager != null && smi.CombatManager.HasAvailableSkill())
                                {
                                    TbbDebuger.LogDebug($"[MutanterStateMachine] 目标在攻击范围内且可以攻击，直接攻击 From AttackStates.chasing.chasing GoTo状态 AttackStates.attacking");
                                    smi.GoTo(attackStates.attacking);
                                    break;
                                }
                            }
                            else
                            {
                                // 目标不在攻击范围内，导航过去
                                int targetCell = Grid.PosToCell(target.gameObject);
                                if (smi.NavigatorInstance.CanReach(targetCell, occupyArea.OccupiedCellsOffsets))
                                {
                                    smi.NavigatorInstance.GoTo(Grid.PosToCell(target.gameObject), occupyArea.OccupiedCellsOffsets);
                                    break;
                                }
                            }
                        }
                        catch (System.Exception e)
                        {
                            TbbDebuger.LogError($"[MutanterStateMachine] 处理目标时发生错误: {e.Message}");
                            continue;
                        }
                    }

                }, UpdateRate.SIM_1000ms)
                .EventTransition(GameHashes.NavigationFailed, attackStates.chasing.chasing, smi => !smi.IsStateMachineLocked)
                .EventTransition(GameHashes.DestinationReached, attackStates.attacking, smi =>
                {
                    try
                    {
                        // 状态机锁定时不执行状态转换
                        if (smi.IsStateMachineLocked)
                        {
                            return false;
                        }

                        // 检查是否可以攻击
                        var targets = smi.EmotionSMI?.GetThreaters();
                        if (targets != null && targets.Count > 0 && targets[0] != null && targets[0].gameObject != null)
                        {
                            bool canAttack = smi.CombatManager != null && smi.CombatManager.HasAvailableSkill();
                            TbbDebuger.LogDebug($"[MutanterStateMachine] 目标在攻击范围内且可以攻击，直接攻击 From AttackStates.chasing.chasing GoTo状态 AttackStates.attacking 可以攻击: {canAttack}");
                            return canAttack;
                        }
                        return false;
                    }
                    catch (System.Exception e)
                    {
                        TbbDebuger.LogError($"[MutanterStateMachine] EventTransition条件检查时发生错误: {e.Message}");
                        return false;
                    }
                })
                .Transition(hostile, smi => !smi.IsStateMachineLocked && smi.EmotionSMI != null && smi.EmotionSMI.INSANITYValue > smi.def.sanityThresholdToAttack)
                .Transition(incapacitated, smi => !smi.IsStateMachineLocked && smi.HealthInstance != null && smi.HealthInstance.hitPoints <= 10f)
                .Transition(attackStates.staggering, smi => !smi.IsStateMachineLocked && smi.IsStaggered);
            // 执行攻击状态
            attackStates.attacking
                .Enter(smi =>
                {
                    TbbDebuger.LogDebug($"[MutanterStateMachine] 进入 AttackStates.attacking");
                    // 执行攻击
                    bool attackSuccess = smi.ExecuteAttack();
                    if (!attackSuccess)
                    {
                        // 攻击失败，转换到追逐状态
                        if (!smi.IsStateMachineLocked)
                        {
                            TbbDebuger.LogDebug($"[MutanterStateMachine] 攻击失败，转换到追逐状态");
                            smi.SetCurrentTarget(null);
                            smi.GoTo(attackStates.chasing);
                        }
                    }
                })
                .Update((smi, dt) =>
                {
                    TbbDebuger.LogDebug($"[MutanterStateMachine] Attacking状态Update - 锁定状态: {smi.IsStateMachineLocked}, 攻击状态: {smi.CombatManager?.IsAttacking}");
                    // 状态机锁定时不执行状态转换
                    if (smi.IsStateMachineLocked)
                    {
                        TbbDebuger.LogDebug($"[MutanterStateMachine] 状态机锁定，跳过状态转换");
                        return;
                    }

                    // 检查攻击是否完成
                    if (smi.CombatManager != null && !smi.CombatManager.IsAttacking)
                    {
                        TbbDebuger.LogDebug($"[MutanterStateMachine] 攻击完成，转换到追逐状态");
                        if (!smi.IsStateMachineLocked)
                        {
                            smi.SetCurrentTarget(null);
                            smi.GoTo(attackStates.chasing);
                        }
                    }
                }, UpdateRate.SIM_1000ms)
                .Transition(hostile, smi => !smi.IsStateMachineLocked && smi.EmotionSMI != null && smi.EmotionSMI.INSANITYValue > smi.def.sanityThresholdToAttack)
                .Transition(incapacitated, smi => !smi.IsStateMachineLocked && smi.HealthInstance != null && smi.HealthInstance.hitPoints <= 10f)
                .Transition(attackStates.staggering, smi => !smi.IsStateMachineLocked && smi.IsStaggered)
                .Transition(attackStates.retreating, smi => !smi.IsStateMachineLocked && smi.HealthInstance != null && smi.HealthInstance.hitPoints < smi.HealthInstance.maxHitPoints * 0.3f);
            // 撤退逃跑状态
            attackStates.retreating
                .Enter(smi =>
                {
                    TbbDebuger.LogDebug($"[MutanterStateMachine] Entering AttackStates.retreating");
                })
                .Update((smi, dt) =>
                {
                    // TODO: 实现撤退逻辑
                    // 这里简化处理，实际项目中应该实现远离威胁的逻辑

                    // 检查生命值是否恢复
                    if (smi.HealthInstance != null && smi.HealthInstance.hitPoints > smi.HealthInstance.maxHitPoints * 0.5f)
                    {
                        // 生命值恢复，转换到扫描状态
                        smi.GoTo(attackStates.chasing);
                    }
                }, UpdateRate.SIM_200ms)
                .Transition(hostile, smi => smi.EmotionSMI != null && smi.EmotionSMI.INSANITYValue > smi.def.sanityThresholdToAttack)
                .Transition(incapacitated, smi => smi.HealthInstance != null && smi.HealthInstance.hitPoints <= 10f)
                .Transition(attackStates.staggering, smi => smi.IsStaggered);

            // 受击硬直状态
            attackStates.staggering
                .Enter(smi =>
                {
                    TbbDebuger.LogDebug($"[MutanterStateMachine] Entering AttackStates.staggering");
                })
                .ScheduleGoTo(0.1f, attackStates.chasing) // 硬直时间1.5秒
                .Transition(incapacitated, smi => smi.HealthInstance != null && smi.HealthInstance.hitPoints <= 10f)
                .Transition(attackStates.retreating, smi => smi.HealthInstance != null && smi.HealthInstance.hitPoints < smi.HealthInstance.maxHitPoints * 0.3f);
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

            private Navigator navigator;
            public Navigator NavigatorInstance => navigator ??= master.gameObject.GetComponent<Navigator>();

            private bool _isStaggered = false;
            public bool IsStaggered => _isStaggered;

            private MutanterCombatManager _combatManager;
            public MutanterCombatManager CombatManager => _combatManager ??= master.gameObject.GetComponent<MutanterCombatManager>();

            private MutanterSkillComponent _skillComponent;
            public MutanterSkillComponent SkillComponent => _skillComponent ??= master.gameObject.GetComponent<MutanterSkillComponent>();

            private bool _isStateMachineLocked = false;
            public bool IsStateMachineLocked { get => _isStateMachineLocked; }

            private GameObject _currentTarget;
            public GameObject CurrentTarget => _currentTarget;

            public void SetCurrentTarget(GameObject target)
            {
                _currentTarget = target;
            }

            public void OnAttackComplete()
            {
                // 攻击完成，转换到追逐状态
                var smi = master.gameObject.GetSMI<StatesInstance>();
                TbbDebuger.LogDebug($"[MutanterStateMachine] {gameObject.name} OnAttackComplete, 当前状态 = {smi?.GetStatus()}, 锁定状态: {smi?.IsStateMachineLocked}");
                if (smi != null && smi.IsInsideState(sm.attackStates.attacking))
                {
                    TbbDebuger.LogDebug($"[MutanterStateMachine] 攻击完成回调，转换到追逐状态");
                    smi.SetCurrentTarget(null);
                    smi.GoTo(sm.attackStates.chasing);
                }
            }
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

                // 订阅伤害事件来设置硬直状态
                Subscribe((int)GameHashes.HealthChanged, OnDamageTaken);
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
            public bool ExecuteAttack()
            {
                // 检查生命值，确保只有在生命值大于0时才执行攻击
                if (smi.HealthInstance != null && smi.HealthInstance.hitPoints <= 0f)
                {
                    return false;
                }

                // 获取战斗管理器
                var combatManager = smi.master.gameObject.GetComponent<MutanterCombatManager>();

                if (combatManager != null && smi.EmotionSMI != null)
                {
                    var threaters = smi.EmotionSMI.GetThreaters();
                    TbbDebuger.LogDebug($"[MutanterStateMachine] 攻击目标: {threaters.Count} 个");

                    if (threaters != null && threaters.Count > 0)
                    {
                        GameObject target = null;
                        foreach (var threater in threaters)
                        {
                            if (threater != null && threater.gameObject != null
                            && threater.gameObject.GetComponent<Health>().hitPoints > 0f)
                            {
                                target = threater.gameObject;
                                break;
                            }
                        }
                        // 使用战斗管理器执行攻击（通过队列）
                        if (target != null)
                        {
                            TbbDebuger.LogDebug($"[MutanterStateMachine] 攻击目标: {target.name}");
                            smi.SetCurrentTarget(target);
                            return combatManager.QueueExecuteAttack(target);
                        }
                    }
                }
                return false;
            }
            public void LockStateMachine()
            {
                _isStateMachineLocked = true;
                TbbDebuger.LogDebug($"[MutanterStateMachine] 状态机已锁定");
            }

            public void UnlockStateMachine()
            {
                _isStateMachineLocked = false;
                TbbDebuger.LogDebug($"[MutanterStateMachine] 状态机已解锁");
            }
            private void OnDamageTaken(object data)
            {
                // 当受到伤害时设置硬直状态
                //_isStaggered = true;

                // 一段时间后重置硬直状态
                //gameObject.StartCoroutine(ResetStaggeredState());
            }

            private System.Collections.IEnumerator ResetStaggeredState()
            {
                yield return new WaitForSeconds(1.5f); // 与硬直状态持续时间一致
                _isStaggered = false;
                TbbDebuger.LogDebug($"[MutanterStateMachine] {gameObject.name} staggered state reset");
            }

            protected override void OnCleanUp()
            {
                Unsubscribe((int)MutanterGameHashes.MutanterContained, OnContained);
                Unsubscribe((int)MutanterGameHashes.MutanterBreachContained, OnBreachContained);
                Unsubscribe((int)GameHashes.HealthChanged, OnDamageTaken);
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
            // 移动到新单元格的方法
            public void MoveToNewCell()
            {
                var navigator = smi.NavigatorInstance;
                if (navigator == null) return;

                var kpid = smi.GetComponent<KPrefabID>();
                if (kpid == null)
                    return;

                // 检查是否为静止状态
                if (kpid.HasTag(GameTags.StationaryIdling))
                    return;

                // 使用类似IdleStates的移动逻辑
                MoveCellQuery query = new(navigator.CurrentNavType)
                {
                    allowLiquid = kpid.HasTag(GameTags.Amphibious),
                    submerged = kpid.HasTag(GameTags.Creatures.Submerged)
                };

                int cell = Grid.PosToCell(navigator);
                if (navigator.CurrentNavType == NavType.Hover && IsExposedToSpace(cell))
                {
                    int num = 0;
                    int cell2 = cell;
                    for (int index = 0; index < 10; ++index)
                    {
                        cell2 = Grid.CellBelow(cell2);
                        if (Grid.IsValidCell(cell2) && !Grid.IsSolidCell(cell2) && IsExposedToSpace(cell2))
                            ++num;
                        else
                            break;
                    }
                    query.lowerCellBias = num == 10;
                }
                navigator.RunQuery(query);
                if (navigator.CanReach(query.GetResultCell()))
                    navigator.GoTo(query.GetResultCell());
            }

            // 检查单元格是否暴露在太空
            static bool IsExposedToSpace(int cell)
            {
                if (!Grid.IsValidCell(cell))
                    return false;

                // 简单检查：如果单元格上方没有固体方块，且元素是真空，则认为暴露在太空
                int cellAbove = Grid.CellAbove(cell);
                return Grid.IsValidCell(cellAbove) && Grid.Element[cellAbove].IsVacuum;
            }

            // 移动单元格查询类
            class MoveCellQuery : PathFinderQuery
            {
                private NavType navType;
                private int targetCell = Grid.InvalidCell;
                private int maxIterations;

                public bool allowLiquid { get; set; }
                public bool submerged { get; set; }
                public bool lowerCellBias { get; set; }

                public MoveCellQuery(NavType navType)
                {
                    Reset(navType);
                }

                public void Reset(NavType navType)
                {
                    this.navType = navType;
                    this.maxIterations = UnityEngine.Random.Range(5, 25);
                    this.targetCell = Grid.InvalidCell;
                    this.allowLiquid = false;
                    this.submerged = false;
                    this.lowerCellBias = false;
                }
                public override bool IsMatch(int cell, int parent_cell, int cost)
                {
                    if (!Grid.IsValidCell(cell) || Grid.ObjectLayers[9].ContainsKey(cell))
                        return false;
                    bool flag1 = this.submerged || Grid.IsNavigatableLiquid(cell);
                    bool flag2 = this.navType != NavType.Swim;
                    bool flag3 = this.navType == NavType.Swim || this.allowLiquid;
                    if (flag1 && !flag3 || !flag1 && !flag2)
                        return false;
                    if (this.targetCell == Grid.InvalidCell || !this.lowerCellBias)
                    {
                        this.targetCell = cell;
                    }
                    else
                    {
                        int num = Grid.CellRow(this.targetCell);
                        if (Grid.CellRow(cell) < num)
                            this.targetCell = cell;
                    }
                    return --this.maxIterations <= 0;
                }
                public override int GetResultCell()
                {
                    return this.targetCell;
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
    }
}
