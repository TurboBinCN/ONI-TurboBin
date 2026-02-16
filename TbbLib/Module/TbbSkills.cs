using Database;
using HarmonyLib;
using System;
using System.Collections.Generic;

namespace TBB.He.TbbLib.Module
{
    public class TbbSkills : TbbModule<TbbSkills>
    {
        private readonly List<Func<Skill>> _registerActions = new List<Func<Skill>>();
        protected override void Initialized()
        {
            Harmony.Patch(AccessTools.Constructor(typeof(Skills), new[] { typeof(ResourceSet) }),
                postfix: new HarmonyMethod(typeof(TbbSkills), nameof(Skills_Constructor_Postfix)));
        }

        public TbbSkills Add(Func<Skill> registerAction)
        {
            Instance._registerActions.Add(registerAction);
            return Instance;
        }
        public static void Skills_Constructor_Postfix(Skills __instance, ResourceSet parent)
        {
            if (Instance != null)
            {
                foreach (var registerAction in Instance._registerActions)
                {
                    Skill skillToAdd = registerAction.Invoke();
                    if (skillToAdd != null)
                    {
                        __instance.Add(skillToAdd);
                    }
                }
            }
        }
    }
}
