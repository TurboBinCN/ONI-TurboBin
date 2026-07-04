using KSerialization;
using MutantFarmLab.tbbLibs;
using STRINGS;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MutantFarmLab
{
    /// <summary>
    /// 辐射粒子配件核心控制器
    /// 核心Patch ： BuildLocationPatch.cs 控制建筑叠放问题
    /// </summary>
    [SerializationConfig(MemberSerialization.OptIn)]
    public class RadiationParticleAdapterController : KMonoBehaviour, ISaveLoadable, IGameObjectEffectDescriptor, ISingleSliderControl, ISliderControl
    {
        // 绑定核心组件
        private HighEnergyParticleStorage _particleStorage;
        private HighEnergyParticleStorage ParticleStorage
        {
            get => _particleStorage ??= GetComponent<HighEnergyParticleStorage>();
        }
        private RadiationEmitter _radiationEmitter;
        private LogicPorts _logicPorts;
        private LogicPorts HEP_RQ_LogicPort
        {
            get => _logicPorts ??= GetComponent<LogicPorts>();
        }
        private bool _logicPort_holder = false;
        public float lowParticleThreshold = 200f;

        private int _powerBase = 300;
        // 配置参数
        public float ParticleConsumeRate{set;get;} = 0.1f;
        public float LowParticleThreshold { set; get; } = 200f;

        // 状态缓存
        private GameObject _bindFarmTile;
        private bool _isBindFarmTileValid;
        private bool _isParticleEnough;

        private float UPDATE_INTERVAL;
        [Serialize]
        public int RadiationLevel = 1;

        public void OnSimTick(RadiationParticleAdapterStates.GameInstance smi,float dt)
        {
            UPDATE_INTERVAL = dt;
            RunCoreDetectLogic();
        }

        #region 组件生命周期（规范实现）
        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
        }
        protected override void OnCleanUp()
        {
            base.OnCleanUp();
        }
        protected override void OnSpawn()
        {
            base.OnSpawn();
            _isBindFarmTileValid = false;

            if (_radiationEmitter == null){
                _radiationEmitter = GetComponent<RadiationEmitter>();
                _radiationEmitter.emitRads = RadiationLevel * _powerBase;
            }

            Rotatable component = GetComponent<Rotatable>();
            bool flag = component != null && _radiationEmitter != null;
            if (flag)
            {
                switch (component.GetOrientation())
                {
                    case Orientation.FlipH:
                    case Orientation.R90:
                    case Orientation.R180:
                    case Orientation.FlipV:
                    case Orientation.R270:
                        _radiationEmitter.emitDirection = 270f;
                        _radiationEmitter.emissionOffset = new Vector3(0f, -2f, 0);
                        break;

                    case Orientation.Neutral:
                        _radiationEmitter.emitDirection = 90f;
                        _radiationEmitter.emissionOffset = new Vector3(0f, 2f, 0);
                        break;
                }
                _radiationEmitter.Refresh();
            }
            _radiationEmitter.SetEmitting(false);
            Subscribe(-905833192, new Action<object>(OnCopySettings));
        }

        private void OnCopySettings(object obj)
        {
            RadiationParticleAdapterController component = ((GameObject)obj).GetComponent<RadiationParticleAdapterController>();
            if (component != null)
            {
                RadiationLevel = component.RadiationLevel;
                _radiationEmitter?.Refresh();
            }
        }
        #endregion

        #region 核心业务逻辑
        private void RunCoreDetectLogic()
        {
            CheckAndValidateFarmTileBind();
            JudgeParticleEnoughState();
            ControlRadiation_Logic_ParticleConsume();
        }
        #endregion

        /// <summary>
        /// 【终极精准版】种植砖绑定校验核心函数
        /// ✅ 严格遵循规则：以自身为中心 + 仅检测上下左右1格 + 仅判定自己占据的格子
        /// ✅ 适配配件逻辑：仅在自身占据格子内绑定种植砖/水培砖，0误判、无冗余
        /// ✅ 完全匹配ObjectLayer枚举+种植砖判定规则，100%可用
        /// </summary>
        private void CheckAndValidateFarmTileBind()
        {
            // ===================== 【步骤1：缓存复用，极致性能】 =====================
            _isBindFarmTileValid = false;
            if (_bindFarmTile != null && _bindFarmTile.activeInHierarchy)
            {
                _isBindFarmTileValid = true;
                return;
            }

            int adapterCenterCell = Grid.PosToCell(gameObject); // 自身中心格子（判定基准）
            if (!Grid.IsValidCell(adapterCenterCell))
            {
                //PUtil.LogDebug($"⚠️ 种植砖绑定失败：自身中心格子无效，格子ID={adapterCenterCell}");
                return;
            }

            // ✅ 规则1：以自身为中心，仅上下左右各1格（4个正方向，无斜向）
            CellOffset[] checkOffsets = new[]
            {
                CellOffset.none,   // 自身
                new CellOffset(0, 1),   // 上格
                new CellOffset(0, -1),  // 下格
                new CellOffset(-1, 0),  // 左格
                new CellOffset(1, 0)    // 右格
            };

            foreach (var offset in checkOffsets)
            {
                int targetCell = Grid.OffsetCell(adapterCenterCell, offset);
                if (!Grid.IsValidCell(targetCell))
                {
                    continue;
                }

                GameObject cellObjectaa = Grid.Objects[targetCell, (int)ObjectLayer.FoundationTile];
                TbbDebuger.LogDebug($"🔍 检测目标格子 (Layer-FoundationTile) ={targetCell} | 目标格子对象={GetObjectNameSafe(cellObjectaa)}");
                GameObject cellObjectbb = Grid.Objects[targetCell, (int)ObjectLayer.Building];
                TbbDebuger.LogDebug($"🔍 检测目标格子 (Layer-Building) ={targetCell} | 目标格子对象={GetObjectNameSafe(cellObjectbb)}");

                GameObject cellObj = Grid.Objects[targetCell, (int)ObjectLayer.Building];
                bool isSelfOccupyCell = cellObj != null && cellObj == this.gameObject;
                if (!isSelfOccupyCell)
                {
                    TbbDebuger.LogDebug($"⚠️ 检测目标格子非自身占据，跳过 | 目标格子ID={targetCell} | 目标格子对象={GetObjectNameSafe(cellObj)}");
                    continue;
                }

                // ✅ 满足所有规则：自身中心±1格 + 是自己占据的格子 → 检测种植砖
                GameObject farmTileObj = Grid.Objects[targetCell, (int)ObjectLayer.FoundationTile];
                if (farmTileObj == null){ 
                    TbbDebuger.LogDebug($"⚠️ 检测目标格子无种植砖，跳过 | 目标格子ID={targetCell}");
                    continue;
                }

                // ✅ 精准判定种植砖，命中即绑定
                if (IsTargetFarmTile(farmTileObj))
                {
                    _bindFarmTile = farmTileObj;
                    _isBindFarmTileValid = true;
                    TbbDebuger.LogDebug($"✅ 绑定成功✅ 自身占据格子={targetCell} | 种植砖={GetObjectNameSafe(farmTileObj)}");
                    return;
                }
            }

            TbbDebuger.LogDebug($"⚠️ 绑定失败：自身中心上下左右1格范围内，无自身占据且包含种植砖的格子");
        }

        #region 核心辅助方法（精准无错，可复用）
        private string GetObjectNameSafe(GameObject go)
        {
            if (go == null)
                return "null";
            try
            {
                return go.name;
            }
            catch (NullReferenceException)
            {
                return "[destroyed]";
            }
        }

        private bool IsTargetFarmTile(GameObject targetObj)
        {
            if (targetObj == null)
                return false;

            KPrefabID prefabId = targetObj.GetComponent<KPrefabID>();
            TbbDebuger.LogDebug($"🔍 判定目标对象={GetObjectNameSafe(targetObj)} | prefabId={prefabId?.HasTag(GameTags.CodexCategories.FarmBuilding).ToString() ?? "null"}");
            if (prefabId != null && prefabId.HasTag(GameTags.CodexCategories.FarmBuilding)) return true;

            string objName = GetObjectNameSafe(targetObj);
            if (objName.Contains("FarmTile")) return true;
            return objName.Contains("Hydroponic");
        }
        #endregion

        
#region 粒子状态判定 + 联动控制
        private void JudgeParticleEnoughState()
        {
            _isParticleEnough = ParticleStorage != null && ParticleStorage.GetAmountAvailable(GameTags.HighEnergyParticle) > 0;
        }

        private void ControlRadiation_Logic_ParticleConsume()
        {
            bool canEmitRadiation = _isBindFarmTileValid && _isParticleEnough;
            if (canEmitRadiation && ParticleStorage != null)
            {
                ParticleStorage.ConsumeAndGet(ParticleConsumeAmount());
            }
            JudgeParticleEnoughState();
            _radiationEmitter.SetEmitting(_isParticleEnough);

            updateLogicPortLogic();
        }
        private float ParticleConsumeAmount(float dt = 1)
        {
            return ParticleConsumeRate * RadiationLevel * dt;
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

            HEP_RQ_LogicPort.SendSignal(RadiationParticleAdapterConfig.HEP_RQ_LOGIC_PORT_ID, highEnergyParticaleRQSignal);
        }
        #endregion
        #region 侧边栏提示（核心）
        public List<Descriptor> GetDescriptors(GameObject go)
        {
            List<Descriptor> descriptors = new List<Descriptor>();

            // 未检测到种植砖 → 显示【Requirement】类型警告（原生黄色警告样式）
            if (!_isBindFarmTileValid)
            {
                descriptors.Add(new Descriptor(
                    STRINGS.BUILDINGS.PREFABS.RADIATIONPARTICLEADAPTER.WARNING_NO_FARMTILE,
                    STRINGS.BUILDINGS.PREFABS.RADIATIONPARTICLEADAPTER.WARNING_NO_FARMTILE_TOOLTIP,
                    Descriptor.DescriptorType.Requirement, 
                    false
                ));
                
            }
            // 粒子不足 → 显示【Information】类型提示（原生蓝色信息样式）
            else if (!_isParticleEnough)
            {
                descriptors.Add(new Descriptor(
                    STRINGS.BUILDINGS.PREFABS.RADIATIONPARTICLEADAPTER.WARNING_LOW_PARTICLE,
                    STRINGS.BUILDINGS.PREFABS.RADIATIONPARTICLEADAPTER.WARNING_LOW_PARTICLE_TOOLTIP,
                    Descriptor.DescriptorType.Information, 
                    false
                ));
            }
            else
            {
                descriptors.Add(new Descriptor(
                    string.Format(STRINGS.UI.STATUSITEMS.CONSUMERATEPARTICLES.NAME, ParticleConsumeAmount()),
                    string.Format(STRINGS.UI.STATUSITEMS.CONSUMERATEPARTICLES.TOOLTIP, ParticleConsumeAmount()),
                    Descriptor.DescriptorType.Requirement,
                    false
                ));
            }

            return descriptors;
        }

        //SliderControl
        public string SliderTitleKey => STRINGS.UI.UISIDESCREENS.SLIDERCONTROL.TITLE;

        public string SliderUnits => STRINGS.UI.UNITSUFFIXES.RADIATION.RADLEVEL;
        public int SliderDecimalPlaces(int index) => 0;

        public float GetSliderMin(int index) => 0;

        public float GetSliderMax(int index) => 5f;

        public float GetSliderValue(int index) => RadiationLevel;
        public string GetSliderTooltipKey(int index) => STRINGS.UI.UISIDESCREENS.SLIDERCONTROL.TOOLTIP;
        public string GetSliderTooltip(int index) => STRINGS.UI.UISIDESCREENS.SLIDERCONTROL.TOOLTIP;

        public void SetSliderValue(float percent, int index)
        {
            RadiationLevel = (int)percent;
            _radiationEmitter.emitRads = RadiationLevel * 300;
            _radiationEmitter.Refresh();
        }

        #endregion
    }
    public class RadiationParticleAdapterStates : GameStateMachine<RadiationParticleAdapterStates, RadiationParticleAdapterStates.StatesInstance, IStateMachineTarget, RadiationParticleAdapterStates.Def> {

        public State root;
        public override void InitializeStates(out BaseState default_state)
        {
            default_state = root;
            root
                .Enter(smi => smi.master.GetComponent<RadiationParticleAdapterController>())
                .Update((smi,dt) => smi.master.GetComponent<RadiationParticleAdapterController>().OnSimTick(smi,dt), UpdateRate.SIM_1000ms);
        }
        public class StatesInstance : GameInstance
        {
            public StatesInstance(IStateMachineTarget master, Def def) : base(master, def)
            {
            }
        }
        public class Def : BaseDef { }
    }
}