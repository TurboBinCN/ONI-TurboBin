using PeterHan.PLib.Core;
using PeterHan.PLib.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KSerialization;

namespace MutantFarmLab
{
    public enum MutantSeedID
    {
        MealwoodSeed,
        WheatSeed,
        SlimeMoldSeed,
        ThimbleReedSeed,
        BristleBlossomSeed,
        PinchaPepparSeed,
        SleetWheatSeed
    }

    public static class SeedInfo
    {
        public static readonly IReadOnlyDictionary<MutantSeedID, (string Name, bool IsMutant)> Data =
            new Dictionary<MutantSeedID, (string, bool)>()
            {
                { MutantSeedID.MealwoodSeed, ("羽叶薯蓣种子", true) },
                { MutantSeedID.WheatSeed, ("冰麦麦粒", false) },
                { MutantSeedID.SlimeMoldSeed, ("气囊芦荟种子", true) },
                { MutantSeedID.ThimbleReedSeed, ("顶针芦苇种子", true) },
                { MutantSeedID.BristleBlossomSeed, ("刺花种子", true) },
                { MutantSeedID.PinchaPepparSeed, ("胡椒种子", true) },
                { MutantSeedID.SleetWheatSeed, ("冰霜小麦种子", true) }
            };

        public static (string Name, bool IsMutant) Get(MutantSeedID id)
        {
            return Data.TryGetValue(id, out var info) ? info : ("未知种子", false);
        }
    }

    [SerializationConfig(MemberSerialization.OptIn)]
    public class MutantFarmLabSideScreen : SideScreenContent
    {
        // 固定尺寸常量
        private const float SCREEN_WIDTH = 280f;
        private const float ROW_HEIGHT = 40f;
        private const float SCROLL_HEIGHT = 300f;

        // UI控件引用（初始化时赋默认值）
        private GameObject rootPanel = null;
        private GameObject scrollView = null;
        private GameObject scrollContent = null;
        private Dictionary<MutantSeedID, GameObject> seedRows = new Dictionary<MutantSeedID, GameObject>();
        private Dictionary<MutantSeedID, bool> seedStates = new Dictionary<MutantSeedID, bool>();

        // ========== 核心修复：安全创建UI（无重复组件+无空引用） ==========
        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            try
            {
                // 1. 安全初始化状态字典（判空）
                if (seedStates == null)
                    seedStates = new Dictionary<MutantSeedID, bool>();
                if (seedRows == null)
                    seedRows = new Dictionary<MutantSeedID, GameObject>();

                seedStates.Clear();
                seedRows.Clear();
                foreach (MutantSeedID id in Enum.GetValues(typeof(MutantSeedID)))
                    seedStates[id] = true;

                // 2. 安全配置根节点（避免重复组件）
                RectTransform rootRect = gameObject.GetComponent<RectTransform>();
                if (rootRect == null)
                    rootRect = gameObject.AddComponent<RectTransform>();
                // 关键：SideScreen标准锚点（解决飘出）
                rootRect.anchorMin = new Vector2(1, 0);
                rootRect.anchorMax = new Vector2(1, 1);
                rootRect.pivot = new Vector2(1, 0.5f);
                rootRect.sizeDelta = new Vector2(SCREEN_WIDTH, 0);
                rootRect.anchoredPosition = Vector2.zero;

                // 3. 安全清空子物体（判空）
                if (transform != null)
                {
                    for (int i = transform.childCount - 1; i >= 0; i--)
                    {
                        Transform child = transform.GetChild(i);
                        if (child != null)
                            DestroyImmediate(child.gameObject);
                    }
                }

                // 4. 安全创建根面板（核心修复：避免重复RectTransform）
                rootPanel = PUIElements.CreateUI(gameObject, "RootPanel");
                if (rootPanel == null)
                {
                    PUtil.LogError("根面板创建失败");
                    return;
                }
                // 安全获取RectTransform（不重复添加）
                RectTransform rootPanelRect = rootPanel.GetComponent<RectTransform>();
                if (rootPanelRect == null)
                    rootPanelRect = rootPanel.AddComponent<RectTransform>();
                rootPanelRect.anchorMin = Vector2.zero;
                rootPanelRect.anchorMax = Vector2.one;
                rootPanelRect.sizeDelta = Vector2.zero;

                // 根面板背景（解决图层覆盖）
                Image rootBg = rootPanel.GetComponent<Image>();
                if (rootBg == null)
                    rootBg = rootPanel.AddComponent<Image>();
                rootBg.color = new Color(0.08f, 0.08f, 0.08f, 1f);
                rootBg.type = Image.Type.Sliced;

                // 根面板布局（安全添加）
                VerticalLayoutGroup rootLayout = rootPanel.GetComponent<VerticalLayoutGroup>();
                if (rootLayout == null)
                    rootLayout = rootPanel.AddComponent<VerticalLayoutGroup>();
                rootLayout.spacing = 5;
                rootLayout.padding = new RectOffset(10, 10, 10, 10);
                rootLayout.childControlWidth = true;
                rootLayout.childControlHeight = true;
                rootLayout.childForceExpandWidth = true;
                rootLayout.childForceExpandHeight = false;

                // 5. 安全创建标题（无空引用）
                GameObject titleObj = PUIElements.CreateUI(rootPanel, "TitleText");
                if (titleObj == null)
                {
                    PUtil.LogError("标题创建失败");
                    return;
                }
                TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
                if (titleText == null)
                    titleText = titleObj.AddComponent<TextMeshProUGUI>();
                titleText.text = "选择要分析的种子类型：";
                titleText.fontSize = 16;
                titleText.color = Color.white;
                titleText.alignment = TextAlignmentOptions.Left;

                LayoutElement titleLayout = titleObj.GetComponent<LayoutElement>();
                if (titleLayout == null)
                    titleLayout = titleObj.AddComponent<LayoutElement>();
                titleLayout.minHeight = 30;
                titleLayout.flexibleWidth = 1f;

                // 6. 安全创建滚动容器（核心修复）
                scrollView = PUIElements.CreateUI(rootPanel, "ScrollView");
                if (scrollView == null)
                {
                    PUtil.LogError("滚动容器创建失败");
                    return;
                }
                ScrollRect scrollRect = scrollView.GetComponent<ScrollRect>();
                if (scrollRect == null)
                    scrollRect = scrollView.AddComponent<ScrollRect>();
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

                LayoutElement scrollLayout = scrollView.GetComponent<LayoutElement>();
                if (scrollLayout == null)
                    scrollLayout = scrollView.AddComponent<LayoutElement>();
                scrollLayout.minWidth = SCREEN_WIDTH - 20;
                scrollLayout.minHeight = SCROLL_HEIGHT;
                scrollLayout.flexibleWidth = 1f;

                // 7. 安全创建滚动内容（无空引用）
                scrollContent = PUIElements.CreateUI(scrollView, "ScrollContent");
                if (scrollContent == null)
                {
                    PUtil.LogError("滚动内容创建失败");
                    return;
                }
                RectTransform contentRect = scrollContent.GetComponent<RectTransform>();
                if (contentRect == null)
                    contentRect = scrollContent.AddComponent<RectTransform>();
                contentRect.anchorMin = Vector2.zero;
                contentRect.anchorMax = new Vector2(1, 0);
                contentRect.pivot = new Vector2(0, 1);

                VerticalLayoutGroup contentLayout = scrollContent.GetComponent<VerticalLayoutGroup>();
                if (contentLayout == null)
                    contentLayout = scrollContent.AddComponent<VerticalLayoutGroup>();
                contentLayout.spacing = 2;
                contentLayout.childControlWidth = true;
                contentLayout.childControlHeight = true;
                contentLayout.childForceExpandWidth = true;

                ContentSizeFitter contentFitter = scrollContent.GetComponent<ContentSizeFitter>();
                if (contentFitter == null)
                    contentFitter = scrollContent.AddComponent<ContentSizeFitter>();
                contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                // 8. 安全创建滚动条（无重复组件）
                GameObject scrollbarObj = PUIElements.CreateUI(scrollView, "Scrollbar");
                if (scrollbarObj == null)
                {
                    PUtil.LogError("滚动条创建失败");
                    return;
                }
                Scrollbar scrollbar = scrollbarObj.GetComponent<Scrollbar>();
                if (scrollbar == null)
                    scrollbar = scrollbarObj.AddComponent<Scrollbar>();
                scrollbar.direction = Scrollbar.Direction.BottomToTop;

                RectTransform scrollbarRect = scrollbarObj.GetComponent<RectTransform>();
                if (scrollbarRect == null)
                    scrollbarRect = scrollbarObj.AddComponent<RectTransform>();
                scrollbarRect.anchorMin = new Vector2(1, 0);
                scrollbarRect.anchorMax = new Vector2(1, 1);
                scrollbarRect.sizeDelta = new Vector2(8, 0);

                Image scrollbarImage = scrollbarObj.GetComponent<Image>();
                if (scrollbarImage == null)
                    scrollbarImage = scrollbarObj.AddComponent<Image>();
                scrollbarImage.color = new Color(0.2f, 0.2f, 0.2f);

                // 关联滚动条（判空）
                if (scrollRect != null && contentRect != null && scrollbar != null)
                {
                    scrollRect.content = contentRect;
                    scrollRect.verticalScrollbar = scrollbar;
                }

                // 9. 安全创建种子行（逐个判空）
                foreach (MutantSeedID id in Enum.GetValues(typeof(MutantSeedID)))
                {
                    if (scrollContent != null)
                        CreateSeedRow(id);
                    else
                        PUtil.LogWarning($"跳过种子行 {id} 创建：滚动内容为空");
                }
            }
            catch (Exception e)
            {
                PUtil.LogError($"UI初始化错误: {e.Message}\n{e.StackTrace}");
            }
        }

        // ========== 安全创建种子行（无重复组件+无空引用） ==========
        private void CreateSeedRow(MutantSeedID id)
        {
            try
            {
                var seedInfo = SeedInfo.Get(id);

                // 1. 安全创建行对象
                GameObject rowObj = PUIElements.CreateUI(scrollContent, $"SeedRow_{id}");
                if (rowObj == null)
                {
                    PUtil.LogError($"种子行 {id} 创建失败");
                    return;
                }
                seedRows[id] = rowObj;

                // 2. 安全配置行RectTransform（不重复添加）
                RectTransform rowRect = rowObj.GetComponent<RectTransform>();
                if (rowRect == null)
                    rowRect = rowObj.AddComponent<RectTransform>();
                rowRect.anchorMin = Vector2.zero;
                rowRect.anchorMax = new Vector2(1, 0);
                rowRect.sizeDelta = new Vector2(0, ROW_HEIGHT);

                // 3. 安全添加行背景
                Image rowBg = rowObj.GetComponent<Image>();
                if (rowBg == null)
                    rowBg = rowObj.AddComponent<Image>();
                rowBg.color = seedStates[id] ? new Color(0.2f, 0.15f, 0.25f) : new Color(0.12f, 0.12f, 0.12f);
                rowBg.type = Image.Type.Sliced;

                // 4. 安全添加按钮
                Button rowButton = rowObj.GetComponent<Button>();
                if (rowButton == null)
                    rowButton = rowObj.AddComponent<Button>();
                rowButton.targetGraphic = rowBg;
                // 闭包捕获当前id（避免循环变量问题）
                MutantSeedID currentId = id;
                rowButton.onClick.AddListener(() =>
                {
                    try
                    {
                        seedStates[currentId] = !seedStates[currentId];
                        // 安全更新背景
                        if (rowBg != null)
                            rowBg.color = seedStates[currentId] ? new Color(0.2f, 0.15f, 0.25f) : new Color(0.12f, 0.12f, 0.12f);
                        // 安全更新状态
                        UpdateSeedStatus(rowObj, seedStates[currentId]);
                    }
                    catch (Exception e)
                    {
                        PUtil.LogError($"点击种子行 {currentId} 错误: {e.Message}");
                    }
                });

                // 5. 安全添加行布局
                HorizontalLayoutGroup rowLayout = rowObj.GetComponent<HorizontalLayoutGroup>();
                if (rowLayout == null)
                    rowLayout = rowObj.AddComponent<HorizontalLayoutGroup>();
                rowLayout.spacing = 8;
                rowLayout.padding = new RectOffset(8, 8, 4, 4);
                rowLayout.childControlWidth = true;
                rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = true;

                // 6. 安全创建图标
                GameObject iconObj = PUIElements.CreateUI(rowObj, "SeedIcon");
                if (iconObj != null)
                {
                    LayoutElement iconLayout = iconObj.GetComponent<LayoutElement>();
                    if (iconLayout == null)
                        iconLayout = iconObj.AddComponent<LayoutElement>();
                    iconLayout.minWidth = 24;
                    iconLayout.minHeight = 24;
                    iconLayout.flexibleWidth = 0;

                    Image iconImage = iconObj.GetComponent<Image>();
                    if (iconImage == null)
                        iconImage = iconObj.AddComponent<Image>();
                    iconImage.color = seedInfo.IsMutant ? new Color(0.7f, 0.2f, 0.7f) : new Color(0.2f, 0.5f, 0.7f);
                    iconImage.preserveAspect = true;

                    // 圆形遮罩（安全添加）
                    Mask mask = iconObj.GetComponent<Mask>();
                    if (mask == null)
                        mask = iconObj.AddComponent<Mask>();
                    mask.showMaskGraphic = false;

                    GameObject circleObj = PUIElements.CreateUI(iconObj, "CircleMask");
                    if (circleObj != null)
                    {
                        Image circleImage = circleObj.GetComponent<Image>();
                        if (circleImage == null)
                            circleImage = circleObj.AddComponent<Image>();
                        circleImage.color = Color.white;

                        RectTransform circleRect = circleObj.GetComponent<RectTransform>();
                        if (circleRect == null)
                            circleRect = circleObj.AddComponent<RectTransform>();
                        circleRect.anchorMin = Vector2.zero;
                        circleRect.anchorMax = Vector2.one;
                    }
                }

                // 7. 安全创建名称文本（确保显示）
                GameObject nameObj = PUIElements.CreateUI(rowObj, "SeedName");
                if (nameObj != null)
                {
                    LayoutElement nameLayout = nameObj.GetComponent<LayoutElement>();
                    if (nameLayout == null)
                        nameLayout = nameObj.AddComponent<LayoutElement>();
                    nameLayout.flexibleWidth = 1f;
                    nameLayout.minHeight = 20;

                    TextMeshProUGUI nameText = nameObj.GetComponent<TextMeshProUGUI>();
                    if (nameText == null)
                        nameText = nameObj.AddComponent<TextMeshProUGUI>();
                    nameText.text = seedInfo.Name; // 直接赋值，无缺失
                    nameText.fontSize = 14;
                    nameText.color = Color.white;
                    nameText.alignment = TextAlignmentOptions.Left;
                    nameText.overflowMode = TextOverflowModes.Ellipsis;
                }

                // 8. 安全创建状态文本
                GameObject statusObj = PUIElements.CreateUI(rowObj, "SeedStatus");
                if (statusObj != null)
                {
                    LayoutElement statusLayout = statusObj.GetComponent<LayoutElement>();
                    if (statusLayout == null)
                        statusLayout = statusObj.AddComponent<LayoutElement>();
                    statusLayout.minWidth = 80;
                    statusLayout.flexibleWidth = 0;
                    statusLayout.minHeight = 20;

                    TextMeshProUGUI statusText = statusObj.GetComponent<TextMeshProUGUI>();
                    if (statusText == null)
                        statusText = statusObj.AddComponent<TextMeshProUGUI>();
                    statusText.text = seedStates[id] ? "将会分析" : "不会分析";
                    statusText.fontSize = 12;
                    statusText.color = seedStates[id] ? Color.white : Color.gray;
                    statusText.alignment = TextAlignmentOptions.Right;

                    // 存储引用（安全添加）
                    SeedStatusRef refComp = rowObj.GetComponent<SeedStatusRef>();
                    if (refComp == null)
                        refComp = rowObj.AddComponent<SeedStatusRef>();
                    refComp.statusText = statusText;
                }
            }
            catch (Exception e)
            {
                PUtil.LogError($"创建种子行 {id} 错误: {e.Message}");
            }
        }

        // ========== 安全更新种子状态 ==========
        private void UpdateSeedStatus(GameObject rowObj, bool isEnabled)
        {
            if (rowObj == null)
                return;

            SeedStatusRef refComp = rowObj.GetComponent<SeedStatusRef>();
            if (refComp != null && refComp.statusText != null)
            {
                refComp.statusText.text = isEnabled ? "将会分析" : "不会分析";
                refComp.statusText.color = isEnabled ? Color.white : Color.gray;
            }
        }

        // ========== 状态文本引用组件 ==========
        private class SeedStatusRef : MonoBehaviour
        {
            public TextMeshProUGUI statusText;
        }

        // ========== SideScreen核心方法（安全判空） ==========
        public override bool IsValidForTarget(GameObject target)
        {
            if (target == null) return false;

            KPrefabID kPrefabID = target.GetComponent<KPrefabID>();
            if (kPrefabID == null) return false;

            // 仅作用于自定义建筑，排除植物分析仪
            return kPrefabID.HasTag(TagManager.Create("MutantFarmLab")) &&
                   !kPrefabID.HasTag(TagManager.Create("PlantAnalyzer"));
        }

        public override string GetTitle()
        {
            return "可变异种子管理";
        }

        public override void SetTarget(GameObject target)
        {
            base.SetTarget(target);
            // 安全激活UI
            if (rootPanel != null)
                rootPanel.SetActive(true);
        }

        // ========== 安全清理资源 ==========
        protected override void OnCleanUp()
        {
            base.OnCleanUp();
            try
            {
                if (seedRows != null)
                {
                    foreach (var row in seedRows.Values)
                    {
                        if (row != null)
                            Destroy(row);
                    }
                    seedRows.Clear();
                }

                if (seedStates != null)
                    seedStates.Clear();

                if (rootPanel != null)
                    Destroy(rootPanel);
            }
            catch (Exception e)
            {
                PUtil.LogError($"清理资源错误: {e.Message}");
            }
        }
    }
}