using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace MSBuildHelper
{
    public class GetReallyReferencedAssembliesAtFolder : Task
    {
        [Required]
        public string AssemblyName { get; set; }

        [Required]
        public string ReferencedAssembliesFolder { get; set; }

        [Output]
        public ITaskItem[] ReallyReferencedAssemblies { get; set; }

        public override bool Execute()
        {
            try
            {
                if (!File.Exists(AssemblyName))
                {
                    Log.LogError($"Assembly file not found: {AssemblyName}");
                    return false;
                }

                if (!Directory.Exists(ReferencedAssembliesFolder))
                {
                    Log.LogError($"Referenced assemblies folder not found: {ReferencedAssembliesFolder}");
                    return false;
                }

                // Get all DLL files in the referenced assemblies folder
                var referencedDlls = new HashSet<string>(Directory.GetFiles(ReferencedAssembliesFolder, "*.dll", SearchOption.TopDirectoryOnly));
                // Analyze dependencies
                var dependencies = new HashSet<string>();
                AnalyzeDependencies(AssemblyName, referencedDlls, dependencies);

                // Convert to ITaskItem array
                var taskItems = new List<ITaskItem>();
                foreach (var dependency in dependencies)
                {
                    taskItems.Add(new TaskItem(dependency));
                }

                ReallyReferencedAssemblies = taskItems.ToArray();

                return true;
            }
            catch (Exception e)
            {
                Log.LogMessage(MessageImportance.High, "=== ERROR in GetReallyReferencedAssembliesAtFolder Task ===");
                Log.LogErrorFromException(e, true);
                return false;
            }
        }

        private void AnalyzeDependencies(string assemblyPath, HashSet<string> referencedDlls, HashSet<string> dependencies)
        {
            try
            {
                // Load the assembly
                var assembly = Assembly.LoadFrom(assemblyPath);

                // Get all referenced assemblies
                var referencedAssemblies = assembly.GetReferencedAssemblies();
                
                foreach (var referencedAssembly in referencedAssemblies)
                {
                    // Find the referenced assembly in the referencedDlls folder
                    string referencedAssemblyPath = null;
                    foreach (var dllPath in referencedDlls)
                    {
                        if (Path.GetFileNameWithoutExtension(dllPath).Equals(referencedAssembly.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            referencedAssemblyPath = dllPath;
                            break;
                        }
                    }

                    // If found and not already processed
                    if (referencedAssemblyPath != null && !dependencies.Contains(referencedAssemblyPath))
                    {
                        // Add to dependencies
                        dependencies.Add(referencedAssemblyPath);

                        // Recursively analyze dependencies of this assembly
                        AnalyzeDependencies(referencedAssemblyPath, referencedDlls, dependencies);
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogWarningFromException(e, true);
            }
        }
    }
}