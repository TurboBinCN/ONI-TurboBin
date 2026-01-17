using PeterHan.PLib.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using TUNING;
using UnityEngine;
using static MutantFarmLab.MutantFarmLabStates;

namespace MutantFarmLab
{
    public class MutantFarmLabWorkable : Workable
    {
        public MutantFarmLabWorkable()
        {
            multitoolContext = "MutantFarmLab_Research";
        }
        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            this.requiredSkillPerk = Db.Get().SkillPerks.CanIdentifyMutantSeeds.Id;
            this.shouldShowSkillPerkStatusItem = true;
            this.workerStatusItem = Db.Get().DuplicantStatusItems.AnalyzingGenes;
            this.attributeConverter = Db.Get().AttributeConverters.ResearchSpeed;
            this.attributeExperienceMultiplier = DUPLICANTSTATS.ATTRIBUTE_LEVELING.PART_DAY_EXPERIENCE;
            this.skillExperienceSkillGroup = Db.Get().SkillGroups.Research.Id;
            this.skillExperienceMultiplier = SKILLS.PART_DAY_EXPERIENCE;
            this.overrideAnims = new KAnimFile[]
            {
                Assets.GetAnim("anim_interacts_genetic_analysisstation_kanim")
            };
            base.SetWorkTime(MutantFarmLabConfig.MutationDuration);
            this.showProgressBar = true;
            this.lightEfficiencyBonus = true;



        }
        public override Vector3 GetWorkOffset()
        {
            return new Vector3(2f, 0f, 0f);
        }
        protected override void OnSpawn()
        {
            base.OnSpawn();
            _particleStorage ??= gameObject.GetComponent<HighEnergyParticleStorage>();
            _controller ??= gameObject.GetComponent<MutantFarmLabController>();

        }
        protected override void OnStartWork(WorkerBase worker)
        {
            base.OnStartWork(worker);

            //消耗粒子
            if (!CanDoMutation())
            {
                StopWork(worker, true);
                return;
            }
            var validSeed = SeedStorage.items.FirstOrDefault(PlantSeedManager.IsSeedValidForMutation);
            if (validSeed != null)
                base.GetComponent<KSelectable>().AddStatusItem(Db.Get().BuildingStatusItems.ComplexFabricatorResearching, validSeed);
            ParticleStorage.ConsumeAndGet(MutantFarmLabConfig.ParticleConsumeAmount);
            _controller.updateLogicPortLogic();
        }
        
        protected override void OnStopWork(WorkerBase worker)
        {
            base.OnStopWork(worker);
            base.GetComponent<KSelectable>().RemoveStatusItem(Db.Get().BuildingStatusItems.ComplexFabricatorResearching, false);
        }

        protected override void OnCompleteWork(WorkerBase worker)
        {
            base.OnCompleteWork(worker);

            SpawnFinalMutantSeed();

            var smi = gameObject.GetComponent<MutantFarmLabStates.StatesInstance>();
            if (smi != null)
            {
                if (smi._isTaskExecuting)
                {
                    smi._isTaskExecuting = false;
                    smi.ProcessMutationTaskQueue();
                }
            }
        }

        private bool CanDoMutation()
        {
            if (!HasEnoughParticles || !HasValidSeed || !IsMachineOperational)
            {
                return false;
            }
            return true;
        }

        public void SpawnFinalMutantSeed()
        {
            var rawSeed = SeedStorage.items.FirstOrDefault(PlantSeedManager.IsSeedValidForMutation);
            if (rawSeed == null || !PlantSeedManager.IsSeedValidForMutation(rawSeed))
                return;

            var rawSeedPickupable = rawSeed.GetComponent<Pickupable>();
            if (rawSeedPickupable == null)
            {
                PUtil.LogError("[SpawnFinalMutantSeed] 找到的种子对象缺少 Pickupable 组件！");
                return;
            }
            var seedComp = rawSeed.GetComponent<PlantableSeed>();
            if (seedComp == null)
                return;

            Pickupable singleSeedToUse;
            if (rawSeedPickupable.TotalAmount > 1f)
            {
                // 如果是堆叠，取一个单位
                singleSeedToUse = rawSeedPickupable.TakeUnit(1f);
                PUtil.LogDebug($"[SpawnFinalMutantSeed] 从堆叠中取出一个单位种子进行变异。原堆叠剩余数量: {rawSeedPickupable.TotalAmount}");
            }
            else
            {
                // 如果不是堆叠，直接使用整个对象
                // 注意：这里需要从存储中移除它，因为 TakeUnit 会自动处理存储，但直接使用对象不会
                // 取决于后续逻辑，有时可能需要 Drop
                // 但为了模仿 GeneticAnalysisStation，我们先把它从存储中移除
                SeedStorage.Remove(rawSeed);
                singleSeedToUse = rawSeedPickupable;
                PUtil.LogDebug($"[SpawnFinalMutantSeed] 使用单个种子进行变异。");
            }

            if (singleSeedToUse == null)
            {
                PUtil.LogError("[SpawnFinalMutantSeed] 无法获取单个种子对象（可能 TakeUnit 失败）！");
                return; // 如果 TakeUnit 失败，singleSeedToUse 会是 null
            }

            var singleSeedGameObject = singleSeedToUse.gameObject;
            try
            {
                var dropPos = gameObject.transform.position + new Vector3(0f, 1.5f, 0f);
                var mutantSeed = PlantSeedManager.GenerateMutantSubspeciesSeed(singleSeedGameObject, dropPos, SeedStorage, true);
                if (mutantSeed == null)
                {
                    PUtil.LogError("[SpawnFinalMutantSeed] 生成变异种子失败！");
                    // 如果变异失败，需要处理这个被取出来的单个种子
                    // 一种方式是将其放回存储
                    SeedStorage.Store(singleSeedGameObject);
                    return;
                }
                singleSeedToUse.DeleteObject();
                //UnityEngine.Object.Destroy(singleSeedGameObject);
            }
            catch (Exception e)
            {
                //SeedStorage.Drop(seedComp.PlantID, new List<GameObject> { rawSeed });
                PUtil.LogError($"[变异失败] 物种：{seedComp.PlantID}，错误：{e.Message}");
                // 发生异常时，也应将取出的单个种子放回存储
                if (singleSeedGameObject != null && singleSeedGameObject.activeSelf)
                {
                    SeedStorage.Store(singleSeedGameObject);
                }
            }
        }

        [MyCmpReq]
        public Operational MachineOperational;
        [MyCmpReq]
        public Storage SeedStorage;

        public bool HasValidSeed
        {
            get
            {
                if (!IsMachineOperational || SeedStorage == null) return false;
                return PlantSeedManager.HasValidMutationSeed(SeedStorage);
            }
        }

        public bool IsMachineOperational
        {
            get => MachineOperational != null && MachineOperational.IsOperational;
        }

        private HighEnergyParticleStorage _particleStorage;
        private MutantFarmLabController _controller;

        private HighEnergyParticleStorage ParticleStorage
        {
            get => _particleStorage ??= gameObject.GetComponent<HighEnergyParticleStorage>();
        }

        public bool HasEnoughParticles
        {
            get
            {
                if (!IsMachineOperational || ParticleStorage == null) return false;
                return ParticleStorage.GetAmountAvailable(GameTags.HighEnergyParticle) >= MutantFarmLabConfig.ParticleConsumeAmount;
            }
        }
    }
}