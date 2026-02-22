using HarmonyLib;
using KSerialization;
using MutantFarmLab.mutantplants;
using PeterHan.PLib.Core;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace MutantFarmLab
{

    public static class PlantablePlotGameObject
    {
        public static string storageName = "Dual_Head_Plot";
        public class BackPlatablePlot : PlantablePlot
        {
            protected override void OnPrefabInit()
            {
                base.OnPrefabInit();
            }
        }
        public class SubGoStorage : Storage
        {
            protected override void OnPrefabInit()
            {
                base.OnPrefabInit();
            }
            protected override void OnCleanUp()
            {
                PUtil.LogDebug($"SubGoStorage OnCleanUp DropALl items.");
                DropAll();
                base.OnCleanUp();
            }
        }
        public static GameObject Init(GameObject parentGo)
        {
            GameObject SubGameObject;

            SubGameObject = new GameObject(storageName);
            SubGameObject.SetActive(false); // ← 关键！先禁用
            SubGameObject.transform.SetParent(parentGo.transform, false);
            SubGameObject.transform.localPosition = Vector3.zero;//setParent false这里是偏移
            SubGameObject.transform.position = parentGo.transform.position;

            var kPrefabID = SubGameObject.AddOrGet<KPrefabID>();
            kPrefabID.PrefabTag = TagManager.Create(storageName + "Tag");
            kPrefabID.AddTag(GameTags.CodexCategories.FarmBuilding, false);
            kPrefabID.AddTag(GameTags.FarmTiles, false);
            kPrefabID.AddTag(GameTags.StorageLocker, false);

            Storage storage = SubGameObject.AddOrGet<SubGoStorage>();
            storage.name = storageName;
            storage.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
            storage.capacityKg = 2000f;
            storage.showInUI = true;
            storage.showUnreachableStatus = true;
            storage.allowClearable = true;
            storage.SetOffsetTable(OffsetGroups.InvertedStandardTable);

            var kSelectable = SubGameObject.AddOrGet<KSelectable>();
            kSelectable.SetName(storageName);
            kSelectable.IsSelectable = false;

            var plantablePlot = SubGameObject.AddOrGet<BackPlatablePlot>();
            plantablePlot.AddDepositTag(GameTags.CropSeed);
            plantablePlot.AddDepositTag(GameTags.WaterSeed);
            //以下必须设置，已经放在Farmtile中，影响肥料系统
            //plantablePlot.occupyingObjectRelativePosition.y = 1f;
            //plantablePlot.SetFertilizationFlags(true, true);

            SubGameObject.AddComponent<Prioritizable>(); // ← 关键！不然PlantablePlot崩溃

            parentGo.AddOrGet<SubStorageSaver>();

            //SubGameObject.SetActive(true);//需要的时候SetActive 否则种植砖底下会有两个未种植的图标
            if (parentGo != null)
                PUtil.LogDebug($"[SubGO] transfromGOName:[{parentGo.name}] transfromGOID：[{parentGo.GetMyWorldId()}] transform localPosition:[{parentGo.transform.localPosition.ToString()}] transform postion:[{parentGo.transform.position}] SubGameObject worldID:[{SubGameObject.GetMyWorldId()}] SubGameObjectl localPosition:[{SubGameObject.transform.localPosition.ToString()} subGameObject postion: [{SubGameObject.transform.position}]");

            return SubGameObject;
        }
        public static GameObject GetGameObject(GameObject farmtileObj)
        {
            if (farmtileObj != null)
            {
                return farmtileObj.transform.Find(storageName)?.gameObject;
            }
            return null;
        }
        public static void setActive(GameObject farmtileObj, bool active)
        {
            if (farmtileObj != null)
            {
                farmtileObj.transform.Find(storageName)?.gameObject?.SetActive(active);
            }
        }

        public static void SetUpFarmPlotTags(GameObject go, GameObject subGo)
        {
            go.GetComponent<KPrefabID>().prefabSpawnFn += delegate (GameObject inst)
            {
                Rotatable component = inst.GetComponent<Rotatable>();
                PlantablePlot component2 = subGo.GetComponent<PlantablePlot>();

                switch (component.GetOrientation())
                {
                    case Orientation.Neutral:
                    case Orientation.FlipH:
                        component2.occupyingObjectRelativePosition.y = 1f;
                        break;
                    case Orientation.R90:
                    case Orientation.R270:
                    case Orientation.R180:
                    case Orientation.FlipV:
                        component2.occupyingObjectRelativePosition.y = -1f;
                        break;
                    case Orientation.NumRotations:
                        break;
                    default:
                        component2.occupyingObjectRelativePosition.y = 1f;
                        break;
                }
            };
        }

    }

    [SerializationConfig(MemberSerialization.OptIn)]
    public class SubStorageSaver : KMonoBehaviour, ISaveLoadable
    {
        public class ItemElement
        {
            public SimHashes id;
            public float Mass;
            public float Temperature;
        }
        [Serialize]
        private List<ItemElement> savedItems = new();

        private Storage _storage;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            var plot = gameObject.transform.Find(PlantablePlotGameObject.storageName);
            if (plot != null)
            {
                _storage = plot.GetComponent<Storage>();
            }
        }

        [OnSerializing]
        public void SerializeStorage()
        {
            savedItems.Clear();
            if (_storage == null || _storage.items == null || _storage.items.Count <= 0) return;

            foreach (var item in _storage.items)
            {
                if (item.TryGetComponent(out PrimaryElement primary))
                {
                    savedItems.Add(new ItemElement
                    {
                        id = primary.Element.id,
                        Mass = primary.Mass,
                        Temperature = primary.Temperature
                    });
                }
            }
        }

        [OnDeserialized]
        public void DeserializeStorage()
        {

            if (savedItems.Count <= 0) return;

            if (_storage == null)
            {
                var plot = gameObject.transform.Find(PlantablePlotGameObject.storageName);
                if (plot != null)
                {
                    _storage = plot.GetComponent<Storage>();
                }
            }
            if (_storage == null) return;
            foreach (var elem in savedItems)
            {
                GameObject prefab = Assets.GetPrefab(elem.id.CreateTag());
                if (prefab == null) continue;

                GameObject itemGo = Util.KInstantiate(prefab);
                itemGo.SetActive(true);
                _storage.Store(itemGo);

                if (itemGo.TryGetComponent(out PrimaryElement primary))
                {
                    primary.Mass = elem.Mass;
                    primary.Temperature = elem.Temperature;
                }
            }
            savedItems.Clear();
        }
    }
    [HarmonyPatch(typeof(HydroponicFarmConfig), "DoPostConfigureComplete")]
    public static class HydroponicFarmConfig_DoPostConfigureComplete_Patches
    {
        [HarmonyPostfix]
        public static void Postfix(ref GameObject go)
        {
            if (!PlantMutationRegister.DUAL_HEAD_ENABLED) return;
            var sub = PlantablePlotGameObject.Init(go);
            var plantablePlot = sub.AddOrGet<PlantablePlot>();
            plantablePlot.occupyingObjectRelativePosition.y = 1f;

            plantablePlot.SetFertilizationFlags(true, true);

            go.AddOrGet<DualHeadReceptacleMarker>();

            PlantablePlotGameObject.SetUpFarmPlotTags(go, sub);
        }
    }
    [HarmonyPatch(typeof(FarmTileConfig), "DoPostConfigureComplete")]
    public static class FarmTileConfig_DoPostConfigureComplete_Patches
    {
        [HarmonyPostfix]
        public static void Postfix(ref GameObject go)
        {
            if (!PlantMutationRegister.DUAL_HEAD_ENABLED) return;
            var sub = PlantablePlotGameObject.Init(go);
            PlantablePlot plantablePlot = sub.AddOrGet<PlantablePlot>();
            plantablePlot.occupyingObjectRelativePosition.y = 1f;

            plantablePlot.SetFertilizationFlags(true, false);

            go.AddOrGet<DualHeadReceptacleMarker>();

            PlantablePlotGameObject.SetUpFarmPlotTags(go, sub);
        }
    }
    [HarmonyPatch(typeof(SingleEntityReceptacle), "OnOccupantDestroyed")]
    public class SingleEntityReceptacle_OnOccupantDestroyed_Patches
    {
        public static bool Prefix(SingleEntityReceptacle __instance, object data)
        {

            try
            {
                var name = __instance.gameObject.name;
            }
            catch (Exception ex)
            {
                PUtil.LogWarning($"{ex.Message}\n{ex.StackTrace}");
                return false;
            }
            return true;
        }
    }
}