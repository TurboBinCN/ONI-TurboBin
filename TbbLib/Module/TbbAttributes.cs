using Database;
using HarmonyLib;
using System.Collections.Generic;
using TBB.He.TbbLib.Module;

namespace TbbLib.Module
{
    public class TbbAttributes : TbbModule<TbbAttributes>
    {
        private readonly List<System.Action<Attributes>> _registerActions = new();
        protected override void Initialized()
        {
            Harmony.Patch(AccessTools.Constructor(typeof(Attributes), new[] { typeof(ResourceSet) }),
                postfix: new HarmonyMethod(typeof(TbbAttributes), nameof(Attributes_Constructor_Postfix)));
        }
        private static void Attributes_Constructor_Postfix(Attributes __instance)
        {
            if (Instance != null)
            {
                foreach (var registerAction in Instance._registerActions)
                {
                    registerAction.Invoke(__instance);
                }
            }
        }

        public TbbAttributes Add(System.Action<Attributes> registerAction)
        {
            _registerActions.Add(registerAction);
            return Instance;
        }
    }
}
