using HarmonyLib;
using Klei;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TBB.He.TbbLib.Module;
using TBB.He.TbbLib.Utils;
using TBBHe.TbbLib.Debuger;

namespace TbbLib.Module
{
    public class TbbCodexEntries : TbbModule<TbbCodexEntries>
    {
        private string MOD_DIRECTORY_ROOT = "";
        private static readonly string CODEX_FILE_PATH_RELATIVE = "assets/codex/";
        private static readonly string CODEX_FILE_PREFIX = "DynamicCodexEntry_";

        private Dictionary<string,string> entities = new();

        protected override void Initialized()
        {
            MOD_DIRECTORY_ROOT = Mod.ContentPath + "/";

            Harmony.Patch(typeof(CodexCache), "CollectYAMLEntries",
                postfix: new HarmonyMethod(typeof(TbbCodexEntries), nameof(CodexCache_CollectYAMLEntitries_Postfix)));
        }
        public TbbCodexEntries ADD(string entry,string category)
        {
            entities.Add(category,entry);
            return Instance;
        }
        public static void CodexCache_CollectYAMLEntitries_Postfix(List<CategoryEntry> categories)
        {
            try
            {
                var instance = Instance;
                if (instance == null || instance.entities.Count == 0)
                {
                    TbbDebuger.LogDebug("[TbbCodexEntries] 没有要加载的 Codex 条目.");
                    return;
                }
                var fileCategoryMap = new List<(FileHandle file, string category)>();
                var searchBasePath = instance.fullPath(CODEX_FILE_PATH_RELATIVE);


                foreach (var kvp in instance.entities)
                {
                    var searchPath = Path.Combine(searchBasePath, $"{kvp.Key}/");
                    var expectedFileName = $"{CODEX_FILE_PREFIX}{kvp.Value}.yaml";

                    var searchPattern = Path.Combine(searchPath, expectedFileName);
                    var filesInThisCategory = new List<FileHandle>();

                    FileSystem.GetFiles(searchPath, expectedFileName, filesInThisCategory);
                    TbbDebuger.LogDebug($"[TbbCodexEntries] 文件: {searchPath} -> {expectedFileName}");
                    foreach (var file in filesInThisCategory)
                    {
                        if (!fileCategoryMap.Exists(existingTuple => existingTuple.file.full_path == file.full_path))
                        {
                            fileCategoryMap.Add((file, kvp.Key)); // 将文件和其类别绑定
                            TbbDebuger.LogDebug($"[TbbCodexEntries] 发现 Codex 文件: {expectedFileName} -> {file.full_path}");
                        }
                    }
                }

                if (fileCategoryMap.Count == 0)
                {
                    TbbDebuger.LogDebug($"[TbbCodexEntries] 未找到.{CODEX_FILE_PATH_RELATIVE}*.yaml 相关的CodexCache文件");
                    return;
                }
                TbbDebuger.LogDebug($"[TbbStoryTraits] list.Count:[{fileCategoryMap.Count}]");
                FieldInfo widgetTagMappingsField = typeof(CodexCache).GetField("widgetTagMappings", BindingFlags.NonPublic | BindingFlags.Static);
                if (widgetTagMappingsField == null)
                {
                    TbbDebuger.LogWarning("[TbbCodexEntries]反射获取属性'widgetTagMappings'为NULL");
                    return;
                }
                List<Tuple<string, Type>> widgetTagMappings = (List<Tuple<string, Type>>)widgetTagMappingsField.GetValue(null);

                foreach (var (file, category) in fileCategoryMap)
                {
                    TbbDebuger.LogDebug($"[TbbCodexEntries]开始加载[{file.full_path}]");
                    CodexEntry customEntry = YamlIO.LoadFile<CodexEntry>(file, new YamlIO.ErrorHandler(MyYamlErrorHandler), widgetTagMappings);

                    if (customEntry == null)
                    {
                        TbbDebuger.LogDebug($"[TbbCodexEntries]加载 CodexEntry {file.full_path} 失败");
                    }
                    else
                    {
                        customEntry.category = category.ToUpper();
                        CodexCache.AddEntry(customEntry.id, customEntry, categories);

                        TbbDebuger.LogDebug($"[TbbCodexEntries]成功加载 Codex entry id: {customEntry.id} parentID:{customEntry.parentId} YAML:[{file.full_path}]");
                    }
                }
            }
            catch (Exception e)
            {
                TbbDebuger.LogWarning($"[TbbCodexEntries] 错误: {e.Message}\n{e.StackTrace}");
            }
        }
        private string fullPath(string path)
        {
            return Path.Combine(MOD_DIRECTORY_ROOT, path);
        }
        private static void MyYamlErrorHandler(YamlIO.Error error, bool force_log_as_warning)
        {
            TbbDebuger.LogWarning($"[TbbCodexEntries] YAML Parse Error in {error.file.full_path}: {error.message}");
        }
    }
}
