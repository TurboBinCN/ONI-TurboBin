using MutantContainmentProject.Buildings;
using System.Collections.Generic;
using TBB.He.TbbLib.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MutantContainmentProject.SideScreen
{
    public class ContainmentMonitorSideScreen : TbbSideScreenContent
    {
        [TbbSideScreen.CopyField, SerializeField] private GameObject rowContainer;
        [TbbSideScreen.CopyField, SerializeField] private HierarchyReferences rowPrefab;
        [TbbSideScreen.CopyField, SerializeField] private LocText message;
        [TbbSideScreen.CopyField, SerializeField] private GameObject contents;

        private ContainmentMonitor.Instance target;
        private List<HierarchyReferences> rows = new List<HierarchyReferences>();

        private static Dictionary<string, SecureAction> _actionButtons = new();

        protected override void OnSpawn()
        {
            base.OnSpawn();
            //按钮：本能/沟通/洞察/压迫
            _actionButtons.Add(STRINGS.UI.UISIDESCREENS.CONTAINTMENTMONITOR.ACTION_INSTINCT, SecureAction.Instinct);
            _actionButtons.Add(STRINGS.UI.UISIDESCREENS.CONTAINTMENTMONITOR.ACTION_COMMUNICATION, SecureAction.Communicate);
            _actionButtons.Add(STRINGS.UI.UISIDESCREENS.CONTAINTMENTMONITOR.ACTION_RECONNAISSANCE, SecureAction.Reconnaissance);
            _actionButtons.Add(STRINGS.UI.UISIDESCREENS.CONTAINTMENTMONITOR.ACTION_INTIMIDATION, SecureAction.Intimidation);

            this.Refresh();
        }
        public override bool IsValidForTarget(GameObject target)
        {
            return target?.GetComponent<KPrefabID>()?.IsPrefabID(ContainmentMonitorStationConfig.ID) == true;
        }

        public override void SetTarget(GameObject target)
        {
            this.target = target.GetSMI<ContainmentMonitor.Instance>(); // 先获取 SMI
            target.GetComponent<ContainmentMonitorWorkable>();
            this.Refresh();
        }
        private void Refresh()
        {
            if (this.target == null)
            {
                return;
            }
            this.DrawActionMenu();
        }
        private void DrawActionMenu()
        {
            int num = 0;
            foreach (var kvp in _actionButtons)
            {
                HierarchyReferences hierarchyReferences;
                if (num < this.rows.Count)
                {
                    hierarchyReferences = this.rows[num];
                }
                else
                {
                    hierarchyReferences = Util.KInstantiateUI<HierarchyReferences>(this.rowPrefab.gameObject, this.rowContainer, false);
                    this.rows.Add(hierarchyReferences);
                }
                this.ConfigureButton(hierarchyReferences, kvp.Key);
                this.rows[num].gameObject.SetActive(true);
                num++;
            }
            for (int i = num; i < this.rows.Count; i++)
            {
                this.rows[i].gameObject.SetActive(false);
            }

            this.message.text = STRINGS.UI.UISIDESCREENS.CONTAINTMENTMONITOR.ACTION;
            this.contents.gameObject.SetActive(true);
        }
        private void ConfigureButton(HierarchyReferences button, string text)
        {
            TMP_Text reference = button.GetReference<LocText>("Label");
            Image reference2 = button.GetReference<Image>("Icon");
            LocText reference3 = button.GetReference<LocText>("ProgressLabel");
            button.GetReference<ToolTip>("ToolTip");


            reference.text = text;
            //reference2.sprite = Def.GetUISprite(seedID, "ui", false).first;
            bool isActive = (this.target.CurrentAction == _actionButtons[text]);
            reference3.text = isActive ? STRINGS.UI.UISIDESCREENS.CONTAINTMENTMONITOR.ACTION_ON : STRINGS.UI.UISIDESCREENS.CONTAINTMENTMONITOR.ACTION_OFF;

            KToggle component = button.GetComponent<KToggle>();
            component.isOn = isActive;

            component.ClearOnClick();
            component.onClick += delegate ()
            {
                this.target.CurrentAction = _actionButtons[text];
                this.Refresh();
            };
        }
        public override string GetTitle() => STRINGS.UI.UISIDESCREENS.CONTAINTMENTMONITOR.TITLE;

    }
}
