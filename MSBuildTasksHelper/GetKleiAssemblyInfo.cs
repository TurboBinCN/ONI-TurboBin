using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.Reflection;

namespace ONI_TurboBin
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
                Log.LogMessage(MessageImportance.High, $"Reading assembly '{AssemblyCSharp}'");
                var assembly = Assembly.ReflectionOnlyLoadFrom(AssemblyCSharp);
                var flag = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
                var KleiVersion = assembly.GetType("KleiVersion", true);
                KleiBuildNumber = ((uint)KleiVersion.GetField("ChangeList", flag).GetRawConstantValue()).ToString();
                KleiBuildBranch = (string)KleiVersion.GetField("BuildBranch", flag).GetRawConstantValue();
                try
                {
                    // 躺下，ReflectionOnlyLoad不会增加依赖性，我们手动拖动它
                    foreach (var dll in new string[] { "UnityEngine.CoreModule.dll", "netstandard.dll" })
                    {
                        var file = Path.Combine(LibraryPath, dll);
                        if (File.Exists(file))
                            Assembly.ReflectionOnlyLoadFrom(file);
                    }
                    var LaunchInitializer = assembly.GetType("LaunchInitializer", true);
                    KleiGameVersion = (string)LaunchInitializer.GetField("PREFIX", flag).GetRawConstantValue() +
                        ((int)LaunchInitializer.GetField("UPDATE_NUMBER", flag).GetRawConstantValue()).ToString();
                }
                catch (Exception e)
                {
                    Log.LogWarningFromException(e, true);
                    KleiGameVersion = INVALID;
                }
                Log.LogMessage(MessageImportance.High, $"Game Version: {KleiGameVersion}-{KleiBuildNumber}\t Branch: {KleiBuildBranch}");
                result = true;
            }
            catch (Exception e)
            {
                Log.LogMessage(MessageImportance.High, $"An error occurred while executing '{nameof(GetKleiAssemblyInfo)}'");
                Log.LogErrorFromException(e, true);
            }
            return result;
        }
    }
}
