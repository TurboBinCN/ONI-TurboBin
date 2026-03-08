using HarmonyLib;
using System;
using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;
using TBB.He.TbbLib.Module;
using UnityEngine;

namespace TBB.He.TbbLib.SingleToneInstance
{
    public class TbbSingleTone : TbbModule<TbbSingleTone>
    {
        private List<Type> _singleTones = new();
        protected override void Initialized()
        {
            Harmony.Patch(AccessTools.Method(typeof(Game), "OnSpawn"),
                postfix: new HarmonyMethod(typeof(TbbSingleTone), nameof(Game_OnSpawn_Postfix)));
        }

        public static void Game_OnSpawn_Postfix()
        {

            TbbDebuger.LogDebug($"[TbbSingleTone] 注册Mod全局单例");
            if (Instance == null) return;
            GameObject ModSingletonManagerGameObject = new GameObject("ModSingletonManager");
            ModSingletonManagerGameObject.SetActive(true);
            foreach (Type t in Instance._singleTones)
            {
                if (ModSingletonManagerGameObject.GetComponent(t) == null)
                {
                    TbbDebuger.LogDebug($"[TbbSingleTone] 注册单例:[{t.FullName}]");
                    ModSingletonManagerGameObject.AddComponent(t);
                }
            }
        }
        public TbbSingleTone Add<TSingleToneComponent>()
            where TSingleToneComponent : KMonoBehaviour
        {
            if (Instance != null)
            {
                Instance._singleTones.Add(typeof(TSingleToneComponent));
            }
            return Instance;
        }

    }
}
