using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ONI_TurboBin
{
    /*
     * 由于PLIB现在具有模块化设计
     * 获取编译项目实际需要的plib*.dll列表以生成ILREPAC
     * 减少输出文件的大小
     *必须指定新编译的原始DLL文件和所有PLIB*.DLL的位置
     * 
     *注意！！！
     *因为将下载的库卸载到.没有人为的方法：
     *a）必须使ILRepak创建新的输出文件
     *b）在编译后，msbuild本身关闭，不挂在内存中，也不在下次编译时抛出错误
     * 要设置环境变量 MSBUILDDISABLENODEREUSE=1
     */
    public class GetReallyReferencedAssembliesAtFolder : Task
    {
        [Required]
        public string AssemblyName { get; set; }
        [Required]
        public string ReferencedAssembliesFolder { get; set; }

        [Output]
        public string[] ReallyReferencedAssemblies { get; set; }

        private void GetReallyReferencedAssemblies(string AssemblyName, List<string> list)
        {
            var assembly = Assembly.ReflectionOnlyLoadFrom(AssemblyName);
            foreach (AssemblyName an in assembly.GetReferencedAssemblies())
            {
                var file = Path.Combine(ReferencedAssembliesFolder, an.Name + ".dll");
                if (!list.Contains(file) && File.Exists(file))
                {
                    list.Add(file);
                    GetReallyReferencedAssemblies(file, list);
                }
            }
        }

        public override bool Execute()
        {
            bool result = false;
            try
            {
                var list = new List<string>();
                GetReallyReferencedAssemblies(AssemblyName, list);
                ReallyReferencedAssemblies = list.ToArray();
                result = true;
            }
            catch (Exception e)
            {
                Log.LogMessage(MessageImportance.High, $"An error occurred while executing '{nameof(GetReallyReferencedAssembliesAtFolder)}'");
                Log.LogErrorFromException(e, true);
            }
            return result;
        }
    }
}
