using HarmonyLib;
using MutantContainmentProject.Skills;
using UnityEngine;
using Klei.AI;
using static GameUtil;

namespace MutantContainmentProject.Patches
{
    [HarmonyPatch(typeof(MinionPersonalityPanel))]
    [HarmonyPatch("RefreshAttributesPanel")]
    public class MinionPersonalityPanelPatch
    {
        public static void Postfix(CollapsibleDetailContentPanel targetPanel, GameObject targetEntity)
        {
            try
            {
                if (targetPanel == null || targetEntity == null)
                    return;

                // 显示转换后的属性值
                try
                {
                    // 防御属性转换
                    DisplayAttributeConverter(targetPanel, targetEntity, MutanterAttributeConverters.AttributePhysicalDefenseConverterID);
                    DisplayAttributeConverter(targetPanel, targetEntity, MutanterAttributeConverters.AttributeMentalDefenseConverterID);

                    // 自律属性转换
                    DisplayAttributeConverter(targetPanel, targetEntity, MutanterAttributeConverters.AttributeContainmentSpeedConverterID);
                    DisplayAttributeConverter(targetPanel, targetEntity, MutanterAttributeConverters.AttributeSafetyMeasureSuccessRateConverterID);

                    // 正义属性转换
                    DisplayAttributeConverter(targetPanel, targetEntity, MutanterAttributeConverters.AttributeAttackDamageConverterID);
                    DisplayAttributeConverter(targetPanel, targetEntity, MutanterAttributeConverters.AttributeAttackSpeedConverterID);
                }
                catch { }
            }
            catch { }
        }

        private static void DisplayAttributeConverter(CollapsibleDetailContentPanel targetPanel, GameObject gameObject, string converterID)
        {
            try
            {
                if (targetPanel == null || gameObject == null || string.IsNullOrEmpty(converterID))
                    return;

                var converter = Db.Get().AttributeConverters.Get(converterID);
                if (converter != null)
                {
                    var converterInstance = converter.Lookup(gameObject);
                    if (converterInstance != null)
                    {
                        float value = converterInstance.Evaluate();
                        string colorPrefix = AttributeConverterDisplayHelper.GetColorPrefix(value);
                        string colorSuffix = "</color>";
                        
                        // 获取格式化显示值
                        string formattedValue = AttributeConverterDisplayHelper.GetFormattedValue(converter, converterID, value);
                        
                        targetPanel.SetLabel(converterID, $"{converter.Name}: {colorPrefix}{formattedValue}{colorSuffix}", converter.description);
                    }
                }
            }
            catch { }
        }
    }
}