using KSerialization;
using PeterHan.PLib.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using static FetchChore;
using static StructureTemperaturePayload;

namespace MutantFarmLab
{
    public class RadiationFarmTile : KMonoBehaviour, ISaveLoadable, ISliderControl, IGameObjectEffectDescriptor, ISingleSliderControl
    {
        public Storage _storage;
        private RadiationEmitter _radiationEmitter;
        [Serialize]
        private int _powerLevel = 1;
        private float _consumRate = 0.001f;
        private int _powerBase = 300;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();

            
        }
        protected override void OnSpawn()
        {
            base.OnSpawn();
            var allStorages = GetComponentsInChildren<Storage>(true); // true：包含非激活物体
            foreach (var storage in allStorages)
            {
                if (storage.name == RadiationFarmTileConfig.RadiationStorageName)
                {
                    _storage = storage;
                    break;
                }
            }
            
            _radiationEmitter = GetComponent<RadiationEmitter>();
            _radiationEmitter.emitRads = EmittedRads();
            _radiationEmitter.SetEmitting(false);

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
                        _radiationEmitter.emissionOffset = new Vector3(0f, -1f, 0);
                        break;

                    case Orientation.Neutral:
                        _radiationEmitter.emitDirection = 90f;
                        _radiationEmitter.emissionOffset = new Vector3(0f, 1f, 0);
                        break;
                }
                _radiationEmitter.Refresh();
            }
        }
        private float EmittedRads()
        {
            return _powerLevel * _powerBase;
        }
        protected override void OnCleanUp()
        {
            base.OnCleanUp();
        }

        public void refresh()
        {
            _radiationEmitter.emitRads = EmittedRads();
            if(_radiationEmitter.isActiveAndEnabled) _radiationEmitter.Refresh();
            if (canEmitter() && !_radiationEmitter.isActiveAndEnabled)
            {
                _radiationEmitter.SetEmitting(true);
            }
            else
            {
                _radiationEmitter.SetEmitting(false);
            }
        }
        public bool canEmitter()
        {
            if(_storage != null && _powerLevel > 0) {
                return _storage.GetMassAvailable(RadiationFarmTileConfig.EnergySource) > 0; 
            }
            return false;
        }
        private float ConsumAmount(float dt)
        {
            return _powerLevel * dt * _consumRate;
        }
        public void updatePerTick(RadiationFarmTileStates.StatesInstance smi, float dt)
        {
            
            if (canEmitter())
            {
                _storage.ConsumeIgnoringDisease(RadiationFarmTileConfig.EnergySource, ConsumAmount(dt));
                _radiationEmitter.SetEmitting(true);
                return;
            }
            _radiationEmitter.SetEmitting(false) ;
        }
        public string SliderTitleKey => STRINGS.UI.UNITSUFFIXES.RADIATION.SETTING_RAD_LEVEL;

        public string SliderUnits => STRINGS.UI.UNITSUFFIXES.RADIATION.RADLEVEL;

        public float GetSliderMax(int index) => 5f;
        public float GetSliderMin(int index) => 0;

        public string GetSliderTooltip(int index) => "";

        public string GetSliderTooltipKey(int index) => "";
        public float GetSliderValue(int index) => _powerLevel;

        public void SetSliderValue(float percent, int index)
        {
            _powerLevel = (int)percent;
            refresh();
        }

        public int SliderDecimalPlaces(int index) => 0;

        public List<Descriptor> GetDescriptors(GameObject go)
        {
            List<Descriptor> descriptors = new List<Descriptor>();
            if (!canEmitter())
            {
                descriptors.Add(new Descriptor(
                    STRINGS.BUILDINGS.PREFABS.RADIATIONFARMTILE.LOW_URANIUMORE,
                    STRINGS.BUILDINGS.PREFABS.RADIATIONFARMTILE.LOW_URANIUMORE_TOOLTIP,
                    Descriptor.DescriptorType.Requirement,
                    false
                ));
            }
            else
            {
                descriptors.Add(new Descriptor(
                    string.Format(STRINGS.UI.STATUSITEMS.CONSUMERATEURANIUMORE.NAME, ConsumAmount(1f) * 1000),
                    string.Format(STRINGS.UI.STATUSITEMS.CONSUMERATEURANIUMORE.TOOLTIP, ConsumAmount(1f) * 1000),
                    Descriptor.DescriptorType.Effect,
                    false
                ));
            }

                return descriptors;
        }
    }
    public class RadiationFarmTileStates : GameStateMachine<RadiationFarmTileStates, RadiationFarmTileStates.StatesInstance, IStateMachineTarget, RadiationFarmTileStates.Def>
    {
        public State idle;    // 无铀矿-闲置状态
        public State running; // 有铀矿-运行状态

        public override void InitializeStates(out BaseState default_state)
        {
            default_state = idle;

            idle
                .Enter(smi =>
                {
                    if (smi.radiationEmitter != null)
                        smi.radiationEmitter.SetEmitting(false);
                })
                .Transition(running, smi => smi.radiationFarmTile.canEmitter(), UpdateRate.SIM_1000ms);
            running
                .Enter(smi =>
                {
                    if (smi.radiationEmitter != null)
                        smi.radiationEmitter.SetEmitting(true);
                })
                .Update((smi, dt) => smi.radiationFarmTile.updatePerTick(smi,dt), UpdateRate.SIM_1000ms)
                .Transition(idle, smi => !smi.radiationFarmTile.canEmitter(), UpdateRate.SIM_1000ms)
                .Exit(smi =>
                {
                    if (smi.radiationEmitter != null)
                        smi.radiationEmitter.SetEmitting(false);
                });
        }

        #region ===== 状态机实例类（核心修复：根除所有引用错误） =====
        public class StatesInstance : GameInstance
        {
            public RadiationFarmTile radiationFarmTile;
            public RadiationEmitter radiationEmitter;

            public StatesInstance(IStateMachineTarget master, Def def) : base(master, def)
            {
                radiationFarmTile = master.GetComponent<RadiationFarmTile>();
                radiationEmitter = radiationFarmTile.GetComponentInChildren<RadiationEmitter>();
            }

            #endregion
        }

        public class Def : BaseDef { }
    }
}