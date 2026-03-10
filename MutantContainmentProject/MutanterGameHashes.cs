using System;

namespace MutantContainmentProject
{
    public static class MutanterGameHashes
    {
        // 畸变体收容相关事件
        public const int MutanterContained = 2147483647; // 最大整数值
        public const int MutanterBreachContained = 2147483646;
        
        // 其他可能的事件
        public const int MutanterSanityChanged = 2147483645;
        public const int MutanterAttack = 2147483644;
    }
}
