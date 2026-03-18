using Database;
using MutantContainmentProject.MutanterComponent;
using MutantContainmentProject.Room;
using System;
using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;
using UnityEngine;
using UnityEngine.UI;

namespace MutantContainmentProject.Buildings
{
    /**
     * 收容状态管理器 (ContainmentManager)
        功能: 监控并管理收容设施的状态，是触发畸变体行为转变的核心。
        监控项:
        收容等级: A级（最高）、B级、C级（最低）。
        设施完整性: 门、墙、设备的损坏程度。
        人员配置: 是否有足够员工在场。
        环境参数: 灯光、氧气、温度等。
        作用: 其状态变化是 EmotionMonitor 和 AberrationStateMachine 的重要输入，直接导致收容等级下降、焦躁或失控。
     */
    public class ContainmentMonitor : GameStateMachine<ContainmentMonitor, ContainmentMonitor.Instance, IStateMachineTarget, ContainmentMonitor.Def>
    {
        public class OperationalState : State { }

        public State Unoperational;

        public OperationalState Operational;

        public override void InitializeStates(out BaseState default_state)
        {
            default_state = Operational;

            Unoperational
                .TagTransition(GameTags.Operational, Operational, false)
                .Update("FindMutanters", delegate (Instance smi, float dt)
                {
                    smi.FindMutanter(smi);
                }, UpdateRate.SIM_1000ms, false); ;

            Operational
                .TagTransition(GameTags.Operational, Unoperational, true)
                .Update("FindMutanters", delegate (Instance smi, float dt)
                    {
                        smi.FindMutanter(smi);
                    }, UpdateRate.SIM_1000ms, false);
        }



        public class Instance : GameInstance
        {
            [SerializeField]
            public SecureAction CurrentAction = SecureAction.None;
            [SerializeField]
            public bool AlwaysExecute = false; // 执行模式：true为总是执行，false为正常执行
            private List<KPrefabID> _mutantersInRoom = new();
            [MyCmpAdd]
            public ManuallySetRemoteWorkTargetComponent remoteChore;

            private global::Room _mutanterContainer;
            private int onRoomUpdatedHandle;
            private Instance activeMonitor;
            private bool isControlStationActive = false;

            private List<MutanterSecurableMonitor.Instance> targetSecurables = new();

            public List<MutanterSecurableMonitor.Instance> TargetSecurables
            {
                get { return targetSecurables; }
            }
            private int onBuildingSelectHandle;


            public Instance(IStateMachineTarget master, Def def) : base(master, def)
            {
                onRoomUpdatedHandle = Subscribe(144050788, new Action<object>(OnRoomUpdated));
                Subscribe(493375141, new Action<object>(OnRefreshUserMenu));
                onBuildingSelectHandle = Subscribe(-1503271301, new Action<object>(OnBuildingSelect));
            }
            private void OnBuildingSelect(object data)
            {
                var selectable = gameObject.GetComponent<KSelectable>();
                if (selectable == null) return;
                selectable.RemoveStatusItem(ContainmentMonitorBuildingStatusItems.Instance.MutanterBeReleased);
            }
            protected override void OnCleanUp()
            {
                base.OnCleanUp();
                Unsubscribe(ref onRoomUpdatedHandle);
                Unsubscribe(ref onBuildingSelectHandle);
            }

            private void OnRefreshUserMenu(object data)
            {
                Game.Instance.userMenu.AddButton(gameObject, new KIconButtonMenu.ButtonInfo(
                    "action_discover",
                    AlwaysExecute ? STRINGS.UI.UISIDESCREENS.CONTAINTMENTMONITOR.BUTTON_NORMAL_EXECUTE : STRINGS.UI.UISIDESCREENS.CONTAINTMENTMONITOR.BUTTON_ALWAYS_EXECUTE,
                    ToggleAlwaysExecute,
                    tooltipText: AlwaysExecute ? STRINGS.UI.UISIDESCREENS.CONTAINTMENTMONITOR.NORMAL_EXECUTE_TOOLTIP : STRINGS.UI.UISIDESCREENS.CONTAINTMENTMONITOR.ALWAYS_EXECUTE_TOOLTIP
                ), 10f);
            }

            private void OnRoomUpdated(object data)
            {
                if (data == null)
                {
                    return;
                }
                _mutanterContainer = (data as global::Room);

                if (_mutanterContainer.roomType.Id != ContainmentCharmberRoom.ContainmentChamber.Id)
                {
                    TbbDebuger.LogDebug($"_mutanterContainer roomType 不是:[{ContainmentCharmberRoom.ContainmentChamber.Name}]");
                    TriggerContainmentMonitorNoLongerAvailable();
                    //_mutanterContainer = null;

                }
                else
                {
                    TbbDebuger.LogDebug($"_mutanterContainer roomType 形成:[{ContainmentCharmberRoom.ContainmentChamber.Name}]");
                    findMutanterInRoom();
                }
            }


            public void findMutanterInRoom()
            {
                if (_mutanterContainer?.cavity == null)
                {
                    return;
                }
                foreach (int cell in _mutanterContainer.cavity.cells)
                {
                    GameObject gameObject = Grid.Objects[cell, (int)ObjectLayer.Pickupables];
                    if (gameObject == null) continue;

                    KPrefabID component = gameObject.GetComponent<KPrefabID>();
                    if (component.HasTag(MutanterTags.Mutanter) && !_mutantersInRoom.Contains(component)) _mutantersInRoom.Add(component);
                    //TbbDebuger.LogDebug($"cell:[{cell}] layer:[3] name:[{component.name}]");
                }
            }
            public void TriggerContainmentMonitorNoLongerAvailable()
            {
                for (int i = TargetSecurables.Count - 1; i >= 0; i--)
                {
                    MutanterSecurableMonitor.Instance instance = TargetSecurables[i];
                    if (instance.IsNullOrStopped())
                    {
                        TargetSecurables.RemoveAt(i);
                    }
                    else
                    {
                        instance.GoOutOfContainment();
                        this.TargetSecurables.Remove(instance);

                        if (_mutantersInRoom != null) _mutantersInRoom.Clear();
                    }
                }
                TbbDebuger.LogDebug($"[畸变收容所] 房间属性变更，不再是收容所");
                this.activeMonitor = null;
            }
            private void SetRemoteChore(Instance instance, Chore chore)
            {
                instance.remoteChore.SetChore(chore);
            }
            public Chore CreateChore(Instance smi)
            {
                TbbDebuger.LogDebug($"[畸变收容所] 创建安全控制任务 [{smi.gameObject.name}]");
                var chore = new WorkChore<ContainmentMonitorWorkable>(
                    Db.Get().ChoreTypes.Research,
                    gameObject.GetComponent<ContainmentMonitorWorkable>(), null, true, null, null, null, true
                );
                return chore;
            }
            public void FindMutanter(Instance instance)
            {
                findMutanterInRoom();
                //TbbDebuger.LogDebug($"[畸变体收容室] 收容室数量:[{_mutantersInRoom.Count}][{targetSecurable.Count}]");
                foreach (var kprefabID in _mutantersInRoom)
                {
                    MutanterSecurableMonitor.Instance smi = null;
                    try { 
                        smi = kprefabID.gameObject.GetSMI<MutanterSecurableMonitor.Instance>();
                    } catch { }
                    if (smi == null) continue;
                    if (!TargetSecurables.Contains(smi))
                    {
                        smi.SetContainmentMonitor(this);
                        TargetSecurables.Add(smi);
                    }
                    // 应用控制站效果
                    if (isControlStationActive)
                    {
                        smi.ApplyControlStationEffect();
                    }
                    else
                    {
                        smi.RemoveControlStationEffect();
                    }
                    //TbbDebuger.LogDebug($"[畸变收容所] 畸变体:name[{kprefabID.name}] ShouldBeSecured: [{smi.ShouldBeSecured()}] RemoteDockChore:[{instance.remoteChore.RemoteDockChore}] RemoteDockChore.Complete:[{instance.remoteChore.RemoteDockChore?.isComplete}]");
                    if (CurrentAction != SecureAction.None && smi.ShouldBeSecured() && (instance.remoteChore.RemoteDockChore == null || instance.remoteChore.RemoteDockChore?.isComplete == true))
                    {
                        //TbbDebuger.LogDebug($"[畸变收容所] 畸变体:name[{kprefabID.name}] 需要被收容，创建收容任务");
                        instance.SetRemoteChore(instance, CreateChore(instance));
                    }
                }
            }

            public void EnableControlStationEffect()
            {
                isControlStationActive = true;
                foreach (var smi in TargetSecurables)
                {
                    if (smi != null && !smi.IsNullOrStopped())
                    {
                        smi.ApplyControlStationEffect();
                    }
                }
            }

            public void DisableControlStationEffect()
            {
                isControlStationActive = false;
                foreach (var smi in TargetSecurables)
                {
                    if (smi != null && !smi.IsNullOrStopped())
                    {
                        smi.RemoveControlStationEffect();
                    }
                }
            }

            public void SetAlwaysExecute(bool alwaysExecute)
            {
                AlwaysExecute = alwaysExecute;
            }

            public bool GetAlwaysExecute()
            {
                return AlwaysExecute;
            }

            public void ToggleAlwaysExecute()
            {
                AlwaysExecute = !AlwaysExecute;
            }
            public void TriggerBreakout()
            {
                if(TargetSecurables.Count > 0){
                    foreach (var smi in TargetSecurables)
                    {
                        if (smi != null && !smi.IsNullOrStopped())
                        {
                            smi.GoOutOfContainment();
                        }
                    }
                }
            }
        }
        public class Def : BaseDef { }
    }

    public class ContainmentMonitorBuildingStatusItems
    {
        public StatusItem ContainmentSuccess;
        public StatusItem ContainmentFailure;
        public StatusItem CorrosionWarning;
        public StatusItem HighCorrosionWarning;
        public StatusItem CorrosionOverflow;

        public StatusItem WorkerDamage; // 小人受到伤害的状态项

        public StatusItem MutanterBeReleased; // 畸变体被释放的状态项

        private static ContainmentMonitorBuildingStatusItems _instance;
        public static ContainmentMonitorBuildingStatusItems Instance => _instance ??= new ContainmentMonitorBuildingStatusItems();

        public void CreateStatusItems(BuildingStatusItems buildingStatusItems)
        {
            ContainmentSuccess = buildingStatusItems.Add(new StatusItem("ContainmentSuccess", "BUILDINGS", "", StatusItem.IconType.Info, NotificationType.Good, false, OverlayModes.None.ID, true, 129022, null));
            ContainmentSuccess.resolveStringCallback = delegate (string str, object data)
            {
                return STRINGS.BUILDINGS.STATUSITEMS.CONTAINMENTMONITOR.SUCCESS.NAME;
            };
            ContainmentSuccess.resolveTooltipCallback = delegate (string str, object data)
            {
                int workerSubtasks = 0;
                int mutanterSubtasks = 0;

                // 检查data类型
                if (data is ContainmentMonitorWorkable workable)
                {
                    // 兼容旧的方式
                    workerSubtasks = workable.WorkerCompletedSubtasks;
                    mutanterSubtasks = workable.MutanterCompletedSubtasks;
                }
                else if (data is System.Tuple<int, int> tuple)
                {
                    // 处理Tuple类型
                    workerSubtasks = tuple.Item1;
                    mutanterSubtasks = tuple.Item2;
                }

                return string.Format(STRINGS.BUILDINGS.STATUSITEMS.CONTAINMENTMONITOR.SUCCESS.TOOLTIP, workerSubtasks, mutanterSubtasks);
            };

            ContainmentFailure = buildingStatusItems.Add(new StatusItem("ContainmentFailure", "BUILDINGS", "", StatusItem.IconType.Exclamation, NotificationType.Bad, false, OverlayModes.None.ID, true, 129022, null));
            ContainmentFailure.resolveStringCallback = delegate (string str, object data)
            {
                return STRINGS.BUILDINGS.STATUSITEMS.CONTAINMENTMONITOR.FAILURE.NAME;
            };
            ContainmentFailure.resolveTooltipCallback = delegate (string str, object data)
            {
                int workerSubtasks = 0;
                int mutanterSubtasks = 0;

                // 检查data类型
                if (data is ContainmentMonitorWorkable workable)
                {
                    // 兼容旧的方式
                    workerSubtasks = workable.WorkerCompletedSubtasks;
                    mutanterSubtasks = workable.MutanterCompletedSubtasks;
                }
                else if (data is System.Tuple<int, int> tuple)
                {
                    // 处理Tuple类型
                    workerSubtasks = tuple.Item1;
                    mutanterSubtasks = tuple.Item2;
                }

                return string.Format(STRINGS.BUILDINGS.STATUSITEMS.CONTAINMENTMONITOR.FAILURE.TOOLTIP, workerSubtasks, mutanterSubtasks);
            };

            // 小人受到伤害的状态项
            WorkerDamage = buildingStatusItems.Add(new StatusItem("WorkerDamage", "BUILDINGS", "", StatusItem.IconType.Exclamation, NotificationType.Bad, false, OverlayModes.None.ID, true, 129022, null));
            WorkerDamage.resolveStringCallback = delegate (string str, object data)
            {
                return STRINGS.BUILDINGS.STATUSITEMS.WORKER_DAMAGE.NAME;
            };
            WorkerDamage.resolveTooltipCallback = delegate (string str, object data)
            {
                float damage = 0f;
                string damageType = "Unknown";

                // 检查data类型
                if (data is System.Tuple<float, string> damageInfo)
                {
                    damage = damageInfo.Item1;
                    damageType = damageInfo.Item2;
                }

                return string.Format(STRINGS.BUILDINGS.STATUSITEMS.WORKER_DAMAGE.TOOLTIP, damage, damageType);
            };

            // 腐蚀预警状态项
            CorrosionWarning = buildingStatusItems.Add(new StatusItem("CorrosionWarning", "BUILDINGS", "", StatusItem.IconType.Exclamation, NotificationType.Bad, false, OverlayModes.None.ID, true, 129022, null));
            CorrosionWarning.resolveTooltipCallback = delegate (string str, object data)
            {
                float corrosionValue = 0f;
                // 检查data类型
                if (data is float level)
                {
                    corrosionValue = level;
                }
                return string.Format(STRINGS.BUILDINGS.STATUSITEMS.CORROSIONWARNING.TOOLTIP, corrosionValue);
            };

            // 高腐蚀预警状态项
            HighCorrosionWarning = buildingStatusItems.Add(new StatusItem("HighCorrosionWarning", "BUILDINGS", "", StatusItem.IconType.Exclamation, NotificationType.Bad, false, OverlayModes.None.ID, true, 129022, null));
            HighCorrosionWarning.resolveTooltipCallback = delegate (string str, object data)
            {
                float corrosionValue = 0f;
                // 检查data类型
                if (data is float level)
                {
                    corrosionValue = level;
                }
                return string.Format(STRINGS.BUILDINGS.STATUSITEMS.HIGHCORROSIONWARNING.TOOLTIP, corrosionValue);
            };

            // 溢流突破状态项
            CorrosionOverflow = buildingStatusItems.Add(new StatusItem("CorrosionOverflow", "BUILDINGS", "", StatusItem.IconType.Exclamation, NotificationType.Bad, false, OverlayModes.None.ID, true, 129022, null));

            // 畸变体被释放的状态项
            MutanterBeReleased = buildingStatusItems.Add(new StatusItem("MutanterBeReleased", "BUILDINGS", "", StatusItem.IconType.Exclamation, NotificationType.Bad, false, OverlayModes.None.ID, true, 129022, null));
            MutanterBeReleased.AddNotification();
        }
    }

}
