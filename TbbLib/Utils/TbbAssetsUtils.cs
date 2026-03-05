using ProcGen;
using System.IO;
using TBB.He.TbbLib.Debuger;
using TBB.He.TbbLib.Module;
using UnityEngine;
using UnityEngine.UI;
using Path = System.IO.Path;

namespace TBB.He.TbbLib.Utils
{
    public class TbbAssetsUtils : TbbModule<TbbAssetsUtils>
    {
        private static readonly string ASSETS_PATH = "assets/";
        private static readonly string ASSETS_TEXTURE_PATH = $"{ASSETS_PATH}texture/";
        private static readonly string ASSETS_SPRITE_PATH = $"{ASSETS_PATH}sprite/";
        protected override void Initialized()
        {
        }
        public Sprite LoadIamgeSprite(string name) {
            Texture2D texture = null;

            var path_base = Mod.ContentPath;
            var sprite_path = Path.Combine(path_base, ASSETS_SPRITE_PATH);
            var texFile = Path.Combine(sprite_path, $"{name}.png");

            if (File.Exists(texFile))
            {
                byte[] data = File.ReadAllBytes(texFile);
                // 创建一个临时纹理，尺寸会在 LoadImage 时自动调整
                texture = new Texture2D(2, 2);
                texture.LoadImage(data);
                TbbDebuger.LogDebug($"[TbbAssets] 载入纹理 [{texFile}]，尺寸: {texture.width}x{texture.height}");
            }
            else
            {
                TbbDebuger.LogWarning($"[TbbAssets] 纹理图片[{texFile}]不存在");
            }

            Sprite sprite = null;
            if (texture != null)
            {
                // 使用纹理的实际尺寸创建精灵
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), default);
                sprite.name = name;
                TbbDebuger.LogDebug($"[TbbAssets] 纹理 [{sprite.name}]");
            }

            return sprite;
        }
        public TextureAtlas LoadTextureAtlas(string name, string referenceAtlas)
        {

            TextureAtlas referencetileAtlas = global::Assets.GetTextureAtlas(referenceAtlas);
            Texture2D texture = null;

            int customTextureWidth = referencetileAtlas.texture.width;
            int customTextureHeight = referencetileAtlas.texture.height;
            var path_base = Mod.ContentPath;
            var texture_path = Path.Combine(path_base, ASSETS_TEXTURE_PATH);
            var texFile = Path.Combine(texture_path, $"{name}.png");

            if (File.Exists(texFile))
            {
                byte[] data = File.ReadAllBytes(texFile);
                texture = new Texture2D(customTextureWidth, customTextureHeight);
                texture.LoadImage(data);
                TbbDebuger.LogDebug($"[TbbAssets] 载入纹理 [{texFile}]");
            }
            else
            {
                TbbDebuger.LogWarning($"[TbbAssets] 纹理图片[{texFile}]不存在");
            }

            TextureAtlas atlas;
            atlas = ScriptableObject.CreateInstance<TextureAtlas>();
            atlas.name = name;
            atlas.texture = texture;
            atlas.scaleFactor = referencetileAtlas.scaleFactor;
            atlas.items = referencetileAtlas.items;

            return atlas;
        }
    }
}
