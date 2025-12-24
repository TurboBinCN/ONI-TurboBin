using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;

namespace ONI_TurboBin
{
    /*
    На основе анализа файлов mod_info.yaml вычисляем наиболее старые и ненужные архивы для удаления
    */
    public class FindTooOldArchivedVersions : Task
    {
        [Required]
        public string KnownGameVersionsFile { get; set; }
        [Required]
        public string RootModInfoFile { get; set; }
        [Required]
        public string[] ArchivedModInfoFiles { get; set; }

        [Output]
        public string[] TooOldArchivedVersions { get; set; }

        public override bool Execute()
        {
            try
            {
                var deserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();
                KnownGameVersions data;
                // 载著名版本
                if (File.Exists(KnownGameVersionsFile))
                    data = deserializer.Deserialize<KnownGameVersions>(File.ReadAllText(KnownGameVersionsFile));
                else
                    data = new();
                data.PreserveVersion ??= GetKleiAssemblyInfo.INVALID;
                data.KnownVersions.Sort();

                var candidates = new List<string>();
                if (data.KnownVersions.Count > 3)
                {
                    //我们认为
                    //最近的三个版本是beta、main和previous
                    //最后两个版本是“主”和“上一个”
                    //“上一个”必须在“PreserveVersion”中指定
                    int i = data.KnownVersions.FindIndex(info => info.GameVersion == data.PreserveVersion);
                    if (i == -1 || i > data.KnownVersions.Count - 2)
                    {
                        Log.LogError("'PreserveVersion' not specified or invalid, abort cleaning!");
                    }
                    else
                    {
                        int prew = data.KnownVersions[i].MinimumBuildNumber;
                        int live = data.KnownVersions[i + 1].MinimumBuildNumber;

                        var ArchivedVersions = new Dictionary<int, string>();

                        if (File.Exists(RootModInfoFile))
                        {
                            int build = deserializer.Deserialize<ModInfo>(File.ReadAllText(RootModInfoFile)).minimumSupportedBuild;
                            ArchivedVersions[build] = RootModInfoFile;
                        }

                        if (ArchivedModInfoFiles != null)
                        {
                            foreach (var file in ArchivedModInfoFiles)
                            {
                                if (File.Exists(file))
                                {
                                    int build = deserializer.Deserialize<ModInfo>(File.ReadAllText(file)).minimumSupportedBuild;
                                    ArchivedVersions[build] = file;
                                }
                            }
                        }

                        //保留“上一个”和“上一个”的版本
                        //如果没有版本=“上一个”保存另一个最新版本
                        var buildNumbers = ArchivedVersions.Keys.ToList();
                        buildNumbers.Sort();
                        buildNumbers.RemoveAll(build => build >= live);
                        if (buildNumbers.RemoveAll(build => build >= prew) == 0 && buildNumbers.Count > 0)
                        {
                            buildNumbers.RemoveAt(buildNumbers.Count - 1);
                        }
                        foreach (int build in buildNumbers)
                        {
                            if (ArchivedVersions[build] != RootModInfoFile)
                                candidates.Add(Path.GetDirectoryName(ArchivedVersions[build]));
                        }
                    }
                }
                TooOldArchivedVersions = candidates.ToArray();
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
