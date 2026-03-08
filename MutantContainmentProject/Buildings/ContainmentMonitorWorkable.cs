using Klei.AI;
using MutantContainmentProject.MutanterComponent;
using MutantContainmentProject.Skills;
using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;
using TUNING;
using UnityEngine;

namespace MutantContainmentProject.Buildings
{
    public class ContainmentMonitorWorkable : Workable
    {
        private static float maxGainLevel = DUPLICANTSTATS.ATTRIBUTE_LEVELING.MAX_GAINED_ATTRIBUTE_LEVEL;
        //工作常量
        private static readonly float WorkTime = 60f;
        private const float SUBTASK_DURATION = 5f; // 完成一个"子任务"所需的基础时间，单位秒
        private const float TERM_DURATION = 5f; //词条弹出间隔
        //工作状态
        private int workerCompletedSubtasks = 0; // 小人成功完成的子任务数
        private int mutanterCompletedSubtasks = 0; // 畸变体成功完成的子任务数
        private float workerUnitWorkingElapsedTime = 0;
        private float mutanterUnitWorkingElapsedTime = 0;
        private float termUnitShowingElapsedTime = 0;
        
        // 公共属性，供 StatusItems 使用
        public int WorkerCompletedSubtasks => workerCompletedSubtasks;
        public int MutanterCompletedSubtasks => mutanterCompletedSubtasks;
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

            // 设置属性转换器为工作速度
            attributeConverter = Db.Get().AttributeConverters.Get(MutanterAttributeConverters.AttributeWorkingSpeedConverterID);
            // 添加log验证转换器是否正常工作
            TbbDebuger.LogDebug($"[ContainmentMonitorWorkable] 属性转换器: {attributeConverter?.Id ?? "null"}");
            TbbDebuger.LogDebug($"[ContainmentMonitorWorkable] 转换器ID: {MutanterAttributeConverters.AttributeWorkingSpeedConverterID}");
            attributeExperienceMultiplier = DUPLICANTSTATS.ATTRIBUTE_LEVELING.MOST_DAY_EXPERIENCE;
            skillExperienceSkillGroup = MutanterSkillGroups.SkillGroupDisciplineID;
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
            Debug.Log("[ContainmentMonitorWorkable] 工作状态已重置");

            // 清除之前的状态项
            KSelectable selectable = gameObject.GetComponent<KSelectable>();
            if (selectable != null)
            {
                selectable.RemoveStatusItem(ContainmentMonitorBuildingStatusItems.Instance.ContainmentSuccess);
                selectable.RemoveStatusItem(ContainmentMonitorBuildingStatusItems.Instance.ContainmentFailure);
                Debug.Log("[ContainmentMonitorWorkable] 之前的状态项已清除");
            }

            workerGameObject = worker.gameObject;
            Modifiers modifiers = workerGameObject.GetComponent<Modifiers>();
            if (modifiers != null)
            {
                var workingSpeedInstance = modifiers.attributes.Get(MutanterAttributes.AttributeWorkingSpeedID);
                var successRateInstance = modifiers.attributes.Get(MutanterAttributes.AttributeSuccessRateID);
                
                // 检查属性值
                float workingSpeedValue = workingSpeedInstance.GetTotalValue();
                float successRateValue = successRateInstance.GetTotalValue();
                
                Debug.Log($"[ContainmentMonitorWorkable] 原始属性值 - 工作速度: {workingSpeedValue}, 成功率: {successRateValue}");
                Debug.Log($"[ContainmentMonitorWorkable] maxGainLevel: {maxGainLevel}");
                
                workerWorkingSpeedFactor = workingSpeedValue / maxGainLevel;
                workerWorkingSpeedFactor = Mathf.Max(0.5f, workerWorkingSpeedFactor); // 确保最小工作速度
                workerSuccessRateFactor = successRateValue / maxGainLevel;
                workerSuccessRateFactor = Mathf.Max(0.5f, workerSuccessRateFactor); // 提高最小成功率

                // 调试日志
                Debug.Log($"[ContainmentMonitorWorkable] 工作速度因子: {workerWorkingSpeedFactor}, 成功率因子: {workerSuccessRateFactor}");
            }
            else
            {
                Debug.Log("[ContainmentMonitorWorkable] 未找到Modifiers组件");
                // 如果没有Modifiers组件，使用默认值
                workerWorkingSpeedFactor = 0.8f;
                workerSuccessRateFactor = 0.6f;
                Debug.Log($"[ContainmentMonitorWorkable] 使用默认值 - 工作速度因子: {workerWorkingSpeedFactor}, 成功率因子: {workerSuccessRateFactor}");
            }
            
            // 从ContainmentMonitor获取畸变体的速度和成功率
            if (_containmentMonitorSMI != null)
            {
                foreach (var securable in _containmentMonitorSMI.TargetSecurables)
                {
                    if (securable != null && securable.gameObject != null)
                    {
                        MutanterColonyComponent mutanterColony = securable.gameObject.GetComponent<MutanterColonyComponent>();
                        if (mutanterColony != null)
                        {
                            mutanterWorkingSpeedFactor = mutanterColony.WorkingSpeedFactor;
                            mutanterSuccessRateFactor = mutanterColony.SuccessRateFactor;
                            Debug.Log($"[ContainmentMonitorWorkable] 从畸变体获取速度和成功率 - 速度: {mutanterWorkingSpeedFactor}, 成功率: {mutanterSuccessRateFactor}");
                            break; // 只获取第一个畸变体的参数
                        }
                    }
                }
            }
            else
            {
                Debug.Log("[ContainmentMonitorWorkable] 未找到ContainmentMonitor SMI");
            }
            
            Debug.Log($"[ContainmentMonitorWorkable] 开始工作，工人: {worker.name}");
            Debug.Log($"[ContainmentMonitorWorkable] 最终参数 - 工人速度: {workerWorkingSpeedFactor}, 工人成功率: {workerSuccessRateFactor}, 畸变体速度: {mutanterWorkingSpeedFactor}, 畸变体成功率: {mutanterSuccessRateFactor}");
        }
        protected override bool OnWorkTick(WorkerBase worker, float dt)
        {
            // 应用工作速度因子
            float workerEffectiveDt = dt * workerWorkingSpeedFactor;
            float mutanterEffectiveDt = dt * mutanterWorkingSpeedFactor;
            
            workerUnitWorkingElapsedTime += workerEffectiveDt;
            mutanterUnitWorkingElapsedTime += mutanterEffectiveDt;
            termUnitShowingElapsedTime += dt;

            // 每5秒记录一次工作进度
            if ((int)(Time.time / 5) != (int)((Time.time - dt) / 5))
            {
                Debug.Log($"[ContainmentMonitorWorkable] 工作进度: 工人时间={workerUnitWorkingElapsedTime:F2}/{SUBTASK_DURATION}, 畸变体时间={mutanterUnitWorkingElapsedTime:F2}/{SUBTASK_DURATION}");
                Debug.Log($"[ContainmentMonitorWorkable] 子任务计数: 工人={workerCompletedSubtasks}, 畸变体={mutanterCompletedSubtasks}");
            }

            if (workerUnitWorkingElapsedTime >= SUBTASK_DURATION)
            {
                workerUnitWorkingElapsedTime = 0;
                if (Random.value < workerSuccessRateFactor)
                {
                    workerCompletedSubtasks += 1;
                    Debug.Log($"[ContainmentMonitorWorkable] 小人完成子任务，当前计数: {workerCompletedSubtasks}");
                    HandleWorkerSuccess();
                }
                else
                {
                    Debug.Log("[ContainmentMonitorWorkable] 小人子任务失败");
                }
            }
            if (mutanterUnitWorkingElapsedTime >= SUBTASK_DURATION)
            {
                mutanterUnitWorkingElapsedTime = 0;
                if (Random.value < mutanterSuccessRateFactor)
                {
                    mutanterCompletedSubtasks += 1;
                    Debug.Log($"[ContainmentMonitorWorkable] 畸变体完成子任务，当前计数: {mutanterCompletedSubtasks}");
                    HandleMutantSuccess();
                }
                else
                {
                    Debug.Log("[ContainmentMonitorWorkable] 畸变体子任务失败");
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

            // 保存当前子任务数
            int finalWorkerSubtasks = workerCompletedSubtasks;
            int finalMutanterSubtasks = mutanterCompletedSubtasks;
            
            // 检查是否成功
            bool isSuccess = finalWorkerSubtasks > finalMutanterSubtasks;
            
            Debug.Log($"[ContainmentMonitorWorkable] 工作完成，工人子任务数: {finalWorkerSubtasks}, 畸变体子任务数: {finalMutanterSubtasks}");
            Debug.Log($"[ContainmentMonitorWorkable] 收容措施结果: {(isSuccess ? "成功" : "失败")}");

            // 显示状态项
            KSelectable selectable = gameObject.GetComponent<KSelectable>();
            if (selectable != null)
            {
                // 先移除之前的状态项
                selectable.RemoveStatusItem(ContainmentMonitorBuildingStatusItems.Instance.ContainmentSuccess);
                selectable.RemoveStatusItem(ContainmentMonitorBuildingStatusItems.Instance.ContainmentFailure);

                // 直接传递子任务数作为参数
                if (isSuccess)
                {
                    selectable.AddStatusItem(ContainmentMonitorBuildingStatusItems.Instance.ContainmentSuccess, new System.Tuple<int, int>(finalWorkerSubtasks, finalMutanterSubtasks));
                    Debug.Log("[ContainmentMonitorWorkable] 添加成功状态项");
                }
                else
                {
                    selectable.AddStatusItem(ContainmentMonitorBuildingStatusItems.Instance.ContainmentFailure, new System.Tuple<int, int>(finalWorkerSubtasks, finalMutanterSubtasks));
                    Debug.Log("[ContainmentMonitorWorkable] 添加失败状态项");
                }
            }
            else
            {
                Debug.Log("[ContainmentMonitorWorkable] 未找到KSelectable组件");
            }

            if (isSuccess)
            {
                // 成功：生成产出物
                Debug.Log("[ContainmentMonitorWorkable] 开始生成产出物");
                GenerateMutanterProducts();

                // 找到ContainmentMonitor 所在房间中的畸变体，应用Effect/Modifier
                if (_containmentMonitorSMI != null)
                {
                    List<MutanterSecurableMonitor.Instance> list = _containmentMonitorSMI.TargetSecurables;
                    Debug.Log($"[ContainmentMonitorWorkable] 找到 {list.Count} 个畸变体");

                    foreach (var securable in list)
                    {
                        if (securable != null) 
                        {
                            securable.GoInToContaiment();
                            Debug.Log($"[ContainmentMonitorWorkable] 应用收容效果到畸变体: {securable.gameObject.name}");
                        }
                    }
                }
                else
                {
                    Debug.Log("[ContainmentMonitorWorkable] 未找到ContainmentMonitor SMI");
                }
            }
            else
            {
                // 失败：将小人的生命值扣减为0
                if (worker != null && worker.gameObject != null)
                {
                    Health health = worker.gameObject.GetComponent<Health>();
                    if (health != null)
                    {
                        health.Damage(health.maxHitPoints); // 扣减所有生命值
                        Debug.Log($"[ContainmentMonitorWorkable] 工人 {worker.name} 受到致命伤害");
                    }
                    else
                    {
                        Debug.Log("[ContainmentMonitorWorkable] 未找到Health组件");
                    }
                }
                else
                {
                    Debug.Log("[ContainmentMonitorWorkable] 工人对象为空");
                }
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