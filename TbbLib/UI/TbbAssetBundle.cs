using System.IO;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace TBB.He.TbbLib.UI
{
    public class TbbAssetBundle
    {
        public static AssetBundle LoadAssetBundle(string basePath, string assetBundleName, string path = null, bool platformSpecific = false)
        {
            foreach (AssetBundle assetBundle in AssetBundle.GetAllLoadedAssetBundles())
            {
                if (assetBundle.name == assetBundleName)
                {
                    return assetBundle;
                }
            }
            if (path.IsNullOrWhiteSpace())
            {
                path = Path.Combine(basePath, "assets");
            }
            if (platformSpecific)
            {
                RuntimePlatform platform = Application.platform;
                if (platform != RuntimePlatform.OSXPlayer)
                {
                    if (platform != RuntimePlatform.WindowsPlayer)
                    {
                        if (platform == RuntimePlatform.LinuxPlayer)
                        {
                            path = Path.Combine(path, "linux");
                        }
                    }
                    else
                    {
                        path = Path.Combine(path, "windows");
                    }
                }
                else
                {
                    path = Path.Combine(path, "mac");
                }
            }
            path = Path.Combine(path, assetBundleName);
            AssetBundle assetBundle2 = AssetBundle.LoadFromFile(path);
            if (assetBundle2 == null)
            {
                TbbDebuger.LogWarning("Failed to load AssetBundle from path " + path);
                return null;
            }
            return assetBundle2;
        }
    }
}