using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TBB.He.TbbLib.Debuger;
using TBB.He.TbbLib.Utils;

namespace TBB.He.TbbLib.Module
{
    public class TbbTechTree : TbbModule<TbbTechTree>
    {
        private List<TechTreeCategoryInfo> categories = new();
        private List<TechNodeInfo> techNodes = new();
        private Dictionary<string, List<Tag>> searchTermKey = new();

        private static void Db_Initialize_Postfix()
        {
            Instance.RegisterTechTreeTitlesToDb();
            Instance.RegisterTechs();
        }
        private static void ResearchScreenSideBar_OnPrefabInit_Postfix(ResearchScreenSideBar __instance)
        {
            try
            {
                TbbDebuger.LogDebug("添加科技树侧边栏预设筛选项");
                Dictionary<string, List<Tag>> filterPresets = (Dictionary<string, List<Tag>>) TbbHarmonyExtension.GetField(__instance, "filterPresets");
                foreach(var kvp in Instance.searchTermKey)
                {
                    if (!filterPresets.ContainsKey(kvp.Key))
                    {
                        filterPresets[kvp.Key] = kvp.Value;
                        TbbDebuger.LogDebug($"成功添加预设筛选项: {kvp.Key} {Strings.Get("STRINGS.UI.RESEARCHSCREENFILTER_BUTTONS."+ kvp.Key)} with tags [{string.Join(", ", kvp.Value.Select(t => t.ToString()))}]");
                    }
                    else
                    {
                        TbbDebuger.LogWarning($"预设筛选项 {kvp.Key} 已存在，跳过添加");
                    }
                }
            }
            catch (System.Exception e)
            {
                TbbDebuger.LogError($"添加科技树侧边栏预设筛选项: {e.Message}");
                TbbDebuger.LogError(e.StackTrace);
            }
        }
        protected override void Initialized()
        {
            Harmony.Patch(typeof(Db), "Initialize",
                postfix: new HarmonyMethod(typeof(TbbTechTree), nameof(Db_Initialize_Postfix)));
            Harmony.Patch(typeof(ResearchScreenSideBar), "OnPrefabInit",
                postfix: new HarmonyMethod(typeof(TbbTechTree), nameof(ResearchScreenSideBar_OnPrefabInit_Postfix)));
        }

        public TbbTechTree AddCategory(TechTreeCategoryInfo categoryInfo)
        {
            categories.Add(categoryInfo);
            return this;
        }

        public TbbTechTree AddTech(TechNodeInfo techNodeInfo)
        {
            techNodes.Add(techNodeInfo);
            if(techNodeInfo.SearchTermKey != null)
            {
                if (!searchTermKey.ContainsKey(techNodeInfo.SearchTermKey))
                {
                    searchTermKey[techNodeInfo.SearchTermKey] = new List<Tag>();
                }
            }
            return this;
        }

        private void RegisterTechTreeTitlesToDb()
        {
            try
            {
                TbbDebuger.LogDebug("开始直接注册科技树分类到游戏数据库");

                if (Db.Get() == null || Db.Get().TechTreeTitles == null)
                {
                    TbbDebuger.LogError("Db或TechTreeTitles未初始化");
                    return;
                }

                int addedCount = 0;
                foreach (var category in categories)
                {
                    // 检查是否已存在
                    if (Db.Get().TechTreeTitles.Exists(category.Id))
                    {
                        TbbDebuger.LogDebug($"分类 {category.Id} 已存在，跳过");
                        continue;
                    }

                    // 动态计算位置，确保放到所有分类的最后
                    float minY = -8000f; // 默认Y坐标
                    float lastTitleX = 800f; // 默认X坐标

                    if (Db.Get().TechTreeTitles.resources.Count > 0)
                    {
                        // 找到最后的一行（即最小 Y 坐标的 title）
                        var lastTitle = Db.Get().TechTreeTitles.resources.OrderBy(tt => tt.center.y).FirstOrDefault();
                        if (lastTitle != null)
                        {
                            // 用最后一行的 center.y 坐标减去 height/2，确保新分类排在最后
                            minY = lastTitle.center.y - 600f; // 减去 100f 确保有足够的间距
                            lastTitleX = lastTitle.center.x;
                        }
                    }

                    float categoryX = lastTitleX;
                    float categoryY = minY;
                    float width = 300f; // 默认宽度
                    float height = 100f; // 默认高度

                    // 创建ResourceTreeNode
                    var resourceTreeNode = new ResourceTreeNode();
                    resourceTreeNode.Id = "_" + category.Id;
                    resourceTreeNode.Name = "_" + category.Id;
                    resourceTreeNode.nodeX = categoryX - width / 2f;  // 计算nodeX，使center.x = categoryX
                    resourceTreeNode.nodeY = categoryY + height / 2f;  // 计算nodeY，使center.y = categoryY
                    resourceTreeNode.width = width;
                    resourceTreeNode.height = height;

                    // 创建TechTreeTitle并添加到数据库
                    var techTreeTitle = new TechTreeTitle(
                        "_" + category.Id,
                        (ResourceSet)Db.Get().TechTreeTitles,
                        Strings.Get(category.NameKey),
                        resourceTreeNode
                    );

                    Db.Get().TechTreeTitles.resources.Add(techTreeTitle);
                    addedCount++;

                    TbbDebuger.LogDebug($"成功添加分类标题: {category.Id} - {Strings.Get(category.NameKey)} at ({categoryX}, {categoryY})");
                }

                TbbDebuger.LogDebug($"科技树分类注册完成，共添加 {addedCount} 个分类");
            }
            catch (System.Exception e)
            {
                TbbDebuger.LogError($"注册科技树分类失败: {e.Message}");
                TbbDebuger.LogError(e.StackTrace);
            }
        }

        private void RegisterTechs()
        {
            try
            {
                TbbDebuger.LogDebug("开始注册科技到游戏数据库");
                TbbDebuger.LogDebug($"科技节点数量: {techNodes.Count}");

                if (techNodes.Count == 0)
                {
                    TbbDebuger.LogWarning("没有科技节点需要注册");
                    return;
                }

                var techs = Db.Get().Techs;
                if (techs == null)
                {
                    TbbDebuger.LogError("Db.Get().Techs 返回null，无法注册科技");
                    return;
                }

                foreach (var nodeInfo in techNodes)
                {
                    TbbDebuger.LogDebug($"正在注册科技: {nodeInfo.Id} - {nodeInfo.Name}");

                    // 使用科技节点的成本，如果没有则使用默认值
                    Dictionary<string, float> costs = nodeInfo.Costs ?? new Dictionary<string, float>();
                    if (costs.Count == 0)
                    {
                        // 为基础科技设置基础研究点消耗
                        costs.Add(ResearchTypes.ID.BASIC, 100f);
                    }

                    // 创建科技对象
                    Tech tech = new Tech(nodeInfo.Id, nodeInfo.UnlockedItems, techs, costs);
                    if(nodeInfo.SearchTermKey != null) tech.AddSearchTerms(Strings.Get(nodeInfo.SearchTermKey));
                    TbbDebuger.LogDebug($"创建科技对象成功: {tech.Id} 搜索词条：[{(nodeInfo.SearchTermKey != null ?Strings.Get(nodeInfo.SearchTermKey):"")}]");
                    TbbDebuger.LogDebug($"科技消耗: {string.Join(", ", costs.Select(c => $"{c.Key}: {c.Value}"))}");

                    // 创建ResourceTreeNode并设置位置
                    var resourceTreeNode = new ResourceTreeNode();
                    resourceTreeNode.Id = nodeInfo.Id;
                    resourceTreeNode.Name = nodeInfo.Id;
                    resourceTreeNode.width = 200f;
                    resourceTreeNode.height = 100f;

                    // 查找对应的 TechTreeTitle，获取其坐标作为参考
                    var techTreeTitle = Db.Get().TechTreeTitles.resources.FirstOrDefault(tt => tt.Id == "_" + nodeInfo.CategoryId);
                    if (techTreeTitle != null)
                    {
                        // 科技节点的 x 坐标应该在分类 title 的右侧，留出足够的空间
                        float baseX = techTreeTitle.center.x + techTreeTitle.width / 2f + 100f;

                        if (nodeInfo.RequiredTech == null || nodeInfo.RequiredTech.Count == 0)
                        {
                            // 基础科技，放在分类右侧
                            resourceTreeNode.nodeX = baseX;
                        }
                        else
                        {
                            // 有依赖的科技，基于依赖科技的位置计算
                            foreach (var requiredTechId in nodeInfo.RequiredTech)
                            {
                                // 先在 techs 集合中查找依赖科技，因为 techs 集合中的节点已经有了实际的位置
                                var requiredTech = techs.TryGet(requiredTechId);
                                if (requiredTech != null && requiredTech.center != null)
                                {
                                    // 依赖科技的右侧，留出50间距
                                    float baseXForDependent = requiredTech.center.x + requiredTech.width + 50f;

                                    // 查找所有依赖同一个科技的节点，只关注我们自己模块中的节点
                                    int siblingCount = 0;
                                    float maxSiblingWidth = 200f;
                                    List<string> siblingIds = new List<string>();

                                    foreach (var techNodeInfo in techNodes)
                                    {
                                        if (techNodeInfo.RequiredTech != null && techNodeInfo.RequiredTech.Contains(requiredTechId))
                                        {
                                            siblingCount++;
                                            siblingIds.Add(techNodeInfo.Id);
                                        }
                                    }

                                    // 计算当前节点在兄弟节点中的索引
                                    int currentIndex = siblingIds.IndexOf(nodeInfo.Id);

                                    // 计算当前节点的X坐标，确保兄弟节点并排放置
                                    resourceTreeNode.nodeX = baseXForDependent + (maxSiblingWidth + 50f) * currentIndex; // 使用默认宽度200f
                                    break;
                                }
                            }
                        }

                        // 使用分类标题的 Y 坐标，确保科技节点与分类标题对齐
                        resourceTreeNode.nodeY = techTreeTitle.center.y + resourceTreeNode.height / 2f;
                    }
                    else
                    {
                        // 如果 TechTreeTitle 不存在，使用默认位置
                        resourceTreeNode.nodeX = 100f;
                        resourceTreeNode.nodeY = -8000f + resourceTreeNode.height / 2f;
                    }

                    // 设置科技节点的ResourceTreeNode
                    string categoryIdWithUnderscore = "_" + nodeInfo.CategoryId;
                    tech.SetNode(resourceTreeNode, categoryIdWithUnderscore);
                    TbbDebuger.LogDebug($"设置科技节点位置: ({resourceTreeNode.nodeX + resourceTreeNode.width / 2f}, {resourceTreeNode.nodeY - resourceTreeNode.height / 2f}) 分类: {categoryIdWithUnderscore}");

                    // 添加前置科技
                    TbbDebuger.LogDebug($"科技 {nodeInfo.Id} 的前置科技数量: {nodeInfo.RequiredTech?.Count ?? 0}");
                    if (nodeInfo.RequiredTech != null)
                    {
                        foreach (var requiredTechId in nodeInfo.RequiredTech)
                        {
                            TbbDebuger.LogDebug($"正在添加前置科技: {requiredTechId}");
                            Tech requiredTech = techs.TryGet(requiredTechId);
                            if (requiredTech != null)
                            {
                                tech.requiredTech.Add(requiredTech);
                                requiredTech.unlockedTech.Add(tech);
                                TbbDebuger.LogDebug($"成功添加前置科技: {requiredTechId}");
                            }
                            else
                            {
                                TbbDebuger.LogWarning($"前置科技 {requiredTechId} 不存在");
                            }
                        }
                    }

                    // 添加搜索词
                    tech.AddSearchTerms(nodeInfo.Name);
                    TbbDebuger.LogDebug($"添加搜索词成功: {nodeInfo.Name}");

                    // 输出注册信息
                    TbbDebuger.LogDebug($"注册科技成功: {tech.Id} - {tech.Name}");
                    TbbDebuger.LogDebug($"科技已添加到集合，当前科技数量: {techs.resources.Count}");
                }

                // 输出注册完成信息
                TbbDebuger.LogDebug($"共注册 {techNodes.Count} 个科技");
            }
            catch (System.Exception e)
            {
                TbbDebuger.LogError($"注册科技失败: {e.Message}");
                TbbDebuger.LogError(e.StackTrace);
            }
        }

        public class TechTreeCategoryInfo
        {
            public string Id { get; set; }
            public string NameKey { get; set; }

            public TechTreeCategoryInfo(string id, string nameKey)
            {
                Id = id;
                NameKey = nameKey;
            }
        }

        public class TechNodeInfo
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string CategoryId { get; set; }
            public List<string> UnlockedItems { get; set; }
            public List<string> RequiredTech { get; set; }
            public Dictionary<string, float> Costs { get; set; }
            public string SearchTermKey { get; set; }

            public TechNodeInfo(string id, string name, string description, string categoryId, List<string> unlockedItems = null, List<string> requiredTech = null, Dictionary<string, float> costs = null,string searchTermKey = null)
            {
                Id = id;
                Name = name;
                Description = description;
                CategoryId = categoryId;
                UnlockedItems = unlockedItems ?? new List<string>();
                RequiredTech = requiredTech ?? new List<string>();
                Costs = costs ?? new Dictionary<string, float>();
                SearchTermKey = searchTermKey ?? null;
            }
        }
    }
}