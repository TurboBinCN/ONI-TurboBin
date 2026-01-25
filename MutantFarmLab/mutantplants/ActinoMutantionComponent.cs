using HarmonyLib;
using Klei.AI;
using PeterHan.PLib.Core;
using System;
using TUNING;
using UnityEngine;
using static LightColorMenu;
using static STRINGS.CREATURES.STATUSITEMS;

namespace MutantFarmLab.mutantplants
{
    public class ActinoMutantionComponent: KMonoBehaviour
    {
        public Color lightColor = Color.green;
        private Light2D light2D;
        private RadiationEmitter radiationEmitter;
        [MyCmpReq]
        private WiltCondition _wiltCondition;
        [MyCmpReq]
        private Growing _growing;
        //public readonly EffectorValues POSITIVE_DECOR_EFFECT = DECOR.BONUS.TIER3;
        //public readonly EffectorValues NEGATIVE_DECOR_EFFECT = DECOR.PENALTY.TIER3;
        protected override void OnSpawn()
        {
            base.OnSpawn();
            //TODO:移除光照需求组件/重置黑暗环境要求
            var illumCom = gameObject.GetComponent<IlluminationVulnerable>();
            if(illumCom != null)
            {
                illumCom.prefersDarkness = false;
                if(illumCom.LightIntensityThreshold != 0)
                {
                    PUtil.LogDebug($"[辐光菌][{illumCom.LightIntensityThreshold}] removing light requirement for " + gameObject.name);
                    gameObject.GetComponent<Modifiers>().attributes.Add(
                    new AttributeModifier(
                        Db.Get().PlantAttributes.MinLightLux.Id,
                        -illumCom.LightIntensityThreshold,
                        "ActionMutation Symbiosis"
                    ));
                }
            }
            //TODO:移除气体需求组件
            var pressureVulnerable = gameObject.GetComponent<PressureVulnerable>();
            if (pressureVulnerable != null)
            {
                pressureVulnerable.pressureWarning_High *= (1 + PlantMutationRegister.ACTINO_AIRPRESS_RANGE_MOD);
                pressureVulnerable.pressureLethal_High *= (1 + PlantMutationRegister.ACTINO_AIRPRESS_RANGE_MOD);
                pressureVulnerable.pressureWarning_Low *= Math.Abs(1 - PlantMutationRegister.ACTINO_AIRPRESS_RANGE_MOD);
                pressureVulnerable.pressureLethal_Low *= Math.Abs(1 - PlantMutationRegister.ACTINO_AIRPRESS_RANGE_MOD);
            }

            //挂载辐射源组件
            if (DlcManager.FeatureRadiationEnabled())
            {
                radiationEmitter = gameObject.AddOrGet<RadiationEmitter>();
                radiationEmitter.emitType = RadiationEmitter.RadiationEmitterType.Constant;
                radiationEmitter.radiusProportionalToRads = false;
                radiationEmitter.emitRadiusX = 6;
                radiationEmitter.emitRadiusY = radiationEmitter.emitRadiusX;
                radiationEmitter.emitRads = PlantMutationRegister.ACTINO_MIN_RADIATION;
                radiationEmitter.emissionOffset = new Vector3(0f, 0f, 0f);
                gameObject.GetComponent<RadiationEmitter>().SetEmitting(true);
            }
            //挂载光照组件
            if (lightColor != Color.black)
            {
                light2D = gameObject.AddOrGet<Light2D>();
                light2D.Color = lightColor;
                light2D.overlayColour = LIGHT2D.LIGHTBUG_OVERLAYCOLOR;
                light2D.Range = 5f;
                light2D.Angle = 0f;
                light2D.Direction = LIGHT2D.LIGHTBUG_DIRECTION;
                light2D.Offset = LIGHT2D.LIGHTBUG_OFFSET;
                light2D.shape = global::LightShape.Circle;
                light2D.drawOverlay = true;
                light2D.Lux = PlantMutationRegister.ACTINO_LIGHT_LUX;
                
                //gameObject.AddOrGet<LightSymbolTracker>().targetSymbol = "snapTo_light_locator";
            }

            //GameHashes.TooHotWarning/GameHashes.TooColdWarning/GameHashes.Wilt/GameHashes.Grow/GameHashes.WiltRecover
            Subscribe((int)GameHashes.TooHotWarning, (_)=> setActionActive(false));
            Subscribe((int)GameHashes.TooColdWarning, (_) => setActionActive(false));
            Subscribe((int)GameHashes.Grow, OnPlantGrowing);
            Subscribe((int)GameHashes.Wilt, OnPlantStatusChanged);
            Subscribe((int)GameHashes.WiltRecover, OnPlantStatusChanged);
        }

        private void OnPlantGrowing(object obj)
        {
            if(_growing != null && _growing.IsGrowing())
            {
                setActionActive(true);
            }
        }

        private void OnPlantStatusChanged(object obj)
        {
            if (_wiltCondition == null) return;
            if (_wiltCondition.IsWilting())
            {
                setActionActive(false);
            }
            else
            {
                setActionActive(true);
            }
        }
        private void setActionActive(bool active)
        {
            bool canEmmit = true;
            if (!active)
            {
                canEmmit = false;
            }
            if (light2D != null)
                light2D.enabled = canEmmit;
            if (radiationEmitter != null)
                radiationEmitter.SetEmitting(canEmmit);
        }
    }

}
