using System.IO;
using TBB.He.TbbLib.Debuger;
using TBB.He.TbbLib.Module;
using UnityEngine;

namespace TBB.He.TbbLib.Utils
{
    public class TbbAssetsUtils : TbbModule<TbbAssetsUtils>
    {
        private static readonly string ASSETS_PATH = "assets/";
        private static readonly string ASSETS_TEXTURE_PATH = $"{ASSETS_PATH}texture/";
        protected override void Initialized()
        {
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

            TbbDebuger.LogDebug($"containment_tile_solid22:{Assets.GetTextureAtlas("containment_tile_solid")}");
            return atlas;
        }
    }
}
