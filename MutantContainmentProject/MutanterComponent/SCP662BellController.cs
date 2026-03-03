using KSerialization;
using MutantContainmentProject.Mutanters;
using System;
using UnityEngine;

namespace MutantContainmentProject
{
    public class SCP662BellController : KMonoBehaviour
    {
        public const string Id = "SCP662BellController";

        private const float COOLDOWN_TIME = 5 * 600f; // 5 * 600s = 3000s

        [Serialize]
        private float cooldownTimer = 0f;

        [Serialize]
        private bool isOnCooldown = false;

        private int onRefreshUserMenuHandle = -1;
        private int onUpdateHandle = -1;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            // Subscribe to events
            // 493375141: UserMenuRefresh event - triggered when the user menu needs to be refreshed
            onRefreshUserMenuHandle = Subscribe(493375141, new Action<object>(OnRefreshUserMenu));
            // -1697596308: OnStorageChange event - triggered every frame, used for timer updates
            onUpdateHandle = Subscribe(-1697596308, new Action<object>(OnUpdate));
        }

        protected override void OnCleanUp()
        {
            base.OnCleanUp();
            // Unsubscribe from events
            if (onRefreshUserMenuHandle != -1)
            {
                Unsubscribe(onRefreshUserMenuHandle);
                onRefreshUserMenuHandle = -1;
            }
            if (onUpdateHandle != -1)
            {
                Unsubscribe(onUpdateHandle);
                onUpdateHandle = -1;
            }
        }

        private void OnUpdate(object data)
        {
            if (isOnCooldown)
            {
                cooldownTimer -= Time.deltaTime;
                if (cooldownTimer <= 0f)
                {
                    isOnCooldown = false;
                    cooldownTimer = 0f;
                    // Refresh the user menu to update the button
                    Game.Instance.userMenu.Refresh(base.gameObject);
                }
            }
        }

        private void OnRefreshUserMenu(object data)
        {
            string tooltip;
            if (isOnCooldown)
            {
                tooltip = string.Format(MutantContainmentProject.STRINGS.UI.USERMENUACTIONS.SCP662.SUMMON_BUTTON_COOLDOWN_TOOLTIP, Mathf.CeilToInt(cooldownTimer));
                KIconButtonMenu.ButtonInfo button = new KIconButtonMenu.ButtonInfo("action_bell", MutantContainmentProject.STRINGS.UI.USERMENUACTIONS.SCP662.SUMMON_BUTTON_TEXT, null, global::Action.SandboxStoryTraitTool, null, null, null, tooltip, true);
                Game.Instance.userMenu.AddButton(base.gameObject, button, 0.4f);
            }
            else
            {
                tooltip = string.Format(STRINGS.UI.USERMENUACTIONS.SCP662.SUMMON_BUTTON_TOOLTIP, Mathf.CeilToInt(COOLDOWN_TIME));
                KIconButtonMenu.ButtonInfo button = new KIconButtonMenu.ButtonInfo("action_bell", STRINGS.UI.USERMENUACTIONS.SCP662.SUMMON_BUTTON_TEXT, new System.Action(SummonSCP6621), global::Action.SandboxStoryTraitTool, null, null, null, tooltip, true);
                Game.Instance.userMenu.AddButton(base.gameObject, button, 0.4f);
            }
        }

        private void SummonSCP6621()
        {
            // Get the spawn position near SCP-662
            Vector3 spawnPos = base.transform.position + new Vector3(1f, 0f, 0f);
            spawnPos = Grid.CellToPos(Grid.PosToCell(spawnPos));

            // Spawn SCP-662-1
            GameObject scp6621GO = Util.KInstantiate(Assets.GetPrefab(SCP662_1Config.ID), spawnPos);
            if (scp6621GO != null)
            {
                scp6621GO.SetActive(true);

                // Add the instance controller to SCP-662-1
                SCP6621InstanceController instanceController = scp6621GO.AddOrGet<SCP6621InstanceController>();
            }

            // Set cooldown
            isOnCooldown = true;
            cooldownTimer = COOLDOWN_TIME;

            // Refresh the user menu to update the button
            Game.Instance.userMenu.Refresh(base.gameObject);
        }
    }
}