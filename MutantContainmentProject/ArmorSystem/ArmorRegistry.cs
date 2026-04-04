using HarmonyLib;

namespace MutantContainmentProject.ArmorSystem
{
    //[HarmonyPatch(typeof(Game), "OnLoad")]
    public class ArmorRegistry
    {
        public static void Postfix()
        {
            // 初始化防具数据库
            _ = ArmorDB.Instance;
            
            // 确保ArmorManager存在
            _ = ArmorManager.Instance;
        }
    }
}
