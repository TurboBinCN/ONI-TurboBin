using KSerialization;
using STRINGS;
using static LogicGateBase;

namespace MutantFarmLab
{
    public class CustomRadiationLight : KMonoBehaviour, ISingleSliderControl, ISliderControl,ISaveLoadable
    {
        //建筑配置项
        public RadiationEmitter radiationEmitter;
        public HighEnergyParticleStorage particleStorage;
        private LogicPorts _logicPorts;
        public float lowParticleThreshold;
        public float consumerRate = 1;

        //内部配置项
        [Serialize]
        public int RadiationLevel = 0;
        private static readonly EventSystem.IntraObjectHandler<CustomRadiationLight> OnParticleChangedDelegate =
    new EventSystem.IntraObjectHandler<CustomRadiationLight>((component, data) => component.OnParticleChanged(data));
        protected override void OnSpawn()
        {
            base.OnSpawn();
            if (this.radiationEmitter != null)
            {
                this.radiationEmitter.SetEmitting(false);
            }
            _logicPorts = GetComponent<LogicPorts>();
            particleStorage = GetComponent<HighEnergyParticleStorage>();
            Subscribe((int)GameHashes.OnParticleStorageChanged, OnParticleChangedDelegate);

            radiationEmitter.emitRads = RadiationLevel * 300;
            radiationEmitter.Refresh();
            Refresh();
        }

        protected override void OnCleanUp()
        {
            base.OnCleanUp();
            Unsubscribe((int)GameHashes.OnParticleStorageChanged);
        }
        private bool IsRunningAvailable()
        {
            if (particleStorage == null || radiationEmitter == null) return false;
            // 双核心条件：档位必须>0 + 粒子量必须高于阈值，缺一不可
            bool hasValidLevel = RadiationLevel > 0;
            bool hasEnoughParticle = particleStorage.GetAmountAvailable(GameTags.HighEnergyParticle) > lowParticleThreshold;
            return hasValidLevel && hasEnoughParticle;
        }
        private void Refresh()
        {
            bool canRun = IsRunningAvailable();
            radiationEmitter.SetEmitting(canRun);
            KBatchedAnimController anim = GetComponent<KBatchedAnimController>();
            if (anim != null)
            {
                if (canRun && !(anim.GetCurrentFrameIndex() == 0) ) anim.Play("on", KAnim.PlayMode.Loop);
                if (!canRun && !(anim.GetCurrentFrameIndex() == 1)) anim.Play("off", KAnim.PlayMode.Once);
            }
            // 逻辑信号输出：粒子不足=1，充足=0
            bool isParticleEnough = particleStorage.GetAmountAvailable(GameTags.HighEnergyParticle) > lowParticleThreshold;
            _logicPorts?.SendSignal(CustomRadiationLightConfig.LOGIC_PORT_ID, isParticleEnough ? 0 : 1);
        }
        private void OnParticleChanged(object data)
        {
            Refresh();
        }
        public void ConsumeParticlePerTick(float dt)
        {
            if (particleStorage == null || radiationEmitter == null) return;
            // ✅ 前置校验：档位0直接关闭，不消耗粒子
            if (RadiationLevel <= 0)
            {
                radiationEmitter.SetEmitting(false);
                Refresh();
                return;
            }
            float particleAmount = particleStorage.GetAmountAvailable(GameTags.HighEnergyParticle);
            if (particleAmount <= lowParticleThreshold)
            {
                radiationEmitter.SetEmitting(false);
                Refresh();
                return;
            }
            particleStorage.ConsumeAndGet(dt * RadiationLevel/10 * consumerRate);
        }
        public string SliderTitleKey => STRINGS.BUILDINGS.PREFABS.CUSTOMRADIATIONLIGHT.NAME;

        public string SliderUnits => STRINGS.UI.UNITSUFFIXES.RADIATION.RADLEVEL;

        public float GetSliderMax(int index)=> 5f;

        public float GetSliderMin(int index)=> 0f;

        public string GetSliderTooltip(int index) =>"";

        public string GetSliderTooltipKey(int index) => "";

        public float GetSliderValue(int index)=>RadiationLevel;

        public void SetSliderValue(float value, int index)
        {
            if (RadiationLevel == value) return;
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
                {
                    var storage = smi.master.GetComponent<HighEnergyParticleStorage>();
                    var light = smi.master.GetComponent<CustomRadiationLight>();
                    // 双条件：粒子≥200 + 档位>0 → 才允许进入运行状态
                    return storage.GetAmountAvailable(GameTags.HighEnergyParticle) >= 200f && light.RadiationLevel > 0;
                })
                .Enter(smi =>
                {
                    smi.master.GetComponent<RadiationEmitter>().SetEmitting(false);
                    // 兜底强制播放off动画
                    var anim = smi.master.GetComponent<KBatchedAnimController>();
                    if (anim != null) anim.Play("off");
                });
            // 运行状态：粒子充足+档位>0 → on动画+消耗粒子
            running.PlayAnim("on")
                .EventTransition(GameHashes.OnParticleStorageChanged, idle, smi =>
                {
                    var storage = smi.master.GetComponent<HighEnergyParticleStorage>();
                    var light = smi.master.GetComponent<CustomRadiationLight>();
                    // ✅ 任一条件不满足：粒子<200 OR 档位=0 → 切回空闲状态
                    return storage.GetAmountAvailable(GameTags.HighEnergyParticle) < 200f || light.RadiationLevel <= 0;
                })
                .Update((smi, dt) =>
                    smi.master.GetComponent<CustomRadiationLight>().ConsumeParticlePerTick(dt), UpdateRate.RENDER_1000ms)
                .Exit(smi => smi.master.GetComponent<RadiationEmitter>().SetEmitting(false));
        }
        public class Def : BaseDef
        {
        }
    }
}
