using HarmonyLib;
using System.Linq;

namespace MutantContainmentProject.Suits
{
    [HarmonyPatch(typeof(Blueprints), "Get")]
    public class BlackSuitBlueprintRegistry
    {
        public static void Postfix(Blueprints __result)
        {
            // 检查是否已经添加过black_suit相关蓝图，避免重复注册
            bool hasBlackSuit = __result.all.clothingItems.Any(item => 
                item.id == "top_black_suit" || item.id == "shoes_black_suit" || item.id == "plants_black_suit");
            
            if (!hasBlackSuit)
            {
                // 创建并添加BlackSuitBlueprintProvider
                var provider = new BlackSuitBlueprintProvider();
                __result.all.AddBlueprintsFrom(provider);
            }
        }
    }
}