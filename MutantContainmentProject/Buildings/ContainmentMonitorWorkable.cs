using Klei.AI;
using MutantContainmentProject.MutanterComponent;
using MutantContainmentProject.Skills;
using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;
using TUNING;
using UnityEngine;
using static MutantContainmentProject.MutanterComponent.MutanterProductComponent;

namespace MutantContainmentProject.Buildings
{
    public class ContainmentMonitorWorkable : Workable
    {
        private static float maxGainLevel = DUPLICANTSTATS.ATTRIBUTE_LEVELING.MAX_GAINED_ATTRIBUTE_LEVEL;
        //工作常量
        private static readonly float WorkTime = 60f;
        private const float SUBTASK_DURATION = 5f; // 完成一个“子任务”所需的基础时间，单位秒
        private const float TERM_DURATION = 5f; //词条弹出间隔
        //工作状态
        private int workerCompletedSubtasks = 0; // 小人成功完成的子任务数
        private int mutanterCompletedSubtasks = 0; // 畸变体成功完成的子任务数
        private float workerUnitWorkingElapsedTime = 0;
        private float mutanterUnitWorkingElapsedTime = 0;
        private float termUnitShowingElapsedTime = 0;
        //变异体与小人属性
        private float mutanterWorkingSpeedFactor = 1f;
        private float mutanterSuccessRateFactor = 0.3f;
        private float workerWorkingSpeedFactor = 1f;
        private float workerSuccessRateFactor = 1f;

        private ContainmentMonitor.Instance _containmentMonitorSMI;
        private GameObject workerGameObject;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            workLayer = Grid.SceneLayer.BuildingFront;
            SetWorkTime(WorkTime);

            overrideAnims = new KAnimFile[] { Assets.GetAnim("anim_interacts_genetic_analysisstation_kanim") };
            synchronizeAnims = true;

            //attributeConverter = Db.Get().AttributeConverters.Ranching; // 假设使用 ranching 属性
            attributeExperienceMultiplier = DUPLICANTSTATS.ATTRIBUTE_LEVELING.MOST_DAY_EXPERIENCE;
            skillExperienceSkillGroup = MutantContainmentProject.Skills.MutanterSkillGroups.SkillGroupDisciplineID;
            skillExperienceMultiplier = SKILLS.MOST_DAY_EXPERIENCE;

            //shouldShowSkillPerkStatusItem = true;
            //requiredSkillPerk = Db.Get().SkillPerks.CanIdentifyMutantSeeds.Id;

            showProgressBar = true;
            lightEfficiencyBonus = true;

        }
        public override Vector3 GetWorkOffset()
        {
            return new Vector3(2f, 0f, 0f);
        }
        protected override void OnSpawn()
        {
            base.OnSpawn();
            _containmentMonitorSMI = gameObject.GetSMI<ContainmentMonitor.Instance>();
        }

        protected override void OnCleanUp()
        {
            base.OnCleanUp();
        }
        protected override void OnStartWork(WorkerBase worker)
        {
            base.OnStartWork(worker);

            // 重置工作状态
            ResetWorkState();

            workerGameObject = worker.gameObject;
            Modifiers modifiers = workerGameObject.GetComponent<Modifiers>();
            if (modifiers != null)
            {
                var workingSpeedInstance = modifiers.attributes.Get(MutanterAttributes.AttributeWorkingSpeedID);
                var successRateInstance = modifiers.attributes.Get(MutanterAttributes.AttributeSuccessRateID);
                workerWorkingSpeedFactor = workingSpeedInstance.GetTotalValue()/maxGainLevel;
                workerWorkingSpeedFactor = workerWorkingSpeedFactor < 1? workerWorkingSpeedFactor: 0.9f;
                workerSuccessRateFactor = successRateInstance.GetTotalValue()/maxGainLevel;
                workerSuccessRateFactor = workerSuccessRateFactor < 1 ? workerSuccessRateFactor : 0.9f;

            }
        }
        protected override bool OnWorkTick(WorkerBase worker, float dt)
        {
            workerUnitWorkingElapsedTime += dt;
            mutanterUnitWorkingElapsedTime += dt;
            termUnitShowingElapsedTime += dt;

            if (workerUnitWorkingElapsedTime >= SUBTASK_DURATION)
            {
                if (Random.value < workerSuccessRateFactor)
                {
                    workerUnitWorkingElapsedTime = 0;
                    workerCompletedSubtasks += 1;

                    HandleWorkerSuccess();
                }
            }
            if (mutanterUnitWorkingElapsedTime >= SUBTASK_DURATION)
            {
                if (Random.value < workerSuccessRateFactor)
                {
                    mutanterUnitWorkingElapsedTime = 0;
                    mutanterCompletedSubtasks += 1;

                    HandleMutantSuccess();
                }
            }

            if (termUnitShowingElapsedTime >= TERM_DURATION)
            {
                termUnitShowingElapsedTime = 0;
                PopFXManager.Instance.SpawnFX(PopFXManager.Instance.sprite_Plus,
                    (SecureTermDb.Instance.SelectRandomTermByActionType(_containmentMonitorSMI.CurrentAction)).Name, gameObject.transform, 1.5f, false);

            }
            return base.OnWorkTick(worker, dt);
        }

        private void HandleWorkerSuccess()
        {
            // 实现小人成功的具体逻辑
            // 例如，播放动画、给予奖励、更新UI等
        }

        private void HandleMutantSuccess()
        {
            // 实现畸变体成功的具体逻辑
            // 例如，触发负面效果、更新状态等
        }
        private void ResetWorkState()
        {
            workerCompletedSubtasks = 0;
            mutanterCompletedSubtasks = 0;
            workerUnitWorkingElapsedTime = 0;
            mutanterUnitWorkingElapsedTime = 0;
            termUnitShowingElapsedTime = 0;
        }
        protected override void OnCompleteWork(WorkerBase worker)
        {
            base.OnCompleteWork(worker);

            // 生成产出物逻辑
            GenerateMutanterProducts();

            //TODO 失败小人重伤

            //TODO 找到ContainmentMonitor 所在房间中的畸变体，应用Effect/Modifier
            List<MutanterSecurableMonitor.Instance> list = _containmentMonitorSMI.TargetSecurables;

            foreach (var securable in list)
            {
                if (securable != null) securable.GoInToContaiment();
            }

        }

        private void GenerateMutanterProducts()
        {
            // 综合小人成功和畸变体成功来计算产出物基础
            int workerSuccessCount = workerCompletedSubtasks;
            int mutanterSuccessCount = mutanterCompletedSubtasks;
            
            // 计算综合成功率
            // 小人成功率作为主要因素，畸变体成功数作为负面因素
            float baseSuccessRate = workerSuccessRateFactor;
            
            // 畸变体成功数越多，成功率越低
            float mutanterPenalty = mutanterSuccessCount * 0.1f;
            float finalSuccessRate = Mathf.Max(0.1f, baseSuccessRate - mutanterPenalty);
            
            // 计算总任务数，只考虑小人的成功数，畸变体的成功数不增加总产出
            // 同时，畸变体的成功数会减少总任务数
            int totalEffectiveSubtasks = Mathf.Max(1, workerSuccessCount - mutanterSuccessCount);
            // 直接获取监控站管理的畸变体
            if (_containmentMonitorSMI != null && _containmentMonitorSMI.gameObject != null)
            {
                int cell = Grid.PosToCell(_containmentMonitorSMI.gameObject.transform.position);
                List<MutanterSecurableMonitor.Instance> targetSecurables = _containmentMonitorSMI.TargetSecurables;
                
                foreach (var securable in targetSecurables)
                {
                    if (securable != null && securable.gameObject != null)
                    {
                        // 获取畸变体的产出物组件
                        MutanterProductComponent productComponent = securable.gameObject.GetComponent<MutanterProductComponent>();

                        if (productComponent != null)
                        {
                            // 生成产出物
                            var generatedProducts = productComponent.GenerateProducts(finalSuccessRate, totalEffectiveSubtasks);
                            
                            // 处理生成的产出物
                            foreach (var generatedProduct in generatedProducts)
                            {
                                SpawnProduct(cell, generatedProduct.Id, generatedProduct.Amount);
                            }
                        }
                    }
                }
            }
        }

        private void SpawnProduct(int cell, Tag productId, int amount)
        {
            // 尝试将 productId 转换为 SimHashes
            SimHashes elementHash = (SimHashes)Hash.SDBMLower(productId.Name);
            Element element = ElementLoader.FindElementByHash(elementHash);

            if (element != null)
            {
                // 使用元素系统生成产出物
                float spawnAmount = amount;
                float temperature = element.defaultValues.temperature;

                if (element.IsGas || element.IsLiquid)
                {
                    // 对于气体和液体，使用 AddRemoveSubstance 一次性生成
                    SimMessages.AddRemoveSubstance(
                        cell,
                        elementHash,
                        CellEventLogger.Instance.ElementConsumerSimUpdate,
                        spawnAmount,
                        temperature,
                        byte.MaxValue,
                        0
                    );
                }
                else if (element.IsSolid)
                {
                    // 对于固体，使用 SpawnResource 一次性生成
                    element.substance.SpawnResource(
                        Grid.CellToPosCCC(cell, Grid.SceneLayer.Ore),
                        spawnAmount,
                        temperature,
                        byte.MaxValue,
                        0,
                        forceTemperature: true
                    );
                }

                // 显示弹出效果
                PopFXManager.Instance.SpawnFX(
                    PopFXManager.Instance.sprite_Resource,
                    element.name,
                    _containmentMonitorSMI.gameObject.transform
                );
            }
            else
            {
                // 如果找不到元素，尝试直接实例化预制体
                GameObject prefab = Assets.GetPrefab(productId);
                if (prefab != null)
                {
                    // 一次性生成多个预制体
                    for (int i = 0; i < amount; i++)
                    {
                        GameObject productInstance = GameUtil.KInstantiate(prefab, Grid.CellToPosCCC(cell, Grid.SceneLayer.Ore), Grid.SceneLayer.Ore);
                        if (productInstance != null)
                        {
                            PrimaryElement pe = productInstance.GetComponent<PrimaryElement>();
                            if (pe != null)
                            {
                                pe.Units = 1f;
                            }
                            productInstance.SetActive(true);
                        }
                    }
                }
            }
        }
        protected override void OnStopWork(WorkerBase worker)
        {
            base.OnStopWork(worker);
            ResetWorkState();
        }

    }
}
