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
        private CorrosionManager _corrosionManager;
        private GameObject workerGameObject;

        private CorrosionManager CorrosionManagerComp
        {
            get
            {
                if (_corrosionManager == null)
                {
                    _corrosionManager = gameObject.GetComponent<CorrosionManager>();
                }
                return _corrosionManager;
            }
        }

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            workLayer = Grid.SceneLayer.BuildingFront;
            SetWorkTime(WorkTime);

            overrideAnims = new KAnimFile[] { Assets.GetAnim("anim_interacts_genetic_analysisstation_kanim") };
            synchronizeAnims = true;

            attributeConverter = Db.Get().AttributeConverters.Get(MutanterAttributeConverters.AttributeContainmentSpeedConverterID);

            attributeExperienceMultiplier = DUPLICANTSTATS.ATTRIBUTE_LEVELING.MOST_DAY_EXPERIENCE;
            skillExperienceSkillGroup = MutanterSkillGroups.SkillGroupDisciplineID;
            skillExperienceMultiplier = SKILLS.MOST_DAY_EXPERIENCE;

            shouldShowSkillPerkStatusItem = true;
            requiredSkillPerk = Db.Get().SkillPerks.Get(MutanterSkillPerks.CanSecureMutanter).Id;

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
            // 注册站点到管理器
            ContainmentMonitorStationManager.RegisterStation(gameObject);
        }

        protected override void OnCleanUp()
        {
            base.OnCleanUp();
            // 从管理器中注销站点
            ContainmentMonitorStationManager.UnregisterStation(gameObject);
        }
        // 当前操作类型的偏好值加成
        private float currentActionPreferenceBonus = 1f;

        protected override void OnStartWork(WorkerBase worker)
        {
            base.OnStartWork(worker);

            // 标记操作开始，防止操作过程中更新腐蚀值
            if (CorrosionManagerComp != null)
            {
                CorrosionManagerComp.StartOperation();
                TbbDebuger.LogDebug("[ContainmentMonitorWorkable] 标记操作开始");
            }

            // 重置工作状态
            ResetWorkState();
            TbbDebuger.LogDebug("[ContainmentMonitorWorkable] 工作状态已重置");

            // 清除之前的状态项
            KSelectable selectable = gameObject.GetComponent<KSelectable>();
            if (selectable != null)
            {
                selectable.RemoveStatusItem(ContainmentMonitorBuildingStatusItems.Instance.ContainmentSuccess);
                selectable.RemoveStatusItem(ContainmentMonitorBuildingStatusItems.Instance.ContainmentFailure);
                selectable.RemoveStatusItem(ContainmentMonitorBuildingStatusItems.Instance.WorkerDamage);
                TbbDebuger.LogDebug("[ContainmentMonitorWorkable] 之前的状态项已清除");
            }

            workerGameObject = worker.gameObject;
            Modifiers modifiers = workerGameObject.GetComponent<Modifiers>();
            if (modifiers != null)
            {
                var disciplineInstance = modifiers.attributes.Get(MutanterAttributes.AttributeDisciplineID);

                // 获取转换器实例
                var containmentSpeedConverter = Db.Get().AttributeConverters.Get(MutanterAttributeConverters.AttributeContainmentSpeedConverterID);
                var safetyMeasureSuccessRateConverter = Db.Get().AttributeConverters.Get(MutanterAttributeConverters.AttributeSafetyMeasureSuccessRateConverterID);

                // 计算收容速度因子
                float containmentSpeedFactor = 1f;
                if (containmentSpeedConverter != null)
                {
                    var converterInstance = containmentSpeedConverter.Lookup(worker.gameObject);
                    if (converterInstance != null)
                    {
                        containmentSpeedFactor = Mathf.Max(0.1f, 1f + converterInstance.Evaluate());
                    }
                }

                // 计算安全措施成功率因子
                float safetyMeasureSuccessRateFactor = 1f;
                if (safetyMeasureSuccessRateConverter != null)
                {
                    var converterInstance = safetyMeasureSuccessRateConverter.Lookup(worker.gameObject);
                    if (converterInstance != null)
                    {
                        safetyMeasureSuccessRateFactor = Mathf.Max(0.1f, 0.5f + converterInstance.Evaluate());
                    }
                }

                // 综合计算最终因子
                workerWorkingSpeedFactor = containmentSpeedFactor;
                workerSuccessRateFactor = safetyMeasureSuccessRateFactor;

                TbbDebuger.LogDebug($"[ContainmentMonitorWorkable] 工作速度因子: {workerWorkingSpeedFactor}, 成功率因子: {workerSuccessRateFactor}");
                TbbDebuger.LogDebug($"[ContainmentMonitorWorkable] 收容速度: {containmentSpeedFactor}, 安全措施成功率: {safetyMeasureSuccessRateFactor}");

                // 调试日志
                TbbDebuger.LogDebug($"[ContainmentMonitorWorkable] 工作速度因子: {workerWorkingSpeedFactor}, 成功率因子: {workerSuccessRateFactor}");
            }
            else
            {
                TbbDebuger.LogDebug("[ContainmentMonitorWorkable] 未找到Modifiers组件");
                // 如果没有Modifiers组件，使用默认值
                workerWorkingSpeedFactor = 0.8f;
                workerSuccessRateFactor = 0.6f;
                TbbDebuger.LogDebug($"[ContainmentMonitorWorkable] 使用默认值 - 工作速度因子: {workerWorkingSpeedFactor}, 成功率因子: {workerSuccessRateFactor}");
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

                            // 计算当前操作类型的偏好值加成
                            CalculateActionPreferenceBonus(_containmentMonitorSMI.CurrentAction, mutanterColony);

                            TbbDebuger.LogDebug($"[ContainmentMonitorWorkable] 从畸变体获取速度和成功率 - 速度: {mutanterWorkingSpeedFactor}, 成功率: {mutanterSuccessRateFactor}");
                            TbbDebuger.LogDebug($"[ContainmentMonitorWorkable] 当前操作偏好值加成: {currentActionPreferenceBonus}");
                            break; // 只获取第一个畸变体的参数
                        }
                    }
                }
            }
            else
            {
                TbbDebuger.LogDebug("[ContainmentMonitorWorkable] 未找到ContainmentMonitor SMI");
                currentActionPreferenceBonus = 1f; // 默认加成
            }

            TbbDebuger.LogDebug($"[ContainmentMonitorWorkable] 开始工作，工人: {worker.name}");
            TbbDebuger.LogDebug($"[ContainmentMonitorWorkable] 最终参数 - 工人速度: {workerWorkingSpeedFactor}, 工人成功率: {workerSuccessRateFactor}, 畸变体速度: {mutanterWorkingSpeedFactor}, 畸变体成功率: {mutanterSuccessRateFactor}");
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
                TbbDebuger.LogDebug($"[ContainmentMonitorWorkable] 工作进度: 工人时间={workerUnitWorkingElapsedTime:F2}/{SUBTASK_DURATION}, 畸变体时间={mutanterUnitWorkingElapsedTime:F2}/{SUBTASK_DURATION}");
                TbbDebuger.LogDebug($"[ContainmentMonitorWorkable] 子任务计数: 工人={workerCompletedSubtasks}, 畸变体={mutanterCompletedSubtasks}");
            }

            if (workerUnitWorkingElapsedTime >= SUBTASK_DURATION)
            {
                workerUnitWorkingElapsedTime = 0;
                // 应用偏好值加成到成功率
                float adjustedSuccessRate = workerSuccessRateFactor * currentActionPreferenceBonus;
                if (Random.value < adjustedSuccessRate)
                {
                    workerCompletedSubtasks += 1;
                    TbbDebuger.LogDebug($"[ContainmentMonitorWorkable] 小人完成子任务，当前计数: {workerCompletedSubtasks}, 调整后成功率: {adjustedSuccessRate}");
                    HandleWorkerSuccess();
                }
                else
                {
                    TbbDebuger.LogDebug($"[ContainmentMonitorWorkable] 小人子任务失败，调整后成功率: {adjustedSuccessRate}");
                }
            }
            if (mutanterUnitWorkingElapsedTime >= SUBTASK_DURATION)
            {
                mutanterUnitWorkingElapsedTime = 0;
                if (Random.value < mutanterSuccessRateFactor)
                {
                    mutanterCompletedSubtasks += 1;
                    TbbDebuger.LogDebug($"[ContainmentMonitorWorkable] 畸变体完成子任务，当前计数: {mutanterCompletedSubtasks}");
                    HandleMutantSuccess();
                }
                else
                {
                    TbbDebuger.LogDebug("[ContainmentMonitorWorkable] 畸变体子任务失败");
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

        // 固定伤害值
        private const float MUTANTER_DAMAGE_AMOUNT = 10f;

        private void HandleMutantSuccess()
        {
            // 实现畸变体成功的具体逻辑
            // 例如，触发负面效果、更新状态等
            if (workerGameObject != null)
            {
                // 找到当前房间中的畸变体，获取其攻击系统
                if (_containmentMonitorSMI != null)
                {
                    foreach (var securable in _containmentMonitorSMI.TargetSecurables)
                    {
                        if (securable != null && securable.gameObject != null)
                        {
                            MutanterAttackSystem attackSystem = securable.gameObject.GetComponent<MutanterAttackSystem>();
                            if (attackSystem != null)
                            {
                                // 执行攻击
                                bool attackSuccess = attackSystem.TryExecuteAttack(workerGameObject);
                                if (attackSuccess)
                                {
                                    // 显示工人受伤状态项
                                    ShowWorkerDamageStatusItem(MUTANTER_DAMAGE_AMOUNT, "Mutant Attack");
                                }
                                break; // 只让第一个畸变体攻击
                            }
                        }
                    }
                }
                else
                {
                    // 如果没有找到攻击系统，默认执行物理伤害
                    Health health = workerGameObject.GetComponent<Health>();
                    if (health != null)
                    {
                        health.Damage(MUTANTER_DAMAGE_AMOUNT);
                        // 显示工人受伤状态项
                        ShowWorkerDamageStatusItem(MUTANTER_DAMAGE_AMOUNT, "Physical");
                    }
                }
            }
        }

        private void ShowWorkerDamageStatusItem(float damage, string damageType)
        {
            KSelectable selectable = gameObject.GetComponent<KSelectable>();
            if (selectable != null)
            {
                // 显示工人受伤状态项
                selectable.AddStatusItem(ContainmentMonitorBuildingStatusItems.Instance.WorkerDamage, new System.Tuple<float, string>(damage, damageType));
            }
        }
        private void ResetWorkState()
        {
            workerCompletedSubtasks = 0;
            mutanterCompletedSubtasks = 0;
            workerUnitWorkingElapsedTime = 0;
            mutanterUnitWorkingElapsedTime = 0;
            termUnitShowingElapsedTime = 0;
            currentActionPreferenceBonus = 1f; // 重置偏好值加成
        }

        // 计算当前操作类型的偏好值加成
        private void CalculateActionPreferenceBonus(SecureAction action, MutanterColonyComponent mutanterColony)
        {
            if (action == SecureAction.None || mutanterColony == null || workerGameObject == null)
            {
                currentActionPreferenceBonus = 1f;
                return;
            }

            // 根据操作类型确定对应的技能等级
            int skillLevel = GetSkillLevelForAction(action);

            // 获取该操作类型对应等级的偏好值
            float preference = mutanterColony.GetSecureActionPreference(action, skillLevel);

            // 将偏好值转换为加成因子（偏好值越低，加成越低）
            // 偏好值范围：0~100%，加成因子范围：0.5~1.0
            currentActionPreferenceBonus = 0.5f + (preference / 100f) * 0.5f;

            TbbDebuger.LogDebug($"[ContainmentMonitorWorkable] 操作类型: {action}, 技能等级: {skillLevel}, 偏好值: {preference}%, 加成因子: {currentActionPreferenceBonus}");
        }

        // 根据操作类型获取对应的技能等级
        private int GetSkillLevelForAction(SecureAction action)
        {
            if (workerGameObject == null)
            {
                return 0;
            }

            // 获取小人的 MinionResume 组件，它管理小人的技能
            MinionResume minionResume = workerGameObject.GetComponent<MinionResume>();
            if (minionResume == null)
            {
                return 0;
            }

            // 根据操作类型确定对应的技能组
            string skillGroupId = string.Empty;
            switch (action)
            {
                case SecureAction.Instinct: // 本能操作对应勇气技能
                    skillGroupId = MutanterSkillGroups.SkillGroupBraveryID;
                    break;
                case SecureAction.Reconnaissance: // 洞察操作对应防御技能
                    skillGroupId = MutanterSkillGroups.SkillGroupDefenseID;
                    break;
                case SecureAction.Communicate: // 自律操作对应沟通技能
                    skillGroupId = MutanterSkillGroups.SkillGroupDisciplineID;
                    break;
                case SecureAction.Intimidation: // 压迫操作对应正义技能
                    skillGroupId = MutanterSkillGroups.SkillGroupRighteousnessID;
                    break;
                default:
                    return 0;
            }

            // 获取技能组的最高技能等级
            int maxLevel = 0;
            foreach (var skill in Db.Get().Skills.resources)
            {
                // 检查技能是否属于目标技能组，并且小人已经掌握该技能
                if (skill.skillGroup == skillGroupId && minionResume.HasMasteredSkill(skill.Id))
                {
                    // 技能的 tier 属性表示技能等级
                    if (skill.tier > maxLevel)
                    {
                        maxLevel = skill.tier;
                    }
                }
            }

            // 确保等级在0-2之间（对应三个技能等级）
            return Mathf.Clamp(maxLevel, 0, 2);
        }
        protected override void OnCompleteWork(WorkerBase worker)
        {
            base.OnCompleteWork(worker);

            // 保存当前子任务数
            int finalWorkerSubtasks = workerCompletedSubtasks;
            int finalMutanterSubtasks = mutanterCompletedSubtasks;

            // 检查是否成功
            bool isSuccess = finalWorkerSubtasks > finalMutanterSubtasks;

            TbbDebuger.LogDebug($"[ContainmentMonitorWorkable] 工作完成，工人子任务数: {finalWorkerSubtasks}, 畸变体子任务数: {finalMutanterSubtasks}");
            TbbDebuger.LogDebug($"[ContainmentMonitorWorkable] 收容措施结果: {(isSuccess ? "成功" : "失败")}");

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
                    TbbDebuger.LogDebug("[ContainmentMonitorWorkable] 添加成功状态项");
                }
                else
                {
                    selectable.AddStatusItem(ContainmentMonitorBuildingStatusItems.Instance.ContainmentFailure, new System.Tuple<int, int>(finalWorkerSubtasks, finalMutanterSubtasks));
                    TbbDebuger.LogDebug("[ContainmentMonitorWorkable] 添加失败状态项");
                }
            }
            else
            {
                TbbDebuger.LogDebug("[ContainmentMonitorWorkable] 未找到KSelectable组件");
            }

            if (isSuccess)
            {
                // 成功：生成产出物
                TbbDebuger.LogDebug("[ContainmentMonitorWorkable] 开始生成产出物");
                GenerateMutanterProducts();

                // 找到ContainmentMonitor 所在房间中的畸变体，应用Effect/Modifier
                if (_containmentMonitorSMI != null)
                {
                    List<MutanterSecurableMonitor.Instance> list = _containmentMonitorSMI.TargetSecurables;
                    TbbDebuger.LogDebug($"[ContainmentMonitorWorkable] 找到 {list.Count} 个畸变体");

                    foreach (var securable in list)
                    {
                        if (securable != null)
                        {
                            securable.GoInToContaiment();
                            TbbDebuger.LogDebug($"[ContainmentMonitorWorkable] 应用收容效果到畸变体: {securable.gameObject.name}");
                        }
                    }
                }
                else
                {
                    TbbDebuger.LogDebug("[ContainmentMonitorWorkable] 未找到ContainmentMonitor SMI");
                }

                // 处理管控成功（基于执行次数，而非子任务数）
                if (CorrosionManagerComp != null)
                {
                    CorrosionManagerComp.HandleContainmentSuccess();
                    TbbDebuger.LogDebug("[ContainmentMonitorWorkable] 执行成功，调用HandleContainmentSuccess");
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
                        // health.Damage(health.maxHitPoints); // 扣减所有生命值
                        TbbDebuger.LogDebug($"[ContainmentMonitorWorkable] 工人 {worker.name} 受到致命伤害");
                    }
                    else
                    {
                        TbbDebuger.LogDebug("[ContainmentMonitorWorkable] 未找到Health组件");
                    }
                }
                else
                {
                    TbbDebuger.LogDebug("[ContainmentMonitorWorkable] 工人对象为空");
                }

                // 处理管控失败（基于执行次数，而非子任务数）
                if (CorrosionManagerComp != null)
                {
                    CorrosionManagerComp.HandleContainmentFailure();
                    TbbDebuger.LogDebug("[ContainmentMonitorWorkable] 执行失败，调用HandleContainmentFailure");
                }
            }

            // 标记操作结束，恢复腐蚀值更新
            if (CorrosionManagerComp != null)
            {
                CorrosionManagerComp.EndOperation();
                TbbDebuger.LogDebug("[ContainmentMonitorWorkable] 标记操作结束");
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

            // 计算产出抑制因子
            float productionReductionFactor = CalculateProductionReductionFactor();

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
                                // 应用产出抑制因子
                                int reducedAmount = Mathf.Max(1, Mathf.FloorToInt(generatedProduct.Amount * productionReductionFactor));
                                SpawnProduct(cell, generatedProduct.Id, reducedAmount);
                            }
                        }
                    }
                }
            }
        }

        private float CalculateProductionReductionFactor()
        {
            float reductionFactor = 1.0f;

            // 基于腐蚀等级的抑制
            if (CorrosionManagerComp != null)
            {
                switch (CorrosionManagerComp.CurrentCorrosionState)
                {
                    case CorrosionManager.CorrosionState.Stable:
                        reductionFactor *= 1.0f;
                        break;
                    case CorrosionManager.CorrosionState.Warning:
                        reductionFactor *= 0.8f;
                        break;
                    case CorrosionManager.CorrosionState.HighCorrosion:
                        reductionFactor *= 0.6f;
                        break;
                    case CorrosionManager.CorrosionState.Overflow:
                        reductionFactor *= 0.4f;
                        break;
                }
            }

            // 基于全局腐蚀等级的抑制
            var globalErosionManager = GlobalErosionManager.Instance;
            if (globalErosionManager != null)
            {
                switch (globalErosionManager.CurrentErosionLevel)
                {
                    case GlobalErosionManager.ErosionLevel.Safe:
                        reductionFactor *= 1.0f;
                        break;
                    case GlobalErosionManager.ErosionLevel.Alert:
                        reductionFactor *= 0.9f;
                        break;
                    case GlobalErosionManager.ErosionLevel.Crisis:
                        reductionFactor *= 0.7f;
                        break;
                    case GlobalErosionManager.ErosionLevel.Disaster:
                        reductionFactor *= 0.5f;
                        break;
                }
            }

            return Mathf.Max(0.1f, reductionFactor);
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

            // 标记操作结束，恢复腐蚀值更新
            if (CorrosionManagerComp != null)
            {
                CorrosionManagerComp.EndOperation();
                TbbDebuger.LogDebug("[ContainmentMonitorWorkable] 标记操作结束（工作停止）");
            }
        }

    }
}