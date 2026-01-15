using HarmonyLib;
using PeterHan.PLib.Core;
using TUNING;
using UnityEngine;
using static LightColorMenu;

namespace MutantFarmLab.mutantplants
{
    public class ActinoMutantionComponent: KMonoBehaviour
    {
        public Color lightColor = Color.green;
        protected override void OnSpawn()
        {
            base.OnSpawn();
            //TODO:移除光照需求组件/重置黑暗环境要求
            var illumCom = gameObject.GetComponent<IlluminationVulnerable>();
            if(illumCom != null)
            {
                DestroyImmediate(illumCom);
            }
            //TODO:移除气体需求组件
            var gasCom = gameObject.GetComponent<PressureVulnerable>();
            if(gasCom != null)
            {
                DestroyImmediate(gasCom);
            }

            //挂载辐射源组件
            if (DlcManager.FeatureRadiationEnabled())
            {
                RadiationEmitter radiationEmitter = gameObject.AddOrGet<RadiationEmitter>();
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
                Light2D light2D = gameObject.AddOrGet<Light2D>();
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
        }
    }
    //给每个变异株生成时确保挂载组件
    [HarmonyPatch(typeof(MutantPlant), "OnSpawn")]
    public static class EnsureComponentOnSpawn
    {
        [HarmonyPostfix]
        public static void Postfix(MutantPlant __instance)
        {
            if (__instance.gameObject.HasTag(GameTags.Plant) && __instance.MutationIDs?.Contains(PlantMutationRegister.ACTINO_MUT_ID) == true)
            {
                __instance.gameObject.AddOrGet<ActinoMutantionComponent>();
            }
        }
    }
}
