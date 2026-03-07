using Database;
using HarmonyLib;
using System.Collections.Generic;
using TBB.He.TbbLib.Utils;

namespace TBB.He.TbbLib.Module
{
    public class TbbDuplicantsAttributes : TbbModule<TbbDuplicantsAttributes>
    {
        private readonly List<System.Action<Attributes>> _registerActions = new();
        private readonly List<string> _attributeIds = new();
        protected override void Initialized()
        {
            Harmony.Patch(AccessTools.Constructor(typeof(Attributes), new[] { typeof(ResourceSet) }),
                postfix: new HarmonyMethod(typeof(TbbDuplicantsAttributes), nameof(Attributes_Constructor_Postfix)) { priority = 9999 });

            Harmony.Patch(typeof(MinionStartingStats), "GenerateAttributes",
                prefix: new HarmonyMethod(typeof(TbbDuplicantsAttributes), nameof(MinionStartingStats_GenerateAttributes_Prefix)) { priority = 9999 });
        }

        public static void MinionStartingStats_GenerateAttributes_Prefix(MinionStartingStats __instance, int pointsDelta, List<ChoreGroup> disabled_chore_groups)
        {
            if (Instance == null) return;
            foreach (var attributeid in Instance._attributeIds)
            {
                if (!__instance.StartingLevels.ContainsKey(attributeid))
                {
                    __instance.StartingLevels[attributeid] = 0;
                }
            }
            return;
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

        public TbbDuplicantsAttributes Add(System.Action<Attributes> registerAction, string attributeId)
        {
            _registerActions.Add(registerAction);
            _attributeIds.Add(attributeId);
            return Instance;
        }
    }
}
