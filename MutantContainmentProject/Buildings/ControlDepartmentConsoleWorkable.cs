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
            //this.overrideAnims = new KAnimFile[] { Assets.GetAnim("anim_interacts_control_operation_kanim") };
            this.overrideAnims = new KAnimFile[] { Assets.GetAnim("anim_interacts_geotuner_kanim") };
            this.workTime = WORK_TIME;
        }

        protected override void OnCompleteWork(WorkerBase worker)
        {
            base.OnCompleteWork(worker);
            ControlDepartmentConsole.Instance smi = this.GetComponent<ControlDepartmentConsole.Instance>();
            if (smi != null)
            {
                smi.OnResearchCompleted();
            }
        }
    }
}