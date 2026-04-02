using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.Reflection;

namespace MSBuildHelper
{
    /*
     * 在编译过程中获取有关游戏DLL版本的信息.
     * 
     * 注意！！！
     *由于技术限制，应确保同时汇编的项目
     *使用相同版本的assembly-csharp.dll，否则msbuild会导致错误
     *因为将下载的库卸载到.没有人为的方法
     *确保MSBuild在编译后关闭，不会挂在内存中，也不会在下次编译时抛出错误
     *必须设置MSBuildDiSableNoderUse=1环境变量
     *必须设置环境变量 MSBUILDDISABLENODEREUSE=1
     */
    public class GetKleiAssemblyInfo : Task
    {
        [Required]
        public string AssemblyCSharp { get; set; }
        [Required]
        public string LibraryPath { get; set; }

        [Output]
        public string KleiGameVersion { get; set; }
        [Output]
        public string KleiBuildNumber { get; set; }
        [Output]
        public string KleiBuildBranch { get; set; }

        public const string INVALID = "??";

        public override bool Execute()
        {
            bool result = false;
            try
            {
                // Set default values
                string gameVersion = INVALID;
                string buildNumber = INVALID;
                string buildBranch = INVALID;
                
                try
                {
                    // Load the assembly using regular reflection
                    var assembly = Assembly.LoadFrom(AssemblyCSharp);
                    
                    var flag = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
                    
                    // Get KleiVersion information
                    var KleiVersion = assembly.GetType("KleiVersion", true);
                    
                    var changeListField = KleiVersion.GetField("ChangeList", flag);
                    var buildBranchField = KleiVersion.GetField("BuildBranch", flag);
                    
                    // Extract values as strings
                    buildNumber = changeListField.GetValue(null)?.ToString() ?? INVALID;
                    buildBranch = buildBranchField.GetValue(null)?.ToString() ?? INVALID;
                    
                    try
                    {
                        // Get LaunchInitializer information
                        var LaunchInitializer = assembly.GetType("LaunchInitializer", true);
                        
                        var prefixField = LaunchInitializer.GetField("PREFIX", flag);
                        var updateNumberField = LaunchInitializer.GetField("UPDATE_NUMBER", flag);
                        
                        var prefix = prefixField.GetValue(null)?.ToString() ?? "";
                        var updateNumber = updateNumberField.GetValue(null)?.ToString() ?? "";
                        gameVersion = prefix + updateNumber;
                    }
                    catch (Exception e)
                    {
                        Log.LogWarningFromException(e, true);
                        gameVersion = INVALID;
                        Log.LogMessage(MessageImportance.High, $"Using default GameVersion: {gameVersion}");
                    }
                }
                catch (Exception e)
                {
                    Log.LogWarningFromException(e, true);
                    Log.LogMessage(MessageImportance.High, $"Using default values due to exception");
                }
                
                // Assign values to output properties
                KleiGameVersion = gameVersion;
                KleiBuildNumber = buildNumber;
                KleiBuildBranch = buildBranch;
                
                Log.LogMessage(MessageImportance.High, $"==KleiGameVersion: {KleiGameVersion} - {KleiBuildNumber}  Branch: {KleiBuildBranch}==");
                result = true;
            }
            catch (Exception e)
            {
                Log.LogMessage(MessageImportance.High, $"=== ERROR in GetKleiAssemblyInfo Task ===");
                Log.LogErrorFromException(e, true);
            }
            return result;
        }
    }
}