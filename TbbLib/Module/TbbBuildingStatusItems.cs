using Database;
using HarmonyLib;
using System;
using System.Collections.Generic;

namespace TBB.He.TbbLib.Module
{
    public class TbbBuildingStatusItems : TbbModule<TbbBuildingStatusItems>
    {
        private readonly List<System.Action<BuildingStatusItems>> _registerActions = new();
        protected override void Initialized()
        {
            base.Initialized();

            Harmony.Patch(AccessTools.Constructor(typeof(BuildingStatusItems), new[] { typeof(ResourceSet) }),
                postfix: new HarmonyMethod(typeof(TbbBuildingStatusItems), nameof(BuildingStatusItems_Constructor_Postfix)));
        }

        public static void BuildingStatusItems_Constructor_Postfix(BuildingStatusItems __instance)
        {
            if (Instance != null)
            {
                foreach (var registerAction in Instance._registerActions)
                {
                    registerAction.Invoke(__instance);
                }
            }
        }

        public TbbBuildingStatusItems Add(System.Action<BuildingStatusItems> registerAction)
        {
            _registerActions.Add(registerAction);
            return Instance;
        }
    }
}
