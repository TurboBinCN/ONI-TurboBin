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
        private int _powerLevel = 1;
        private float _consumRate = 0.01f;
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
            _radiationEmitter.emitRads = _powerLevel * _powerBase;
            _radiationEmitter.SetEmitting(false);
        }
        protected override void OnCleanUp()
        {
            base.OnCleanUp();
        }

        public void refresh()
        {
            _radiationEmitter.emitRads = _powerLevel * _powerBase;
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
            if(_storage != null) {
                return _storage.GetMassAvailable(RadiationFarmTileConfig.EnergySource) > 0; 
            }
            return false;
        }
        public void updatePerTick(RadiationFarmTileStates.StatesInstance smi, float dt)
        {
            
            if (canEmitter())
            {
                var uraniumOre = _storage.FindFirst(RadiationFarmTileConfig.EnergySource);
                _storage.ConsumeIgnoringDisease(uraniumOre.PrefabID(), _powerLevel * dt * _consumRate);
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