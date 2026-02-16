using Database;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TBB.He.TbbLib.Debuger;
using TBB.He.TbbLib.Utils;

namespace TBB.He.TbbLib.Module
{
    public class TbbStatusItems : TbbModule<TbbStatusItems>
    {
        private List<Type> statusItems = new();
        protected override void Initialized()
        {
            base.Initialized();

            Harmony.Patch(typeof(Db), "Initialize",
                postfix: new HarmonyMethod(typeof(TbbStatusItems), nameof(Db_Initialize_Postfix)));
        }
        public TbbStatusItems Add<TStatusItems>()
            where TStatusItems : StatusItems
        {
            Instance.statusItems.Add(typeof(TStatusItems));
            return Instance;
        }
        public static void Db_Initialize_Postfix()
        {
            foreach (var statusItemType in Instance.statusItems)
            {
                try
                {
                    ResourceSet resourceSetParent = Db.Get().Root;
                    ConstructorInfo constructor = statusItemType.GetConstructor(new Type[] { typeof(ResourceSet) });
                    if (constructor != null)
                    {
                        var statusItemInstance = constructor.Invoke(new object[] { resourceSetParent }) as StatusItems;
                        if (statusItemInstance != null) TbbDebuger.LogDebug($"初始化 StatusItem 成功 ：{statusItemType.FullName} ");
                    }
                    else
                    {
                        TbbDebuger.LogWarning($"创建 StatusItem 实例失败或类型转换失败: {statusItemType.FullName}");
                    }
                }
                catch (Exception ex) { TbbDebuger.LogWarning($"创建或初始化 StatusItem 失败: {statusItemType.FullName}, 错误: {ex.Message}"); }
            }

        }
    }
}
