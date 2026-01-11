using HarmonyLib;
using MutantFarmLab.mutantplants;
using MutantFarmLab.tbbLibs;
using PeterHan.PLib.Core;
using STRINGS;
using System;
using System.Reflection;
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
        private const string BUTTON_TEXT = "第二株";
        private static readonly Color BUTTON_BG_COLOR = new Color32(71, 139, 202, 255);
        private static readonly Color BUTTON_TEXT_COLOR = Color.black;
        private const int BUTTON_FONT_SIZE = 14;

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
            
            PUtil.LogDebug("[DualHead] UI 初始化完成");
            PUtil.LogDebug("[双头株] Init 调试块开始");
            TbbDebuger.PrintGameObjectFullInfo(plotObject);

            var plant = plotObject.GetComponent<PlantablePlot>().Occupant;
            if(plant != null){
            TbbDebuger.PrintGameObjectFullInfo(plant);

            var mdk = plant.GetComponent<Storage>();

                PUtil.LogDebug($"[双头株] plant Name :[{plant.name}] Storagename:[{plant.GetComponent<Storage>().name}] mkd name:[{mdk.name} mdk storage name:[{mdk.storageNetworkID}]filters:[{mdk.storageFilters}]]");
            }
            PUtil.LogDebug("[双头株] Init 调试块结束");
        }

        private bool ValidateInitialization(GameObject plot, GameObject screen)
        {
            if (plot == null || screen == null)
            {
                PUtil.LogWarning("[DualHead] 初始化失败：目标对象为空");
                return false;
            }

            if (_detailsScreen == null)
            {
                PUtil.LogWarning("[DualHead] 未找到 DetailsScreen");
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
                PUtil.LogError("[DualHead] 缺少 PlantablePlot 或 SingleEntityReceptacle");

            if (_planterSideScreen == null)
                PUtil.LogError("[DualHead] 未找到 PlanterSideScreen");
        }

        private void CreateOrShowButton(GameObject sideScreenRoot)
        {
            var buttonArea = FindButtonArea(sideScreenRoot);
            if (buttonArea == null)
            {
                PUtil.LogWarning("[DualHead] 未找到 ButtonArea 容器");
                return;
            }
            if (_dualPlantButton == null)
                _dualPlantButton = CreateButton(buttonArea);

            _dualPlantButton.gameObject.SetActive(false);
            _dualPlantButton.interactable = false;
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
            rect.anchorMin = new Vector2(0.1f, 0f);
            rect.anchorMax = new Vector2(0.4f, 0f);
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
            text.text = BUTTON_TEXT;
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
            PUtil.LogDebug("[DualHead] 按钮点击，开始清空种植盆（保留植株）");

            try
            {
                IsCustomPlantOperation = true;

                ClearPlotWithoutDestroyingPlant();
                ResetPlotToPlantableState();
                RefreshUIAfterDelay();

                PUtil.LogDebug("[DualHead] 操作完成，等待 UI 刷新");
            }
            catch (Exception ex)
            {
                PUtil.LogError($"[DualHead] 操作异常: {ex}");
            }
            finally
            {
                IsCustomPlantOperation = false;
            }
        }

        private void ClearPlotWithoutDestroyingPlant()
        {
            var currentPlant = _targetReceptacle?.Occupant;
            if (currentPlant == null)
            {
                PUtil.LogDebug("[DualHead] 种植盆已为空");
                return;
            }

            // 1. 解绑 Assignable
            if (currentPlant.TryGetComponent<Assignable>(out var assignable))
                assignable.Unassign();

            // 2. 取消 Uproot 标记
            if (currentPlant.TryGetComponent<Uprootable>(out var uprootable))
            {
                uprootable.ForceCancelUproot();
                SetField(uprootable, "isMarkedForUproot", false);
                SetField(uprootable, "chore", null);
            }

            // 3. 移出植株（不销毁）
            //currentPlant.transform.SetParent(null);

            // 4. 清空 receptacle 内部状态
            var receptacle = _targetReceptacle;
            SetField(receptacle, "occupyingObject", null);
            SetField(receptacle, "occupyObjectRef", new Ref<KSelectable>());
            SetField(receptacle, "activeRequest", null);
            SetField(receptacle, "autoReplaceEntity", false);
            SetField(receptacle, "requestedEntityTag", Tag.Invalid);
            SetField(receptacle, "requestedEntityAdditionalFilterTag", Tag.Invalid);

            // 5. 清空 PlantablePlot 的 plantRef
            ClearPlantRef();

            // 6. 调用内部清理方法
            InvokeMethod(receptacle, "UnsubscribeFromOccupant");
            InvokeMethod(receptacle, "UpdateActive");

            PUtil.LogDebug($"[DualHead] 已移出植株 '{currentPlant.name}' 并清空 receptacle");
        }

        private void ClearPlantRef()
        {
            var field = typeof(PlantablePlot).GetField("plantRef", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                var plantRef = (Ref<KPrefabID>)field.GetValue(_targetPlot);
                if (plantRef == null)
                {
                    plantRef = new Ref<KPrefabID>();
                    field.SetValue(_targetPlot, plantRef);
                }
                plantRef.Set(null);
                PUtil.LogDebug("[DualHead] plantRef 已设为 null");
            }
        }

        private void ResetPlotToPlantableState()
        {
            _targetPlot?.SetPreview(Tag.Invalid, false);

            if (_plotOperational != null && !_plotOperational.IsOperational)
                _plotOperational.SetActive(true, false);

            InvokeMethod(_targetReceptacle, "UpdateActive");
            InvokeUpdateStatusItem(_targetReceptacle);
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
                    PUtil.LogDebug("[DualHead] UI 刷新完成");
                }
            });
        }

        #endregion

        #region 反射工具方法

        private void SetField(object obj, string name, object value)
        {
            if (obj == null) return;
            var field = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null)
                field.SetValue(obj, value);
        }
        private static object GetField(object obj, string name)
        {
            if (obj == null) return null;
            var field = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null)
                return field.GetValue(obj);
            return null;
        }
        private void InvokeMethod(object obj, string name, params object[] args)
        {
            if (obj == null) return;
            var types = args == null ? Type.EmptyTypes : Array.ConvertAll(args, a => a?.GetType() ?? typeof(object));
            var method = obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, types, null);
            method?.Invoke(obj, args);
        }

        private void InvokeUpdateStatusItem(object obj)
        {
            if (obj == null) return;

            var noParam = obj.GetType().GetMethod("UpdateStatusItem", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, Type.EmptyTypes, null);
            if (noParam != null)
            {
                noParam.Invoke(obj, null);
                return;
            }

            var withParam = obj.GetType().GetMethod("UpdateStatusItem", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new[] { typeof(KSelectable) }, null);
            if (withParam != null)
            {
                var selectable = obj as KSelectable ?? ((Component)obj).GetComponent<KSelectable>();
                withParam.Invoke(obj, new object[] { selectable });
            }
        }

        #endregion

        #region 公共接口与清理

        //public void RefreshButtonState()
        //{
        //    if (_dualPlantButton != null && _targetReceptacle != null)
        //    {
        //        // ⚠️ 注意：原“双植株计数”逻辑不可靠（依赖距离），建议由 Mod 主逻辑控制
        //        // 此处仅根据当前是否已有植株决定按钮是否可用
        //        _dualPlantButton.interactable = _targetReceptacle.Occupant != null;
        //    }
        //}
        public void RefreshButtonState()
        {
            if (_dualPlantButton == null || _targetReceptacle == null) return;

            //双株变异植株
            var marker = _targetPlot.GetComponent<DualHeadReceptacleMarker>();
            if (marker == null || marker.primaryPlant == null) return;
            //双株已配对
            if (marker.primaryPlant.GetComponent<DualHeadPlantComponent>().twin != null) return;

            _dualPlantButton.interactable = true;
            _dualPlantButton.gameObject.SetActive(true); // 或者隐藏按钮
        }
        private void OnDestroy()
        {
            if (_dualPlantButton != null)
                Destroy(_dualPlantButton.gameObject);

            Instance = null;
        }

        #endregion
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
                PUtil.LogDebug("[Harmony] 拦截 Uproot");
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
                PUtil.LogDebug("[Harmony] 拦截 MarkForUproot");
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
                PUtil.LogDebug("[Harmony] 拦截 OrderRemoveOccupant");
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