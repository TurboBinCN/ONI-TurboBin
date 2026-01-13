using HarmonyLib;
using MutantFarmLab.mutantplants;
using MutantFarmLab.tbbLibs;
using PeterHan.PLib.Core;
using STRINGS;
using System;
using System.Reflection;
using TUNING;
using UnityEngine;
using UnityEngine.UI;

namespace MutantFarmLab
{
    /// <summary>
    /// 为种植盆添加“种植第二株”功能的侧边屏扩展按钮。
    /// 通过临时清空 receptacle 并重置状态，绕过原生单植株限制，实现双植株共存。
    /// </summary>
    public class DualHeadSideScreen : MonoBehaviour
    {
        // === 配置常量 ===
        //private const string BUTTON_TEXT = "第二株";

        private static readonly Color BUTTON_BG_COLOR = new Color32(62, 67, 87, 255);
        private static readonly Color BUTTON_TEXT_COLOR = Color.white;
        private const int BUTTON_FONT_SIZE = 12;

        // === 组件引用 ===
        private Button _dualPlantButton;
        private PlantablePlot _targetPlot;
        private SingleEntityReceptacle _targetReceptacle;
        private PlanterSideScreen _planterSideScreen;
        private DetailsScreen _detailsScreen;
        private Operational _plotOperational;

        // === 状态标记 ===
        public static bool IsCustomPlantOperation { get; set; } = false;

        // === 单例 ===
        public static DualHeadSideScreen Instance { get; private set; }

        #region 生命周期与初始化

        private void Awake()
        {
            if (Instance != null)
            {
                DestroyImmediate(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            _detailsScreen = FindObjectOfType<DetailsScreen>();
        }

        /// <summary>
        /// 初始化按钮并绑定到指定种植盆和 PlanterSideScreen。
        /// </summary>
        public void Init(GameObject plotObject, GameObject sideScreenRoot)
        {
            if (!ValidateInitialization(plotObject, sideScreenRoot))
                return;

            CacheComponents(plotObject, sideScreenRoot);
            CreateOrShowButton(sideScreenRoot);
            
            PUtil.LogDebug("[双头株] UI 初始化完成");
        }

        private bool ValidateInitialization(GameObject plot, GameObject screen)
        {
            if (plot == null || screen == null)
            {
                PUtil.LogWarning("[双头株] 初始化失败：目标对象为空");
                return false;
            }

            if (_detailsScreen == null)
            {
                PUtil.LogWarning("[双头株] 未找到 DetailsScreen");
                return false;
            }

            return true;
        }

        private void CacheComponents(GameObject plot, GameObject screen)
        {
            _targetPlot = plot.GetComponent<PlantablePlot>();
            _targetReceptacle = plot.GetComponent<SingleEntityReceptacle>();
            _planterSideScreen = screen.GetComponent<PlanterSideScreen>();
            _plotOperational = plot.GetComponent<Operational>();

            if (_targetPlot == null || _targetReceptacle == null)
                PUtil.LogError("[双头株] 缺少 PlantablePlot 或 SingleEntityReceptacle");

            if (_planterSideScreen == null)
                PUtil.LogError("[双头株] 未找到 PlanterSideScreen");
        }

        private void CreateOrShowButton(GameObject sideScreenRoot)
        {
            var buttonArea = FindButtonArea(sideScreenRoot);
            if (buttonArea == null)
            {
                PUtil.LogWarning("[双头株] 未找到 ButtonArea 容器");
                return;
            }
            if (_dualPlantButton == null)
                _dualPlantButton = CreateButton(buttonArea);

            _dualPlantButton.gameObject.SetActive(false);
            _dualPlantButton.interactable = true;
            _dualPlantButton.transform.SetAsLastSibling();
            RefreshButtonState();
        }

        #endregion

        #region UI 构建

        private Transform FindButtonArea(GameObject sideScreenRoot)
        {
            var contents = sideScreenRoot.transform.Find("Contents");
            if (contents == null) return null;

            var buttonArea = contents.Find("ButtonArea");
            if (buttonArea != null) return buttonArea;

            foreach (Transform child in contents)
            {
                if (string.Equals(child.name, "ButtonArea", StringComparison.OrdinalIgnoreCase))
                    return child;
            }

            return null;
        }

        private Button CreateButton(Transform parent)
        {
            var btnObj = new GameObject("DualPlantButton");
            btnObj.layer = LayerMask.NameToLayer("UI");
            btnObj.transform.SetParent(parent, false);

            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.03f, 0f);
            rect.anchorMax = new Vector2(0.3f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(0, 35);
            rect.anchoredPosition = new Vector2(0, 8);

            var image = btnObj.AddComponent<Image>();
            image.color = BUTTON_BG_COLOR;
            image.type = Image.Type.Sliced;

            var button = btnObj.AddComponent<Button>();
            button.navigation = Navigation.defaultNavigation;
            button.onClick.AddListener(HandleButtonClick);

            CreateButtonText(btnObj);
            return button;
        }

        private void CreateButtonText(GameObject parent)
        {
            var textObj = new GameObject("ButtonText");
            textObj.layer = LayerMask.NameToLayer("UI");
            textObj.transform.SetParent(parent.transform, false);

            var text = textObj.AddComponent<Text>();
            //text.text = BUTTON_TEXT;
            text.text = STRINGS.UI.UISIDESCREENS.SLIDERCONTROL.ANOTHER_PLANT;
            text.color = BUTTON_TEXT_COLOR;
            text.fontSize = BUTTON_FONT_SIZE;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;

            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var rect = textObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(5, 3);
            rect.offsetMax = new Vector2(-5, -3);
        }

        #endregion

        #region 按钮点击逻辑

        private void HandleButtonClick()
        {
            PUtil.LogDebug("[双头株] 按钮点击，开始清空种植盆（保留植株）");

            try
            {
                IsCustomPlantOperation = true;

                PlantMigrationHelper.ClearPlotWithoutDestroyingPlant(_targetPlot);
                PlantMigrationHelper.ResetPlotToPlantableState(_targetPlot,_plotOperational);
                RefreshUIAfterDelay();

                PUtil.LogDebug("[双头株] 操作完成，等待 UI 刷新");

            }
            catch (Exception ex)
            {
                PUtil.LogError($"[双头株] 操作异常: {ex}");
            }
            finally
            {
                IsCustomPlantOperation = false;
            }
        }

        private void RefreshUIAfterDelay()
        {
            // 触发 DetailsScreen 重新选中
            InvokeMethod(_detailsScreen, "OnSelectionChanged", null);
            InvokeMethod(_detailsScreen, "OnSelectionChanged", _targetPlot.gameObject);

            // 延迟刷新 PlanterSideScreen
            GameScheduler.Instance.Schedule("DualPlantRefresh", 0.35f, _ =>
            {
                if (_planterSideScreen != null && _targetPlot != null)
                {
                    _planterSideScreen.SetTarget(_targetPlot.gameObject);
                    InvokeMethod(_planterSideScreen, "UpdateState", (object)null);
                    InvokeMethod(_planterSideScreen, "RefreshSubspeciesToggles");

                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(
                        _planterSideScreen.GetComponent<RectTransform>()
                    );
                    PUtil.LogDebug("[双头株] UI 刷新完成");
                }
            });
        }

        #endregion


        private void InvokeMethod(object obj, string name, params object[] args)
        {
            if (obj == null) return;
            var types = args == null ? Type.EmptyTypes : Array.ConvertAll(args, a => a?.GetType() ?? typeof(object));
            var method = obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, types, null);
            method?.Invoke(obj, args);
        }
        public void RefreshButtonState()
        {
            bool active = true;
            if (_dualPlantButton == null || _targetReceptacle == null) return;

            //双株变异植株
            var marker = _targetPlot.GetComponent<DualHeadReceptacleMarker>();
            if (marker == null || marker.primaryPlant == null){
                active = false;
            }
            //双株已配对
            else if (marker.primaryPlant.GetComponent<DualHeadPlantComponent>().twin != null)
            {
                active = false;
            }

            _dualPlantButton.gameObject.SetActive(active); // 或者隐藏按钮
        }
        private void OnDestroy()
        {
            if (_dualPlantButton != null)
                Destroy(_dualPlantButton.gameObject);

            Instance = null;
        }

    }

    #region Harmony 补丁：防止自定义操作时触发原生销毁逻辑

    [HarmonyPatch]
    public static class DualPlantHarmonyPatches
    {
        [HarmonyPatch(typeof(Uprootable), nameof(Uprootable.Uproot))]
        [HarmonyPrefix]
        public static bool PreventUprootDuringCustomOperation(Uprootable __instance)
        {
            if (DualHeadSideScreen.IsCustomPlantOperation)
            {
                PUtil.LogDebug("[双头株] 拦截 Uproot");
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Uprootable), nameof(Uprootable.MarkForUproot))]
        [HarmonyPrefix]
        public static bool PreventMarkForUprootDuringCustomOperation(Uprootable __instance)
        {
            if (DualHeadSideScreen.IsCustomPlantOperation)
            {
                PUtil.LogDebug("[双头株] 拦截 MarkForUproot");
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(PlantablePlot), nameof(PlantablePlot.OrderRemoveOccupant))]
        [HarmonyPrefix]
        public static bool PreventOrderRemoveDuringCustomOperation(PlantablePlot __instance)
        {
            if (DualHeadSideScreen.IsCustomPlantOperation)
            {
                PUtil.LogDebug("[双头株] 拦截 OrderRemoveOccupant");
                return false;
            }
            return true;
        }
        [HarmonyPatch(typeof(PlantablePlot), "ValidPlant", MethodType.Getter)]
        public static class PlantablePlot_ValidPlant_Patch
        {
            public static bool Prefix(PlantablePlot __instance, ref bool __result)
            {
                var existing = __instance.Occupant;
                if (existing == null)
                {
                    __result = true; // 原生逻辑
                    return false;
                }

                // 检查是否是双头突变
                var mutant = existing.GetComponent<MutantPlant>();
                if (mutant?.MutationIDs.Contains(PlantMutationRegister.DUAL_HEAD_MUT_ID) == true)
                {
                    // ✅ 关键：允许再种一株（即认为 plot 仍“有效”）
                    __result = true;
                    return false;
                }

                // 非双头突变 → 原生逻辑：已占则无效
                __result = false;
                return false;
            }
        }
        [HarmonyPatch(typeof(PlanterSideScreen))]
        public static class DualHeadSideScreen_Patch
        {
            private static DualHeadSideScreen _sideScreen;

            [HarmonyPatch(nameof(PlanterSideScreen.SetTarget))]
            [HarmonyPostfix]
            public static void OnPlanterSideScreenOpen(PlanterSideScreen __instance, GameObject target)
            {
                if (_sideScreen == null)
                {
                    GameObject extObj = new GameObject("DualHeadSideScreen_Instance");
                    _sideScreen = extObj.AddComponent<DualHeadSideScreen>();
                }
                _sideScreen.Init(target, __instance.gameObject);

                // 🔁 如果 Refresh() 已被移除且 Init() 足够，则无需延迟刷新
                // 如仍需延迟初始化（例如依赖 LayoutRebuilder），可保留协程但不调用 Refresh
            }
        }
    }

    #endregion
}