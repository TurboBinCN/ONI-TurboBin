using Database;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace TBB.He.TbbLib.Module
{
    public class TbbAttributeConverters : TbbModule<TbbAttributeConverters>
    {
        private readonly List<System.Action<AttributeConverters>> _registerActions = new();

        protected override void Initialized()
        {
            Debug.Log("[TbbAttributeConverters] Initialized method called");
            Harmony.Patch(
                AccessTools.Constructor(typeof(AttributeConverters)),
                postfix: new HarmonyMethod(typeof(TbbAttributeConverters), nameof(AttributeConverters_Constructor_Postfix))
            );
            Debug.Log("[TbbAttributeConverters] Harmony patch registered");
        }

        private static void AttributeConverters_Constructor_Postfix(AttributeConverters __instance)
        {
            Debug.Log("[TbbAttributeConverters] AttributeConverters_Constructor_Postfix called");
            if (Instance != null)
            {
                Debug.Log("[TbbAttributeConverters] Instance found, executing register actions count: " + Instance._registerActions.Count);
                foreach (var registerAction in Instance._registerActions)
                {
                    Debug.Log("[TbbAttributeConverters] Executing register action");
                    registerAction.Invoke(__instance);
                }
            }
            else
            {
                Debug.Log("[TbbAttributeConverters] Instance is null");
            }
        }

        public TbbAttributeConverters Add(System.Action<AttributeConverters> registerAction)
        {
            Debug.Log("[TbbAttributeConverters] Add method called, adding register action");
            _registerActions.Add(registerAction);
            Debug.Log("[TbbAttributeConverters] Register actions count: " + _registerActions.Count);
            return Instance;
        }
    }
}