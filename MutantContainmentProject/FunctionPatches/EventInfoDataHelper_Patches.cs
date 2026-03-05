using HarmonyLib;


namespace MutantContainmentProject.FunctionPatches
{
    [HarmonyPatch(typeof(EventInfoDataHelper))]
    [HarmonyPatch("GenerateStoryTraitData")]
    public static class EventInfoDataHelper_GenerateStoryTraitData_Patches
    {
        // 新的扩展方法，允许指定动画名称
        public static EventInfoData GenerateStoryTraitDataWithAnim(
            string titleText,
            string descriptionText,
            string buttonText,
            string animFileName,
            EventInfoDataHelper.PopupType popupType,
            string mainAnim = "event", // 默认值保持兼容
            string buttonTooltip = null,
            UnityEngine.GameObject[] minions = null,
            System.Action callback = null)
        {
            EventInfoData storyTraitData = new EventInfoData(titleText, descriptionText, (HashedString)animFileName);
            storyTraitData.mainAnim = (HashedString)mainAnim; // 设置动画名称
            storyTraitData.minions = minions;
            switch (popupType)
            {
                case EventInfoDataHelper.PopupType.COMPLETE:
                    storyTraitData.showCallback = () => MusicManager.instance.PlaySong("Stinger_StoryTraitUnlock");
                    break;
                default:
                    storyTraitData.showCallback = () => KFMOD.PlayUISound(GlobalAssets.GetSound("StoryTrait_Activation_Popup"));
                    break;
            }
            EventInfoData.Option option = storyTraitData.AddOption(buttonText);
            option.callback = callback;
            option.tooltip = buttonTooltip;
            return storyTraitData;
        }
    }
}
