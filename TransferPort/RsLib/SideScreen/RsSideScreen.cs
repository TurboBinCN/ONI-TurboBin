using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static DetailsScreen;
using Object = UnityEngine.Object;

namespace RsLib
{
    public class RsSideScreen : RsModule<RsSideScreen>
    {
        protected List<ItemInfo> itemInfos = new();

        public RsSideScreen CopyAndCreate<TSourceScreen, TNewScreen>()
            where TSourceScreen : SideScreenContent
            where TNewScreen : RsSideScreenContent
        {
            var info = new ItemInfo();
            info.sourceScreen = typeof(TSourceScreen);
            info.newScreen = typeof(TNewScreen);
            itemInfos.Add(info);
            return this;
        }

        public RsSideScreen Create<TScreen>(SidescreenTabTypes tab)
            where TScreen : SideScreenContent
        {
            ItemInfo info = new ItemInfo();
            info.newScreen = typeof(TScreen);
            itemInfos.Add(info);
            return this;
        }

        public RsSideScreen Add(Func<SideScreenContent> add, bool isPrefab)
        {
            ItemInfo info = new ItemInfo();
            info.add = add;
            info.isPrefab = isPrefab;
            itemInfos.Add(info);
            return this;
        }
        public RsSideScreen Add(SideScreenContent prefab)
        {
            ItemInfo info = new ItemInfo();
            info.prefab = prefab;
            info.isPrefab = true;
            itemInfos.Add(info);
            return this;
        }

        protected override void Initialized()
        {
            Harmony.Patch(
                typeof(DetailsScreen),
                "OnPrefabInit",
                null,
                new HarmonyMethod(typeof(RsSideScreen), nameof(DetailsScreen_OnPrefabInit_Patch))
            );
        }

        private static void DetailsScreen_OnPrefabInit_Patch(
            List<DetailsScreen.SideScreenRef> ___sideScreens)
        {
            foreach (var itemInfo in Instance.itemInfos)
            {
                if (itemInfo.sourceScreen != null && itemInfo.newScreen != null)
                {
                    CreateSideScreen(___sideScreens, itemInfo.sourceScreen,
                        itemInfo.newScreen);
                }
                else if (itemInfo.newScreen != null)
                {
                    CreateSideScreen(___sideScreens, itemInfo.newScreen, itemInfo.tab);
                }
                else if (itemInfo.add != null)
                {
                    AddSideScreen(___sideScreens, itemInfo.add(), itemInfo.isPrefab);
                }
                else if (itemInfo.prefab != null)
                {
                    AddSideScreen(___sideScreens, itemInfo.prefab, true);
                }
            }
        }


        /// <summary>
        ///     创建一个侧边栏面板
        /// </summary>
        /// <param name="existing">已经存在的面板</param>
        /// <param name="parent"></param>
        /// <typeparam name="TSourceScreen"></typeparam>
        /// <typeparam name="TNewScreen"></typeparam>
        /// <returns></returns>
        public static TNewScreen CreateSideScreen<TSourceScreen, TNewScreen>(
            IList<DetailsScreen.SideScreenRef> existing)
            where TSourceScreen : SideScreenContent
            where TNewScreen : RsSideScreenContent
        {
            return (TNewScreen)CreateSideScreen(existing, typeof(TSourceScreen), typeof(TNewScreen));
        }

        public static RsSideScreenContent CreateSideScreen(
            IList<DetailsScreen.SideScreenRef> existing, Type sourceScreen, Type newScreen)
        {
            if (sourceScreen.IsAssignableFrom(typeof(SideScreenContent)))
                    throw new TypeLoadException(
                    "参数sourceScreen不可用的，该类型必须继承" + typeof(SideScreenContent).FullName);

            if (newScreen.IsAssignableFrom(typeof(RsSideScreenContent)))
                    throw new TypeLoadException(
                    "参数newScreen不可用的，该类型必须继承" + typeof(RsSideScreenContent).FullName);

            foreach (var sideScreenRef in existing)
            {
                if (sideScreenRef.screenPrefab.GetType() != sourceScreen) continue;

                var logicBroadcastChannelSideScreen = sideScreenRef.screenPrefab;
                if (logicBroadcastChannelSideScreen != null)
                {
                    var sideScreenRef2 = new DetailsScreen.SideScreenRef();
                    var newScreenInstance = CopySideScreen(logicBroadcastChannelSideScreen, newScreen);
                    sideScreenRef2.name = newScreenInstance.name;
                    sideScreenRef2.screenPrefab = newScreenInstance;
                    sideScreenRef2.screenInstance = newScreenInstance;
                    var transform = newScreenInstance.gameObject.transform;
                    SidescreenTab tabOfType = DetailsScreen.Instance.GetTabOfType(sideScreenRef.tab);
                    transform.SetParent(tabOfType.bodyInstance.transform);
                    transform.localScale = Vector3.one;
                    existing.Insert(0, sideScreenRef2);
                    return newScreenInstance;
                }
            }

            return null;
        }


        public static TNewScreen CreateSideScreen<TNewScreen>(
            IList<DetailsScreen.SideScreenRef> existing, SidescreenTabTypes tab)
            where TNewScreen : SideScreenContent
        {
            return (TNewScreen)CreateSideScreen(existing, typeof(TNewScreen), tab);
        }

        public static SideScreenContent CreateSideScreen(
            IList<DetailsScreen.SideScreenRef> existing, Type newScreen, SidescreenTabTypes tab)
        {
            if (newScreen.IsAssignableFrom(typeof(SideScreenContent)))
                throw new TypeLoadException(
                    "参数newScreen不可用的，该类型必须继承" + typeof(SideScreenContent).FullName);

            GameObject gameObject = new GameObject();
            gameObject.SetActive(true);
            gameObject.name = newScreen.Name;
            SidescreenTab tabOfType = DetailsScreen.Instance.GetTabOfType(tab);
            gameObject.transform.SetParent(tabOfType.bodyInstance.transform, false);
            SideScreenContent sideScreenContent = (SideScreenContent)gameObject.AddComponent(newScreen);

            var sideScreenRef = new DetailsScreen.SideScreenRef();
            sideScreenRef.name = gameObject.name;
            sideScreenRef.screenPrefab = sideScreenContent;
            sideScreenRef.screenInstance = sideScreenContent;
            existing.Add(sideScreenRef);
            return sideScreenContent;
        }

        private static RsSideScreenContent CopySideScreen(SideScreenContent sourceScreen, Type newScreen)
        {
            var gameObject = Object.Instantiate(sourceScreen.gameObject, null, false);
            gameObject.name = newScreen.Name;
            var activeSelf = gameObject.activeSelf;
            gameObject.SetActive(false);
            var sourceScreen2 = (SideScreenContent)gameObject.GetComponent(sourceScreen.GetType());
            var newScreen2 = (RsSideScreenContent)gameObject.AddComponent(newScreen);

            var copyFieldDict = GetCopyFieldDict(newScreen);

            if (copyFieldDict != null && copyFieldDict.Count > 0)
                foreach (var (newName, sourceName) in copyFieldDict)
                {
                    var sourceField = sourceScreen.GetType().GetField(sourceName,
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    var newField = newScreen.GetField(newName,
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (sourceField == null) throw new Exception("not found sourceField, name: " + sourceName);

                    if (newField == null) throw new Exception("not found newField, name: " + newName);

                    newField.SetValue(newScreen2, sourceField.GetValue(sourceScreen2));
                }

            newScreen2.CopyFieldAfter();

            Object.DestroyImmediate(sourceScreen2);
            gameObject.SetActive(activeSelf);
            return newScreen2;
        }


        public static void AddSideScreen(
            IList<DetailsScreen.SideScreenRef> existing, SideScreenContent gameObject, bool isPrefab)
        {
            var sideScreenRef = new DetailsScreen.SideScreenRef();
            sideScreenRef.name = gameObject.name;
            sideScreenRef.screenPrefab = gameObject;
            if (!isPrefab)
            {
                sideScreenRef.screenInstance = gameObject;
            }
            existing.Add(sideScreenRef);
        }

        /// <summary>
        ///     new -> source
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetCopyFieldDict(Type type)
        {
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Dictionary<string, string> dic = new();
            foreach (var info in fields)
            {
                var copyField = (CopyField)Attribute.GetCustomAttribute(info, typeof(CopyField));
                if (copyField != null)
                {
                    if (string.IsNullOrWhiteSpace(copyField.alias))
                        dic.Add(info.Name, info.Name);
                    else
                        dic.Add(info.Name, copyField.alias);
                }
            }

            return dic;
        }

        protected class ItemInfo
        {
            /// <summary>
            /// 新的sideScreen
            /// </summary>
            public Type newScreen;
            /// <summary>
            /// 原sidescreen,如果为null则直接创建
            /// </summary>
            public Type sourceScreen;

            // public SideScreenContent prefab;

            public Func<SideScreenContent> add;

            public bool isPrefab = false;

            public SideScreenContent prefab;

            public SidescreenTabTypes tab;
        }

        [AttributeUsage(AttributeTargets.Field)]
        public class CopyField : Attribute
        {
            public string alias;

            public CopyField()
            {
            }

            public CopyField(string alias)
            {
                this.alias = alias;
            }
        }
    }
}