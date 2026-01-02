using STRINGS;

namespace MutantFarmLab
{
    public class CustomRadiationLight : KMonoBehaviour, ISingleSliderControl, ISliderControl
    {
        //建筑配置项
        public RadiationEmitter radiationEmitter;
        public HighEnergyParticleStorage particleStorage;
        public float lowParticleThreshold;
        public float consumerRate = 1;

        //内部配置项
        private int RadiationLevel = 0;
        private static readonly EventSystem.IntraObjectHandler<CustomRadiationLight> OnParticleChangedDelegate =
    new EventSystem.IntraObjectHandler<CustomRadiationLight>((component, data) => component.OnParticleChanged(data));
        protected override void OnSpawn()
        {
            base.OnSpawn();
            if (this.radiationEmitter != null)
            {
                this.radiationEmitter.SetEmitting(false);
            }
            particleStorage = GetComponent<HighEnergyParticleStorage>();
            Subscribe((int)GameHashes.OnParticleStorageChanged, OnParticleChangedDelegate);
            Refresh();
        }
        protected override void OnCleanUp()
        {
            base.OnCleanUp();
            Unsubscribe((int)GameHashes.OnParticleStorageChanged);
        }
        private void Refresh()
        {
            if (particleStorage != null && particleStorage.GetAmountAvailable(GameTags.HighEnergyParticle) > lowParticleThreshold)
            {
                if (!radiationEmitter.enabled) radiationEmitter.SetEmitting(true);

            }
            else
            {
                radiationEmitter.SetEmitting(false);
            }
        }
        private void OnParticleChanged(object data)
        {
            Refresh();
        }
        public void ConsumeParticlePerTick(float dt)
        {
            if (particleStorage != null && particleStorage.GetAmountAvailable(GameTags.HighEnergyParticle) > lowParticleThreshold) {
                if(!radiationEmitter.enabled) radiationEmitter.SetEmitting(true);
                particleStorage.ConsumeAndGet(dt * RadiationLevel/10 * consumerRate);
            }
            else {
                radiationEmitter.SetEmitting(false);
            }
        }
        public string SliderTitleKey
        {
            get
            {
                return STRINGS.BUILDINGS.PREFABS.CUSTOMRADIATIONLIGHT.NAME;
            }
        }

        public string SliderUnits
        {
            get
            {
                return UI.UNITSUFFIXES.RADIATION.RADS;
            }
        }

        public float GetSliderMax(int index)=> 5f;

        public float GetSliderMin(int index)=> 0f;

        public string GetSliderTooltip(int index)
        {
            return "";
        }

        public string GetSliderTooltipKey(int index)
        {
            return "";
        }

        public float GetSliderValue(int index)=>RadiationLevel;

        public void SetSliderValue(float value, int index)
        {
            RadiationLevel = (int)value;
            radiationEmitter.emitRads = RadiationLevel * 300;
            //不能设置,会引起辐射区域闪烁
            //radiationEmitter.emitRadiusX = (short)RadiationLevel;
            //radiationEmitter.emitRadiusY = (short)RadiationLevel;
            radiationEmitter.Refresh();
            Refresh();
        }

        public int SliderDecimalPlaces(int index)=>0;

    }

    public class CustomRadiationLightSM : GameStateMachine<CustomRadiationLightSM, CustomRadiationLightSM.StatesInstance, IStateMachineTarget, CustomRadiationLightSM.Def>
    {
        public class StatesInstance : GameInstance
        {
            public StatesInstance(IStateMachineTarget master, Def def) : base(master, def) { }
        }

        public State idle; // 空闲状态（粒子不足或未启用）
        public State running; // 运行状态（粒子充足）

        public override void InitializeStates(out BaseState default_state)
        {
            default_state = idle;

            // 空闲状态：不辐射，播放关闭动画
            idle.PlayAnim("off")
                .EventTransition(GameHashes.OnParticleStorageChanged, running, smi =>
                    smi.master.GetComponent<HighEnergyParticleStorage>().GetAmountAvailable(GameTags.HighEnergyParticle) >= 200f)
                .Enter(smi => smi.master.GetComponent<RadiationEmitter>().SetEmitting(false));

            // 运行状态：辐射，播放运行动画，每帧消耗粒子
            running.PlayAnim("on")
                .EventTransition(GameHashes.OnParticleStorageChanged, idle, smi =>
                    smi.master.GetComponent<HighEnergyParticleStorage>().GetAmountAvailable(GameTags.HighEnergyParticle) < 200f)
                .Update((smi, dt) =>
                    smi.master.GetComponent<CustomRadiationLight>().ConsumeParticlePerTick(dt*5),UpdateRate.RENDER_1000ms);
        }
        public class Def : BaseDef
        {
        }
    }
}
