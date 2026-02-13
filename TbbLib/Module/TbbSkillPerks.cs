using Database;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TBB.He.TbbLib.Module;

namespace TbbLib.Module
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
