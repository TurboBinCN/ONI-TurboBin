using MutantContainmentProject.Buildings;
using System.Collections.Generic;
using System.Linq;
using TBB.He.TbbLib.UI;
using UnityEngine;
using UnityEngine.UI;

namespace MutantContainmentProject.SideScreen
{
    public class ControlDepartmentConsoleSideScreen : TbbSideScreenContent
    {
        [TbbSideScreen.CopyField, SerializeField] private GameObject rowPrefab;
        [TbbSideScreen.CopyField, SerializeField] private RectTransform rowContainer;
        [TbbSideScreen.CopyField, SerializeField] private TextStyleSetting AnalyzedTextStyle;
        [TbbSideScreen.CopyField, SerializeField] private TextStyleSetting UnanalyzedTextStyle;

        private ControlDepartmentConsole.Instance targetConsole;
        private Dictionary<object, GameObject> rows = new();
        private int uiRefreshSubHandle = -1;
        private const int MAX_SELECTED_DUPES = 5;
        private LocText headerText;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            // 尝试找到Header文本元素并存储
            FindHeaderText();
        }

        private void FindHeaderText()
        {
            // 通过rowContainer往上查找，直到找到ControlDepartmentConsoleSideScreen节点
            Transform currentTransform = rowContainer.transform;
            while (currentTransform != null)
            {
                if (currentTransform.name == "ControlDepartmentConsoleSideScreen")
                {
                    // 在ControlDepartmentConsoleSideScreen节点中查找HeaderContainer/Header
                    Transform headerContainer = currentTransform.Find("HeaderContainer");
                    if (headerContainer != null)
                    {
                        Transform headerTransform = headerContainer.Find("Header");
                        if (headerTransform != null)
                        {
                            headerText = headerTransform.GetComponent<LocText>();
                            if (headerText == null)
                            {
                                // 如果直接找不到LocText，尝试查找子对象
                                headerText = headerTransform.GetComponentInChildren<LocText>();
                            }
                        }
                    }
                    break;
                }
                currentTransform = currentTransform.parent;
            }
        }

        protected override void OnShow(bool show)
        {
            base.OnShow(show);
            if (rowPrefab != null)
                rowPrefab.SetActive(false);
            if (!show)
                return;
            RefreshOptions();
        }

        public override bool IsValidForTarget(GameObject target)
        {
            return target?.GetComponent<KPrefabID>()?.IsPrefabID(ControlDepartmentConsoleConfig.ID) == true;
        }

        public override void SetTarget(GameObject target)
        {
            // 清除之前的目标和事件订阅
            if (uiRefreshSubHandle != -1 && targetConsole != null)
            {
                targetConsole.gameObject.Unsubscribe(uiRefreshSubHandle);
                uiRefreshSubHandle = -1;
            }

            // 设置新目标
            targetConsole = target.GetSMI<ControlDepartmentConsole.Instance>();
            if (targetConsole != null)
            {
                // 刷新界面
                RefreshOptions();

                // 订阅刷新事件
                uiRefreshSubHandle = target.Subscribe(1980521255, RefreshOptions);
            }
        }

        public override void ClearTarget()
        {
            if (uiRefreshSubHandle == -1 || targetConsole == null)
                return;
            targetConsole.gameObject.Unsubscribe(uiRefreshSubHandle);
            uiRefreshSubHandle = -1;
        }

        private void RefreshOptions(object data = null)
        {
            if (targetConsole == null)
                return;

            int num = 0;
            SetRow(num++, STRINGS.UI.UISIDESCREENS.CONTROLDEPARTMENTCONSOLE.NOTHING, Assets.GetSprite("action_building_disabled"), null, true);
            // 获取当前世界的所有复制人，排除放生人
            List<MinionIdentity> dupes = Components.MinionIdentities.Items
                .Where(d => d != null && d.gameObject != null && d.GetMyWorldId() == targetConsole.GetMyWorldId() && d.GetComponent<KPrefabID>().PrefabTag != BionicMinionConfig.MODEL)
                .ToList();

            // 设置Header文本
            SetHeaderText(STRINGS.BUILDINGS.PREFABS.CONTROLDEPARTMENTCONSOLE.SELECT_DUPES_TITLE);

            // 为每个复制人创建行
            foreach (MinionIdentity dupe in dupes)
            {
                SetRow(num++, dupe.name, Db.Get().Personalities.Get(dupe.personalityResourceId).GetMiniIcon(), dupe, true);
            }

            // 隐藏多余的行
            for (int index = num; index < rowContainer.childCount; ++index)
                rowContainer.GetChild(index).gameObject.SetActive(false);

        }

        private void SetHeaderText(string text)
        {
            // 如果headerText为null，尝试查找
            if (headerText == null)
            {
                FindHeaderText();
            }

            // 直接使用存储的headerText成员变量
            if (headerText != null)
            {
                headerText.text = text;
            }
        }

        private void SetRow(int idx, string name, Sprite icon, MinionIdentity dupe, bool studied)
        {
            bool flag = dupe == null;
            GameObject gameObject = idx >= rowContainer.childCount ? Util.KInstantiateUI(rowPrefab, rowContainer.gameObject, true) : rowContainer.GetChild(idx).gameObject;
            HierarchyReferences component1 = gameObject.GetComponent<HierarchyReferences>();
            LocText reference1 = component1.GetReference<LocText>("label");
            reference1.text = name;
            reference1.textStyleSetting = studied | flag ? AnalyzedTextStyle : UnanalyzedTextStyle;
            reference1.ApplySettings();
            Image reference2 = component1.GetReference<Image>("icon");
            reference2.sprite = icon;
            reference2.color = studied ? Color.white : new Color(0.0f, 0.0f, 0.0f, 0.5f);
            if (flag)
                reference2.color = Color.black;

            // 检查当前复制人是否已被选中
            bool isSelected = false;
            int selectedCount = 0;
            if (dupe != null)
            {
                isSelected = targetConsole.IsDupeSelected(dupe);
                selectedCount = targetConsole.GetSelectedDupeCount();
            }

            ToolTip[] componentsInChildren = gameObject.GetComponentsInChildren<ToolTip>();
            ToolTip toolTip1 = componentsInChildren.FirstOrDefault();
            if (toolTip1 != null)
            {
                if (dupe != null)
                {
                    toolTip1.SetSimpleTooltip(isSelected ? STRINGS.BUILDINGS.PREFABS.CONTROLDEPARTMENTCONSOLE.SELECTED_DUPES_TOOLTIP : STRINGS.BUILDINGS.PREFABS.CONTROLDEPARTMENTCONSOLE.SELECT_DUPES_TOOLTIP);
                }
                toolTip1.enabled = dupe != null;
            }

            LocText reference3 = component1.GetReference<LocText>("amount");
            reference3.gameObject.SetActive(false);

            MultiToggle component2 = gameObject.GetComponent<MultiToggle>();
            component2.ChangeState(isSelected ? 1 : 0);
            component2.onClick = () =>
            {
                if (dupe == null)
                    return;

                if (isSelected)
                {
                    // 取消选择
                    targetConsole.UnselectDupe(dupe);
                }
                else
                {
                    // 选择，但最多只能选择5个
                    if (selectedCount < MAX_SELECTED_DUPES)
                    {
                        targetConsole.SelectDupe(dupe);
                    }
                }
                RefreshOptions();
            };

            component2.onDoubleClick = () =>
            {
                if (dupe != null)
                {
                    GameUtil.FocusCamera(dupe.transform.GetPosition());
                    return true;
                }
                return false;
            };
        }

        public override string GetTitle() => STRINGS.BUILDINGS.PREFABS.CONTROLDEPARTMENTCONSOLE.NAME;
    }
}