using Database;
using HarmonyLib;
using System;
using System.Collections.Generic;
using TBB.He.TbbLib.Module;

namespace TbbLib.Module
{
    public class TbbSkillGroups : TbbModule<TbbSkillGroups>
    {
        private readonly List<Func<SkillGroup>> _registerActions = new List<Func<SkillGroup>>();
        protected override void Initialized()
        {
            Harmony.Patch(AccessTools.Constructor(typeof(SkillGroups), new[] { typeof(ResourceSet) }),
                postfix: new HarmonyMethod(typeof(TbbSkillGroups), nameof(SkillGroups_Constructor_Postfix)));
        }
        private static void SkillGroups_Constructor_Postfix(SkillGroups __instance, ResourceSet parent)
        {
            if (Instance != null)
            {
                foreach (var registerAction in Instance._registerActions)
                {
                    SkillGroup skillGroupToAdd = registerAction.Invoke();
                    if (skillGroupToAdd != null)
                    {
                        __instance.Add(skillGroupToAdd);
                    }
                }
            }
        }

        public TbbSkillGroups Add(Func<SkillGroup> registerAction)
        {
            Instance._registerActions.Add(registerAction);
            return Instance;
        }
    }
}
