using HarmonyLib;
using System.Collections.Generic;
using TBB.He.TbbLib.Utils;

namespace TBB.He.TbbLib.Module
{
    public class TbbEffect : TbbModule<TbbEffect>
    {
        private readonly List<System.Action> _registerActions = new();
        protected override void Initialized()
        {
            base.Initialized();

            Harmony.Patch(typeof(Db), "Initialize",
                postfix: new HarmonyMethod(typeof(TbbEffect), nameof(Db_Initialize_Postfix)));
        }
        public TbbEffect Add(System.Action registerAction)
        {
            _registerActions.Add(registerAction);
            return Instance;
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
    }
}
