using HarmonyLib;
using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;
using TBB.He.TbbLib.Module;
using TBB.He.TbbLib.Utils;
using UnityEngine;

namespace TbbLib.UI
{
    public class TbbColorSet : TbbModule<TbbColorSet>
    {
        private Dictionary<string, Color32> colors = new();
        protected override void Initialized()
        {
            Harmony.Patch(typeof(GlobalAssets), "OnPrefabInit",
                postfix: new HarmonyMethod(typeof(TbbColorSet), nameof(GloablAssets_OnPrefabInit_Postfix)));
        }
        public TbbColorSet Add(string name, Color32 color)
        {
            colors.Add(name, color);
            return this;
        }
        public static void GloablAssets_OnPrefabInit_Postfix(GlobalAssets __instance)
        {
            __instance.colorSet.RefreshLookup();
            Dictionary<string, Color32> namedLookup = (Dictionary<string, Color32>)TbbHarmonyExtension.GetField(__instance.colorSet, "namedLookup");

            foreach (var color in Instance.colors)
            {
                namedLookup.Add(color.Key, color.Value);
            }
        }
    }
}
