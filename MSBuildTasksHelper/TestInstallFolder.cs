using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using YamlDotNet.Serialization;

namespace ONI_TurboBin
{
    /*
    测试要安装新编译mod的目标文件夹
    决定
    *是否存档以前安装的mod
    *在文件夹根或存档中安装新mod
    基于modu info.yaml文件的可用性和mod所针对的游戏版本的比较
    */
    public class TestInstallFolder : Task
    {
        [Required]
        public string KnownGameVersionsFile { get; set; }
        [Required]
        public string RootModInfoFile { get; set; }
        [Required]
        public string CurrentGameVersion { get; set; }
        [Required]
        public int CurrentBuildNumber { get; set; }

        [Output]
        public string PreviousGameVersion { get; set; }
        [Output]
        public int PreviousBuildNumber { get; set; }
        [Output]
        public bool DoInstallToRootFolder { get; set; }
        [Output]
        public bool NeededArchiving { get; set; }

        public override bool Execute()
        {
            try
            {
                KnownGameVersions data;
                //下载已知版本
                if (File.Exists(KnownGameVersionsFile))
                {
                    data = new DeserializerBuilder().IgnoreUnmatchedProperties().Build()
                        .Deserialize<KnownGameVersions>(File.ReadAllText(KnownGameVersionsFile));
                }
                else
                    data = new();
                data.PreserveVersion ??= GetKleiAssemblyInfo.INVALID;
                data.KnownVersions.Sort();
                //如果需要，添加当前版本
                bool need_write = false;
                if (CurrentGameVersion != GetKleiAssemblyInfo.INVALID)
                {
                    int i = data.KnownVersions.FindIndex(info => info.GameVersion == CurrentGameVersion);
                    if (i == -1)
                    {
                        data.KnownVersions.Add(new GameVersionInfo() { GameVersion = CurrentGameVersion, MinimumBuildNumber = CurrentBuildNumber });
                        need_write = true;
                    }
                    else if (data.KnownVersions[i].MinimumBuildNumber > CurrentBuildNumber)
                    {
                        var info = data.KnownVersions[i];
                        info.MinimumBuildNumber = CurrentBuildNumber;
                        data.KnownVersions[i] = info;
                        need_write = true;
                    }
                    data.KnownVersions.Sort();
                }
                // 然后 记录
                if (need_write)
                {
                    File.WriteAllText(KnownGameVersionsFile,
                        new SerializerBuilder().Build().Serialize(data));
                }

                // mod中没有文件信息 == 将其直接放入根
                if (!File.Exists(RootModInfoFile))
                {
                    PreviousBuildNumber = 0;
                    PreviousGameVersion = GetKleiAssemblyInfo.INVALID;
                    DoInstallToRootFolder = true;
                    NeededArchiving = false;
                    return true;
                }

                // 读取现有的mod.info
                var deserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();
                var modInfo = deserializer.Deserialize<ModInfo>(File.ReadAllText(RootModInfoFile));
                PreviousBuildNumber = modInfo.minimumSupportedBuild;
                PreviousGameVersion = GetKleiAssemblyInfo.INVALID;

                // 我们试图找到一个匹配的版本。
                for (int j = 0; j < data.KnownVersions.Count; j++)
                {
                    if (PreviousBuildNumber < data.KnownVersions[j].MinimumBuildNumber)
                        break;
                    PreviousGameVersion = data.KnownVersions[j].GameVersion;
                }

                if (CurrentGameVersion == GetKleiAssemblyInfo.INVALID)
                {
                    //由于某种原因，无法正确计算当前版本
                    //==以防万一，放在档案里面
                    DoInstallToRootFolder = false;
                    NeededArchiving = false;
                }
                else if (CurrentGameVersion == PreviousGameVersion) // 版本匹配 == 直接放入根中，不需要备份
                {
                    DoInstallToRootFolder = true;
                    NeededArchiving = false;
                }
                else //版本不匹配
                {
                    if (CurrentBuildNumber > PreviousBuildNumber) //当前编译编号 > 上一版编译编号，需要存档
                    {
                        DoInstallToRootFolder = true;
                        NeededArchiving = true;
                    }
                    else //不需要存档
                    {
                        DoInstallToRootFolder = false;
                        NeededArchiving = false;
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Log.LogMessage(MessageImportance.High, $"An error occurred while executing '{nameof(TestInstallFolder)}'");
                Log.LogErrorFromException(e, true);
            }
            return false;
        }
    }
}
