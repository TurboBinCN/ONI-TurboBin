using HarmonyLib;
using System;
using System.Reflection;

namespace MutantFarmLab
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
            var field = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null)
                return field.GetValue(obj);
            return null;
        }
    }
}