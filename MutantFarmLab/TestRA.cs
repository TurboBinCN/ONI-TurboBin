using System;
using PeterHan.PLib.Core;
using STRINGS;
namespace MutantFarmLab
{

    public class TestRA : KMonoBehaviour, ISingleSliderControl, ISliderControl
    {
        HighEnergyParticleStorage particleStorage;
        private int RadiationLevel = 0;
        public float lowParticleThreshold;
        protected override void OnSpawn()
        {
            base.OnSpawn();
            if (this.radiationEmitter != null)
            {
                this.radiationEmitter.SetEmitting(true);
            }
            particleStorage = GetComponent<HighEnergyParticleStorage>();
            Subscribe((int)GameHashes.OnParticleStorageChanged, OnParticleChangedDelegate);
            Refresh();
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
        public string SliderTitleKey
        {
            get
            {
                return BUILDINGS.PREFABS.DEVRADIATIONGENERATOR.NAME;
            }
        }
        public void OnParticleChangedDelegate(Object data)
        {
            
        }
        public string SliderUnits
        {
            get
            {
                return UI.UNITSUFFIXES.RADIATION.RADS;
            }
        }

        // Token: 0x06002EE6 RID: 12006 RVA: 0x0010EC27 File Offset: 0x0010CE27
        public float GetSliderMax(int index)
        {
            return 5f;
        }

        // Token: 0x06002EE7 RID: 12007 RVA: 0x0010EC2E File Offset: 0x0010CE2E
        public float GetSliderMin(int index)
        {
            return 0f;
        }

        // Token: 0x06002EE8 RID: 12008 RVA: 0x0010EC35 File Offset: 0x0010CE35
        public string GetSliderTooltip(int index)
        {
            return "";
        }

        public string GetSliderTooltipKey(int index)
        {
            return "";
        }

        public float GetSliderValue(int index)
        {
            return RadiationLevel;
        }

        public void SetSliderValue(float value, int index)
        {
            RadiationLevel = (int) value;
            this.radiationEmitter.emitRads = RadiationLevel * 300;
            //this.radiationEmitter.emitRadiusX = (short) RadiationLevel;
            //this.radiationEmitter.emitRadiusY = (short) RadiationLevel;
            this.radiationEmitter.Refresh();
        }

        // Token: 0x06002EEC RID: 12012 RVA: 0x0010EC69 File Offset: 0x0010CE69
        public int SliderDecimalPlaces(int index)
        {
            return 0;
        }

        // Token: 0x04001BC4 RID: 7108
        [MyCmpReq]
        private RadiationEmitter radiationEmitter;
    }

}
