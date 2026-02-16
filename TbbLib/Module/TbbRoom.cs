using HarmonyLib;
using System.Collections.Generic;
using TBB.He.TbbLib.Utils;

namespace TBB.He.TbbLib.Module
{
    public class TbbRoom : TbbModule<TbbRoom>
    {
        private readonly List<System.Action> _registerActions = new List<System.Action>();
        protected override void Initialized()
        {
            Harmony.Patch(typeof(Db), "Initialize",
                postfix: new HarmonyMethod(typeof(TbbRoom), nameof(Db_Initialize_Postfix)));
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

        public TbbRoom Add(System.Action registerAction)
        {
            _registerActions.Add(registerAction);
            return this;
        }
    }
}
