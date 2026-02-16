using MutantContainmentProject.MutanterComponent;
using MutantContainmentProject.Room;
using System;
using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

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
                .TagTransition(GameTags.Operational, Operational, false);

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
            private List<KPrefabID> _mutantersInRoom = new();
            [MyCmpAdd]
            public ManuallySetRemoteWorkTargetComponent remoteChore;

            private global::Room _mutanterContainer;
            private int onRoomUpdatedHandle;
            private Instance activeMonitor;

            private List<MutanterSecurableMonitor.Instance> targetSecurable = new List<MutanterSecurableMonitor.Instance>();

            public List<MutanterSecurableMonitor.Instance> TargetSecurables
            {
                get { return targetSecurable; }
            }
            public Instance(IStateMachineTarget master, Def def) : base(master, def)
            {
                onRoomUpdatedHandle = Subscribe(144050788, new Action<object>(OnRoomUpdated));
            }
            protected override void OnCleanUp()
            {
                base.OnCleanUp();
                Unsubscribe(ref onRoomUpdatedHandle);
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
                    findMutanterInRoom();
                }
            }
            public void findMutanterInRoom()
            {
                foreach (int cell in _mutanterContainer.cavity.cells)
                {
                    GameObject gameObject = Grid.Objects[cell, (int)ObjectLayer.Pickupables];
                    if (!(gameObject == null))
                    {
                        KPrefabID component = gameObject.GetComponent<KPrefabID>();
                        if (component.HasTag(MutanterTags.Mutanter)) _mutantersInRoom.Add(component);
                        TbbDebuger.LogDebug($"cell:[{cell}] layer:[3] name:[{component.name}]");
                    }
                }
            }
            public void TriggerContainmentMonitorNoLongerAvailable()
            {
                for (int i = targetSecurable.Count - 1; i >= 0; i--)
                {
                    MutanterSecurableMonitor.Instance instance = targetSecurable[i];
                    if (instance.IsNullOrStopped() || instance.States.IsNullOrStopped())
                    {
                        targetSecurable.RemoveAt(i);
                    }
                    else
                    {
                        instance.GoOutOfContainment();
                        this.targetSecurable.Remove(instance);

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
                if (_mutanterContainer == null) return;

                if (this.targetSecurable.Count == 2)
                {
                    return;
                }

                if (_mutantersInRoom == null) findMutanterInRoom();
                TbbDebuger.LogDebug($"[畸变收容所] _mutantersInRoom:[{_mutantersInRoom.Count}]");
                foreach (var kprefabID in _mutantersInRoom)
                {
                    var smi = kprefabID.GetSMI<MutanterSecurableMonitor.Instance>();
                    if (smi == null) continue;
                    if (!targetSecurable.Contains(smi))
                    {
                        smi.States.SetContainmentMonitor(this);
                        targetSecurable.Add(smi);
                    }
                    TbbDebuger.LogDebug($"[畸变收容所] 畸变体:name[{kprefabID.name}] ShouldBeSecured: [{smi.ShouldBeSecured()}] RemoteDockChore:[{instance.remoteChore.RemoteDockChore}] RemoteDockChore.Complete:[{instance.remoteChore.RemoteDockChore?.isComplete}]");
                    if (CurrentAction != SecureAction.None && smi.ShouldBeSecured() && (instance.remoteChore.RemoteDockChore == null || (instance.remoteChore.RemoteDockChore?.isComplete == true)))
                    {
                        TbbDebuger.LogDebug($"[畸变收容所] 畸变体:name[{kprefabID.name}] 需要被收容，创建收容任务");
                        instance.SetRemoteChore(instance, CreateChore(instance));
                    }
                }
            }
        }
        public class Def : BaseDef { }
    }
}
