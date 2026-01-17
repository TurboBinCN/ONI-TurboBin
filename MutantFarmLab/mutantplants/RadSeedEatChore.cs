using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MutantFarmLab.mutantplants
{
    public class RadSeedEatChore : Chore<RadSeedEatChore.StatesInstance>
    {
        private RadSeedEatWorkable radSeedEat;
        private Pickupable pickupable;

        public RadSeedEatChore(RadSeedEatWorkable master) : base(Db.Get().ChoreTypes.Eat, master, null, false, null, null, null, PriorityScreen.PriorityClass.personalNeeds, 9, false, true, 0, false, ReportManager.ReportType.WorkTime)
        {
            radSeedEat = master;
            pickupable = radSeedEat.GetComponent<Pickupable>();
            smi = new StatesInstance(this);
            AddPrecondition(ChorePreconditions.instance.CanPickup, pickupable);
            AddPrecondition(CanCure, this);
            AddPrecondition(IsConsumptionPermitted, this);

        }
        public override void Begin(Chore.Precondition.Context context)
        {
            smi.sm.source.Set(pickupable.gameObject, smi, false);
            smi.sm.requestedcount.Set(1f, smi, false);
            smi.sm.eater.Set(context.consumerState.gameObject, smi, false);
            base.Begin(context);
            new RadSeedEatChore(radSeedEat);
        }
        public static readonly Chore.Precondition CanCure = new Chore.Precondition
        {
            id = "CanCure",
            description = global::STRINGS.DUPLICANTS.CHORES.PRECONDITIONS.CAN_CURE,
            fn = delegate (ref Chore.Precondition.Context context, object data)
            {
                return ((RadSeedEatChore)data).radSeedEat.CanBeTakenBy(context.consumerState.gameObject);
            }
        };
        public static readonly Chore.Precondition IsConsumptionPermitted = new Chore.Precondition
        {
            id = "IsConsumptionPermitted",
            description = global::STRINGS.DUPLICANTS.CHORES.PRECONDITIONS.IS_CONSUMPTION_PERMITTED,
            fn = delegate (ref Chore.Precondition.Context context, object data)
            {
                RadSeedEatChore radSeedEatChore = (RadSeedEatChore)data;
                ConsumableConsumer consumableConsumer = context.consumerState.consumableConsumer;
                return consumableConsumer == null || consumableConsumer.IsPermitted(radSeedEatChore.radSeedEat.PrefabID().Name);
            }
        };
        public class StatesInstance : GameStateMachine<States, StatesInstance, RadSeedEatChore, object>.GameInstance
        {
            public StatesInstance(RadSeedEatChore master) : base(master)
            {
            }
        }

        public class States : GameStateMachine<States, StatesInstance, RadSeedEatChore>
        {
            public TargetParameter eater;
            public TargetParameter source;

            public TargetParameter chunk;

            public FloatParameter requestedcount;

            public FloatParameter actualcount;

            public FetchSubState fetch;

            public State eatradseed;
            public override void InitializeStates(out BaseState default_state)
            {
                default_state = fetch;

                Target(eater);

                fetch.InitializeStates(eater, source, chunk, requestedcount, actualcount, eatradseed, null);

                eatradseed.ToggleAnims("anim_eat_floor_kanim", 0f).ToggleTag(GameTags.Edible).ToggleWork("EatRadSeed", delegate (StatesInstance smi)
                {
                    RadSeedEatWorkable workable = chunk.Get<RadSeedEatWorkable>(smi);
                    eater.Get<WorkerBase>(smi).StartWork(new WorkerBase.StartWorkInfo(workable));
                }, (smi) => true, null, null);
            }
        }
    }
}
