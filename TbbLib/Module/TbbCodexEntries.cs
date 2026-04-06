using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TBB.He.TbbLib.Debuger;
using TBB.He.TbbLib.Utils;

namespace TBB.He.TbbLib.Module
{
    public class TbbCodexEntries : TbbModule<TbbCodexEntries>
    {
        private string MOD_DIRECTORY_ROOT = "";
        private static readonly string CODEX_FILE_PATH_RELATIVE = "assets/codex/";

        private List<string> customDirectories = new();

        protected override void Initialized()
        {
            MOD_DIRECTORY_ROOT = Mod.ContentPath + "/";

            Harmony.Patch(typeof(CodexCache), "CollectYAMLEntries",
                postfix: new HarmonyMethod(typeof(TbbCodexEntries), nameof(CodexCache_CollectYAMLEntitries_Postfix)));
        }
        public TbbCodexEntries AddDirectory(string directory)
        {
            customDirectories.Add(directory);
            return Instance;
        }
        public static void CodexCache_CollectYAMLEntitries_Postfix(List<CategoryEntry> categories)
        {
            try
            {
                var instance = Instance;
                if (instance == null)
                {
                    TbbDebuger.LogDebug("[TbbCodexEntries] 没有要加载的 Codex 条目.");
                    return;
                }

                // 保存原始的 baseEntryPath
                FieldInfo baseEntryPathField = typeof(CodexCache).GetField("baseEntryPath", BindingFlags.NonPublic | BindingFlags.Static);
                if (baseEntryPathField == null)
                {
                    TbbDebuger.LogWarning("[TbbCodexEntries] 反射获取属性'baseEntryPath'为NULL");
                    return;
                }

                string originalBaseEntryPath = (string)baseEntryPathField.GetValue(null);

                try
                {
                    // 确定要加载的目录列表
                    List<string> directoriesToLoad = new();

                    // 如果有自定义目录，只加载自定义目录
                    if (instance.customDirectories.Count > 0)
                    {
                        directoriesToLoad.AddRange(instance.customDirectories);
                    }
                    else
                    {
                        // 否则加载默认目录
                        directoriesToLoad.Add(CODEX_FILE_PATH_RELATIVE);
                    }

                    // 加载所有指定的目录
                    foreach (string directory in directoriesToLoad)
                    {
                        string codexPath = instance.FullPath(directory);
                        if (Directory.Exists(codexPath))
                        {
                            // 设置 baseEntryPath 为当前目录
                            baseEntryPathField.SetValue(null, codexPath);

                            // 加载根目录下的词条
                            TbbDebuger.LogDebug($"[TbbCodexEntries] 开始加载 codex 目录: {codexPath}");
                            foreach (CodexEntry entry in CodexCache.CollectEntries(""))
                            {
                                if (entry != null && entry.id != null && entry.contentContainers != null && Game.IsCorrectDlcActiveForCurrentSave((IHasDlcRestrictions)entry))
                                {
                                    if (CodexCache.entries.ContainsKey(CodexCache.FormatLinkID(entry.id)))
                                    {
                                        CodexCache.MergeEntry(entry.id, entry);
                                    }
                                    else
                                    {
                                        CodexCache.AddEntry(entry.id, entry, categories);
                                        entry.customContentLength = entry.contentContainers.Count;
                                    }
                                    TbbDebuger.LogDebug($"[TbbCodexEntries] 成功加载 Codex entry id: {entry.id}");
                                }
                            }

                            // 加载子目录下的词条
                            foreach (string subDirectory in Directory.GetDirectories(codexPath))
                            {
                                string folderName = Path.GetFileNameWithoutExtension(subDirectory);
                                TbbDebuger.LogDebug($"[TbbCodexEntries] 开始加载 codex 子目录: {folderName}");
                                foreach (CodexEntry entry in CodexCache.CollectEntries(folderName))
                                {
                                    if (entry != null && entry.id != null && entry.contentContainers != null && Game.IsCorrectDlcActiveForCurrentSave((IHasDlcRestrictions)entry))
                                    {
                                        if (CodexCache.entries.ContainsKey(CodexCache.FormatLinkID(entry.id)))
                                        {
                                            CodexCache.MergeEntry(entry.id, entry);
                                        }
                                        else
                                        {
                                            CodexCache.AddEntry(entry.id, entry, categories);
                                            entry.customContentLength = entry.contentContainers.Count;
                                        }
                                        TbbDebuger.LogDebug($"[TbbCodexEntries] 成功加载 Codex entry id: {entry.id} from folder: {folderName}");
                                    }
                                }
                            }
                        }
                        else
                        {
                            TbbDebuger.LogDebug($"[TbbCodexEntries] 目录不存在: {codexPath}");
                        }
                    }
                }
                finally
                {
                    // 恢复原始的 baseEntryPath
                    baseEntryPathField.SetValue(null, originalBaseEntryPath);
                }
            }
            catch (Exception e)
            {
                TbbDebuger.LogWarning($"[TbbCodexEntries] 错误: {e.Message}\n{e.StackTrace}");
            }
        }
        private string FullPath(string path)
        {
            return Path.Combine(MOD_DIRECTORY_ROOT, path);
        }
    }
}
