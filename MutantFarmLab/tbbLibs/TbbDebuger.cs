using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace MutantFarmLab.tbbLibs
{
    public static class TbbDebuger
    {
        public enum LogLevel
        {
            None = 0,
            Error = 1,
            Warning = 2,
            Info = 3,
            Debug = 4
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
            if (!ignoreLevel && !IsLevelAllowed(level)) return;

            switch (logType)
            {
                case LogType.Log:
                    Debug.LogFormat("[Tbb/{0}] {1}", assemblyName, message);
                    break;
                case LogType.Warning:
                    Debug.LogWarningFormat("[Tbb/{0}] {1}", assemblyName, message);
                    break;
                case LogType.Error:
                    Debug.LogErrorFormat("[Tbb/{0}] {1}", assemblyName, message);
                    break;
            }
        }
        public static void LogDebug(object message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Assembly callingAssembly = Assembly.GetCallingAssembly();
            string fileName = Path.GetFileName(filePath);
            string logPrefix = $"{callingAssembly?.GetName()?.Name}:{fileName}:{memberName}({lineNumber})";
            WriteLog(LogLevel.Debug, LogType.Log, $"[DEBUG] {message}", logPrefix);
        }
        public static void LogWarning(object message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Assembly callingAssembly = Assembly.GetCallingAssembly();
            string fileName = Path.GetFileName(filePath);
            string logPrefix = $"{callingAssembly?.GetName()?.Name}:{fileName}:{memberName}({lineNumber})";
            WriteLog(LogLevel.Warning, LogType.Log, $"[WARNING] {message}", logPrefix);
        }
        public static void LogError(object message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Assembly callingAssembly = Assembly.GetCallingAssembly();
            string fileName = Path.GetFileName(filePath);
            string logPrefix = $"{callingAssembly?.GetName()?.Name}:{fileName}:{memberName}({lineNumber})";
            var errorMsg = $"[ERROR] {message}";
            if (EnableErrorStackTrace)
            {
                errorMsg += $"\n调用栈：{Environment.StackTrace}";
            }
            WriteLog(LogLevel.Error, LogType.Error, errorMsg, logPrefix);
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
        private static string GetGameObjectFullInfoString(GameObject rootObj, int indentLevel = 4)
        {
            if (rootObj == null)
            {
                return "[Error] 传入的GameObject为空！\n";
            }

            StringBuilder outputBuilder = new StringBuilder();

            string indent = new string(' ', indentLevel * 4);
            string separator = $"{indent}========================================";

            outputBuilder.AppendLine(separator);
            outputBuilder.AppendLine($"{indent}[GameObject 基础信息]");
            outputBuilder.AppendLine($"{indent}名称: {rootObj.name}");
            outputBuilder.AppendLine($"{indent}实例ID: {rootObj.GetInstanceID()}");
            outputBuilder.AppendLine($"{indent}标签: {rootObj.tag}");
            outputBuilder.AppendLine($"{indent}层级: {rootObj.layer} ({LayerMask.LayerToName(rootObj.layer)})");
            outputBuilder.AppendLine($"{indent}激活状态: {rootObj.activeSelf}");
            outputBuilder.AppendLine($"{indent}场景: {rootObj.scene.name}");

            outputBuilder.AppendLine($"{indent}[组件列表]");
            int componentCount = rootObj.GetComponentCount();
            outputBuilder.AppendLine($"{indent}组件总数: {componentCount}");

            for (int i = 0; i < componentCount; i++)
            {
                try
                {
                    Component component = rootObj.GetComponentAtIndex(i);
                    if (component == null)
                    {
                        outputBuilder.AppendLine($"{indent}→ 索引{i}: 空组件（Unity内置隐藏组件）");
                        continue;
                    }

                    Type compType = component.GetType();
                    outputBuilder.AppendLine($"{indent}→ 索引{i}: {compType.FullName} (简称: {compType.Name})");
                }
                catch (Exception ex)
                {
                    outputBuilder.AppendLine($"{indent}→ 索引{i}: 获取组件失败: {ex.Message}");
                }
            }

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

                string childInfo = GetGameObjectFullInfoString(childObj, indentLevel + 1);
                outputBuilder.Append(childInfo);
            }

            outputBuilder.AppendLine(separator);
            outputBuilder.AppendLine();

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
            return null;
        }
        public static string GetFullPath(this Transform transform)
        {
            if (transform.parent == null)
                return "/" + transform.name;
            return transform.parent.GetFullPath() + "/" + transform.name;
        }

        public static void LogUITree(Transform targetTransform)
        {
            LogUITree(targetTransform, null);
        }

        public static void LogUITree(Transform targetTransform, Transform rootTransform)
        {
            WriteLog(LogLevel.Debug, LogType.Log, "=== UI树调试 ===", Assembly.GetCallingAssembly()?.GetName()?.Name ?? "?");
            if (targetTransform == null)
            {
                WriteLog(LogLevel.Error, LogType.Error, "目标Transform为null！", Assembly.GetCallingAssembly()?.GetName()?.Name ?? "?");
                return;
            }
            
            if (rootTransform == null)
            {
                rootTransform = FindUITreeRoot(targetTransform);
            }
            
            WriteLog(LogLevel.Debug, LogType.Log, GetUITreeString(rootTransform, 0, targetTransform), Assembly.GetCallingAssembly()?.GetName()?.Name ?? "?");
            WriteLog(LogLevel.Debug, LogType.Log, "====================", Assembly.GetCallingAssembly()?.GetName()?.Name ?? "?");
        }

        private static Transform FindUITreeRoot(Transform transform)
        {
            if (transform == null)
                return null;

            Transform current = transform;
            while (current != null)
            {
                if (current.gameObject.GetComponent("KScreen") != null)
                {
                    return current;
                }

                if (current.parent == null)
                {
                    return current;
                }

                current = current.parent;
            }

            return transform;
        }

        private static string GetUITreeString(Transform transform, int indentLevel, Transform targetTransform)
        {
            if (transform == null)
                return "";

            StringBuilder sb = new StringBuilder();
            string indent = new string(' ', indentLevel * 4);

            string nodeName = transform.name;
            bool isTarget = transform == targetTransform;

            string uiComponentTypes = GetUIComponentTypes(transform.gameObject);

            sb.AppendLine($"{indent}{(isTarget ? "[CURRENT] " : "")}[{uiComponentTypes}] {nodeName}");

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                sb.Append(GetUITreeString(child, indentLevel + 1, targetTransform));
            }

            return sb.ToString();
        }

        private static string GetUIComponentTypes(GameObject gameObject)
        {
            if (gameObject == null)
                return "Transform";

            List<string> uiTypes = new List<string>();

            uiTypes.Add("Transform");

            Component[] components = gameObject.GetComponents<Component>();

            foreach (Component component in components)
            {
                if (component == null)
                    continue;

                string componentName = component.GetType().Name;

                if (IsUIComponent(componentName))
                {
                    uiTypes.Add(componentName);
                }
            }

            return string.Join(", ", uiTypes);
        }

        private static bool IsUIComponent(string componentName)
        {
            string[] uiComponentNames = {
                "Text", "Image", "Button", "Toggle", "Slider", "Scrollbar", "ScrollView",
                "InputField", "Dropdown", "Canvas", "CanvasRenderer", "RectTransform",
                "Mask", "RectMask2D", "GridLayoutGroup", "HorizontalLayoutGroup", "VerticalLayoutGroup",
                "LayoutElement", "AspectRatioFitter", "ContentSizeFitter", "EventSystem",
                "StandaloneInputModule", "TouchInputModule", "TextMeshProUGUI", "Image", "Icon"
            };

            return Array.Exists(uiComponentNames, name => name.Equals(componentName, StringComparison.OrdinalIgnoreCase));
        }

        private static int GetComponentCount(this GameObject gameObject)
        {
            if (gameObject == null) return 0;
            Component[] allComponents = gameObject.GetComponents<Component>();
            return allComponents.Length;
        }
    }
}