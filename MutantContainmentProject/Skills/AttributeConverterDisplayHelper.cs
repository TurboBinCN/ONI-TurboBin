using MutantContainmentProject.Skills;
using Klei.AI;
using static GameUtil;

namespace MutantContainmentProject.Patches
{
    public static class AttributeConverterDisplayHelper
    {
        /// <summary>
        /// 获取属性转换器的基础值
        /// </summary>
        /// <param name="converterID">转换器ID</param>
        /// <returns>基础值</returns>
        public static float GetBaseValue(string converterID)
        {
            float baseValue = 1.0f; // 基础值默认为1
            
            // 根据转换器ID设置不同的基础值
            if (converterID == MutanterAttributeConverters.AttributeAttackDamageConverterID)
            {
                // 攻击伤害：系统默认值，来自DUPLICANTSTATS
                baseValue = 1.0f;
            }
            else if (converterID == MutanterAttributeConverters.AttributeAttackSpeedConverterID)
            {
                // 攻击速度：系统默认值，来自DUPLICANTSTATS
                baseValue = 2.0f;
            }
            else if (converterID == MutanterAttributeConverters.AttributePhysicalDefenseConverterID)
            {
                // 物理防御：系统可能有默认值，设为0
                baseValue = 0.0f;
            }
            else if (converterID == MutanterAttributeConverters.AttributeMentalDefenseConverterID)
            {
                // 精神防御：系统没有，默认值设为1
                baseValue = 1.0f;
            }
            else if (converterID == MutanterAttributeConverters.AttributeContainmentSpeedConverterID)
            {
                // 收容速度：系统没有，默认值设为1
                baseValue = 1.0f;
            }
            else if (converterID == MutanterAttributeConverters.AttributeSafetyMeasureSuccessRateConverterID)
            {
                // 安全措施成功率：系统没有，默认值设为1
                baseValue = 1.0f;
            }
            
            return baseValue;
        }

        /// <summary>
        /// 获取属性转换器的格式化显示值
        /// </summary>
        /// <param name="converter">属性转换器</param>
        /// <param name="converterID">转换器ID</param>
        /// <param name="value">转换后的值</param>
        /// <returns>格式化后的显示值</returns>
        public static string GetFormattedValue(AttributeConverter converter, string converterID, float value)
        {
            string formattedValue;
            
            if (converterID == MutanterAttributeConverters.AttributePhysicalDefenseConverterID || converterID == MutanterAttributeConverters.AttributeMentalDefenseConverterID)
            {
                // 防御属性：直接显示 "- 百分比"
                formattedValue = $"- {converter.formatter.GetFormattedValue(value, GameUtil.TimeSlice.None)}";
            }
            else if (converterID == MutanterAttributeConverters.AttributeSafetyMeasureSuccessRateConverterID)
            {
                // 安全措施成功率：直接显示 "百分比"
                formattedValue = converter.formatter.GetFormattedValue(value, GameUtil.TimeSlice.None);
            }
            else
            {
                // 获取基础值
                float baseValue = GetBaseValue(converterID);
                
                // 其他属性：基础值 + 倍率
                float finalValue = baseValue * (1.0f + value);
                formattedValue = $"{baseValue.ToString("0.0")} + {converter.formatter.GetFormattedValue(value, GameUtil.TimeSlice.None)} = {finalValue.ToString("0.0")}";
            }
            
            return formattedValue;
        }

        /// <summary>
        /// 获取值的颜色前缀
        /// </summary>
        /// <param name="value">值</param>
        /// <returns>颜色前缀</returns>
        public static string GetColorPrefix(float value)
        {
            return value > 0 ? "<color=#00ff00>" : "<color=#ff0000>";
        }
    }
}