using Database;
using HarmonyLib;
using System.Collections.Generic;

namespace TBB.He.TbbLib.Module
{
    public class TbbStories : TbbModule<TbbStories>
    {
        private readonly List<System.Action<Stories>> _registerActions = new();
        protected override void Initialized()
        {
            Harmony.Patch(AccessTools.Constructor(typeof(Stories), new[] { typeof(ResourceSet) }),
                postfix: new HarmonyMethod(typeof(TbbStories), nameof(Stories_Constructor_Postfix)));
        }

        private static void Stories_Constructor_Postfix(Stories __instance)
        {
            if (Instance != null)
            {
                foreach (var registerAction in Instance._registerActions)
                {
                    registerAction.Invoke(__instance);
                }
            }
        }
        public TbbStories Add(System.Action<Stories> registerAction)
        {
            _registerActions.Add(registerAction);
            return Instance;
        }
    }
}
