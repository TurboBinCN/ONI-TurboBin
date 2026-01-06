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

        #region 按钮点击核心逻辑（拔除+重置+界面切换）
        private void OnDualPlantButtonClick()
        {
            PUtil.LogDebug("[第二株按钮] 点击触发，执行拔除逻辑 ✔️");

            try
            {
                // ========== 步骤1：拔除当前植株（原生流程） ==========
                RemoveCurrentPlant();

                // ========== 步骤2：重置种植盆状态 ==========
                ResetPlantablePlotState();

                // ========== 步骤3：强制刷新UI，切回选择植株界面 ==========
                RefreshSideScreen();

                PUtil.LogDebug("[第二株按钮] 操作完成，已切回选择植株界面 ✔️");
            }
            catch (Exception ex)
            {
                PUtil.LogError($"[第二株按钮] 执行异常【{ex.GetType().Name}】：{ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 拔除当前种植盆中的植株（原生流程）
        /// </summary>
        private void RemoveCurrentPlant()
        {
            // 1. 获取当前植株
            GameObject currentPlant = _targetPlantPlot.Occupant;
            if (currentPlant == null)
            {
                PUtil.LogDebug("[第二株按钮] 种植盆无植株，无需拔除");
                return;
            }

            // 2. 调用原生拔除逻辑（触发完整回调）
            Uprootable uprootable = currentPlant.GetComponent<Uprootable>();
            if (uprootable != null)
            {
                // 标记为拔除（触发原生事件）
                uprootable.MarkForUproot(true);
                // 立即执行拔除
                uprootable.Uproot();
                PUtil.LogDebug("[第二株按钮] 已调用原生Uproot方法拔除植株");
                return; // 拔除成功，无需兜底
            }

            // 兜底：通过反射调用无参数的ClearOccupant方法
            InvokeProtectedMethodWithParams(_targetReceptacle, "ClearOccupant", null);
            PUtil.LogDebug("[第二株按钮] 兜底清空植株（反射调用ClearOccupant）");
        }

        /// <summary>
        /// 重置种植盆状态（恢复到可选择种子的初始状态）
        /// </summary>
        private void ResetPlantablePlotState()
        {
            try
            {
                // 1. 取消当前种植请求（公开方法，可直接调用）
                _targetReceptacle.CancelActiveRequest();

                // 2. 重置请求Tag（通过反射设置私有字段）
                SetPrivateField(_targetPlantPlot, "requestedEntityTag", Tag.Invalid);
                SetPrivateField(_targetPlantPlot, "requestedEntityAdditionalFilterTag", Tag.Invalid);

                // 3. 清空预览（公开方法）
                _targetPlantPlot.SetPreview(Tag.Invalid, false);

                // 修复：调用无参数的UpdateStatusItem重载版本
                // 优先调用无参数版本，避免歧义
                InvokeUpdateStatusItemWithoutParams(_targetReceptacle);

                PUtil.LogDebug("[第二株按钮] 种植盆状态已重置");
            }
            catch (Exception ex)
            {
                PUtil.LogWarning($"[第二株按钮] 重置状态时警告：{ex.Message}");
                // 重置失败不影响核心功能，继续执行
            }
        }

        /// <summary>
        /// 刷新侧边屏，强制切回选择植株界面
        /// </summary>
        private void RefreshSideScreen()
        {
            try
            {
                // 1. 触发DetailsScreen选中变更（模拟玩家重新点击种植盆）
                MethodInfo onSelectionChanged = typeof(DetailsScreen).GetMethod(
                    "OnSelectionChanged",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                    null,
                    new[] { typeof(GameObject) },
                    null
                );

                if (onSelectionChanged != null)
                {
                    onSelectionChanged.Invoke(_detailsScreen, new object[] { _targetPlantPlot.gameObject });
                    PUtil.LogDebug("[第二株按钮] 触发OnSelectionChanged刷新界面");
                }

                // 2. 重新绑定PlanterSideScreen到种植盆
                _planterSideScreen.SetTarget(_targetPlantPlot.gameObject);

                // 3. 强制刷新UI布局
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(_planterSideScreen.GetComponent<RectTransform>());
            }
            catch (Exception ex)
            {
                PUtil.LogWarning($"[第二株按钮] 刷新界面时警告：{ex.Message}");
            }
        }
        #endregion

        #region 反射辅助方法（修复歧义匹配问题）
        /// <summary>
        /// 调用对象的受保护/私有方法（指定参数类型，避免歧义）
        /// </summary>
        /// <param name="obj">目标对象</param>
        /// <param name="methodName">方法名</param>
        /// <param name="paramTypes">参数类型数组（无参数传null）</param>
        /// <param name="parameters">方法参数（无参数传null）</param>
        private void InvokeProtectedMethodWithParams(object obj, string methodName, Type[] paramTypes, object[] parameters = null)
        {
            if (obj == null || string.IsNullOrEmpty(methodName))
                return;

            try
            {
                // 指定参数类型查找方法，避免歧义
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
                }
                else
                {
                    PUtil.LogError($"[反射] 未找到方法：{obj.GetType().Name}.{methodName} (参数类型：{(paramTypes == null ? "无参数" :  paramTypes)})");
                }
            }
            catch (AmbiguousMatchException)
            {
                PUtil.LogError($"[反射] 方法{methodName}存在多个重载，无法确定调用版本");
            }
            catch (Exception ex)
            {
                PUtil.LogError($"[反射] 调用{methodName}失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 调用无参数的UpdateStatusItem方法（专门处理歧义问题）
        /// </summary>
        private void InvokeUpdateStatusItemWithoutParams(object obj)
        {
            if (obj == null) return;

            try
            {
                // 方案1：优先查找无参数的UpdateStatusItem
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
                    return;
                }

                // 方案2：查找带KSelectable参数的版本，传入当前对象的KSelectable
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
                    return;
                }

                PUtil.LogWarning("[反射] 未找到可用的UpdateStatusItem重载版本");
            }
            catch (Exception ex)
            {
                PUtil.LogWarning($"[反射] 调用UpdateStatusItem失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 设置对象的私有/受保护字段值
        /// </summary>
        private void SetPrivateField(object obj, string fieldName, object value)
        {
            if (obj == null || string.IsNullOrEmpty(fieldName))
                return;

            try
            {
                FieldInfo field = obj.GetType().GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );

                if (field != null)
                {
                    field.SetValue(obj, value);
                }
                else
                {
                    PUtil.LogError($"[反射] 未找到字段：{obj.GetType().Name}.{fieldName}");
                }
            }
            catch (Exception ex)
            {
                PUtil.LogError($"[反射] 设置字段{fieldName}失败：{ex.Message}");
            }
        }
        #endregion

        #region 辅助方法：校验种植数量
        /// <summary>
        /// 检查是否达到双株种植上限（简化版）
        /// </summary>
        private bool IsDualPlantLimitReached()
        {
            try
            {
                int count = 0;
                // 查找当前种植盆周围1格内的植株数量
                foreach (PlantablePlot plot in FindObjectsOfType<PlantablePlot>())
                {
                    if (plot == null || plot.Occupant == null) continue;
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
                return false; // 计数失败默认未达上限
            }
        }

        /// <summary>
        /// 刷新方法（兼容Patch调用）
        /// </summary>
        public void Refresh()
        {
            RefreshButtonState();
        }

        /// <summary>
        /// 刷新按钮状态
        /// </summary>
        public void RefreshButtonState()
        {
            if (_dualPlantButton == null) return;
            try
            {
                _dualPlantButton.interactable = !IsDualPlantLimitReached() && _targetPlantPlot.Occupant != null;
                PUtil.LogDebug($"[第二株按钮] 状态刷新：{(IsDualPlantLimitReached() ? "禁用" : "启用")}");
            }
            catch (Exception ex)
            {
                PUtil.LogWarning($"[第二株按钮] 刷新状态失败：{ex.Message}");
                _dualPlantButton.interactable = true; // 兜底启用按钮
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

}