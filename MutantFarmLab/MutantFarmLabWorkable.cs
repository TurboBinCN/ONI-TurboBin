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

            var seedComp = rawSeed.GetComponent<PlantableSeed>();
            if (seedComp == null)
                return;

            try
            {
                var dropPos = gameObject.transform.position + new Vector3(0f, 1.5f, 0f);
                var mutantSeed = PlantSeedManager.GenerateMutantSubspeciesSeed(rawSeed, dropPos, SeedStorage, true);
                if (mutantSeed == null)
                    return;

                SeedStorage.Remove(rawSeed);
                UnityEngine.Object.Destroy(rawSeed);
            }
            catch (Exception e)
            {
                SeedStorage.Drop(seedComp.PlantID, new List<GameObject> { rawSeed });
                PUtil.LogError($"[变异失败] 物种：{seedComp.PlantID}，错误：{e.Message}");
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