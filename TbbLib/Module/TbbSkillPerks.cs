using Database;
using HarmonyLib;
using System.Collections.Generic;

namespace TBB.He.TbbLib.Module
{
    public class TbbSkillPerks : TbbModule<TbbSkillPerks>
    {
        private List<System.Action<SkillPerks>> _registerActions = new();
        protected override void Initialized()
        {
            Harmony.Patch(AccessTools.Constructor(typeof(SkillPerks), new[] { typeof(ResourceSet) }),
                postfix: new HarmonyMethod(typeof(TbbSkillPerks), nameof(ChoreTypes_Constructor_Postfix)));
        }
        public TbbSkillPerks Add(System.Action<SkillPerks> action)
        {
            Instance._registerActions.Add(action);
            return Instance;
        }
        public static void ChoreTypes_Constructor_Postfix(SkillPerks __instance, ResourceSet parent)
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
