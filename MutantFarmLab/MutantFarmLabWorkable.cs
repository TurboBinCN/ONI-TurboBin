using MutantFarmLab.tbbLibs;
using PeterHan.PLib.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using TUNING;
using UnityEngine;
using static STRINGS.UI.SCHEDULEGROUPS;

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
        protected override void OnSpawn()
        {
            base.OnSpawn();
            _particleStorage ??= gameObject.GetComponent<HighEnergyParticleStorage>();
        }
        protected override void OnStartWork(WorkerBase worker)
        {
            base.OnStartWork(worker);
            base.GetComponent<KSelectable>().AddStatusItem(Db.Get().BuildingStatusItems.ComplexFabricatorResearching, this.SeedStorage.FindFirst(GameTags.Seed));

            //消耗粒子
            PUtil.LogDebug("[MutantChore] 消耗粒子开始变异");
            if (!CanDoMutation())
            {
                PUtil.LogDebug("[MutantChore] 任务条件不满足,取消任务");
                StopWork(worker, true);
                base.GetComponent<KSelectable>().RemoveStatusItem(Db.Get().BuildingStatusItems.ComplexFabricatorResearching, false);
                return;
            }
            ParticleStorage.ConsumeAndGet(MutantFarmLabConfig.ParticleConsumeAmount);
            PUtil.LogDebug($"[变异开始] 消耗{MutantFarmLabConfig.ParticleConsumeAmount}高能粒子，计时启动");
        }

        protected override void OnStopWork(WorkerBase worker)
        {
            base.OnStopWork(worker);
            base.GetComponent<KSelectable>().RemoveStatusItem(Db.Get().BuildingStatusItems.ComplexFabricatorResearching, false);
        }

        protected override void OnCompleteWork(WorkerBase worker)
        {
            base.OnCompleteWork(worker);

            //完成种子变异
            PUtil.LogDebug("[MutantChore] 开始执行种子变异");
            SpawnFinalMutantSeed();
        }
        private bool CanDoMutation()
        {
            if (!HasEnoughParticles || !HasValidSeed || !IsMachineOperational){ 
                return false; 
            }
            return true;
        }
        public bool HasEnoughParticles
        {
            get
            {
                if (!IsMachineOperational || ParticleStorage != null) return false;
                return ParticleStorage.GetAmountAvailable(GameTags.HighEnergyParticle) >= MutantFarmLabConfig.ParticleConsumeAmount;
            }
        }
        public void SpawnFinalMutantSeed()
        {
            var rawSeed = SeedStorage.items.FirstOrDefault(item => IsSeedValidForMutation(item));
            if (rawSeed == null) return;

            var seedComp = rawSeed.GetComponent<PlantableSeed>();
            if (seedComp == null) return;

            try
            {
                var dropPos = gameObject.transform.position + new Vector3(-3f, 1.5f, 0f);
                //var dropPos = Grid.CellToPosCBC(Grid.PosToCell(master.transform.gameObject) + new CellOffset(-2, 1), Grid.SceneLayer.Front);
                var mutantSeed = PlantSeedManager.GenerateMutantSubspeciesSeed(rawSeed, dropPos, SeedStorage, true);
                if (mutantSeed == null) //生成失败回滚
                {
                    PUtil.LogDebug($"[变异失败] 物种：{seedComp.PlantID} → 变异种子生成返回Null");
                    return;
                }
                SeedStorage.Remove(rawSeed, false);
                UnityEngine.Object.Destroy(rawSeed);
                PUtil.LogDebug($"[变异成功] 物种：{seedComp.PlantID} → 变异种子：{mutantSeed?.name}");
            }
            catch (Exception e)
            {
                SeedStorage.Drop(seedComp.PlantID, new List<GameObject> { rawSeed });
                PUtil.LogError($"[变异失败] 物种：{seedComp.PlantID}，错误：{e.Message}");
            }
        }
        private bool IsSeedValidForMutation(GameObject seedObj)
        {
            if (seedObj == null) return false;
            var seedComp = seedObj.GetComponent<PlantableSeed>();
            bool isSeed = seedObj.HasTag(GameTags.Seed);
            bool isNotMutated = !seedObj.HasTag(GameTags.MutatedSeed);
            return isSeed && isNotMutated;
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
                return SeedStorage.items.Any(IsSeedValidForMutation);
            }
        }
        public bool IsMachineOperational
        {
            get => MachineOperational != null && MachineOperational.IsOperational;
        }

        private HighEnergyParticleStorage _particleStorage;

        private HighEnergyParticleStorage ParticleStorage
        {
            get => _particleStorage ??= gameObject.GetComponent<HighEnergyParticleStorage>();
        }
    }

}
