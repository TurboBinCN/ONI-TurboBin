using HarmonyLib;
using Klei;

namespace MutantFarmLab
{
    public static class StringInjectorPatch
    {
        [HarmonyPatch(typeof(EntityConfigManager), "LoadGeneratedEntities")]
        public class InjectAllStrings_Prefix
        {
            private static void Prefix()
            {
                // ✅ ========== 你的极简合法写法 ↓↓↓ 一行一个，直观无冗余 ==========
                // 食物文本注入
                Strings.Add("STRINGS.ITEMS.FOOD.RADSEED.NAME", STRINGS.ITEMS.FOOD.RADSEED.NAME);
                Strings.Add("STRINGS.ITEMS.FOOD.RADSEED.DESC", STRINGS.ITEMS.FOOD.RADSEED.DESC);
                // 辐射清零效果注入
                Strings.Add("STRINGS.DUPLICANTS.MODIFIERS.RADCLEAR.NAME", STRINGS.DUPLICANTS.MODIFIERS.RADCLEAR.NAME);
                Strings.Add("STRINGS.DUPLICANTS.MODIFIERS.RADCLEAR.DESCRIPTION", STRINGS.DUPLICANTS.MODIFIERS.RADCLEAR.DESCRIPTION);
                // 辐射免疫效果注入
                Strings.Add("STRINGS.DUPLICANTS.MODIFIERS.RADIMMUNITY.NAME", STRINGS.DUPLICANTS.MODIFIERS.RADIMMUNITY.NAME);
                Strings.Add("STRINGS.DUPLICANTS.MODIFIERS.RADIMMUNITY.DESCRIPTION", STRINGS.DUPLICANTS.MODIFIERS.RADIMMUNITY.DESCRIPTION);
            }
        }
    }
}