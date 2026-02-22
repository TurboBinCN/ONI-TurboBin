using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace MutantContainmentProject.FunctionPatches
{
    public static class MiningNeutroniumSetting
    {
        public static float NeutroniumDropMultiplier { get; set; } = 100f;
        public static float NeutroniumDropAmount { get; set; } = 0.001f;
    }
    public static class CodeInstructionExtensions
    {
        /// <summary>Replaces neutronium's ID with 0 in a list of IL instructions.</summary>
        /// <remarks>
        /// The game directly checks for neutronium in various methods.
        /// This method can be used in transpiler patches to replace neutronium's ID in lookups with a 0,
        /// bypassing the game's "if neutronium then can't do anything" limits.  
        /// </remarks>
        public static IEnumerable<CodeInstruction> NeutroniumToObsidian(
          this IEnumerable<CodeInstruction> instructions
        )
        {
            return instructions.Select(instruction =>
            {
                // Whenever the method tries to do anything with Neutronium ID, substitute it with 0
                if (instruction.opcode == OpCodes.Ldc_I4 && instruction.operand is (Int32)SimHashes.Unobtanium)
                {
                    instruction.operand = 0;
                }
                return instruction;
            });
        }
    }
    public static class ElementExtensions
    {
        public static Boolean IsNeutronium(this Element e) =>
          e.id == SimHashes.Unobtanium;

    }
    [HarmonyPatch(typeof(Diggable), nameof(Diggable.Undiggable))]
    public class Diggable_Undiggable
    {
        public static void Postfix(Element e, ref Boolean __result)
        {
            if (e.IsNeutronium())
                __result = false;
        }
    }


    [HarmonyPatch(typeof(Diggable), nameof(Diggable.GetApproximateDigTime))]
    class Diggable_GetApproximateDigTime_Patches
    {
        static void Prefix(Int32 cell)
        {

            if (Grid.Element[cell].id != SimHashes.Unobtanium) return;

            //"Dig time for cell {cell} (mass: {Grid.Mass[cell]}) not calculated yet, " +
            //"setting hardness to that of Obsidian.");
            Grid.Element[cell].hardness = ElementLoader.FindElementByHash(SimHashes.Obsidian).hardness;
        }

    }

    [HarmonyPatch(
      declaringType: typeof(BuildingDef),
      methodName: "IsAreaClear",
      argumentTypes: new Type[] {
            typeof(GameObject), typeof(Int32), typeof(Orientation), typeof(ObjectLayer),
            typeof(ObjectLayer), typeof(Boolean), typeof(Boolean), typeof(String), typeof(Boolean)
      },
      argumentVariations: new ArgumentType[] {
            ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal,
            ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal
      }
    )]
    public static class BuildingDef_IsAreaClear_Patches
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.NeutroniumToObsidian();
        }
    }

    [HarmonyPatch(declaringType: typeof(BuildingDef), methodName: "IsAreaValid")]
    public static class IsAreaValid
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.NeutroniumToObsidian();
        }
    }
    [HarmonyPatch(typeof(Diggable), "OnSpawn")]
    class Diggable_OnSpawn_Patches
    {
        static void Prefix(ref Diggable __instance)
        {
            if (Grid.Element[Grid.PosToCell(__instance.gameObject)].id != SimHashes.Unobtanium) return;

            Grid.Element[Grid.PosToCell(__instance.gameObject)].hardness = ElementLoader.FindElementByHash(SimHashes.Obsidian).hardness;
        }


        static void Postfix(ref Diggable __instance)
        {
            if (Grid.Element[Grid.PosToCell(__instance.gameObject)].id != SimHashes.Unobtanium) return;

            __instance.SetWorkTime(Diggable.GetApproximateDigTime(Grid.PosToCell(__instance)));
            __instance.WorkTimeRemaining = __instance.workTime;
            Grid.Element[Grid.PosToCell(__instance.gameObject)].hardness = ElementLoader.FindElementByHash(SimHashes.Obsidian).hardness;
        }


        [HarmonyPatch(typeof(Diggable), "UpdateColor"), HarmonyPostfix]
        static void UpdateColor(ref Diggable __instance, ref HashedString ___multitoolContext)
        {
            if (Grid.Element[Grid.PosToCell(__instance.gameObject)].id != SimHashes.Unobtanium) return;

            return;
        }
        [HarmonyPatch(typeof(WorldDamage), nameof(WorldDamage.OnDigComplete)), HarmonyPrefix]
        static void OnDigComplete(ref Single mass, ref UInt16 element_idx)
        {
            if (!ElementLoader.elements[element_idx].IsNeutronium()) return;

            mass = MiningNeutroniumSetting.NeutroniumDropMultiplier * mass * MiningNeutroniumSetting.NeutroniumDropAmount;
        }
    }
}
