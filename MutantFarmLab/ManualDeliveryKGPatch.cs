using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using STRINGS;
using UnityEngine;
using HarmonyLib;

namespace MutantFarmLab
{
    public static class ManualDeliveryKGPatch
    {
        private static readonly EventSystem.IntraObjectHandler<ManualDeliveryKG> OnCopySettingsDelegate =
            new((component, data) => component.OnCopySettings(data));

        private static EventSystem.IntraObjectHandler<ManualDeliveryKG> OnRefreshUserMenuDelegate;

        private const string PATCH_KEY = "Patch.ManualDeliveryKG.OnCopySettings";
        private static bool _patched = false;

        public static void Patch(Harmony harmony)
        {
            if (!_patched)
            {
                _patched = true;
                OnRefreshUserMenuDelegate = Traverse.Create<ManualDeliveryKG>()
                    .Field<EventSystem.IntraObjectHandler<ManualDeliveryKG>>(nameof(OnRefreshUserMenuDelegate)).Value;
                harmony.Patch(typeof(ManualDeliveryKG), nameof(OnSpawn),
                    postfix: new HarmonyMethod(typeof(ManualDeliveryKGPatch), nameof(OnSpawn)));
                harmony.Patch(typeof(ManualDeliveryKG), nameof(OnCleanUp),
                    prefix: new HarmonyMethod(typeof(ManualDeliveryKGPatch), nameof(OnCleanUp)));
                harmony.Patch(AccessTools.Method(typeof(ManualDeliveryKG), "OnRefreshUserMenu"),
                    transpiler: new HarmonyMethod(typeof(ManualDeliveryKGPatch), nameof(Transpiler)));
            }
        }

        private static void OnSpawn(ManualDeliveryKG __instance)
        {
            if (__instance.allowPause)
            {
                if (__instance.GetComponents<ManualDeliveryKG>().ToList().IndexOf(__instance) > 0)
                    __instance.Unsubscribe((int)GameHashes.RefreshUserMenu, OnRefreshUserMenuDelegate, true);
                else
                    __instance.Subscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
            }
        }

        private static void OnCleanUp(ManualDeliveryKG __instance)
        {
            if (__instance.allowPause)
                __instance.Unsubscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate, true);
        }

        private static void OnCopySettings(this ManualDeliveryKG @this, object data)
        {
            if (@this.allowPause)
            {
                int index = @this.GetComponents<ManualDeliveryKG>().ToList().IndexOf(@this);
                var others = ((GameObject)data).GetComponents<ManualDeliveryKG>();
                if (others != null && index >= 0 && index < others.Length && others[index] != null)
                {
                    bool paused = Traverse.Create(others[index]).Field<bool>("userPaused").Value;
                    Traverse.Create(@this).Field<bool>("userPaused").Value = paused;
                    @this.Pause(paused, "OnCopySettings");
                }
            }
        }

        private static string ResolveTooltip(string tooltip, ManualDeliveryKG manualDelivery)
        {
            return $"{tooltip}\n{string.Format(BUILDING.STATUSITEMS.WAITINGFORMATERIALS.LINE_ITEM_UNITS, manualDelivery.RequestedItemTag.ProperName())}";
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            return instructions.Transpile(original, transpiler);
        }

        private static bool transpiler(ref List<CodeInstruction> instructions)
        {
            var Tooltip1 = typeof(UI.USERMENUACTIONS.MANUAL_DELIVERY)
                .GetField(nameof(UI.USERMENUACTIONS.MANUAL_DELIVERY.TOOLTIP), BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            var Tooltip2 = typeof(UI.USERMENUACTIONS.MANUAL_DELIVERY)
                .GetField(nameof(UI.USERMENUACTIONS.MANUAL_DELIVERY.TOOLTIP_OFF), BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            var Resolver = typeof(ManualDeliveryKGPatch).GetMethod(nameof(ResolveTooltip), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            bool result = false;
            if (Tooltip1 != null && Tooltip2 != null && Resolver != null)
            {
                for (int i = 0; i < instructions.Count(); i++)
                {
                    if (instructions[i].LoadsField(Tooltip1) || instructions[i].LoadsField(Tooltip2))
                    {
                        i++;
                        instructions.Insert(++i, new CodeInstruction(OpCodes.Ldarg_0));
                        instructions.Insert(++i, new CodeInstruction(OpCodes.Call, Resolver));
                        result = true;
                    }
                }
            }
            return result;
        }
    }
}