using HarmonyLib;
using Klei;
using ProcGen;
using System;
using System.Collections.Generic;
using System.Reflection;
using TBB.He.TbbLib.Utils;
using TBBHe.TbbLib.Debuger;
using UnityEngine;
using Path = System.IO.Path;

namespace TBB.He.TbbLib.Module
{
    public class TbbStoryTraits : TbbModule<TbbStoryTraits>
    {
        private static Dictionary<string, WorldTrait> storyTraits = new();
        private static readonly string STOTY_TRAITS_WORLDGEN_PATH = "assets/worldgen/";
        //private static readonly string STOTY_TRAITS_WORLDGEN_PATH = "assets/worldgen/storytraits";

        private static readonly string STORY_TRAITS_TEMPLATE_PATH = "assets/templates/";
        private static readonly string MOD_TEMPLATE_PREFIX = "mod__";
        //CODEX 用于CodexEntry 百科词条
        private static readonly string CODEX_FILE_PATH = "assets/codex/StoryTraits/";
        private static readonly string CODEX_FILE_PREFIX = "DynamicCodexEntry_StoryTrait";

        private static bool traitsAddedToCandidates = false;

        private bool storyStraitsLoaded = false;

        private string MOD_DIRECTORY_ROOT = "";

        private List<string> storiyStaits = new();
        protected override void Initialized()
        {
            MOD_DIRECTORY_ROOT = Mod.ContentPath + "/";
            //故事特质
            //Harmony.Patch(typeof(TemplateSpawning), "SpawnStoryTraitTemplates",
            //    prefix: new HarmonyMethod(typeof(TbbStoryTraits), nameof(TemplateSpawning_SpawnStoryTraitTemplates_Prefix)));

            Harmony.Patch(typeof(SettingsCache), "LoadStoryTraits",
                postfix: new HarmonyMethod(typeof(TbbStoryTraits), nameof(SettingsCache_LoadStoryTraits_Postfix)));

            Harmony.Patch(typeof(TemplateCache), "RewriteTemplateYaml",
                postfix: new HarmonyMethod(typeof(TbbStoryTraits), nameof(TemplateCache_RewriteTemplateYaml_Postfix)));

            //故事特质-百科词条
            Harmony.Patch(typeof(CodexCache), "CollectYAMLEntries",
                postfix: new HarmonyMethod(typeof(TbbStoryTraits), nameof(CodexCache_CollectYAMLEntitries_Postfix)));
            //故事


        }

        public static void SettingsCache_LoadStoryTraits_Postfix(string path, string prefix, List<YamlIO.Error> errors)
        {
            TbbDebuger.LogDebug($"[TbbStoryTraits]worldgenFolderPath:[{path}]");
            if (Instance.storyStraitsLoaded) return;

            LoadCustomStoryTraits(errors);
            foreach (var error in errors)
            {
                TbbDebuger.LogWarning($"[TbbStoryTraits] erro:{error.message}\n{error.inner_exception}");
            }

            Instance.storyStraitsLoaded = true;
        }
        public static void LoadCustomStoryTraits(List<YamlIO.Error> errors)
        {
            List<FileHandle> list = new List<FileHandle>();
            Dictionary<string, WorldTrait> settingCacheStoryTraits = Traverse
                .Create(typeof(SettingsCache))          // 定位到 SettingsCache 类
                .Field("storyTraits")                   // 定位到名为 storyTraits 的字段
                .GetValue<Dictionary<string, WorldTrait>>();
            var path = Instance.fullPath(FileSystem.Normalize(Path.Combine(STOTY_TRAITS_WORLDGEN_PATH)));
            FileSystem.GetFiles(FileSystem.Normalize(Path.Combine(path, "storytraits")), "*.yaml", list);
            TbbDebuger.LogDebug($"[故事特质] 搜索到故事特质：[{path}] *.yaml [{list.Count}]");
            list.Sort((FileHandle s1, FileHandle s2) => string.Compare(s1.full_path, s2.full_path, StringComparison.OrdinalIgnoreCase));

            foreach (FileHandle file in list)
            {
                TbbDebuger.LogDebug($"[故事特质] 搜索到故事特质：[{file.full_path}]");
                TbbHarmonyExtension.CallStaticMethod(typeof(SettingsCache), "LoadTrait", new object[] {
                   file,path,"",settingCacheStoryTraits,errors
                });
                TbbDebuger.LogDebug($"[故事特质] 添加故事特质成功 [{file.full_path}]");
            }
        }
        public static void CodexCache_CollectYAMLEntitries_Postfix(List<CategoryEntry> categories)
        {
            try
            {
                List<FileHandle> list = new List<FileHandle>();

                var path = Instance.fullPath(FileSystem.Normalize(Path.Combine(CODEX_FILE_PATH)));
                foreach (var name in Instance.storiyStaits)
                {
                    var tempResults = new List<FileHandle>();
                    FileSystem.GetFiles(path, $"{CODEX_FILE_PREFIX}{name}.yaml", tempResults);
                    TbbDebuger.LogDebug($"[百科词条] {CODEX_FILE_PREFIX}{name}.yaml");
                    foreach (var foundFile in tempResults)
                    {
                        if (!list.Exists(existingFile => existingFile.full_path == foundFile.full_path))
                        {
                            list.Add(foundFile);
                        }
                    }
                }
                if (list.Count == 0)
                {
                    TbbDebuger.LogDebug($"[百科词条] 未找到.{CODEX_FILE_PREFIX}*.yaml 相关的CodexCache文件");
                    return;
                }
                FieldInfo widgetTagMappingsField = typeof(CodexCache).GetField("widgetTagMappings", BindingFlags.NonPublic | BindingFlags.Static);
                if (widgetTagMappingsField == null)
                {
                    TbbDebuger.LogWarning("[百科词条]反射获取属性'widgetTagMappings'为NULL");
                    return;
                }
                List<Tuple<string, Type>> widgetTagMappings = (List<Tuple<string, Type>>)widgetTagMappingsField.GetValue(null);

                foreach (var f in list)
                {
                    TbbDebuger.LogDebug($"[百科词条]开始加载[{f.full_path}]");
                    CodexEntry customEntry = YamlIO.LoadFile<CodexEntry>(f, new YamlIO.ErrorHandler(MyYamlErrorHandler), widgetTagMappings);

                    if (customEntry == null)
                    {
                        TbbDebuger.LogDebug($"[百科词条]加载 CodexEntry {f.full_path} 失败");
                    }
                    else
                    {
                        customEntry.category = "StoryTraits".ToUpper();
                        CodexCache.AddEntry(customEntry.id, customEntry, categories);

                        TbbDebuger.LogDebug($"[百科词条]成功加载 Codex entry id: {customEntry.id} parentID:{customEntry.parentId} YAML:[{f.full_path}]");
                    }
                }
            }
            catch (Exception e)
            {
                TbbDebuger.LogWarning($"[百科词条] 错误: {e.Message}\n{e.StackTrace}");
            }
        }
        private static void MyYamlErrorHandler(YamlIO.Error error, bool force_log_as_warning)
        {
            TbbDebuger.LogWarning($"[百科词条] YAML Parse Error in {error.file.full_path}: {error.message}");
        }
        public TbbStoryTraits ADD(string name)
        {
            //var errors = new List<YamlIO.Error>();
            //LoadStoryTraits(name, "", errors);
            //foreach (var error in errors)
            //{
            //    TbbDebuger.LogWarning($"[TbbStoryTraits] erro:{error.message}\n{error.inner_exception}");
            //}
            storiyStaits.Add(name);
            return Instance;
        }
        private string fullPath(string path)
        {
            return Path.Combine(MOD_DIRECTORY_ROOT, path);
        }
        public void LoadStoryTraits(string name, string prefix, List<YamlIO.Error> errors)
        {
            List<FileHandle> list = new List<FileHandle>();

            var path = fullPath(FileSystem.Normalize(Path.Combine(STOTY_TRAITS_WORLDGEN_PATH)));
            FileSystem.GetFiles(path, $"{name}.yaml", list);

            list.Sort((FileHandle s1, FileHandle s2) => string.Compare(s1.full_path, s2.full_path, StringComparison.OrdinalIgnoreCase));
            foreach (FileHandle file in list)
            {
                LoadTrait(file, path, prefix, storyTraits, errors);
            }
        }
        private static void LoadTrait(FileHandle file, string path, string prefix, Dictionary<string, WorldTrait> traitsDict, List<YamlIO.Error> errors)
        {
            WorldTrait worldTrait = YamlIO.LoadFile<WorldTrait>(file, delegate (YamlIO.Error error, bool force_log_as_warning)
            {
                errors.Add(error);
            }, null);

            string text = FileHandleToScopedPath(file, path, prefix);
            worldTrait.filePath = text;
            DebugUtil.DevAssert(!traitsDict.ContainsKey(text), "Overwriting trait " + text + " already exists", null);
            traitsDict[text] = worldTrait;
        }
        private static string FileHandleToScopedPath(FileHandle file, string path, string prefix)
        {
            int num = FirstUncommonCharacter(path, file.full_path);
            string text = (num > -1) ? file.full_path.Substring(num) : file.full_path;
            text = Path.Combine(Path.GetDirectoryName(text), Path.GetFileNameWithoutExtension(text));
            text = text.Replace('\\', '/');
            return prefix + text;
        }
        private static int FirstUncommonCharacter(string a, string b)
        {
            int num = Mathf.Min(a.Length, b.Length);
            int num2 = -1;
            while (++num2 < num)
            {
                if (a[num2] != b[num2])
                {
                    return num2;
                }
            }
            return num2;
        }
        public static void TemplateCache_RewriteTemplateYaml_Postfix(string scopePath, ref string __result)
        {
            if (scopePath.StartsWith(MOD_TEMPLATE_PREFIX))
            {
                // Extract the part after 'mod__'
                var pathAfterPrefix = scopePath.Substring(MOD_TEMPLATE_PREFIX.Length);

                var fullPath = Instance.fullPath(FileSystem.Normalize(Path.Combine(STORY_TRAITS_TEMPLATE_PATH, pathAfterPrefix + ".yaml")));

                __result = fullPath;
            }
        }
        private static void TemplateSpawning_SpawnStoryTraitTemplates_Prefix(ref WorldGenSettings settings)
        {
            MutatedWorldData mutatedWorldData = Traverse.Create(settings).Field("mutatedWorldData").GetValue<MutatedWorldData>();
            if (mutatedWorldData == null || !traitsAddedToCandidates) return;
            if (mutatedWorldData.storyTraitCandidates == null) mutatedWorldData.storyTraitCandidates = new();
            foreach (var kvp in storyTraits)
            {
                mutatedWorldData.storyTraitCandidates.Add(kvp.Value);
                TbbDebuger.LogDebug($"[TbbStoryTraits] 添加故事特质[{kvp.Value.filePath}] 到 storyTraitCandidates");
            }
            traitsAddedToCandidates = true;
            return;
        }
    }
}
