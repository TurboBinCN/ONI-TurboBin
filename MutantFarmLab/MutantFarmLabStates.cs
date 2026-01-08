using KSerialization;
using MutantFarmLab.tbbLibs;
using PeterHan.PLib.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static STRINGS.UI;

namespace MutantFarmLab
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public class MutantFarmLabStates : GameStateMachine<MutantFarmLabStates, MutantFarmLabStates.StatesInstance, IStateMachineTarget, MutantFarmLabStates.Def>
    {
        #region ===== 配置项=====
        public static float _particleConsumeAmount = 100f;
        public float MutationDuration = 3f;
        #endregion

        #region ===== 状态定义 =====
        public State idle;
        public State noParticles;
        public State ready;
        public State outputReady;
        #endregion

        public override void InitializeStates(out BaseState default_state)
        {
            default_state = idle;

            root
                .Update("MutationTimerTick", UpdateMutationTimer, UpdateRate.SIM_1000ms)
                .Update("UnifiedStateCheck", (smi, dt) =>smi.UnifiedStateManager(true), UpdateRate.SIM_1000ms)
                .EventTransition(GameHashes.OperationalChanged, idle, smi => !smi.IsMachineOperational)
                .EventTransition(GameHashes.OperationalChanged, ready, smi => smi.IsMachineOperational && smi.HasValidSeed && smi.HasEnoughParticles)
                .Enter(OnInitRoot)
                .Exit(smi => { if (smi.controller != null) smi.controller.ResetMutationTimer(); });

            idle
                .Enter(smi => smi.ClearMutantSeedOutput())
                .EventTransition(GameHashes.OnStorageChange, noParticles, smi => smi.HasValidSeed && !smi.HasEnoughParticles && smi.IsMachineOperational)
                .EventTransition(GameHashes.OnStorageChange, ready, smi => smi.HasValidSeed && smi.HasEnoughParticles && smi.IsMachineOperational);

            noParticles
                .EventTransition(GameHashes.OnStorageChange, ready, smi => smi.HasEnoughParticles && smi.IsMachineOperational && smi.HasValidSeed)
                .EventTransition(GameHashes.OnStorageChange, idle, smi => !smi.HasValidSeed || !smi.IsMachineOperational);

            ready
                .Enter(OnReadyStateEnter)
                .Exit(smi => smi.CancelMutationWorkChore())
                .Update("CheckChoreStatus", (smi, dt) => {
                    if (smi.IsMachineOperational && smi.HasValidSeed && smi.HasEnoughParticles)
                    {
                        if (smi._mutationWorkChore == null || smi._mutationWorkChore.isComplete || !smi._mutationWorkChore.IsValid())
                        {
                            smi._mutationWorkChore = null;
                            smi.CreateMutationWorkChore();
                        }
                    }
                }, UpdateRate.SIM_200ms);
        }

        #region ===== 全局通用方法 =====
        private void OnInitRoot(StatesInstance smi)
        {
            smi.controller = smi.master.gameObject.GetComponent<MutantFarmLabController>();

            smi.controller.ResetMutationTimer();
        }
        private void OnReadyStateEnter(StatesInstance smi)
        {
            var workable = smi.gameObject.GetComponent<MutantFarmLabWorkable>();
            if (!smi.IsMachineOperational || !smi.HasValidSeed || !smi.HasEnoughParticles)
            {
                if (workable != null) workable.enabled = false;
                return;
            }
            if (workable != null)
            {
                workable.enabled = true;
                smi.CreateMutationWorkChore();
                smi._mutationWorkChore = null;
            }
        }
        private void UpdateMutationTimer(StatesInstance smi, float dt)
        {
            if (smi.GetCurrentState() == ready && smi.controller != null && smi.IsMachineOperational)
                smi.controller.currentMutationTime += dt;
        }
        #endregion

        #region ===== 状态机实例类【核心修复所有报错】=====
        public class StatesInstance : GameInstance
        {
            public readonly Queue<Chore> _mutationTaskQueue = new Queue<Chore>();

            public bool _isTaskExecuting = false;

            [MyCmpReq] public Storage SeedStorage;
            [MyCmpReq] public Operational MachineOperational;
            [MyCmpReq] public FlatTagFilterable SeedFilter;
            [MyCmpReq] public TreeFilterable TreeSeedFilter;

            public MutantFarmLabController controller;
            private HighEnergyParticleStorage _particleStorage;
            private readonly Tag[] _forbiddenTags = { GameTags.MutatedSeed };
            public List<Tag> ValidSeedTags = new List<Tag>();

            public Chore _mutationWorkChore;


            public StatesInstance(IStateMachineTarget master, Def def) : base(master, def)
            {
                InitSeedDeliverySystem();
                InitSeedFilterSystem();
                SeedStorage.Subscribe((int)GameHashes.OnStorageChange, (obj) =>
                {
                    if (SeedStorage.items.Count > 0)
                    {
                        PUtil.LogDebug($"仓储种子数变更：{SeedStorage.items.Count}");
                    }
                });
                controller = gameObject.GetComponent<MutantFarmLabController>();

            }
            protected override void OnCleanUp()
            {
                base.OnCleanUp();
            }
            
            #region ===== 初始化子系统 =====
            private void InitSeedDeliverySystem()
            {
                PlantSeedManager.InitPlantSeedMapping();
                foreach (var seedInfo in PlantSeedManager.GetAllPlantSeedInfos())
                {
                    if (!seedInfo.SeedID.IsValid || ValidSeedTags.Contains(seedInfo.SeedID)) continue;
                    ValidSeedTags.Add(seedInfo.SeedID);
                    var mdkg = MutantFarmLabConfig.AddSeedMDKG(master.gameObject, seedInfo.SeedID);
                    mdkg.ForbiddenTags = _forbiddenTags;
                    mdkg.SetStorage(SeedStorage);
                    mdkg.enabled = false;
                }
                SeedStorage.storageFilters = ValidSeedTags;
            }

            private void InitSeedFilterSystem()
            {
                SeedFilter.tagOptions.Clear();
                SeedFilter.tagOptions.AddRange(ValidSeedTags);
                SeedFilter.selectedTags.AddRange(ValidSeedTags);
                if (Game.Instance == null || !Game.Instance.IsLoading())
                {
                    SeedFilter.selectedTags.Clear();
                }
                SeedFilter.currentlyUserAssignable = true;
                TreeSeedFilter.OnFilterChanged += _ => UnifiedStateManager();
            }
            #endregion

            #region ===== 核心业务方法 =====
            private void SyncMDKGWithFilter(bool OperationalOPT = false)
            {
                foreach (var mdkg in master.gameObject.GetComponents<ManualDeliveryKG>())
                {
                    bool isTagSelected = SeedFilter.selectedTags.Contains(mdkg.RequestedItemTag);
                    bool canDelivery = isTagSelected && this.IsMachineOperational;
                    mdkg.Pause(!canDelivery, !canDelivery ? (isTagSelected ? "设备断电，暂停配送" : "筛选未勾选，暂停配送") : "筛选勾选+通电，启用配送");
                    mdkg.enabled = canDelivery;

                    if (canDelivery && mdkg.enabled)
                    {
                        mdkg.enabled = true;
                        mdkg.Pause(false, "设备通电");
                    }

                    if (!canDelivery && !_isTaskExecuting && !OperationalOPT)
                    {
                        if (mdkg == null || string.IsNullOrEmpty(mdkg.RequestedItemTag.Name) || !mdkg.RequestedItemTag.IsValid)
                        {
                            continue;
                        }
                        if (SeedStorage == null || SeedStorage.items.Count == 0) continue;

                        Tag targetGameTag = mdkg.RequestedItemTag;
                        List<GameObject> needDropItems = new List<GameObject>();
                        foreach (var item in SeedStorage.items)
                        {
                            if (item == null || !item.activeInHierarchy) continue;
                            var kprefab = item.GetComponent<KPrefabID>();
                            if (kprefab == null || !kprefab.PrefabTag.IsValid) continue;
                            if (kprefab.PrefabTag.Name == targetGameTag.Name) needDropItems.Add(item);
                        }

                        foreach (var dropItem in needDropItems)
                        {
                            SeedStorage.Drop(dropItem);
                        }
                    }
                }
                SeedStorage.Trigger((int)GameHashes.OnStorageChange, SeedStorage);
            }

            public void ClearMutantSeedOutput()
            {
                if (SeedStorage == null) return;
                var mutantSeeds = SeedStorage.items.Where(item => item.HasTag(GameTags.MutatedSeed)).ToList();
                foreach (var seed in mutantSeeds)
                {
                    SeedStorage.Remove(seed, false);
                    UnityEngine.Object.Destroy(seed);
                }
            }
            #endregion

            #region ===== 核心判定属性 =====
            public bool IsMachineOperational
            {
                get => MachineOperational != null && MachineOperational.IsOperational;
            }

            public bool HasValidSeed
            {
                get
                {
                    if (!IsMachineOperational || SeedStorage == null) return false;
                    return PlantSeedManager.HasValidMutationSeed(SeedStorage);
                }
            }

            public bool HasEnoughParticles
            {
                get
                {
                    if (!IsMachineOperational) return false;
                    return ParticleStorage != null && ParticleStorage.Particles >= MutantFarmLabConfig.ParticleConsumeAmount;
                }
            }

            #endregion

            #region ===== 辅助工具方法 =====
            private HighEnergyParticleStorage ParticleStorage
            {
                get => _particleStorage ??= master.gameObject.GetComponent<HighEnergyParticleStorage>();
            }
            #endregion

            public void CreateMutationWorkChore()
            {
                if (!IsMachineOperational || !HasValidSeed || !HasEnoughParticles)
                {
                    return;
                }
                if (_mutationWorkChore != null)
                {
                    return;
                }

                var workable = gameObject.GetComponent<MutantFarmLabWorkable>();
                if (workable == null) return;

                _mutationWorkChore = new WorkChore<MutantFarmLabWorkable>(
                    Db.Get().ChoreTypes.Research,
                    workable, null, true, null, null, null, true
                );

                if (_mutationWorkChore != null)
                {
                    _mutationTaskQueue.Enqueue(_mutationWorkChore);
                }
            }

            public void CancelMutationWorkChore()
            {
                if (_mutationWorkChore != null)
                {
                    _mutationWorkChore.Cancel("状态退出，终止当前变异任务");
                    _mutationWorkChore = null;
                }

                if (_mutationTaskQueue.Count > 0)
                {
                    _mutationTaskQueue.Clear();
                }

                _isTaskExecuting = false;
            }

            public void AutoCheckWorkableStatus()
            {
                var workable = master.gameObject.GetComponent<MutantFarmLabWorkable>();
                if (workable == null) return;
                bool isAllReady = IsMachineOperational && HasValidSeed && HasEnoughParticles;
                workable.enabled = isAllReady;
            }

            public void UnifiedStateManager(bool IsMachineOperationalOPT = false)
            {
                if (SeedStorage == null || MachineOperational == null) return;

                SyncMDKGWithFilter(IsMachineOperationalOPT);
                AutoCheckWorkableStatus();
                ProcessMutationTaskQueue();
                SeedStorage.Trigger((int)GameHashes.OnStorageChange, SeedStorage);

                CheckDuplicateTaskInQueue();
            }

            private void CheckDuplicateTaskInQueue()
            {
                var taskHashSet = new HashSet<int>();
                foreach (var task in _mutationTaskQueue)
                {
                    var taskHash = task.GetHashCode();
                    if (taskHashSet.Contains(taskHash))
                    {
                        PUtil.LogWarning($"队列存在重复任务：{taskHash}");
                    }
                    else
                    {
                        taskHashSet.Add(taskHash);
                    }
                }
            }

            public void ProcessMutationTaskQueue()
            {
                if (!IsMachineOperational || !HasValidSeed || !HasEnoughParticles)
                {
                    if (_mutationTaskQueue.Count > 0) _mutationTaskQueue.Clear();
                    _isTaskExecuting = false;
                    return;
                }

                if (_mutationTaskQueue.Count == 0 || _isTaskExecuting) return;

                var executeTask = _mutationTaskQueue.Dequeue();
                if (executeTask == null || !executeTask.IsValid())
                {
                    _isTaskExecuting = false;
                    return;
                }

                _isTaskExecuting = true;
                controller.ResetMutationTimer();
            }

        }

        #endregion

        #region ===== 状态机配置类 =====
        public class Def : BaseDef
        {
        }
        #endregion

        public class MutantFarmLabController : KMonoBehaviour
        {
            public float currentMutationTime = 0f;
            private HighEnergyParticleStorage _particleStorage;
            private MutantFarmLabController _controller;

            private HighEnergyParticleStorage ParticleStorage
            {
                get => _particleStorage ??= gameObject.GetComponent<HighEnergyParticleStorage>();
            }
            public void ResetMutationTimer() => currentMutationTime = 0f;

            
            private bool _logicPort_holder = false;
            private LogicPorts _logicPorts;
            private LogicPorts HEP_RQ_LogicPort
            {
                get => _logicPorts ??= gameObject.GetComponent<LogicPorts>();
            }
            private float ParticleConsumeAmount() => MutantFarmLabConfig.ParticleConsumeAmount;
            private float lowParticleThreshold => MutantFarmLabConfig.lowParticleThreshold;
            public void updateLogicPortLogic()
            {
                float particleAmount = ParticleStorage.Particles;
                int highEnergyParticaleRQSignal = 0;
                float stopThreshold = ParticleStorage.capacity - ParticleConsumeAmount();
                const float floatTolerance = 1e-6f;

                if (particleAmount <= lowParticleThreshold + floatTolerance)
                {
                    highEnergyParticaleRQSignal = 1;
                    _logicPort_holder = true;
                }
                else if (particleAmount >= stopThreshold - floatTolerance)
                {
                    highEnergyParticaleRQSignal = 0;
                    _logicPort_holder = false;
                }
                else
                {
                    highEnergyParticaleRQSignal = _logicPort_holder ? 1 : 0;
                }
                HEP_RQ_LogicPort.SendSignal(MutantFarmLabConfig.HEP_RQ_LOGIC_PORT_ID, highEnergyParticaleRQSignal);
            }
            protected override void OnSpawn()
            {
                base.OnSpawn();
                Subscribe((int)GameHashes.OnParticleStorageChanged, (data) => updateLogicPortLogic());

                updateLogicPortLogic();
            }
            protected override void OnCleanUp()
            {
                base.OnCleanUp();
                Unsubscribe((int)GameHashes.OnParticleStorageChanged);
            }
        }
    }
}