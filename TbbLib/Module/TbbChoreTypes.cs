using Database;
using HarmonyLib;
using System.Collections.Generic;

namespace TBB.He.TbbLib.Module
{
    public class TbbChoreTypes : TbbModule<TbbChoreTypes>
    {
        private List<System.Action<ChoreTypes>> _registerActions = new();
        protected override void Initialized()
        {
            Harmony.Patch(AccessTools.Constructor(typeof(ChoreTypes), new[] { typeof(ResourceSet) }),
                postfix: new HarmonyMethod(typeof(TbbChoreTypes), nameof(ChoreTypes_Constructor_Postfix)));
        }
        public TbbChoreTypes Add(System.Action<ChoreTypes> action)
        {
            Instance._registerActions.Add(action);
            return Instance;
        }
        public static void ChoreTypes_Constructor_Postfix(ChoreTypes __instance, ResourceSet parent)
        {
            if (Instance != null)
            {
                foreach (var registerAction in Instance._registerActions)
                {
                    registerAction.Invoke(__instance);
                }
            }
        }
    }
}
