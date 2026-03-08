using HarmonyLib;
using MutantContainmentProject;
using MutantContainmentProject.Skills;
using UnityEngine;
using Klei.AI;

namespace MutantContainmentProject.Patches
{
    [HarmonyPatch(typeof(SkillMinionWidget))]
    [HarmonyPatch("RefreshToolTip")]
    public class SkillMinionWidgetPatch
    {
        public static void Postfix(SkillMinionWidget __instance, MinionResume resume)
        {
            try
            {
                if (__instance == null || resume == null || resume.gameObject == null)
                    return;

                if (Db.Get() == null || Db.Get().AttributeConverters == null)
                    return;

                // 获取ToolTip组件
                ToolTip tooltip = __instance.GetComponent<ToolTip>();
                if (tooltip == null)
                    return;

                // 显示转换后的属性值
                try
                {
                    // 直接添加到现有的tooltip内容中，而不是清除它
                    tooltip.AddMultiStringTooltip("\n" + STRINGS.SKILLS.CONVERTED_ATTRIBUTES + "\n\n", null);
                    
                    // 防御属性转换
                    DisplayAttributeConverter(tooltip, resume.gameObject, MutanterAttributeConverters.AttributePhysicalDefenseConverterID);
                    DisplayAttributeConverter(tooltip, resume.gameObject, MutanterAttributeConverters.AttributeMentalDefenseConverterID);

                    // 自律属性转换
                    DisplayAttributeConverter(tooltip, resume.gameObject, MutanterAttributeConverters.AttributeContainmentSpeedConverterID);
                    DisplayAttributeConverter(tooltip, resume.gameObject, MutanterAttributeConverters.AttributeSafetyMeasureSuccessRateConverterID);

                    // 正义属性转换
                    DisplayAttributeConverter(tooltip, resume.gameObject, MutanterAttributeConverters.AttributeAttackDamageConverterID);
                    DisplayAttributeConverter(tooltip, resume.gameObject, MutanterAttributeConverters.AttributeAttackSpeedConverterID);
                }
                catch { }
            }
            catch { }
        }

        private static void DisplayAttributeConverter(ToolTip tooltip, GameObject gameObject, string converterID)
        {
            try
            {
                if (tooltip == null || gameObject == null || string.IsNullOrEmpty(converterID))
                    return;

                var converter = Db.Get().AttributeConverters.Get(converterID);
                if (converter != null)
                {
                    var converterInstance = converter.Lookup(gameObject);
                    if (converterInstance != null)
                    {
                        float value = converterInstance.Evaluate();
                        string colorPrefix = value > 0 ? "<color=#00ff00>" : "<color=#ff0000>";
                        string colorSuffix = "</color>";
                        tooltip.AddMultiStringTooltip($"    • {converter.Name}: {colorPrefix}{converter.formatter.GetFormattedValue(value, GameUtil.TimeSlice.None)}{colorSuffix}", null);
                    }
                }
            }
            catch { }
        }
    }
}
