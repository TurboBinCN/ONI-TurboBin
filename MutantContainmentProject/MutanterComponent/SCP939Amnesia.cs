using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    [SkipSaveFileSerialization]
    public class SCP939Amnesia : StateMachineComponent<SCP939Amnesia.StatesInstance>
    {
        public static readonly Chore.Precondition IsSCP939AmnesiaPrecondition = new()
        {
            fn = (ref Chore.Precondition.Context context, object data) =>
            {
                SCP939Amnesia component = context.consumerState.consumer.GetComponent<SCP939Amnesia>();
                return component != null && component.IsAmnesiaActive();
            }
        };

        protected override void OnSpawn() => smi.StartSM();

        public bool IsAmnesiaActive() => smi.IsAmnesiaActive();

        public class StatesInstance : GameStateMachine<States, StatesInstance, SCP939Amnesia, object>.GameInstance
        {
            public StatesInstance(SCP939Amnesia master) : base(master)
            { }

            public bool IsSleeping()
            {
                StaminaMonitor.Instance smi = master.GetSMI<StaminaMonitor.Instance>();
                return smi != null && smi.IsSleeping();
            }

            public bool IsAmnesiaActive() => GetCurrentState() == sm.sleepy;

            public GameObject CreateFloorLocator()
            {
                Sleepable safeFloorLocator = SleepChore.GetSafeFloorLocator(master.gameObject);
                safeFloorLocator.effectName = "SCP939AmnesiaSleep";
                safeFloorLocator.stretchOnWake = false;
                return safeFloorLocator.gameObject;
            }
        }

        public class States : GameStateMachine<States, StatesInstance, SCP939Amnesia>
        {
            public State idle;
            public State sleepy;

            public override void InitializeStates(out BaseState default_state)
            {
                default_state = idle;
                root.TagTransition(GameTags.Dead, null);
                idle.Enter("ScheduleNextSleep", smi => smi.ScheduleGoTo(GetNewInterval(300f, 600f), sleepy)); // 5-10分钟内随机
                sleepy.Enter("Is Already Sleeping Check", smi =>
                {
                    if (smi.master.GetSMI<StaminaMonitor.Instance>().IsSleeping())
                        smi.GoTo(idle);
                    else
                        smi.ScheduleGoTo(GetNewInterval(120f, 300f), idle); // 睡眠2-5分钟
                }).ToggleUrge(Db.Get().Urges.Sleep).ToggleChore(CreateSCP939AmnesiaChore, idle);
            }

            private Chore CreateSCP939AmnesiaChore(StatesInstance smi)
            {
                GameObject floorLocator = smi.CreateFloorLocator();
                SleepChore amnesiaChore = new(Db.Get().ChoreTypes.Sleep, smi.master, floorLocator, true, false);
                amnesiaChore.AddPrecondition(IsSCP939AmnesiaPrecondition, null);
                return amnesiaChore;
            }

            private float GetNewInterval(float min, float max)
            {
                return Random.Range(min, max);
            }
        }
    }
}