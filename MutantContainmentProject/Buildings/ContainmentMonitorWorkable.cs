using Klei.AI;
using MutantContainmentProject.Skills;
using System.Collections.Generic;
using TUNING;
using UnityEngine;

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
            //attributeExperienceMultiplier = DUPLICANTSTATS.ATTRIBUTE_LEVELING.MOST_DAY_EXPERIENCE;
            //skillExperienceSkillGroup = Db.Get().SkillGroups.Ranching.Id;
            //skillExperienceMultiplier = SKILLS.MOST_DAY_EXPERIENCE;

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

            //TODO 生成中间态物质逻辑 依赖于workerCompletedSubtasks/mutanterCompletedSubtasks

            //TODO 失败小人重伤

            //TODO 找到ContainmentMonitor 所在房间中的畸变体，应用Effect/Modifier
            List<MutanterSecurableMonitor.Instance> list = _containmentMonitorSMI.TargetSecurables;

            foreach (var securable in list)
            {
                if (securable != null) securable.GoInToContaiment();
            }

        }
        protected override void OnStopWork(WorkerBase worker)
        {
            base.OnStopWork(worker);
            ResetWorkState();
        }

    }
}
