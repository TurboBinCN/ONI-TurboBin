using Klei.AI;
using HarmonyLib;
using System;
using UnityEngine;

namespace MutantContainmentProject.Mutanters
{
    public class SCP049_2Config : IEntityConfig, IHasDlcRestrictions
    {
        public static string ID = "MUTANTER_SCP049_2";
        public const float MASS = 100f;
        private const float WIDTH = 1f;
        private const float HEIGHT = 2f;

        public string[] GetRequiredDlcIds() => DlcManager.EXPANSION1;

        public string[] GetForbiddenDlcIds() => null;

        public GameObject CreatePrefab()
        {
            return BaseRoverConfig.BaseRover(ID, STRINGS.ENTITY.MUTANTER.MUTANTER_SCP049_2.NAME, GameTags.Robots.Models.ScoutRover, STRINGS.ENTITY.MUTANTER.MUTANTER_SCP049_2.DESCRIPTION, "scout_bot_kanim", MASS, WIDTH, HEIGHT, TUNING.ROBOTS.SCOUTBOT.CARRY_CAPACITY, TUNING.ROBOTS.SCOUTBOT.DIGGING, TUNING.ROBOTS.SCOUTBOT.CONSTRUCTION, TUNING.ROBOTS.SCOUTBOT.ATHLETICS, TUNING.ROBOTS.SCOUTBOT.HIT_POINTS, TUNING.ROBOTS.SCOUTBOT.BATTERY_CAPACITY, TUNING.ROBOTS.SCOUTBOT.BATTERY_DEPLETION_RATE, Db.Get().Amounts.InternalChemicalBattery, false);
        }

        public void OnPrefabInit(GameObject inst)
        {
            BaseRoverConfig.OnPrefabInit(inst, Db.Get().Amounts.InternalChemicalBattery);
        }

        public void OnSpawn(GameObject inst)
        {
            BaseRoverConfig.OnSpawn(inst);
            Effects effects = inst.GetComponent<Effects>();
            if ((UnityEngine.Object) inst.transform.parent == (UnityEngine.Object) null)
            {
                if (effects.HasEffect("ScoutBotCharging"))
                    effects.Remove("ScoutBotCharging");
            }
            else if (!effects.HasEffect("ScoutBotCharging"))
                effects.Add("ScoutBotCharging", false);
            inst.Subscribe(856640610, (Action<object>) (_ =>
            {
                if ((UnityEngine.Object) inst.transform.parent == (UnityEngine.Object) null)
                {
                    if (!effects.HasEffect("ScoutBotCharging"))
                        return;
                    effects.Remove("ScoutBotCharging");
                }
                else
                {
                    if (effects.HasEffect("ScoutBotCharging"))
                        return;
                    effects.Add("ScoutBotCharging", false);
                }
            }));
        }

        public string[] GetAnyRequiredDlcIds() => null;
        public string[] GetDlcIds() => DlcManager.EXPANSION1;
    }

    // Harmony Patch to fix GridRestrictionSerializer GetTagId for MUTANTER_SCP049_2
    [HarmonyPatch(typeof(GridRestrictionSerializer))]
    [HarmonyPatch("GetTagId")]
    public static class GridRestrictionSerializer_GetTagId_Patch
    {
        public static bool Prefix(Tag gameTag, ref int __result)
        {
            // Check if the tag is MUTANTER_SCP049_2
            if (gameTag.Name == "MUTANTER_SCP049_2")
            {
                // Return a default ID instead of throwing an error
                __result = 0;
                return false; // Skip the original method
            }
            return true; // Continue with the original method for other tags
        }
    }
}
