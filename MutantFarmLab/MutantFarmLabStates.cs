using PeterHan.PLib.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MutantFarmLab
{
    public class MutantFarmLabStates : GameStateMachine<MutantFarmLabStates, MutantFarmLabStates.StatesInstance, IStateMachineTarget, MutantFarmLabStates.Def>
    {
        #region ===== 配置项=====
        public float ParticleConsumeAmount = 10f;
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
                // ✅ 新增：核心巡检，自动激活/禁用Workable，保证条件就绪即开工
                .Update("AutoCheckWorkableStatus", (smi, dt) => smi.AutoCheckWorkableStatus(), UpdateRate.SIM_1000ms)
                .Enter(OnInitRoot)
                .Exit(smi => { if (smi.controller != null) smi.controller.ResetMutationTimer(); });
            idle
                .Enter(smi => smi.ClearMutantSeedOutput())
                .EventTransition(GameHashes.OnStorageChange, noParticles, smi => smi.HasValidSeed && !smi.HasEnoughParticles && smi.IsMachineOperational)
                .EventTransition(GameHashes.OnStorageChange, ready, smi => smi.HasValidSeed && smi.HasEnoughParticles && smi.IsMachineOperational)
                .ToggleStatusItem(Db.Get().BuildingStatusItems.FabricatorIdle, smi => true);

            // 2. 缺粒子状态
            noParticles
                .Enter(smi => PUtil.LogDebug("[状态流转] 进入缺粒子状态，等待高能粒子补给"))
                .EventTransition(GameHashes.OnStorageChange, ready, smi => smi.HasEnoughParticles && smi.IsMachineOperational)
                .EventTransition(GameHashes.OnStorageChange, idle, smi => !smi.HasValidSeed || !smi.IsMachineOperational)
                .ToggleStatusItem(Db.Get().BuildingStatusItems.WaitingForMaterials, smi => true);

            ready
                .Enter(OnReadyStateEnter)
                .Exit(smi => smi.CancelMutationWorkChore()) // 状态退出时取消未完成的Chore
                .ToggleStatusItem(Db.Get().BuildingStatusItems.ComplexFabricatorResearching, smi => true);
        }

        #region ===== 全局通用方法 =====
        private void OnInitRoot(StatesInstance smi)
        {
            if (smi.master.gameObject.GetComponent<HighEnergyParticleStorage>() == null)
                smi.master.gameObject.AddComponent<HighEnergyParticleStorage>();

            if (smi.master.gameObject.GetComponent<MutantFarmLabController>() == null)
                smi.controller = smi.master.gameObject.AddComponent<MutantFarmLabController>();
            else
                smi.controller = smi.master.gameObject.GetComponent<MutantFarmLabController>();

            smi.controller.ResetMutationTimer();
            PUtil.LogDebug("[状态机初始化] 根状态初始化完成，组件加载完毕");
        }
        private void OnReadyStateEnter(StatesInstance smi)
        {
            PUtil.LogDebug("[状态流转] 进入就绪状态 → 开始校验条件+指派小人任务");
            // ✅ 前置双重校验：粒子+种子+设备全部就绪，才创建任务
            if (smi.IsMachineOperational && smi.HasValidSeed && smi.HasEnoughParticles)
            {
                var workable = smi.gameObject.GetComponent<MutantFarmLabWorkable>();
                if (workable != null)
                {
                    workable.enabled = true; // ✅ 强制激活Workable，确保任务可绑定
                    smi.CreateMutationWorkChore(); // 创建任务
                    PUtil.LogDebug("✅ 条件全满足 → Workable激活+Chore任务创建成功！");
                }
                else
                    PUtil.LogError("❌ 创建任务失败 → 未找到Workable组件");
            }
            else
                PUtil.LogWarning("⚠️ 未创建任务 → 粒子/种子/设备未就绪");
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
            [MyCmpReq] public Storage SeedStorage;
            [MyCmpReq] public Operational MachineOperational;
            [MyCmpReq] public FlatTagFilterable SeedFilter;
            [MyCmpReq] public TreeFilterable TreeSeedFilter;

            public MutantFarmLabController controller;
            private HighEnergyParticleStorage _particleStorage;
            private readonly Tag[] _forbiddenTags = { GameTags.MutatedSeed };
            public List<Tag> ValidSeedTags = new List<Tag>();

            private Chore _mutationWorkChore; // 缓存Chore实例，避免重复创建


            public StatesInstance(IStateMachineTarget master, Def def) : base(master, def)
            {
                InitSeedDeliverySystem();
                InitSeedFilterSystem();
                PUtil.LogDebug("[实例初始化] 状态机实例创建完成");
            }

            #region ===== 初始化子系统 =====
            private void InitSeedDeliverySystem()
            {
                PlantSeedManager.InitPlantSeedMapping();
                foreach (var seedInfo in PlantSeedManager.GetAllPlantSeedInfos())
                {
                    if (!seedInfo.SeedID.IsValid || ValidSeedTags.Contains(seedInfo.SeedID)) continue;
                    ValidSeedTags.Add(seedInfo.SeedID);
                    var mdkg = MutantFarmLabConfig.AddSeedMDKG(master.gameObject, seedInfo.SeedID, MutantFarmLabConfig.Deliverycapacity);
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
                    SeedFilter.selectedTags.Clear(); // 仅新建筑初始化时清空，读档不执行
                    PUtil.LogDebug($"✅ 新建筑筛选框初始化 → 所有种子全未勾选");
                }
                SeedFilter.currentlyUserAssignable = true;
                TreeSeedFilter.OnFilterChanged += _ => SyncMDKGWithFilter();
            }
            #endregion

            #region ===== 核心业务方法 =====
            private void SyncMDKGWithFilter()
            {
                foreach (var mdkg in master.gameObject.GetComponents<ManualDeliveryKG>())
                {
                    bool isTagSelected = SeedFilter.selectedTags.Contains(mdkg.RequestedItemTag);
                    // 核心逻辑：选中=取消暂停（Pause false），未选中=暂停（Pause true）
                    mdkg.Pause(!isTagSelected, isTagSelected ? "筛选勾选，取消暂停配送" : "筛选未勾选，暂停配送");
                    // MDKG启用状态跟随暂停状态，保持逻辑统一
                    mdkg.enabled = isTagSelected;
                }
            }

            public void StartSeedMutation()
            {
                PUtil.LogDebug($"===HasEnoughParticles:{HasEnoughParticles} HasValidSeed:{HasValidSeed} IsMachineOperational:{IsMachineOperational}======");
                if (!HasEnoughParticles || !HasValidSeed || !IsMachineOperational) return;

                ParticleStorage.ConsumeAndGet(MutantFarmLabConfig.ParticleConsumeAmount);
                controller.ResetMutationTimer();
                PUtil.LogDebug($"[变异开始] 消耗{MutantFarmLabConfig.ParticleConsumeAmount}高能粒子，计时启动");
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
                if (mutantSeeds.Count > 0)
                    PUtil.LogDebug($"[清理完成] 移除{mutantSeeds.Count}个残留变异种子");
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
                    return SeedStorage.items.Any(IsSeedValidForMutation);
                }
            }

            public bool HasEnoughParticles
            {
                get
                {
                    if (!IsMachineOperational) return false;
                    return ParticleStorage != null && ParticleStorage.GetAmountAvailable(GameTags.HighEnergyParticle) >= MutantFarmLabConfig.ParticleConsumeAmount;
                }
            }

            #endregion

            #region ===== 辅助工具方法 =====
            private HighEnergyParticleStorage ParticleStorage
            {
                get => _particleStorage ??= master.gameObject.GetComponent<HighEnergyParticleStorage>();
            }

            private bool IsSeedValidForMutation(GameObject seedObj)
            {
                if (seedObj == null) return false;
                var seedComp = seedObj.GetComponent<PlantableSeed>();
                bool isSeed = seedObj.HasTag(GameTags.Seed);
                bool isNotMutated = !seedObj.HasTag(GameTags.MutatedSeed);
                return isSeed && isNotMutated;
            }
            #endregion


            // ✅ 核心：创建自动工作的Chore，关联你的Workable
            public void CreateMutationWorkChore()
            {
                if (!IsMachineOperational || !HasValidSeed || !HasEnoughParticles || _mutationWorkChore != null)
                {
                    PUtil.LogDebug("❌ 任务创建拦截 → 条件不满足/任务已存在");
                    return;
                }
                var workable = gameObject.GetComponent<MutantFarmLabWorkable>();
                if (workable == null) return;

                // 缺氧原生Chore创建API，自动指派小人执行Workable
                _mutationWorkChore = new WorkChore<MutantFarmLabWorkable>(
                    Db.Get().ChoreTypes.Research, // 工作类型（科研，可自定义）
                    workable, // 关联你的Workable
                    null,
                    true,
                    null,
                    null,
                    null,
                    true
                );
                if (_mutationWorkChore != null)
                {
                    PUtil.LogDebug("✅ 小人任务创建成功！已加入系统任务队列，等待小人指派");
                }
                else
                {
                    PUtil.LogError("❌ 任务创建失败 → WorkChore实例创建返回Null");
                }
            }

            // 取消Chore，避免状态退出后小人继续工作
            public void CancelMutationWorkChore()
            {
                if (_mutationWorkChore != null)
                {
                    _mutationWorkChore.Cancel("状态退出，取消工作");
                    _mutationWorkChore = null;
                }
            }

            public void AutoCheckWorkableStatus()
            {
                var workable = master.gameObject.GetComponent<MutantFarmLabWorkable>();
                if (workable == null) return;
                // 条件就绪 → 激活Workable；否则禁用
                bool isAllReady = IsMachineOperational && HasValidSeed && HasEnoughParticles;
                workable.enabled = isAllReady;
                //PUtil.LogDebug($"🔍 自动巡检 → 条件就绪[{isAllReady}] → Workable激活[{workable.enabled}]");
            }

        }
        #endregion

        #region ===== 状态机配置类 =====
        public class Def : BaseDef
        {
        }
        #endregion

        #region ===== 计时器控制器 =====
        public class MutantFarmLabController : KMonoBehaviour
        {
            public float currentMutationTime = 0f;
            public void ResetMutationTimer() => currentMutationTime = 0f;
        }
        #endregion
    }
}