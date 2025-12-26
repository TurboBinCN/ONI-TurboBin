using Database;
using Klei.AI;
using KSerialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MutantFarmLab
{
    // 变异农场实验室状态机（完全适配缺氧源码）
    public class MutantFarmLabStates : GameStateMachine<MutantFarmLabStates, MutantFarmLabStates.StatesInstance, IStateMachineTarget, MutantFarmLabStates.Def>
    {
        #region 配置项（直接存储，无需存档重写）
        public float particleConsumeAmount = 10f; // 每次消耗高能粒子数量
        public float mutationTime = 3f; // 变异所需时间（秒）
        #endregion

        #region 状态定义
        public State idle; // 空闲（无有效种子）
        public State waitingForParticles; // 等待粒子（有种子但粒子不足）
        public State mutating; // 变异中（消耗粒子+生成变异种子）
        public State outputting; // 输出就绪（变异种子已生成）
        #endregion

        #region 核心数据（从官方源码获取）
        private readonly List<string> _officialMutationIDs = new List<string>
        {
            "moderatelyLoose", "moderatelyTight", "extremelyTight",
            "bonusLice", "sunnySpeed", "slowBurn",
            "blooms", "loadedWithFruit", "rottenHeaps", "heavyFruit"
        };
        private PlantMutations _plantMutations;
        #endregion

        public override void InitializeStates(out BaseState default_state)
        {
            default_state = idle;
            _plantMutations = Db.Get().PlantMutations; // 初始化官方变异配置

            // 根状态：全局配置
            root
                .Update("MutationTimerUpdate", UpdateMutationTimer)
                .Enter(OnInitRoot);

            // 1. 空闲状态
            idle
                .EventTransition(GameHashes.OnStorageChange, waitingForParticles, smi => HasValidSeed(smi))
                .Enter(smi => ClearOutput(smi))
                // 修正：BuildingStatusItems正确路径
                .ToggleStatusItem(Db.Get().BuildingStatusItems.FabricatorIdle, smi => smi.master);

            // 2. 等待粒子状态
            waitingForParticles
                .EventTransition(GameHashes.OnStorageChange, mutating, smi => HasEnoughParticles(smi))
                .EventTransition(GameHashes.OnStorageChange, idle, smi => !HasValidSeed(smi))
                .ToggleStatusItem(Db.Get().BuildingStatusItems.WaitingForMaterials, smi => smi.master);

            // 3. 变异中状态
            mutating
                .Enter(smi => StartMutation(smi))
                .Transition(outputting, smi => IsMutationComplete(smi))
                .ToggleStatusItem(Db.Get().BuildingStatusItems.ComplexFabricatorProducing, smi => smi.master);

            // 4. 输出状态
            outputting
                .Enter(smi => SpawnMutantSeed(smi))
                .Transition(idle, smi => IsOutputCollected(smi))
                .ToggleStatusItem(Db.Get().BuildingStatusItems.FabricatorEmpty, smi => smi.master)
                .ToggleChore(this.CreateChore, MutantFarmLabStates.SetRemoteChore, this.idle);
        }
        //创建种子运送任务
        private Chore CreateChore(MutantFarmLabStates.StatesInstance smi)
        {
            return new WorkChore<MutantFarmLabWorkable>(Db.Get().ChoreTypes.AnalyzeSeed, smi.workable, null, true, null, null, null, true, null, false, true, null, false, true, true, PriorityScreen.PriorityClass.basic, 5, false, true);
        }

        private static void SetRemoteChore(MutantFarmLabStates.StatesInstance smi, Chore chore)
        {
            smi.remoteChore.SetChore(chore);
        }
        #region 根状态初始化
        private void OnInitRoot(StatesInstance smi)
        {
            // 初始化高能粒子存储组件
            smi.master.gameObject.AddOrGet<HighEnergyParticleStorage>();
            // 初始化Storage过滤（仅允许种子）
            smi.storage.storageFilters = new List<Tag>() { GameTags.Seed };
            // 重置计时器
            var controller = smi.master.GetComponent<MutantFarmLabController>();
            if (controller == null)
            {
                controller = smi.master.gameObject.AddOrGet<MutantFarmLabController>();
            }
            controller.ResetMutationTimer();
        }
        #endregion

        #region 核心功能方法（无语法错误版）
        private bool HasValidSeed(StatesInstance smi)
        {
            if (smi.storage == null) return false;

            foreach (var item in smi.storage.items)
            {
                bool isSeed = item.HasTag(GameTags.Seed);
                bool isNotTree = item.GetComponent<PlantableSeed>()?.PlantID != "ForestTreeSeed";
                bool isNotMutated = item.GetComponent<MutantPlant>() == null;

                if (isSeed && isNotTree && isNotMutated)
                {
                    return true;
                }
            }
            return false;
        }

        private bool HasEnoughParticles(StatesInstance smi)
        {
            var particleStorage = smi.master.gameObject.GetComponent<HighEnergyParticleStorage>();
            return particleStorage != null && particleStorage.GetAmountAvailable(GameTags.HighEnergyParticle) >= particleConsumeAmount;
        }

        private void StartMutation(StatesInstance smi)
        {
            if (!HasEnoughParticles(smi)) return;

            // 消耗高能粒子（源码官方用法）
            var particleStorage = smi.master.gameObject.GetComponent<HighEnergyParticleStorage>();
            particleStorage.ConsumeAndGet(particleConsumeAmount);

            // 重置计时器
            var controller = smi.master.GetComponent<MutantFarmLabController>();
            if (controller != null)
            {
                controller.ResetMutationTimer();
            }

            Debug.Log($"MutantFarmLab: 消耗 {particleConsumeAmount} 单位高能粒子，开始变异");
        }

        private void UpdateMutationTimer(StatesInstance smi, float dt)
        {
            if (smi.GetCurrentState() == mutating)
            {
                var controller = smi.master.GetComponent<MutantFarmLabController>();
                if (controller != null)
                {
                    controller.currentMutationTime += dt;
                }
            }
        }

        private bool IsMutationComplete(StatesInstance smi)
        {
            var controller = smi.master.GetComponent<MutantFarmLabController>();
            return controller != null && controller.currentMutationTime >= mutationTime;
        }

        /// <summary>
        /// 生成变异种子（核心方法，全适配源码）
        /// </summary>
        private void SpawnMutantSeed(StatesInstance smi)
        {
            if (smi.storage == null || !HasValidSeed(smi)) return;

            // 1. 获取有效种子
            GameObject rawSeed = null;
            PlantableSeed rawSeedComp = null;
            foreach (var item in smi.storage.items)
            {
                if (item.HasTag(GameTags.Seed) && item.GetComponent<PlantableSeed>()?.PlantID != "ForestTreeSeed")
                {
                    rawSeed = item;
                    rawSeedComp = rawSeed.GetComponent<PlantableSeed>();
                    break;
                }
            }
            if (rawSeed == null || rawSeedComp == null)
            {
                Debug.LogError("MutantFarmLab: 未找到有效普通种子");
                return;
            }

            // 2. 移除原种子
            smi.storage.ConsumeIgnoringDisease(rawSeed.tag, 1);

            // 3. 随机变异ID
            string randomMutationID = _officialMutationIDs[UnityEngine.Random.Range(0, _officialMutationIDs.Count)];
            PlantMutation validMutation = _plantMutations.Get(randomMutationID);
            if (validMutation == null)
            {
                randomMutationID = "moderatelyLoose";
                validMutation = _plantMutations.Get(randomMutationID);
            }

            // 4. 实例化变异种子（修正：改用Util.KInstantiate，源码通用方法）
            string mutantSeedName = $"MutantSeed_{rawSeedComp.PlantID}_{randomMutationID}";
            GameObject mutantSeed = Util.KInstantiate(rawSeed, smi.master.gameObject.transform.position, Quaternion.identity);
            mutantSeed.name = mutantSeedName;

            // 5. 配置MutantPlant组件（修正：TagManager.Create → new Tag）
            MutantPlant mutantPlantComp = mutantSeed.AddOrGet<MutantPlant>();
            if (mutantPlantComp != null)
            {
                mutantPlantComp.SpeciesID = new Tag(rawSeedComp.PlantID); // 核心修正
                mutantPlantComp.SetSubSpecies(new List<string> { randomMutationID });
                mutantPlantComp.Analyze();
                mutantSeed.AddTag(GameTags.MutatedSeed);
                // 注册亚种（源码逻辑）
                if (PlantSubSpeciesCatalog.Instance != null)
                {
                    PlantSubSpeciesCatalog.Instance.DiscoverSubSpecies(mutantPlantComp.GetSubSpeciesInfo(), mutantPlantComp);
                }
            }

            // 6. 存入Storage
            smi.storage.Store(mutantSeed);

            Debug.Log($"MutantFarmLab: 变异种子生成成功 - 物种={rawSeedComp.PlantID}，变异={randomMutationID}");
        }

        private bool IsOutputCollected(StatesInstance smi)
        {
            if (smi.storage == null) return true;

            foreach (var item in smi.storage.items)
            {
                if (item.HasTag(GameTags.MutatedSeed) || item.GetComponent<MutantPlant>() != null)
                {
                    return false;
                }
            }
            return true;
        }

        private void ClearOutput(StatesInstance smi)
        {
            if (smi.storage == null) return;

            List<GameObject> toRemove = new List<GameObject>();
            foreach (var item in smi.storage.items)
            {
                if (item.HasTag(GameTags.MutatedSeed) || item.GetComponent<MutantPlant>() != null)
                {
                    toRemove.Add(item);
                }
            }

            foreach (var item in toRemove)
            {
                smi.storage.ConsumeIgnoringDisease(item.tag, 1);
                UnityEngine.Object.Destroy(item);
            }
        }
        #endregion

        #region 状态机实例类
        public class StatesInstance : GameInstance
        {
            [MyCmpReq]
            public Storage storage;

            [MyCmpAdd]
            public ManuallySetRemoteWorkTargetComponent remoteChore;

            [Serialize]
            private HashSet<Tag> forbiddenSeeds;

            [MyCmpReq]
            public ManualDeliveryKG manualDelivery;

            [MyCmpReq]
            public MutantFarmLabWorkable workable;

            public StatesInstance(IStateMachineTarget master, Def def) : base(master, def)
            {
                // 修正：Storage过滤改用allowItemFn（无SetFilter方法）
                //storage.allowItemFn = (item) => item.HasTag(GameTags.Seed);
                storage.storageFilters = new List<Tag>() { GameTags.Seed };
            }
            public bool GetSeedForbidden(Tag seedID)
            {
                if (this.forbiddenSeeds == null)
                {
                    this.forbiddenSeeds = new HashSet<Tag>();
                }
                return this.forbiddenSeeds.Contains(seedID);
            }
            public void SetSeedForbidden(Tag seedID, bool forbidden)
            {
                if (this.forbiddenSeeds == null)
                {
                    this.forbiddenSeeds = new HashSet<Tag>();
                }
                bool flag;
                if (forbidden)
                {
                    flag = this.forbiddenSeeds.Add(seedID);
                }
                else
                {
                    flag = this.forbiddenSeeds.Remove(seedID);
                }
                if (flag)
                {
                    this.RefreshFetchTags();
                }
            }
            private void RefreshFetchTags()
            {
                if (this.forbiddenSeeds == null)
                {
                    this.manualDelivery.ForbiddenTags = null;
                    return;
                }
                Tag[] array = new Tag[this.forbiddenSeeds.Count];
                int num = 0;
                foreach (Tag tag in this.forbiddenSeeds)
                {
                    array[num++] = tag;
                    this.storage.Drop(tag);
                }
                this.manualDelivery.ForbiddenTags = array;
            }
        }
        #endregion

        #region 状态机配置类（修正：删除无效重写）
        public class Def : BaseDef
        {
            // 直接存储配置项，无需OnSerialize/OnDeserialize
            public float savedParticleConsumeAmount = 10f;
            public float savedMutationTime = 3f;
        }
        #endregion

        #region 辅助控制器
        public class MutantFarmLabController : KMonoBehaviour
        {
            public float currentMutationTime = 0f;

            public void ResetMutationTimer()
            {
                currentMutationTime = 0f;
            }
        }
        #endregion

    }
}