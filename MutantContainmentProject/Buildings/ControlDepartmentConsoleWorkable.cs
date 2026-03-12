namespace MutantContainmentProject.Buildings
{
    public class ControlDepartmentConsoleWorkable : Workable
    {
        private const float WORK_TIME = 30f;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            this.workerStatusItem = Db.Get().DuplicantStatusItems.Researching;
            //this.requiredSkillPerk = Db.Get().SkillPerks.AllowGeyserTuning.Id;
            this.overrideAnims = new KAnimFile[] { Assets.GetAnim("anim_interacts_geotuner_kanim") };
            this.workTime = WORK_TIME;
        }

        protected override void OnStartWork(WorkerBase worker)
        {
            base.OnStartWork(worker);
            this.GetComponent<KBatchedAnimController>().Play("working_pre", KAnim.PlayMode.Once);
            this.GetComponent<KBatchedAnimController>().Queue("working_loop", KAnim.PlayMode.Loop);
        }

        protected override void OnCompleteWork(WorkerBase worker)
        {
            base.OnCompleteWork(worker);
            this.GetComponent<KBatchedAnimController>().Play("working_pst", KAnim.PlayMode.Once);
            ControlDepartmentConsole.Instance smi = this.GetComponent<ControlDepartmentConsole.Instance>();
            if (smi != null)
            {
                smi.OnResearchCompleted();
            }
        }

        protected override void OnStopWork(WorkerBase worker)
        {
            base.OnStopWork(worker);
            this.GetComponent<KBatchedAnimController>().Play("idle", KAnim.PlayMode.Once);
        }
    }
}