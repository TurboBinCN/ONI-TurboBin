using HarmonyLib;
using KMod;

namespace MutantFarmLab
{
    public abstract class TbbModule<T> where T : TbbModule<T>, new()
    {
        public static T Instance { get; private set; }

        protected Mod Mod { get; private set; }
        protected Harmony Harmony { get; private set; }

        public static T Initialize(Mod mod, Harmony harmony)
        {
            if (Instance != null)
            {
                Debug.LogWarningFormat("{0} is already initialized", typeof(T).FullName);
            }
            else
            {
                Instance = new T { Mod = mod, Harmony = harmony };
                Instance.Initialized();
                Debug.LogFormat("{0} initialized", typeof(T).FullName);
            }

            return Instance;
        }

        /// <summary>
        ///     会在第一次调用RegisterModule方法后，执行该方法
        /// </summary>
        protected virtual void Initialized()
        {
        }
    }
}