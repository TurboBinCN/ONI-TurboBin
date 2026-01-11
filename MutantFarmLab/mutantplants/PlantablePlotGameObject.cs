using HarmonyLib;
using Klei.AI;
using KSerialization;
using MutantFarmLab.mutantplants;
using MutantFarmLab.tbbLibs;
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

        public static GameObject Init(GameObject parentGo)
        {
            GameObject SubGameObject;
            PUtil.LogDebug($"[SubGO] OnSpawn called. Parent: {parentGo.name}");

            SubGameObject = new GameObject(storageName);
            SubGameObject.SetActive(false); // ← 关键！先禁用
            SubGameObject.transform.SetParent(parentGo.transform, false);
            SubGameObject.transform.localPosition = Vector3.zero;//setParent false这里是偏移
            SubGameObject.transform.position = parentGo.transform.position;

            var kPrefabID = SubGameObject.AddOrGet<KPrefabID>();
            kPrefabID.PrefabTag = TagManager.Create(storageName + "Tag");
            //kPrefabID.InstanceID = KPrefabID.GetUniqueID();
            kPrefabID.AddTag(GameTags.StorageLocker, false);

            Storage storage = SubGameObject.AddOrGet<Storage>();
            storage.name = storageName;
            storage.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
            storage.capacityKg = 2000f;
            storage.allowItemRemoval = true;
            storage.showInUI = true;
            storage.showUnreachableStatus = true;
            storage.SetOffsetTable(OffsetGroups.InvertedStandardTable);

            var kSelectable = SubGameObject.AddOrGet<KSelectable>();
            kSelectable.SetName(storageName);
            kSelectable.IsSelectable = true;

            var plantablePlot = SubGameObject.AddOrGet<PlantablePlot>();
            plantablePlot.AddDepositTag(GameTags.CropSeed);
            plantablePlot.AddDepositTag(GameTags.WaterSeed);
            //以下必须设置，放在Farmtile中，影响肥料系统
            //plantablePlot.occupyingObjectRelativePosition.y = 1f;
            //plantablePlot.SetFertilizationFlags(true, true);

            SubGameObject.AddComponent<Prioritizable>(); // ← 关键！不然PlantablePlot崩溃

            parentGo.AddOrGet<SubStorageSaver>();

            PUtil.LogDebug($"[SubGO] Activating...");

            //SubGameObject.SetActive(true);
            KPrefabIDTracker.Get().Register(SubGameObject.AddOrGet<KPrefabID>());

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

    }

    [SerializationConfig(MemberSerialization.OptIn)]
    public class SubStorageSaver : KMonoBehaviour, ISaveLoadable
    {
        [Serialize]
        private List<ItemElement> savedItems = new List<ItemElement>();

        public class ItemElement
        {
            public SimHashes id;
            public float Mass;
            public float Temperature;
        }

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
            if (_storage == null || _storage.items == null) return;

            if (_storage.items.Count == 0) return;

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
            PUtil.LogDebug($"[SubStorageSaver] OnDeserialized");
            if (savedItems.Count <= 0 || _storage == null) return;
            PUtil.LogDebug($"[SubStorageSaver]storage is not null. saveItems Count:[{savedItems.Count}]");
            TbbDebuger.PrintGameObjectFullInfo(_storage.gameObject);
            PUtil.LogDebug($"[SubStorageSaver] storage.gameObject name :[{_storage.gameObject.name}] [{_storage.gameObject.GetMyWorldId()}] parent name:[{_storage.gameObject.transform?.parent?.gameObject?.name}]");
            foreach (var elem in savedItems)
            {
                GameObject prefab = Assets.GetPrefab(elem.id.CreateTag());
                if (prefab == null) continue;

                GameObject itemGo = Util.KInstantiate(prefab);
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
    public static class HydroponicFarmConfig_Patches
    {
        [HarmonyPostfix]
        public static void Postfix(ref GameObject go)
        {
            var sub = PlantablePlotGameObject.Init(go);
            var plantablePlot = sub.AddOrGet<PlantablePlot>();
            plantablePlot.occupyingObjectRelativePosition.y = 1f;

            plantablePlot.SetFertilizationFlags(true, true);

            go.AddOrGet<DualHeadReceptacleMarker>();
        }
    }
    [HarmonyPatch(typeof(FarmTileConfig), "DoPostConfigureComplete")]
    public static class FarmTileConfig_Patches
    {
        [HarmonyPostfix]
        public static void Postfix(ref GameObject go)
        {
            var sub = PlantablePlotGameObject.Init(go);
            PlantablePlot plantablePlot = sub.AddOrGet<PlantablePlot>();
            plantablePlot.occupyingObjectRelativePosition = new Vector3(0f, 1f, 0f);
            
            plantablePlot.SetFertilizationFlags(true, false);

            go.AddOrGet<DualHeadReceptacleMarker>();
        }
    }
}