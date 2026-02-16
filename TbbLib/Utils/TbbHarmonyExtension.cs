using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using TBB.He.TbbLib.Debuger;

namespace TBB.He.TbbLib.Utils
{
    public static class TbbHarmonyExtension
    {
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
                    Debug.LogWarningFormat("Unable to find method {0} on type {1}", methodName, type.FullName);
            }
            catch (AmbiguousMatchException ex)
            {
                Debug.LogException(ex);
            }
        }
        public static void InvokeMethod(object obj, string name, params object[] args)
        {
            if (obj == null) return;
            var types = args == null ? Type.EmptyTypes : Array.ConvertAll(args, a => a?.GetType() ?? typeof(object));
            var method = obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, types, null);
            method?.Invoke(obj, args);
        }
        public static bool InvokeMethod(out object __result, object obj, string name, params object[] args)
        {
            __result = null;
            if (obj == null) return false;
            var types = args == null ? Type.EmptyTypes : Array.ConvertAll(args, a => a?.GetType() ?? typeof(object));
            var method = obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, types, null);
            if (method != null)
            {
                __result = method?.Invoke(obj, args);
                return true;
            }
            return false;
        }
        public static bool InvokeMethodDebug(out object __result, object obj, string name, params object[] args)
        {
            __result = null;
            if (obj == null)
            {
                TbbDebuger.LogDebug($"InvokeMethod: obj is null.");
                return false;
            }

            Type objType = obj.GetType();
            TbbDebuger.LogDebug($"InvokeMethod: Searching for method '{name}' on type '{objType.FullName}'.");

            Type[] argTypes = args == null ? Type.EmptyTypes : Array.ConvertAll(args, a => a?.GetType() ?? typeof(object));

            // 明确指定 BindingFlags 以查找实例和非公共方法
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            // 尝试查找方法
            MethodInfo method = objType.GetMethod(name, flags, null, argTypes, null);

            if (method != null)
            {
                TbbDebuger.LogDebug($"InvokeMethod: Found method '{method.Name}' with signature: ({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))})");
                try
                {
                    __result = method.Invoke(obj, args);
                    TbbDebuger.LogDebug($"InvokeMethod: Successfully invoked method. Result: [{__result}]");
                    return true;
                }
                catch (Exception ex)
                {
                    TbbDebuger.LogDebug($"InvokeMethod: Exception while invoking method: {ex}");
                    return false;
                }
            }
            else
            {
                // 详细列出所有匹配名称的方法，包括参数，以便调试
                MethodInfo[] allMethods = objType.GetMethods(flags).Where(m => m.Name == name).ToArray();
                TbbDebuger.LogDebug($"InvokeMethod: Method '{name}' not found with given parameters.");
                TbbDebuger.LogDebug($"InvokeMethod: Available methods named '{name}' on '{objType.FullName}':");
                foreach (var m in allMethods)
                {
                    var paramStr = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}" + (p.IsOptional ? $" = {p.DefaultValue}" : "")));
                    TbbDebuger.LogDebug($"  - {m.ReturnType.Name} {m.Name}({paramStr}) - {(m.IsPublic ? "Public" : m.IsPrivate ? "Private" : m.IsFamily ? "Protected" : "Other")}");
                }
                TbbDebuger.LogDebug($"InvokeMethod: Searched for signature: ({string.Join(", ", argTypes.Select(t => t.Name))})");
                return false; // 方法未找到
            }
        }
        public static void SetField(object obj, string name, object value)
        {
            if (obj == null) return;
            var field = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null)
                field.SetValue(obj, value);
        }
        public static object GetField(object obj, string name)
        {
            if (obj == null) return null;
            //Traverse.Create(obj).Field<List<FetchList2>>("fetchLists").Value;
            var field = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null)
                return field.GetValue(obj);
            return null;
        }
        public static TResult CallStaticMethod<TResult>(Type type, string methodName, params object[] args)
        {
            return Traverse.Create(type)
            .Method(methodName)
            .GetValue<TResult>(args);
        }
        public static TResult CallInstanceMethod<TResult>(object instance, string methodName, params object[] parameters)
        {
            return Traverse.Create(instance)
                .Method(methodName)
                .GetValue<TResult>(parameters);
        }

        // 无返回值的重载（简化 void 方法调用）
        public static void CallStaticMethod(Type type, string methodName, params object[] parameters)
        {
            Traverse.Create(type).Method(methodName, parameters).GetValue();
        }

        public static void CallInstanceMethod(object instance, string methodName, params object[] parameters)
        {
            Traverse.Create(instance).Method(methodName, parameters).GetValue();
        }
    }
}