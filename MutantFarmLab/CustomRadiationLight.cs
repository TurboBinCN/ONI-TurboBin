using KSerialization;
using PeterHan.PLib.Core;
using STRINGS;
using UnityEngine;
using static LogicGateBase;

namespace MutantFarmLab
{
    public class CustomRadiationLight : KMonoBehaviour, ISingleSliderControl, ISliderControl,ISaveLoadable
    {
        //建筑配置项
        public RadiationEmitter radiationEmitter;
        private LogicPorts _logicPorts;
        public float consumerRate = 1;
        private LogicPorts HEP_RQ_LogicPort
        {
            get => _logicPorts ??= GetComponent<LogicPorts>();
        }
        private bool _logicPort_holder = false;
        public float lowParticleThreshold = 200f;
        public HighEnergyParticleStorage _particleStorage;
        private HighEnergyParticleStorage ParticleStorage
        {
            get => _particleStorage ??= GetComponent<HighEnergyParticleStorage>();
        }
        //内部配置项
        [Serialize]
        public int RadiationLevel = 0;
        protected override void OnSpawn()
        {
            base.OnSpawn();
            if (this.radiationEmitter != null)
            {
                this.radiationEmitter.SetEmitting(false);
            }
            Subscribe((int)GameHashes.OnParticleStorageChanged, OnParticleChanged);
            Trigger((int)GameHashes.OnParticleStorageChanged);


            Rotatable component = GetComponent<Rotatable>();
            bool flag = component != null && radiationEmitter != null;
            if (flag)
            {
                switch (component.GetOrientation())
                {
                    case Orientation.FlipH:
                    case Orientation.R90:
                    case Orientation.R180:
                    case Orientation.FlipV:
                    case Orientation.R270:
                        radiationEmitter.emitDirection = 90f;
                        radiationEmitter.emissionOffset = new Vector3(1f, 1f, 0f);
                        break;

                    case Orientation.Neutral:
                        radiationEmitter.emitDirection = 270f;
                        radiationEmitter.emissionOffset = new Vector3(1f, -1f, 0f);
                        break;
                }
                radiationEmitter.Refresh();
            }
            radiationEmitter.emitRads = RadiationRads();
            radiationEmitter.Refresh();
            Refresh();
        }

        protected override void OnCleanUp()
        {
            base.OnCleanUp();
            Unsubscribe((int)GameHashes.OnParticleStorageChanged);
        }
        private float ParticleConsumeAmount(float dt = 1)
        {
            return dt * RadiationLevel / 10 * consumerRate;
        }
        private float RadiationRads()
        {
            return RadiationLevel * 300;
        }
        private bool IsRunningAvailable()
        {
            if (ParticleStorage == null || radiationEmitter == null) return false;
            // 双核心条件：档位必须>0 + 粒子量必须高于阈值，缺一不可
            bool hasValidLevel = RadiationLevel > 0;
            bool hasEnoughParticle = ParticleStorage.GetAmountAvailable(GameTags.HighEnergyParticle) > ParticleConsumeAmount();
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
        }
        private void OnParticleChanged(object data)
        {
            updateLogicPortLogic();
            Refresh();
        }
        public void updateLogicPortLogic()
        {
            int highEnergyParticaleRQSignal = 0;
            float particleAmount = ParticleStorage.GetAmountAvailable(GameTags.HighEnergyParticle);
            float stopThreshold = ParticleStorage.capacity - ParticleConsumeAmount();
            const float floatTolerance = 1e-6f;

            if (particleAmount <= lowParticleThreshold + floatTolerance)
            {
                highEnergyParticaleRQSignal = 1;
                _logicPort_holder = true; 
            }
            else if (particleAmount >= stopThreshold - floatTolerance)
            {
                highEnergyParticaleRQSignal = 0;
                _logicPort_holder = false;
            }
            else
            {
                highEnergyParticaleRQSignal = _logicPort_holder ? 1 : 0;
            }

            HEP_RQ_LogicPort.SendSignal(CustomRadiationLightConfig.LOGIC_PORT_ID, highEnergyParticaleRQSignal);
        }
        public void ConsumeParticlePerTick(float dt)
        {
            if (ParticleStorage == null || radiationEmitter == null) return;
            if (RadiationLevel <= 0)
            {
                radiationEmitter.SetEmitting(false);
                Refresh();
                return;
            }
            float particleAmount = ParticleStorage.GetAmountAvailable(GameTags.HighEnergyParticle);
            if (particleAmount <= lowParticleThreshold)
            {
                radiationEmitter.SetEmitting(false);
                Refresh();
                return;
            }
            ParticleStorage.ConsumeAndGet(ParticleConsumeAmount(dt));
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
            radiationEmitter.emitRads = RadiationRads();
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
