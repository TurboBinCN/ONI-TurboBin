using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;

namespace MSBuildHelper
{
    public class WriteYamlFiles : Task
    {
        [Required]
        public string OutputPath { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required]
        public string StaticID { get; set; }

        public string RequiredDlcIds { get; set; }

        public string ForbiddenDlcIds { get; set; }

        [Required]
        public int MinimumSupportedBuild { get; set; }
        [Required]
        public string Version { get; set; }

        [Required]
        public int APIVersion { get; set; }

        public override bool Execute()
        {
            try
            {
                Log.LogMessage(MessageImportance.High, "=== Starting WriteYamlFiles Task ===");
                Log.LogMessage(MessageImportance.High, $"OutputPath: '{OutputPath}'");
                Log.LogMessage(MessageImportance.High, $"Title: '{Title}'");
                Log.LogMessage(MessageImportance.High, $"Description: '{Description}'");
                Log.LogMessage(MessageImportance.High, $"StaticID: '{StaticID}'");
                Log.LogMessage(MessageImportance.High, $"RequiredDlcIds: '{RequiredDlcIds}'");
                Log.LogMessage(MessageImportance.High, $"ForbiddenDlcIds: '{ForbiddenDlcIds}'");
                Log.LogMessage(MessageImportance.High, $"MinimumSupportedBuild: {MinimumSupportedBuild}");
                Log.LogMessage(MessageImportance.High, $"Version: '{Version}'");
                Log.LogMessage(MessageImportance.High, $"APIVersion: {APIVersion}");

                Log.LogMessage(MessageImportance.High, "Creating Mod object...");
                var mod = new Mod
                {
                    title = Title,
                    description = Description,
                    staticID = StaticID
                };

                Log.LogMessage(MessageImportance.High, "Creating ModInfo object...");
                var modInfo = new ModInfo
                {
                    requiredDlcIds = !string.IsNullOrEmpty(RequiredDlcIds) ? RequiredDlcIds.Split(',') : Array.Empty<string>(),
                    forbiddenDlcIds = !string.IsNullOrEmpty(ForbiddenDlcIds) ? ForbiddenDlcIds.Split(',') : Array.Empty<string>(),
                    minimumSupportedBuild = MinimumSupportedBuild,
                    APIVersion = APIVersion,
                    version = Version
                };

                Log.LogMessage(MessageImportance.High, "Serializing to YAML...");
                var serializer = new YamlDotNet.Serialization.SerializerBuilder().ConfigureDefaultValuesHandling(YamlDotNet.Serialization.DefaultValuesHandling.OmitEmptyCollections).Build();
                var modYaml = serializer.Serialize(mod);
                var modInfoYaml = serializer.Serialize(modInfo);

                Log.LogMessage(MessageImportance.High, "Generated mod.yaml:");
                Log.LogMessage(MessageImportance.High, modYaml);
                Log.LogMessage(MessageImportance.High, "Generated mod_info.yaml:");
                Log.LogMessage(MessageImportance.High, modInfoYaml);

                var modPath = Path.Combine(OutputPath, "mod.yaml");
                var modInfoPath = Path.Combine(OutputPath, "mod_info.yaml");

                Log.LogMessage(MessageImportance.High, $"Writing to files...");
                Log.LogMessage(MessageImportance.High, $"mod.yaml path: '{modPath}'");
                Log.LogMessage(MessageImportance.High, $"mod_info.yaml path: '{modInfoPath}'");

                try
                {
                    File.WriteAllText(modPath, modYaml);
                    File.WriteAllText(modInfoPath, modInfoYaml);
                    Log.LogMessage(MessageImportance.High, "Files written successfully");
                }
                catch (Exception e)
                {
                    Log.LogErrorFromException(e, true);
                    return false;
                }

                Log.LogMessage(MessageImportance.High, "=== WriteYamlFiles Task Completed ===");
                return true;
            }
            catch (Exception e)
            {
                Log.LogMessage(MessageImportance.High, "=== ERROR in WriteYamlFiles Task ===");
                Log.LogErrorFromException(e, true);
                return false;
            }
        }
    }

    public class Mod
    {
        public string title { get; set; }
        public string description { get; set; }
        public string staticID { get; set; }
    }

    public class ModInfo
    {
        public string[] requiredDlcIds { get; set; }
        public string[] forbiddenDlcIds { get; set; }
        public int minimumSupportedBuild { get; set; }
        public int APIVersion { get; set; }
        public string version { get; set; }
    }
}