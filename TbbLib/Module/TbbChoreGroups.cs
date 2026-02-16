using Database;
using HarmonyLib;
using System.Collections.Generic;

namespace TBB.He.TbbLib.Module
{
    public class TbbChoreGroups : TbbModule<TbbChoreGroups>
    {
        private List<System.Action<ChoreGroups>> _registerActions = new();
        protected override void Initialized()
        {
            Harmony.Patch(AccessTools.Constructor(typeof(ChoreGroups), new[] { typeof(ResourceSet) }),
                postfix: new HarmonyMethod(typeof(TbbChoreGroups), nameof(ChoreTypes_Constructor_Postfix)));
        }
        public TbbChoreGroups Add(System.Action<ChoreGroups> action)
        {
            Instance._registerActions.Add(action);
            return Instance;
        }
        public static void ChoreTypes_Constructor_Postfix(ChoreGroups __instance, ResourceSet parent)
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
