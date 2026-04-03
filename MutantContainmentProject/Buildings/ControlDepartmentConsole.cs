using Klei.AI;
using MutantContainmentProject.MutanterEffect;
using System;
using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.Buildings
{
    public class ControlDepartmentConsole : GameStateMachine<ControlDepartmentConsole, ControlDepartmentConsole.Instance, IStateMachineTarget, ControlDepartmentConsole.Def>
    {
        private Signal activateSignal;
        [SerializeField]
        public FloatParameter activationTimer;
        private NonOperationalState nonOperational;
        private OperationalState operational;
        public BoolParameter hasBeenWorkedByResearcher;
        private const string OnAnimName = "on";
        private const string OffAnimName = "off";
        private const string ActiveAnimName = "active";

        public override void InitializeStates(out BaseState default_state)
        {
            default_state = operational;
            serializable = SerializeType.ParamsOnly;

            nonOperational
                .DefaultState(nonOperational.off)
                .Enter(smi => smi.RefreshLogicOutput())
                .TagTransition(GameTags.Operational, operational);

            root
                .Enter((smi) => RefreshAnimation(smi))
                .Enter(smi => RefreshStorageRequirements(smi));

            nonOperational.off
                .PlayAnim(OffAnimName);

            operational
                .DefaultState(operational.idle)
                .TagTransition(GameTags.Operational, nonOperational, true)
                .PlayAnim(OnAnimName)
                .Enter(smi => smi.RefreshLogicOutput());

            operational.idle
                .ParamTransition(hasBeenWorkedByResearcher, operational.active, IsTrue)
                .ParamTransition(hasBeenWorkedByResearcher, operational.researcherInteractionNeeded, IsFalse);
            operational.researcherInteractionNeeded
                .EventTransition(GameHashes.UpdateRoom, operational.researcherInteractionNeeded.blocked, smi => !WorkRequirementsMet(smi))
                .EventTransition(GameHashes.UpdateRoom, operational.researcherInteractionNeeded.available, WorkRequirementsMet)
                .EventTransition(GameHashes.OnStorageChange, operational.researcherInteractionNeeded.available, WorkRequirementsMet)
                .EventTransition(GameHashes.OnStorageChange, operational.researcherInteractionNeeded.blocked, smi => !WorkRequirementsMet(smi))
                .ParamTransition(hasBeenWorkedByResearcher, operational.active, IsTrue);

            operational.researcherInteractionNeeded.blocked
                .ToggleMainStatusItem(Db.Get().BuildingStatusItems.GeoTunerResearchNeeded).DoNothing();

            operational.researcherInteractionNeeded.available
                .DefaultState(operational.researcherInteractionNeeded.available.waitingForDupe)
                .ToggleRecurringChore(new Func<Instance, Chore>(CreateResearchChore))
                .WorkableCompleteTransition(smi => smi.workable, operational.researcherInteractionNeeded.completed);

            operational.researcherInteractionNeeded.available.waitingForDupe
                .ToggleMainStatusItem(Db.Get().BuildingStatusItems.GeoTunerResearchNeeded)
                .WorkableStartTransition(smi => smi.workable, operational.researcherInteractionNeeded.available.inProgress);

            operational.researcherInteractionNeeded.available.inProgress
                .ToggleMainStatusItem(Db.Get().BuildingStatusItems.GeoTunerResearchInProgress)
                .WorkableStopTransition(smi => smi.workable, operational.researcherInteractionNeeded.available.waitingForDupe);

            operational.researcherInteractionNeeded.completed
                .Enter(OnResearchCompleted);

            operational.active
                .Toggle("EnergyConsumption", smi => smi.operational.SetActive(true), smi => smi.operational.SetActive(false))
                .Toggle("ActiveAnimations", PlayActiveAnimation, StopPlayingActiveAnimation)
                .Toggle("GlobalEffect", ApplyGlobalEffect, RemoveGlobalEffect)
                .Toggle("ContainmentControl", ApplyContainmentControl, RemoveContainmentControl)
                .Update(new Action<Instance, float>(ActivationTimerUpdate),UpdateRate.SIM_1000ms)
                .ParamTransition(activationTimer, operational.researcherInteractionNeeded, IsLTEZero)
                .Exit(smi => smi.sm.hasBeenWorkedByResearcher.Set(false, smi));
        }

        private static void RefreshStorageRequirements(Instance smi)
        {
            smi.storage.capacityKg = smi.def.materialQuantity;
            smi.storage.storageFilters = new List<Tag>()
            {
                smi.def.requiredMaterial
            };
            smi.manualDelivery.AbortDelivery("Setting up delivery request");
            smi.manualDelivery.capacity = smi.def.materialQuantity;
            smi.manualDelivery.refillMass = smi.def.materialQuantity;
            smi.manualDelivery.MinimumMass = smi.def.materialQuantity;
            smi.manualDelivery.RequestedItemTag = smi.def.requiredMaterial;
        }

        private static void RefreshAnimation(Instance smi)
        {
            smi.RefreshAnimation();
        }

        private static void PlayActiveAnimation(Instance smi)
        {
            smi.animController.Play((HashedString)ActiveAnimName, KAnim.PlayMode.Loop);
        }

        private static void StopPlayingActiveAnimation(Instance smi)
        {
            smi.animController.Play((HashedString)OnAnimName);
        }

        private static void OnResearchCompleted(Instance smi)
        {
            // 消耗材料
            smi.storage.ConsumeAllIgnoringDisease();
            
            // 设置激活状态
            smi.sm.hasBeenWorkedByResearcher.Set(true, smi);
            smi.sm.activationTimer.Set(MutanterEffects.MUTANTER_CTROL_SPEED_BOOST_DURATION, smi); 
        }

        private static void ActivationTimerUpdate(Instance smi, float dt)
        {
            float num = smi.sm.activationTimer.Get(smi) - dt;
            smi.sm.activationTimer.Set(num, smi);
        }

        private Chore CreateResearchChore(Instance smi)
        {
            return new WorkChore<ControlDepartmentConsoleWorkable>(Db.Get().ChoreTypes.Research, smi.workable);
        }

        private static void ApplyGlobalEffect(Instance smi)
        {
            foreach (MinionIdentity dupe in smi.GetSelectedDupes())
            {
                if (dupe != null && dupe.gameObject != null)
                {
                    Effects effects = dupe.GetComponent<Effects>();
                    if (effects != null && !effects.HasEffect(MutanterEffects.MUTANTER_CONTROL_SPEED_EFFECT))
                    {
                        effects.Add(MutanterEffects.MUTANTER_CONTROL_SPEED_EFFECT, true);
                    }
                }
            }
        }

        private static void RemoveGlobalEffect(Instance smi)
        {
            foreach (MinionIdentity dupe in smi.GetSelectedDupes())
            {
                if (dupe != null && dupe.gameObject != null)
                {
                    Effects effects = dupe.GetComponent<Effects>();
                    if (effects != null && effects.HasEffect(MutanterEffects.MUTANTER_CONTROL_SPEED_EFFECT))
                    {
                        effects.Remove(MutanterEffects.MUTANTER_CONTROL_SPEED_EFFECT);
                    }
                }
            }
        }

        private static void ApplyContainmentControl(Instance smi)
        {
            var stations = ContainmentMonitorStationManager.GetAllStations();

            // 实现对收容室的控制逻辑
            foreach (KPrefabID station in stations)
            {
                if (station != null)
                {
                    ContainmentMonitor.Instance monitor = station.gameObject.GetSMI<ContainmentMonitor.Instance>();
                    monitor?.EnableControlStationEffect();
                }
            }
        }

        private static void RemoveContainmentControl(Instance smi)
        {
            foreach (KPrefabID station in ContainmentMonitorStationManager.GetAllStations())
            {
                if (station != null)
                {
                    ContainmentMonitor.Instance monitor = station.gameObject.GetSMI<ContainmentMonitor.Instance>();
                    monitor?.DisableControlStationEffect();
                }
            }
        }

        public static bool WorkRequirementsMet(Instance smi)
        {
            return IsInLabRoom(smi) && (double) smi.storage.MassStored() >= (double) smi.def.materialQuantity && smi.GetSelectedDupeCount() > 0;
        }

        public static bool IsInLabRoom(Instance smi) => smi.roomTracker.IsInCorrectRoom();

        public class Def : BaseDef
        {
            public string OUTPUT_LOGIC_PORT_ID;
            public Tag requiredMaterial;
            public float materialQuantity;
        }

        public class NonOperationalState : State
        {
            public State off;
        }

        public class OperationalState : State
        {
            public State idle;
            public ResearchState researcherInteractionNeeded;
            public State active;
        }

        public class ResearchState : State
        {
            public State blocked;
            public ResearchProgress available;
            public State completed;
        }

        public class ResearchProgress : State
        {
            public State waitingForDupe;
            public State inProgress;
        }

        public new class Instance : GameInstance
        {
            [MyCmpReq]
            public Operational operational;
            [MyCmpReq]
            public Storage storage;
            [MyCmpReq]
            public ManualDeliveryKG manualDelivery;
            [MyCmpReq]
            public ControlDepartmentConsoleWorkable workable;
            [MyCmpReq]
            public LogicPorts logicPorts;
            [MyCmpReq]
            public RoomTracker roomTracker;
            [MyCmpReq]
            public KBatchedAnimController animController;

            private List<MinionIdentity> selectedDupes = new();
            private const int MAX_SELECTED_DUPES = 5;

            public Instance(IStateMachineTarget master, Def def)
                : base(master, def)
            {
            }

            public override void StartSM()
            {
                base.StartSM();
                this.RefreshLogicOutput();
            }

            public void RefreshAnimation()
            {
                this.animController.Play((HashedString)OnAnimName);
            }

            public void OnResearchCompleted()
            {
                this.sm.hasBeenWorkedByResearcher.Set(true, this);
                this.sm.activationTimer.Set(3600f, this);
            }

            public void RefreshLogicOutput()
            {
                bool isActive = this.GetCurrentState() == this.smi.sm.operational.active;
                this.logicPorts.SendSignal((HashedString)this.def.OUTPUT_LOGIC_PORT_ID, isActive ? 1 : 0);
            }

            public void SelectDupe(MinionIdentity dupe)
            {
                if (!selectedDupes.Contains(dupe) && selectedDupes.Count < MAX_SELECTED_DUPES)
                {
                    selectedDupes.Add(dupe);
                    // 如果控制台处于激活状态，立即应用效果
                    if (GetCurrentState() == smi.sm.operational.active)
                    {
                        ApplySpeedEffectToDupe(dupe);
                    }
                    // 小人列表更新，检查工作要求
                    CheckWorkRequirements();
                }
            }

            public void UnselectDupe(MinionIdentity dupe)
            {
                if (selectedDupes.Contains(dupe))
                {
                    selectedDupes.Remove(dupe);
                    // 移除效果
                    RemoveSpeedEffectFromDupe(dupe);
                    // 小人列表更新，检查工作要求
                    CheckWorkRequirements();
                }
            }

            private void CheckWorkRequirements()
            {
                // 触发 OnStorageChange 事件，这样会检查工作要求并更新状态
                this.gameObject.Trigger((int)GameHashes.OnStorageChange);
            }

            public bool IsDupeSelected(MinionIdentity dupe)
            {
                return selectedDupes.Contains(dupe);
            }

            public int GetSelectedDupeCount()
            {
                return selectedDupes.Count;
            }

            public List<MinionIdentity> GetSelectedDupes()
            {
                return selectedDupes;
            }

            private void ApplySpeedEffectToDupe(MinionIdentity dupe)
            {
                if (dupe != null && dupe.gameObject != null)
                {
                    Effects effects = dupe.GetComponent<Effects>();
                    if (effects != null && !effects.HasEffect(MutanterEffect.MutanterEffects.MUTANTER_CONTROL_SPEED_EFFECT))
                    {
                        effects.Add(MutanterEffect.MutanterEffects.MUTANTER_CONTROL_SPEED_EFFECT, true);
                    }
                }
            }

            private void RemoveSpeedEffectFromDupe(MinionIdentity dupe)
            {
                if (dupe != null && dupe.gameObject != null)
                {
                    Effects effects = dupe.GetComponent<Effects>();
                    if (effects != null && effects.HasEffect(MutanterEffect.MutanterEffects.MUTANTER_CONTROL_SPEED_EFFECT))
                    {
                        effects.Remove(MutanterEffect.MutanterEffects.MUTANTER_CONTROL_SPEED_EFFECT);
                    }
                }
            }

            protected override void OnCleanUp()
            {
                RemoveGlobalEffect(this);
                RemoveContainmentControl(this);
                selectedDupes.Clear();
            }
        }
    }
}