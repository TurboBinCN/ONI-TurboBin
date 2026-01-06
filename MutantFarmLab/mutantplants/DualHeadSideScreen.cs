using HarmonyLib;
using PeterHan.PLib.Core;
using STRINGS;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace MutantFarmLab
{
    public class DualHeadSideScreen : MonoBehaviour
    {
        // 按钮文本与样式配置
        private const string BTN_DISPLAY_TEXT = "种植第二株";
        private const int MAX_DUAL_PLANT_COUNT = 2;
        private static readonly Color BTN_BACKGROUND_COLOR = new Color32(71, 139, 202, 255);
        private static readonly Color BTN_TEXT_COLOR = Color.white;
        private const int BTN_TEXT_SIZE = 14;

        // 核心组件引用
        private Button _dualPlantButton;
        private PlantablePlot _targetPlantPlot;
        private SingleEntityReceptacle _targetReceptacle;
        private PlanterSideScreen _planterSideScreen;
        private DetailsScreen _detailsScreen;
        private Operational _plotOperational;

        // 新增：标记是否为自定义操作（避免原生销毁）
        public static bool IsCustomPlantOperation = false;

        #region 单例初始化
        public static DualHeadSideScreen Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                DestroyImmediate(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 获取全局DetailsScreen（游戏主详情面板）
            _detailsScreen = GameObject.FindObjectOfType<DetailsScreen>();
        }
        #endregion

        #region 初始化入口（绑定目标种植盆和侧边屏）
        public void Init(GameObject targetPlotObj, GameObject planterSideScreenRoot)
        {
            if (targetPlotObj == null || planterSideScreenRoot == null)
            {
                PUtil.LogError("[第二株按钮] 初始化失败：目标地块/侧边屏为空");
                return;
            }

            // 获取核心组件
            _targetPlantPlot = targetPlotObj.GetComponent<PlantablePlot>();
            _targetReceptacle = targetPlotObj.GetComponent<SingleEntityReceptacle>();
            _planterSideScreen = planterSideScreenRoot.GetComponent<PlanterSideScreen>();
            _plotOperational = targetPlotObj.GetComponent<Operational>(); // 新增：获取运行状态组件

            // 校验核心组件
            if (_targetPlantPlot == null || _targetReceptacle == null)
            {
                PUtil.LogError("[第二株按钮] 初始化失败：缺少PlantablePlot/SingleEntityReceptacle组件");
                return;
            }
            if (_planterSideScreen == null)
            {
                PUtil.LogError("[第二株按钮] 初始化失败：未找到PlanterSideScreen");
                return;
            }
            if (_detailsScreen == null)
            {
                PUtil.LogError("[第二株按钮] 初始化失败：未找到DetailsScreen");
                return;
            }
            if (_plotOperational == null)
            {
                PUtil.LogWarning("[第二株按钮] 未找到Operational组件，使用默认状态");
            }

            // 查找按钮容器并创建按钮
            Transform buttonArea = FindTargetButtonArea(planterSideScreenRoot);
            if (buttonArea == null)
            {
                PUtil.LogError("[第二株按钮] 初始化失败：未找到ButtonArea容器");
                return;
            }

            if (_dualPlantButton == null)
            {
                CreateDualPlantButton(buttonArea.gameObject);
                PUtil.LogDebug("[第二株按钮] 按钮创建成功");
            }

            // 激活按钮
            _dualPlantButton.gameObject.SetActive(true);
            _dualPlantButton.interactable = true;
            _dualPlantButton.transform.SetAsLastSibling();

            PUtil.LogDebug("[第二株按钮] 初始化完成 ✔️");
        }
        #endregion

        #region 查找按钮容器
        private Transform FindTargetButtonArea(GameObject planterSideScreenRoot)
        {
            // 查找PlanterSideScreen的Contents/ButtonArea
            Transform contentsTrans = planterSideScreenRoot.transform.Find("Contents");
            if (contentsTrans == null)
            {
                PUtil.LogError("[第二株按钮] 未找到Contents容器");
                return null;
            }

            Transform buttonAreaTrans = contentsTrans.Find("ButtonArea");
            if (buttonAreaTrans != null)
                return buttonAreaTrans;

            // 兜底：查找所有包含ButtonArea的子节点
            foreach (Transform child in contentsTrans)
            {
                if (child.name.Equals("ButtonArea", StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }

            return null;
        }
        #endregion

        #region 创建按钮UI
        private void CreateDualPlantButton(GameObject parentButtonArea)
        {
            // 1. 创建按钮GameObject
            GameObject btnObj = new GameObject("DualPlantButton");
            btnObj.transform.SetParent(parentButtonArea.transform, false);
            btnObj.layer = LayerMask.NameToLayer("UI");

            // 2. 添加RectTransform
            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0f);
            btnRect.anchorMax = new Vector2(0.8f, 0f);
            btnRect.pivot = new Vector2(0.5f, 0f);
            btnRect.sizeDelta = new Vector2(0f, 35f);
            btnRect.anchoredPosition = new Vector2(0f, 8f);

            // 3. 添加背景图片
            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.color = BTN_BACKGROUND_COLOR;
            btnBg.type = Image.Type.Sliced;

            // 4. 添加Button组件
            _dualPlantButton = btnObj.AddComponent<Button>();
            _dualPlantButton.navigation = Navigation.defaultNavigation;
            _dualPlantButton.onClick.AddListener(OnDualPlantButtonClick);

            // 5. 添加按钮文本
            CreateButtonText(btnObj);
        }

        private void CreateButtonText(GameObject btnObj)
        {
            GameObject textObj = new GameObject("ButtonText");
            textObj.transform.SetParent(btnObj.transform, false);
            textObj.layer = LayerMask.NameToLayer("UI");

            Text text = textObj.AddComponent<Text>();
            text.text = BTN_DISPLAY_TEXT;
            text.color = BTN_TEXT_COLOR;
            text.fontSize = BTN_TEXT_SIZE;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;

            // 文本自适应按钮大小
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5f, 3f);
            textRect.offsetMax = new Vector2(-5f, -3f);
        }
        #endregion

        #region 按钮点击核心逻辑（修复种植按钮灰色问题）
        private void OnDualPlantButtonClick()
        {
            PUtil.LogDebug("[第二株按钮] 点击触发，执行清空种植盆逻辑 ✔️");

            try
            {
                // 核心标记：自定义操作，禁止销毁植株
                IsCustomPlantOperation = true;

                // 步骤1：只清空种植盆，不调用原生拔除逻辑
                ClearPlantablePlotWithoutDestroy();

                // 步骤2：重置种植盆状态（关键修复：恢复可种植状态）
                ResetPlantablePlotState();

                // 步骤3：强制刷新UI和PlanterSideScreen（修复按钮灰色）
                RefreshSideScreenFull();

                // 重置标记
                IsCustomPlantOperation = false;

                PUtil.LogDebug("[第二株按钮] 操作完成，已切回选择植株界面 ✔️");
            }
            catch (Exception ex)
            {
                // 异常时重置标记，避免影响原生逻辑
                IsCustomPlantOperation = false;
                PUtil.LogError($"[第二株按钮] 执行异常【{ex.GetType().Name}】：{ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 核心修复：只清空种植盆关联，不销毁植株本体
        /// </summary>
        private void ClearPlantablePlotWithoutDestroy()
        {
            // 1. 获取当前植株（使用公开API，无反射）
            GameObject currentPlant = _targetReceptacle.Occupant;
            if (currentPlant == null)
            {
                PUtil.LogDebug("[第二株按钮] 种植盆无植株，无需清空");
                return;
            }

            try
            {
                // 2. 优先使用公开API解除关联
                if (currentPlant.TryGetComponent(out Assignable assignable))
                {
                    assignable.Unassign();
                    PUtil.LogDebug("[第二株按钮] 已通过Assignable.Unassign解除植株关联");
                }

                // 3. 解除PlantablePlot与植株的绑定
                SetPrivateField(_targetPlantPlot, "plantRef", new Ref<KPrefabID>());
                InvokeProtectedMethodWithParams(_targetReceptacle, "UnsubscribeFromOccupant", Type.EmptyTypes);

                // 4. 清空SingleEntityReceptacle的核心字段
                SetPrivateField(_targetReceptacle, "occupyingObject", null);
                SetPrivateField(_targetReceptacle, "occupyObjectRef", new Ref<KSelectable>());

                // 5. 取消植株销毁标记
                Uprootable uprootable = currentPlant.GetComponent<Uprootable>();
                if (uprootable != null)
                {
                    uprootable.ForceCancelUproot();
                    SetPrivateField(uprootable, "isMarkedForUproot", false);
                    SetPrivateField(uprootable, "chore", null);
                    PUtil.LogDebug("[第二株按钮] 已取消Uprootable销毁标记和任务");
                }

                // 6. 解除植株父物体关联，保留植株本体
                currentPlant.transform.SetParent(null);

                PUtil.LogDebug("[第二株按钮] 已清空种植盆关联，保留植株本体");
            }
            catch (Exception ex)
            {
                PUtil.LogWarning($"[第二株按钮] 清空种植盆时警告：{ex.Message}");
            }
        }

        /// <summary>
        /// 关键修复：完全重置种植盆状态，恢复可种植能力
        /// </summary>
        private void ResetPlantablePlotState()
        {
            try
            {
                // 1. 基础重置（原有逻辑）
                _targetReceptacle.CancelActiveRequest();
                _targetPlantPlot.SetPreview(Tag.Invalid, false);
                InvokeUpdateStatusItemWithoutParams(_targetReceptacle);

                // 2. 关键修复1：重置requestedEntityTag为无效（允许重新选择种子）
                SetPrivateField(_targetReceptacle, "requestedEntityTag", Tag.Invalid);
                SetPrivateField(_targetReceptacle, "requestedEntityAdditionalFilterTag", Tag.Invalid);
                // 在 ResetPlantablePlotState() 中添加：
                SetPrivateField(_targetPlantPlot, "isRemoving", false);

                // 3. 关键修复2：恢复种植盆运行状态为可操作
                if (_plotOperational != null && !_plotOperational.IsOperational)
                {
                    _plotOperational.SetActive(true, false);
                    //_plotOperational.SetFlag(Operational.Flag.Type.Active, true);
                    PUtil.LogDebug("[第二株按钮] 已恢复种植盆Operational状态为激活");
                }

                // 4. 关键修复3：重置autoReplaceEntity为false（避免自动补种）
                SetPrivateField(_targetReceptacle, "autoReplaceEntity", false);
                SetPrivateField(_targetReceptacle, "activeRequest", null);
                // 5. 关键修复4：更新种植盆激活状态
                InvokeProtectedMethodWithParams(_targetReceptacle, "UpdateActive", Type.EmptyTypes);

                // 在 ResetPlantablePlotState() 最后添加：
                if (_targetPlantPlot != null)
                {
                    MethodInfo onSpawned = typeof(PlantablePlot).GetMethod("OnSpawned", BindingFlags.Instance | BindingFlags.NonPublic);
                    onSpawned?.Invoke(_targetPlantPlot, null);
                }

                PUtil.LogDebug("[第二株按钮] 种植盆状态已完全重置，恢复可种植能力");
            }
            catch (Exception ex)
            {
                PUtil.LogWarning($"[第二株按钮] 重置状态时警告：{ex.Message}");
            }
        }

        /// <summary>
        /// 增强版UI刷新：修复种植按钮灰色问题（适配PlanterSideScreen源码）
        /// </summary>
        private void RefreshSideScreenFull()
        {
            try
            {
                // 1. 强制刷新DetailsScreen选中状态
                MethodInfo onSelectionChanged = typeof(DetailsScreen).GetMethod(
                    "OnSelectionChanged",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                    null,
                    new[] { typeof(GameObject) },
                    null
                );

                if (onSelectionChanged != null)
                {
                    onSelectionChanged.Invoke(_detailsScreen, new object[] { null }); // 先取消选中
                    onSelectionChanged.Invoke(_detailsScreen, new object[] { _targetPlantPlot.gameObject }); // 重新选中
                    PUtil.LogDebug("[第二株按钮] 强制刷新DetailsScreen选中状态");
                }

                // 2. 重新初始化PlanterSideScreen（关键修复：适配源码无Refresh方法）
                if (_planterSideScreen != null)
                {
                    // 先清空原有目标
                    SetPrivateField(_planterSideScreen, "targetReceptacle", null);
                    // 重新绑定目标（调用公开的SetTarget方法）
                    _planterSideScreen.SetTarget(_targetPlantPlot.gameObject);
                    // 调用PlanterSideScreen的UpdateState方法刷新状态
                    InvokeProtectedMethodWithParams(_planterSideScreen, "UpdateState", new[] { typeof(object) }, new object[] { null });
                    // 调用RefreshSubspeciesToggles刷新变异种子列表
                    InvokeProtectedMethodWithParams(_planterSideScreen, "RefreshSubspeciesToggles", Type.EmptyTypes);
                    PUtil.LogDebug("[第二株按钮] 已重新初始化PlanterSideScreen");
                }

                // 3. 强制刷新UI布局
                Canvas.ForceUpdateCanvases();
                if (_planterSideScreen != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(_planterSideScreen.GetComponent<RectTransform>());
                }

                // 4. 延迟刷新（兜底方案，确保UI完全更新）
                GameScheduler.Instance.Schedule("DualPlantRefresh", 0.1f, (_) =>
                {
                    if (_planterSideScreen != null)
                    {
                        // 延迟调用UpdateState确保状态同步
                        InvokeProtectedMethodWithParams(_planterSideScreen, "UpdateState", new[] { typeof(object) }, new object[] { null });
                    }
                    PUtil.LogDebug("[第二株按钮] 延迟刷新UI完成");
                });

            }
            catch (Exception ex)
            {
                PUtil.LogWarning($"[第二株按钮] 刷新界面时警告：{ex.Message}");
            }
        }
        #endregion

        #region 反射辅助方法
        private void InvokeProtectedMethodWithParams(object obj, string methodName, Type[] paramTypes, object[] parameters = null)
        {
            if (obj == null || string.IsNullOrEmpty(methodName))
                return;

            try
            {
                MethodInfo method = obj.GetType().GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                    null,
                    paramTypes ?? Type.EmptyTypes,
                    null
                );

                if (method != null)
                {
                    method.Invoke(obj, parameters ?? null);
                    PUtil.LogDebug($"[反射] 成功调用方法：{obj.GetType().Name}.{methodName}");
                }
                else
                {
                    PUtil.LogWarning($"[反射] 未找到方法：{obj.GetType().Name}.{methodName}");
                }
            }
            catch (Exception ex)
            {
                PUtil.LogWarning($"[反射] 调用{methodName}失败：{ex.Message}");
            }
        }

        private void InvokeUpdateStatusItemWithoutParams(object obj)
        {
            if (obj == null) return;

            try
            {
                MethodInfo method = obj.GetType().GetMethod(
                    "UpdateStatusItem",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                    null,
                    Type.EmptyTypes,
                    null
                );

                if (method != null)
                {
                    method.Invoke(obj, null);
                    PUtil.LogDebug("[反射] 成功调用无参数UpdateStatusItem");
                    return;
                }

                MethodInfo methodWithParam = obj.GetType().GetMethod(
                    "UpdateStatusItem",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                    null,
                    new[] { typeof(KSelectable) },
                    null
                );

                if (methodWithParam != null)
                {
                    KSelectable selectable = obj as KSelectable ?? ((Component)obj).GetComponent<KSelectable>();
                    methodWithParam.Invoke(obj, new object[] { selectable });
                    PUtil.LogDebug("[反射] 成功调用带参数UpdateStatusItem");
                }
            }
            catch (Exception ex)
            {
                PUtil.LogWarning($"[反射] 调用UpdateStatusItem失败：{ex.Message}");
            }
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            if (obj == null || string.IsNullOrEmpty(fieldName))
                return;

            try
            {
                Type objType = obj.GetType();
                FieldInfo field = objType.GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );

                if (field != null)
                {
                    field.SetValue(obj, value);
                    PUtil.LogDebug($"[反射] 成功设置字段：{objType.Name}.{field.Name} = {value ?? "null"}");
                }
                else
                {
                    PUtil.LogWarning($"[反射] 未找到字段：{objType.Name}.{fieldName}");
                }
            }
            catch (Exception ex)
            {
                PUtil.LogWarning($"[反射] 设置字段{fieldName}失败：{ex.Message}");
            }
        }
        #endregion

        #region 辅助方法：校验种植数量
        private bool IsDualPlantLimitReached()
        {
            try
            {
                int count = 0;
                foreach (PlantablePlot plot in FindObjectsOfType<PlantablePlot>())
                {
                    if (plot == null || plot.GetComponent<SingleEntityReceptacle>().Occupant == null) continue;
                    if (Vector3.Distance(plot.transform.position, _targetPlantPlot.transform.position) < 1f)
                    {
                        count++;
                    }
                }
                return count >= MAX_DUAL_PLANT_COUNT;
            }
            catch (Exception ex)
            {
                PUtil.LogWarning($"[第二株按钮] 计数检查失败：{ex.Message}");
                return false;
            }
        }

        public void Refresh()
        {
            RefreshButtonState();
        }

        public void RefreshButtonState()
        {
            if (_dualPlantButton == null) return;
            try
            {
                _dualPlantButton.interactable = !IsDualPlantLimitReached() && _targetReceptacle.Occupant != null;
                PUtil.LogDebug($"[第二株按钮] 状态刷新：{(IsDualPlantLimitReached() ? "禁用" : "启用")}");
            }
            catch (Exception ex)
            {
                PUtil.LogWarning($"[第二株按钮] 刷新状态失败：{ex.Message}");
                _dualPlantButton.interactable = true;
            }
        }
        #endregion

        #region 清理资源
        private void OnDestroy()
        {
            try
            {
                if (_dualPlantButton != null)
                {
                    Destroy(_dualPlantButton.gameObject);
                }
                Instance = null;
            }
            catch (Exception ex)
            {
                PUtil.LogWarning($"[第二株按钮] 销毁失败：{ex.Message}");
            }
        }
        #endregion
    }

    #region Harmony补丁：仅保留核心拦截逻辑（移除无效补丁）
    public static class DualPlantPatch
    {
        [HarmonyPatch(typeof(Uprootable), "Uproot")]
        public static class Uprootable_Uproot_Patch
        {
            public static bool Prefix(Uprootable __instance)
            {
                if (DualHeadSideScreen.IsCustomPlantOperation)
                {
                    PUtil.LogDebug("[补丁] 拦截原生Uproot逻辑，避免销毁植株");
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Uprootable), "MarkForUproot")]
        public static class Uprootable_MarkForUproot_Patch
        {
            public static bool Prefix(Uprootable __instance)
            {
                if (DualHeadSideScreen.IsCustomPlantOperation)
                {
                    PUtil.LogDebug("[补丁] 拦截MarkForUproot，避免标记销毁");
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(PlantablePlot), "OrderRemoveOccupant")]
        public static class PlantablePlot_OrderRemoveOccupant_Patch
        {
            public static bool Prefix(PlantablePlot __instance)
            {
                if (DualHeadSideScreen.IsCustomPlantOperation)
                {
                    PUtil.LogDebug("[补丁] 拦截OrderRemoveOccupant，避免原生拔除");
                    return false;
                }
                return true;
            }
        }
    }
    #endregion
}