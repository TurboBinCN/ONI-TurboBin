using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using TBB.He.TbbLib.Debuger;

namespace TBB.He.TbbLib.Utils
{
    public static class TbbHarmonyExtension
    {
        /// <summary>
        /// 修补指定类型的方法
        /// </summary>
        /// <param name="instance">Harmony实例</param>
        /// <param name="type">目标类型</param>
        /// <param name="methodName">方法名称</param>
        /// <param name="prefix">前缀方法</param>
        /// <param name="postfix">后缀方法</param>
        public static void Patch(
            this Harmony instance,
            Type type,
            string methodName,
            HarmonyMethod prefix = null,
            HarmonyMethod postfix = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (string.IsNullOrEmpty(methodName))
                throw new ArgumentNullException(nameof(methodName));
            
            try
            {
                var method = type.GetMethod(methodName,
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                    instance.Patch(method, prefix, postfix);
                else
                    TbbDebuger.LogWarning($"无法找到类型 {type.FullName} 上的方法 {methodName}");
            }
            catch (AmbiguousMatchException ex)
            {
                TbbDebuger.LogError($"修补方法 {methodName} 时出现歧义匹配异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 调用对象的方法
        /// </summary>
        /// <param name="obj">目标对象</param>
        /// <param name="name">方法名称</param>
        /// <param name="args">方法参数</param>
        /// <param name="debug">是否输出调试信息</param>
        public static void InvokeMethod(object obj, string name, bool debug = false, params object[] args)
        {
            object result;
            InvokeMethod(out result, obj, name, debug, args);
        }
        
        /// <summary>
        /// 调用对象的方法并返回结果
        /// </summary>
        /// <param name="result">方法返回值</param>
        /// <param name="obj">目标对象</param>
        /// <param name="name">方法名称</param>
        /// <param name="debug">是否输出调试信息</param>
        /// <param name="args">方法参数</param>
        /// <returns>是否成功调用</returns>
        public static bool InvokeMethod(out object result, object obj, string name, bool debug = false, params object[] args)
        {
            result = null;
            if (obj == null)
            {
                if (debug)
                    TbbDebuger.LogDebug($"InvokeMethod: 对象为 null。");
                return false;
            }

            Type objType = obj.GetType();
            if (debug)
                TbbDebuger.LogDebug($"InvokeMethod: 在类型 '{objType.FullName}' 上搜索方法 '{name}'。");

            Type[] argTypes = args == null ? Type.EmptyTypes : Array.ConvertAll(args, a => a?.GetType() ?? typeof(object));

            // 明确指定 BindingFlags 以查找实例和非公共方法
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            // 尝试查找方法
            MethodInfo method = objType.GetMethod(name, flags, null, argTypes, null);

            if (method != null)
            {
                if (debug)
                    TbbDebuger.LogDebug($"InvokeMethod: 找到方法 '{method.Name}'，签名: ({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))})");
                try
                {
                    result = method.Invoke(obj, args);
                    if (debug)
                        TbbDebuger.LogDebug($"InvokeMethod: 成功调用方法。结果: [{result}]");
                    return true;
                }
                catch (Exception ex)
                {
                    if (debug)
                        TbbDebuger.LogDebug($"InvokeMethod: 调用方法时出现异常: {ex}");
                    return false;
                }
            }
            else
            {
                if (debug)
                {
                    // 详细列出所有匹配名称的方法，包括参数，以便调试
                    MethodInfo[] allMethods = objType.GetMethods(flags).Where(m => m.Name == name).ToArray();
                    TbbDebuger.LogDebug($"InvokeMethod: 未找到带有给定参数的方法 '{name}'。");
                    TbbDebuger.LogDebug($"InvokeMethod: 类型 '{objType.FullName}' 上名为 '{name}' 的可用方法:");
                    foreach (var m in allMethods)
                    {
                        var paramStr = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}" + (p.IsOptional ? $" = {p.DefaultValue}" : "")));
                        TbbDebuger.LogDebug($"  - {m.ReturnType.Name} {m.Name}({paramStr}) - {(m.IsPublic ? "公共" : m.IsPrivate ? "私有" : m.IsFamily ? "保护" : "其他")}");
                    }
                    TbbDebuger.LogDebug($"InvokeMethod: 搜索的签名: ({string.Join(", ", argTypes.Select(t => t.Name))})");
                }
                return false; // 方法未找到
            }
        }
        
        /// <summary>
        /// 设置对象的字段值
        /// </summary>
        /// <param name="obj">目标对象</param>
        /// <param name="name">字段名称</param>
        /// <param name="value">字段值</param>
        public static void SetField(object obj, string name, object value)
        {
            if (obj == null) return;
            
            var field = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null)
                field.SetValue(obj, value);
        }
        
        /// <summary>
        /// 获取对象的字段值
        /// </summary>
        /// <param name="obj">目标对象</param>
        /// <param name="name">字段名称</param>
        /// <returns>字段值</returns>
        public static object GetField(object obj, string name)
        {
            if (obj == null) return null;
            
            var field = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null)
                return field.GetValue(obj);
            return null;
        }
        
        /// <summary>
        /// 调用方法（使用Traverse，支持静态和实例方法）
        /// </summary>
        /// <typeparam name="TResult">返回类型</typeparam>
        /// <param name="target">目标对象或类型</param>
        /// <param name="methodName">方法名称</param>
        /// <param name="parameters">方法参数</param>
        /// <returns>方法返回值</returns>
        public static TResult CallMethod<TResult>(object target, string methodName, params object[] parameters)
        {
            if (target is Type type)
                return Traverse.Create(type).Method(methodName).GetValue<TResult>(parameters);
            else
                return Traverse.Create(target).Method(methodName).GetValue<TResult>(parameters);
        }
        
        /// <summary>
        /// 调用无返回值的方法（使用Traverse，支持静态和实例方法）
        /// </summary>
        /// <param name="target">目标对象或类型</param>
        /// <param name="methodName">方法名称</param>
        /// <param name="parameters">方法参数</param>
        public static void CallMethod(object target, string methodName, params object[] parameters)
        {
            if (target is Type type)
                Traverse.Create(type).Method(methodName, parameters).GetValue();
            else
                Traverse.Create(target).Method(methodName, parameters).GetValue();
        }
    }
}