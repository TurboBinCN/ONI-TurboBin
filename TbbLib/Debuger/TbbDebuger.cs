using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace TBB.He.TbbLib.Debuger
{
    public static class TbbDebuger
    {
        public enum LogLevel
        {
            None = 0,    // 关闭所有日志
            Error = 1,   // 仅输出错误日志
            Warning = 2, // 输出错误 + 警告日志
            Info = 3,    // 输出错误 + 警告 + 信息日志
            Debug = 4    // 输出所有日志（默认）
        }
        private enum LogType
        {
            Log,
            Warning,
            Error
        }
        public static LogLevel GlobalLogLevel { get; set; } = LogLevel.Debug;
        public static bool EnableErrorStackTrace { get; set; } = true;

        private static bool IsLevelAllowed(LogLevel level)
        {
            return GlobalLogLevel >= level && GlobalLogLevel != LogLevel.None;
        }
        private static void WriteLog(LogLevel level, LogType logType, string message, string assemblyName, bool ignoreLevel = false)
        {
            // 分级过滤：非强制日志需检查级别
            if (!ignoreLevel && !IsLevelAllowed(level)) return;

            // 按类型输出到 Unity 控制台
            switch (logType)
            {
                case LogType.Log:
                    Debug.LogFormat("[Tbb/{0}] {1}", assemblyName, message);
                    break;
                case LogType.Warning:
                    Debug.LogWarningFormat("[Tbb/{0}] {1}", assemblyName, message);
                    break;
                case LogType.Error:
                    Debug.LogErrorFormat("[PLib/{0}] {1}", assemblyName, message);
                    break;
            }
        }
        public static void LogDebug(object message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Assembly callingAssembly = Assembly.GetCallingAssembly();
            string fileName = Path.GetFileName(filePath); // 只获取文件名，去掉路径
            string logPrefix = $"{callingAssembly?.GetName()?.Name}:{fileName}:{memberName}({lineNumber})";
            WriteLog(LogLevel.Debug, LogType.Log, $"[DEBUG] {message}", logPrefix);
        }
        public static void LogWarning(object message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Assembly callingAssembly = Assembly.GetCallingAssembly();
            string fileName = Path.GetFileName(filePath); // 只获取文件名，去掉路径
            string logPrefix = $"{callingAssembly?.GetName()?.Name}:{fileName}:{memberName}({lineNumber})";
            WriteLog(LogLevel.Warning, LogType.Log, $"[WARNING] {message}", logPrefix);
        }
        public static void LogError(object message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Assembly callingAssembly = Assembly.GetCallingAssembly();
            string fileName = Path.GetFileName(filePath); // 只获取文件名，去掉路径
            string logPrefix = $"{callingAssembly?.GetName()?.Name}:{fileName}:{memberName}({lineNumber})";
            var errorMsg = $"[ERROR] {message}";
            if (EnableErrorStackTrace)
            {
                errorMsg += $"\n调用栈：{Environment.StackTrace}";
            }
            WriteLog(LogLevel.Warning, LogType.Log, $"[ERROR] {errorMsg}", logPrefix);
        }
        public static void LogForce(string message)
        {
            WriteLog(LogLevel.None, LogType.Log, $"[FORCE] {message}", Assembly.GetCallingAssembly()?.GetName()?.Name ?? "?", ignoreLevel: true);
        }
        public static void LogTrace()
        {
            WriteLog(LogLevel.Debug, LogType.Log, Environment.StackTrace, Assembly.GetCallingAssembly()?.GetName()?.Name ?? "?");
        }
        public static void LogGameObjectFullInfo(GameObject rootObj, int indentLevel = 4)
        {
            WriteLog(LogLevel.Debug, LogType.Log, GetGameObjectFullInfoString(rootObj, indentLevel), Assembly.GetCallingAssembly()?.GetName()?.Name ?? "?");
        }
        /// <summary>
        /// 打印GameObject的完整信息（自身+所有组件+所有子物体）
        /// </summary>
        /// <param name="rootObj">要调试的根GameObject</param>
        /// <param name="indentLevel">缩进层级（外部调用传0即可）</param>
        private static string GetGameObjectFullInfoString(GameObject rootObj, int indentLevel = 4)
        {
            // 1. 核心空值校验（Plib风格：优先防御性编程）
            if (rootObj == null)
            {
                // 返回一个简短的错误信息字符串，而不是调用 LogWarning
                return "[Error] 传入的GameObject为空！\n";
            }

            // 使用 StringBuilder 构建整个输出字符串
            StringBuilder outputBuilder = new StringBuilder();

            // 生成缩进，保持层级清晰（Plib风格：格式化输出）
            string indent = new string(' ', indentLevel * 4);
            string separator = $"{indent}========================================";

            // 2. 添加GameObject基础信息到StringBuilder
            outputBuilder.AppendLine(separator);
            outputBuilder.AppendLine($"{indent}[GameObject 基础信息]");
            outputBuilder.AppendLine($"{indent}名称: {rootObj.name}");
            outputBuilder.AppendLine($"{indent}实例ID: {rootObj.GetInstanceID()}");
            outputBuilder.AppendLine($"{indent}标签: {rootObj.tag}");
            outputBuilder.AppendLine($"{indent}层级: {rootObj.layer} ({LayerMask.LayerToName(rootObj.layer)})");
            outputBuilder.AppendLine($"{indent}激活状态: {rootObj.activeSelf}");
            outputBuilder.AppendLine($"{indent}场景: {rootObj.scene.name}");

            // 3. 添加组件列表信息到StringBuilder
            outputBuilder.AppendLine($"{indent}[组件列表]");
            int componentCount = rootObj.GetComponentCount();
            outputBuilder.AppendLine($"{indent}组件总数: {componentCount}");

            for (int i = 0; i < componentCount; i++)
            {
                try
                {
                    // 使用GetComponentAtIndex获取指定索引的组件
                    Component component = rootObj.GetComponentAtIndex(i);
                    if (component == null)
                    {
                        outputBuilder.AppendLine($"{indent}→ 索引{i}: 空组件（Unity内置隐藏组件）");
                        continue;
                    }

                    // 打印组件类型信息
                    Type compType = component.GetType();
                    outputBuilder.AppendLine($"{indent}→ 索引{i}: {compType.FullName} (简称: {compType.Name})");
                }
                catch (Exception ex)
                {
                    // Plib风格：捕获异常但不中断流程，仅打印警告
                    outputBuilder.AppendLine($"{indent}→ 索引{i}: 获取组件失败: {ex.Message}");
                }
            }

            // 4. 添加子物体列表信息到StringBuilder
            outputBuilder.AppendLine($"{indent}[子物体列表]");
            Transform rootTransform = rootObj.transform;
            int childCount = rootTransform.childCount;
            outputBuilder.AppendLine($"{indent}直接子物体数量: {childCount}");

            for (int i = 0; i < childCount; i++)
            {
                Transform childTransform = rootTransform.GetChild(i);
                if (childTransform == null || childTransform.gameObject == null)
                    continue;

                GameObject childObj = childTransform.gameObject;
                outputBuilder.AppendLine($"{indent}└── 子物体[{i}]: {childObj.name}");

                // 递归获取子物体信息，并添加到StringBuilder
                // 递归调用，获取子物体的字符串信息
                string childInfo = GetGameObjectFullInfoString(childObj, indentLevel + 1);
                // 将子物体的字符串信息追加到当前的StringBuilder
                outputBuilder.Append(childInfo);
            }

            outputBuilder.AppendLine(separator);
            outputBuilder.AppendLine(); // 空行分隔不同GameObject的输出

            // 5. 返回构建好的字符串
            return outputBuilder.ToString();
        }


        public static Transform FindChildByName(Transform parent, string name)
        {
            if (parent.name == name)
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform result = FindChildByName(parent.GetChild(i), name);
                if (result != null)
                    return result;
            }
            return null; // Not found
        }
        public static string GetFullPath(this Transform transform)
        {
            if (transform.parent == null)
                return "/" + transform.name;
            return transform.parent.GetFullPath() + "/" + transform.name;
        }

        /// <summary>
        /// 记录UI树结构，显示每个节点的类型、节点名，并标记当前节点
        /// </summary>
        /// <param name="targetTransform">目标Transform</param>
        public static void LogUITree(Transform targetTransform)
        {
            LogUITree(targetTransform, null);
        }

        /// <summary>
        /// 记录UI树结构，显示每个节点的类型、节点名，并标记当前节点
        /// </summary>
        /// <param name="targetTransform">目标Transform</param>
        /// <param name="rootTransform">自定义根节点，如果为null则自动查找KScreen或父节点</param>
        public static void LogUITree(Transform targetTransform, Transform rootTransform)
        {
            WriteLog(LogLevel.Debug, LogType.Log, "=== UI树调试 ===", Assembly.GetCallingAssembly()?.GetName()?.Name ?? "?");
            if (targetTransform == null)
            {
                WriteLog(LogLevel.Error, LogType.Error, "目标Transform为null！", Assembly.GetCallingAssembly()?.GetName()?.Name ?? "?");
                return;
            }
            
            // 如果没有指定根节点，自动查找KScreen或最近的父节点
            if (rootTransform == null)
            {
                rootTransform = FindUITreeRoot(targetTransform);
            }
            
            // 记录完整UI树
            WriteLog(LogLevel.Debug, LogType.Log, GetUITreeString(rootTransform, 0, targetTransform), Assembly.GetCallingAssembly()?.GetName()?.Name ?? "?");
            WriteLog(LogLevel.Debug, LogType.Log, "====================", Assembly.GetCallingAssembly()?.GetName()?.Name ?? "?");
        }

        /// <summary>
        /// 查找UI树的根节点，优先查找KScreen，否则查找最近的父节点
        /// </summary>
        /// <param name="transform">目标Transform</param>
        /// <returns>UI树的根节点</returns>
        private static Transform FindUITreeRoot(Transform transform)
        {
            if (transform == null)
                return null;

            // 向上查找KScreen组件
            Transform current = transform;
            while (current != null)
            {
                // 检查是否有KScreen组件
                if (current.gameObject.GetComponent("KScreen") != null)
                {
                    return current;
                }

                // 如果到达根节点，停止查找
                if (current.parent == null)
                {
                    return current;
                }

                current = current.parent;
            }

            return transform;
        }

        /// <summary>
        /// 构建UI树的字符串表示
        /// </summary>
        /// <param name="transform">当前Transform</param>
        /// <param name="indentLevel">缩进级别</param>
        /// <param name="targetTransform">目标Transform（用于标记）</param>
        /// <returns>UI树的字符串表示</returns>
        private static string GetUITreeString(Transform transform, int indentLevel, Transform targetTransform)
        {
            if (transform == null)
                return "";

            StringBuilder sb = new StringBuilder();
            string indent = new string(' ', indentLevel * 4);

            // 构建节点信息
            string nodeName = transform.name;
            bool isTarget = transform == targetTransform;

            // 获取UI组件类型
            string uiComponentTypes = GetUIComponentTypes(transform.gameObject);

            // 添加节点信息，标记当前节点
            sb.AppendLine($"{indent}{(isTarget ? "[CURRENT] " : "")}[{uiComponentTypes}] {nodeName}");

            // 递归处理子节点
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                sb.Append(GetUITreeString(child, indentLevel + 1, targetTransform));
            }

            return sb.ToString();
        }

        /// <summary>
        /// 获取GameObject上的UI组件类型
        /// </summary>
        /// <param name="gameObject">目标GameObject</param>
        /// <returns>UI组件类型列表，以逗号分隔</returns>
        private static string GetUIComponentTypes(GameObject gameObject)
        {
            if (gameObject == null)
                return "Transform";

            List<string> uiTypes = new List<string>();

            // 添加Transform类型
            uiTypes.Add("Transform");

            // 获取所有组件
            Component[] components = gameObject.GetComponents<Component>();

            foreach (Component component in components)
            {
                if (component == null)
                    continue;

                string componentName = component.GetType().Name;

                // 检查是否为UI相关组件
                if (IsUIComponent(componentName))
                {
                    uiTypes.Add(componentName);
                }
            }

            return string.Join(", ", uiTypes);
        }

        /// <summary>
        /// 检查组件是否为UI相关组件
        /// </summary>
        /// <param name="componentName">组件名称</param>
        /// <returns>是否为UI组件</returns>
        private static bool IsUIComponent(string componentName)
        {
            // 常见的UI组件名称列表
            string[] uiComponentNames = {
                "Text", "Image", "Button", "Toggle", "Slider", "Scrollbar", "ScrollView",
                "InputField", "Dropdown", "Canvas", "CanvasRenderer", "RectTransform",
                "Mask", "RectMask2D", "GridLayoutGroup", "HorizontalLayoutGroup", "VerticalLayoutGroup",
                "LayoutElement", "AspectRatioFitter", "ContentSizeFitter", "EventSystem",
                "StandaloneInputModule", "TouchInputModule", "TextMeshProUGUI", "Image", "Icon"
            };

            return Array.Exists(uiComponentNames, name => name.Equals(componentName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
