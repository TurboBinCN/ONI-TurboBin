using MutantContainmentProject.Buildings;
using System.Linq;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    public class CircusJokerBehavior : StateMachineComponent<CircusJokerBehavior.StatesInstance>
    {
        public class StatesInstance : GameStateMachine<States, StatesInstance, CircusJokerBehavior, object>.GameInstance
        {
            public StatesInstance(CircusJokerBehavior master) : base(master)
            {
            }
            public void StopIdleStates()
            {
                if (IdleSmi == null) return;
                IdleSmi.GoTo((StateMachine.BaseState) IdleSmi.sm.root);
                IdleSmi.GetComponent<Navigator>().Stop();
            }

            public void ContinueIdleStates()
            {
                if (IdleSmi == null) return;
                IdleSmi.GoTo(IdleSmi.sm.GetDefaultState());
            }
            private Navigator _navigator;
            public Navigator Navigator => _navigator ??= master.gameObject.GetComponent<Navigator>();
            private MoveImmediately _teleporter;
            public MoveImmediately Teleporter => _teleporter ??= master.gameObject.GetComponent<MoveImmediately>();
            private IdleStates.Instance _idleSmi;
            public IdleStates.Instance IdleSmi => _idleSmi ??= master.gameObject.GetSMI<IdleStates.Instance>();
            public GameObject TargetStation;
            public float LastStationVisitTime = 0f;
        }

        public class States : GameStateMachine<States, StatesInstance, CircusJokerBehavior>
        {
            public State idle;
            public State moving;
            public State teleporting;
            public State mischief_pre;
            public State mischief_loop;
            public State mischief_pst;

            public BoolParameter StateArrived;
            public BoolParameter StateMonitorStationFounded;
            public override void InitializeStates(out BaseState default_state)
            {
                default_state = idle;

                idle
                    .ParamTransition(StateMonitorStationFounded, moving, IsTrue)
                    .Update((smi, dt) => FindStation(smi), UpdateRate.SIM_1000ms);

                moving
                    .Enter((smi) =>
                    {
                        StartMove(smi);
                    })
                    .ParamTransition(StateArrived, mischief_pre, IsTrue)
                    .Exit((smi) =>
                    {
                        StateArrived.Set(false, smi);
                    });

                mischief_pre
                    .Enter((smi) =>
                    {
                        RemoveStatusItem(smi);
                    })
                    .PlayAnim("working_pre")
                    .OnAnimQueueComplete(mischief_loop)
                    .ScheduleGoTo(1f, mischief_loop);

                mischief_loop
                    .PlayAnim("working_loop", KAnim.PlayMode.Loop)
                    .ScheduleGoTo(10f, mischief_pst)
                    .Exit((smi) =>
                    {
                        SetCorrosionToFull(smi);
                        AddStatusItem(smi);
                    });

                mischief_pst
                    .Enter((smi) =>
                    {
                        smi.StopIdleStates();
                    })
                    .PlayAnim("working_pst", KAnim.PlayMode.Once)
                    .ScheduleGoTo(2f,idle)
                    .Exit((smi) =>
                    {
                        smi.TargetStation = null;
                        StateMonitorStationFounded.Set(false, smi);
                        smi.LastStationVisitTime = Time.time;
                        smi.ContinueIdleStates();
                    }); ;
            }

            private bool NeedRelease(GameObject mutanterMonitorStation)
            {
                if (mutanterMonitorStation == null) return false;
                var mutanterMonitor = mutanterMonitorStation.GetSMI<ContainmentMonitor.Instance>();
                if (mutanterMonitor == null) return false;
                return mutanterMonitor.TargetSecurables.Count > 0 && mutanterMonitor.TargetSecurables.Any(securable => securable?.gameObject.GetSMI<MutanterSecurableMonitor.Instance>()?.IsSecured() == true);
            }
            private void FindStation(StatesInstance smi)
            {
                var stationKPrefabs = ContainmentMonitorStationManager.GetAllStations();
                smi.TargetStation = null;
                if (Time.time - smi.LastStationVisitTime < 60f && smi.LastStationVisitTime != 0) return;
                TbbDebuger.LogDebug($"CircusJokerBehavior.FindStation: 共 {stationKPrefabs.Count} 个监控站");

                foreach (var stationKPrefab in stationKPrefabs)
                {
                    if (stationKPrefab != null && stationKPrefab.gameObject != null && stationKPrefab.gameObject.activeSelf && NeedRelease(stationKPrefab.gameObject))
                    {
                        smi.TargetStation = stationKPrefab.gameObject;
                        StateMonitorStationFounded.Set(true, smi);
                        TbbDebuger.LogDebug($"CircusJokerBehavior.FindStation: 找到目标站 {smi.TargetStation.GetInstanceID()}");
                        break;
                    }
                }
            }

            private void StartMove(StatesInstance smi)
            {
                if (smi.TargetStation != null)
                {
                    TbbDebuger.LogDebug($"CircusJokerBehavior.StartMove: 目标站 {smi.TargetStation.GetInstanceID()}");
                    int targetCell = Grid.PosToCell(smi.TargetStation.transform.position);

                    if (smi.Navigator == null) return;
                    if (smi.Navigator.CanReach(targetCell))
                    {
                        smi.Navigator.GoTo(targetCell);
                        System.Action<object> NavigatorEvent = (data) =>
                        {
                            if (targetCell == Grid.PosToCell(smi.master.gameObject.transform.position))
                            {
                                StateArrived.Set(true, smi);
                            }
                            smi.Navigator.SetCurrentNavType(NavType.Floor);
                            smi.Navigator.Stop();
                            smi.Navigator.Unsubscribe((int)GameHashes.DestinationReached);
                        };
                        smi.Navigator.Subscribe((int)GameHashes.DestinationReached, NavigatorEvent);
                        smi.Navigator.Subscribe((int)GameHashes.NavigationFailed, (_) =>
                        {
                            smi.Navigator.Stop();
                            smi.Navigator.Unsubscribe((int)GameHashes.NavigationFailed);
                        });
                    }
                    else
                    {
                        TeleportToStation(smi);
                    }
                }
            }

            private void TeleportToStation(StatesInstance smi)
            {
                if (smi.TargetStation != null && smi.Teleporter != null)
                {
                    var targetPos = smi.TargetStation.transform.position;
                    smi.Teleporter.TeleportTo(targetPos);
                    StateArrived.Set(true, smi);
                }
            }

            private void SetCorrosionToFull(StatesInstance smi)
            {
                if (smi.TargetStation != null)
                {
                    var corrosionManager = smi.TargetStation.GetComponent<CorrosionManager>();
                    if (corrosionManager != null)
                    {
                        corrosionManager.CorrosionValue = 100f;
                    }
                }
            }

            private void AddStatusItem(StatesInstance smi)
            {
                var selectable = smi.TargetStation.GetComponent<KSelectable>();
                if (selectable != null)
                {
                    selectable.AddStatusItem(ContainmentMonitorBuildingStatusItems.Instance.MutanterBeReleased);
                }
            }

            private void RemoveStatusItem(StatesInstance smi)
            {
                var selectable = smi.TargetStation.GetComponent<KSelectable>();
                if (selectable != null)
                {
                    selectable.RemoveStatusItem(ContainmentMonitorBuildingStatusItems.Instance.MutanterBeReleased);
                }
            }
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            smi.StartSM();
        }

        protected override void OnCleanUp()
        {
            base.OnCleanUp();
        }
    }
}