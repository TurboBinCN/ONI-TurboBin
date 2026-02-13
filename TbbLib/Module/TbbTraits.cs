using HarmonyLib;
using System.Collections.Generic;
using TBB.He.TbbLib.Module;
using TBB.He.TbbLib.Utils;

namespace TbbLib.Module
{
    public class TbbTraits : TbbModule<TbbTraits>
    {
        private readonly List<System.Action> _registerActions = new();
        protected override void Initialized()
        {
            Harmony.Patch(typeof(Db), "Initialize",
                postfix: new HarmonyMethod(typeof(TbbTraits), nameof(Db_Initialize_Postfix)));
        }
        private static void Db_Initialize_Postfix()
        {
            if (Instance != null)
            {
                foreach (var registerAction in Instance._registerActions)
                {
                    registerAction.Invoke();
                }
            }
        }

        public TbbTraits Add(System.Action registerAction)
        {
            Instance._registerActions.Add(registerAction);
            return Instance;
        }
    }
}
