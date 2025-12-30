using PeterHan.PLib.Core;
using System;
using UnityEngine;

namespace MutantFarmLab.tbbLibs
{
    /// <summary>
    /// 基于Plib风格的GameObject全量调试打印工具
    /// 适配ONI模组开发，无第三方依赖（仅Plib）
    /// </summary>
    public static class TbbDebuger
        {
        /// <summary>
        /// 打印GameObject的完整信息（自身+所有组件+所有子物体）
        /// </summary>
        /// <param name="rootObj">要调试的根GameObject</param>
        /// <param name="indentLevel">缩进层级（外部调用传0即可）</param>
        public static void PrintGameObjectFullInfo(GameObject rootObj, int indentLevel = 0)
        {
            // 1. 核心空值校验（Plib风格：优先防御性编程）
            if (rootObj == null)
            {
                PUtil.LogWarning("[PlibDebug] 传入的GameObject为空！");
                return;
            }

            // 生成缩进，保持层级清晰（Plib风格：格式化输出）
            string indent = new string(' ', indentLevel * 4);
            string separator = $"{indent}========================================";

            // 2. 打印GameObject基础信息
            PUtil.LogDebug(separator);
            PUtil.LogDebug($"{indent}[GameObject 基础信息]");
            PUtil.LogDebug($"{indent}名称: {rootObj.name}");
            PUtil.LogDebug($"{indent}实例ID: {rootObj.GetInstanceID()}");
            PUtil.LogDebug($"{indent}标签: {rootObj.tag}");
            PUtil.LogDebug($"{indent}层级: {rootObj.layer} ({LayerMask.LayerToName(rootObj.layer)})");
            PUtil.LogDebug($"{indent}激活状态: {rootObj.activeSelf}");
            PUtil.LogDebug($"{indent}场景: {rootObj.scene.name}");

            // 3. 遍历所有组件（使用GetComponentAtIndex替代扩展方法）
            PUtil.LogDebug($"{indent}[组件列表]");
            int componentCount = rootObj.GetComponentCount();
            PUtil.LogDebug($"{indent}组件总数: {componentCount}");

            for (int i = 0; i < componentCount; i++)
            {
                try
                {
                    // 使用GetComponentAtIndex获取指定索引的组件
                    Component component = rootObj.GetComponentAtIndex(i);
                    if (component == null)
                    {
                        PUtil.LogDebug($"{indent}→ 索引{i}: 空组件（Unity内置隐藏组件）");
                        continue;
                    }

                    // 打印组件类型信息
                    Type compType = component.GetType();
                    PUtil.LogDebug($"{indent}→ 索引{i}: {compType.FullName} (简称: {compType.Name})");
                }
                catch (Exception ex)
                {
                    // Plib风格：捕获异常但不中断流程，仅打印警告
                    PUtil.LogWarning($"{indent}→ 索引{i}: 获取组件失败: {ex.Message}");
                }
            }

            // 4. 遍历所有子物体（替代rootObj.GetChildren()）
            PUtil.LogDebug($"{indent}[子物体列表]");
            Transform rootTransform = rootObj.transform;
            int childCount = rootTransform.childCount;
            PUtil.LogDebug($"{indent}直接子物体数量: {childCount}");

            for (int i = 0; i < childCount; i++)
            {
                Transform childTransform = rootTransform.GetChild(i);
                if (childTransform == null || childTransform.gameObject == null)
                    continue;

                GameObject childObj = childTransform.gameObject;
                PUtil.LogDebug($"{indent}└── 子物体[{i}]: {childObj.name}");

                // 递归打印子物体的完整信息（缩进+1）
                PrintGameObjectFullInfo(childObj, indentLevel + 1);
            }

            PUtil.LogDebug(separator);
            PUtil.LogDebug(""); // 空行分隔不同GameObject的输出
        }

        /// <summary>
        /// 辅助方法：获取GameObject的组件总数（适配GetComponentAtIndex）
        /// </summary>
        /// <param name="gameObject">目标GameObject</param>
        /// <returns>组件总数</returns>
        private static int GetComponentCount(this GameObject gameObject)
        {
            if (gameObject == null) return 0;

            // 先通过GetComponents获取总数（GetComponentAtIndex需要索引范围）
            Component[] allComponents = gameObject.GetComponents<Component>();
            return allComponents.Length;
        }
    }
}
