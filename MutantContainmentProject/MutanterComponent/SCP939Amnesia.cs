using Klei.AI;
using MutantContainmentProject.MutanterEffect;
using TBB.He.TbbLib.Debuger;
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
            private Effects effects;
            public Effects EffectsInstance => effects ??= gameObject.GetComponent<Effects>();
            public StatesInstance(SCP939Amnesia master) : base(master)
            {
                TbbDebuger.LogDebug($"添加SCP939Amnesia效果");
                EffectsInstance?.Add(Db.Get().effects.Get(MutanterEffects.INHALATIONMIASAM_EFFECT), true);
            }
            public bool IsInhalationMiasam()
            {
                return EffectsInstance != null && EffectsInstance.HasEffect(Db.Get().effects.Get(MutanterEffects.INHALATIONMIASAM_EFFECT));
            }
            public bool IsSleeping()
            {
                StaminaMonitor.Instance smi = master.GetSMI<StaminaMonitor.Instance>();
                return smi != null && smi.IsSleeping();
            }

            public bool IsAmnesiaActive()
            {
                return IsInhalationMiasam();
            }

            public GameObject CreateFloorLocator()
            {
                Sleepable safeFloorLocator = SleepChore.GetSafeFloorLocator(master.gameObject);
                // safeFloorLocator.effectName = "NarcolepticSleep";
                safeFloorLocator.effectName = Db.Get().effects.Get(MutanterEffects.SCP939_AMNESIA_EFFECT).Id;
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
                root
                    .TagTransition(GameTags.Dead, null);
                idle
                    .Enter("ScheduleNextSleep", smi =>
                    {
                        smi.ScheduleGoTo(Random.Range(50f, 150f), sleepy);
                    });
                sleepy
                    .Enter("Is Already Sleeping Check", smi =>
                        {
                            if (smi.master.GetSMI<StaminaMonitor.Instance>().IsSleeping())
                                smi.GoTo(idle);
                            else
                                smi.ScheduleGoTo(Random.Range(300f, 600f), idle);
                        })
                    .ToggleUrge(Db.Get().Urges.Narcolepsy)
                    .ToggleChore(CreateSCP939AmnesiaChore, idle);
            }

            private Chore CreateSCP939AmnesiaChore(StatesInstance smi)
            {
                TbbDebuger.LogDebug($"创建SCP939AmnesiaChore");
                GameObject floorLocator = smi.CreateFloorLocator();
                SleepChore amnesiaChore = new(Db.Get().ChoreTypes.Narcolepsy, smi.master, floorLocator, true, false);
                amnesiaChore.GetPreconditions().Clear();
                amnesiaChore.AddPrecondition(IsSCP939AmnesiaPrecondition, null);
                return amnesiaChore;
            }
        }
    }
}