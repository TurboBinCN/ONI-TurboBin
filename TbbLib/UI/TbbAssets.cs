using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using TBB.He.TbbLib.Debuger;
using TBB.He.TbbLib.Module;
using TBB.He.TbbLib.Utils;
using UnityEngine;

namespace TBB.He.TbbLib.UI
{
    public class TbbAssets : TbbModule<TbbAssets>
    {
        private Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        private Dictionary<string, Sprite> tintedSprites = new Dictionary<string, Sprite>();

        public TbbAssets AddSprite(string key, Sprite sprite)
        {
            sprites.Add(key, sprite);
            return this;
        }

        public TbbAssets AddSprite(Sprite sprite)
        {
            sprites.Add(sprite.name, sprite);
            return this;
        }

        public TbbAssets AddTintedSprite(Sprite sprite)
        {
            tintedSprites.Add(sprite.name, sprite);
            return this;
        }
        public TbbAssets AddTintedSprite(string name, Sprite sprite)
        {
            tintedSprites.Add(name, sprite);
            return this;
        }

        public TbbAssets AddStatusItemIcon(string name, Sprite sprite)
        {
            tintedSprites.Add(name, sprite);
            return this;
        }
        protected override void Initialized()
        {
            Harmony.Patch(typeof(Assets), "OnPrefabInit",
                postfix: new HarmonyMethod(typeof(TbbAssets), nameof(Assets_OnPrefabInit_Postfix)));
        }

        public static void Assets_OnPrefabInit_Postfix()
        {
            if (Instance == null) return;
            foreach (var kv in Instance.sprites)
            {
                Assets.Sprites.Add(new HashedString(kv.Key), kv.Value);
            }
            foreach (var kv in Instance.tintedSprites)
            {
                TintedSprite tintedSprite1 = new TintedSprite();
                tintedSprite1.name = kv.Key;
                tintedSprite1.sprite = kv.Value;
                Assets.TintedSprites.Add(tintedSprite1);
            }
        }
    }
}