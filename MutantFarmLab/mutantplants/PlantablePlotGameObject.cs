using HarmonyLib;
using KSerialization;
using MutantFarmLab.mutantplants;
using MutantFarmLab.tbbLibs;
using PeterHan.PLib.Core;
using System.Collections.Generic;
using System.Runtime.Serialization;
using TemplateClasses;
using UnityEngine;
using static MutantFarmLab.RadiationFarmTileConfig;
using static StructureTemperaturePayload;

namespace MutantFarmLab
{

    public static class PlantablePlotGameObject
    {
        public static string storageName = "Dual_Head_Plot";
        public class BackPlatablePlot: PlantablePlot
        {
            protected override void OnPrefabInit()
            {
                base.OnPrefabInit();
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
            kPrefabID.AddTag(GameTags.StorageLocker, false);

            Storage storage = SubGameObject.AddOrGet<Storage>();
            storage.name = storageName;
            storage.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
            storage.capacityKg = 2000f;
            storage.showInUI = true;
            storage.showUnreachableStatus = true;
            storage.SetOffsetTable(OffsetGroups.InvertedStandardTable);

            var kSelectable = SubGameObject.AddOrGet<KSelectable>();
            kSelectable.SetName(storageName);
            kSelectable.IsSelectable = true;

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
            PUtil.LogDebug($"[SubStorageSaver] OnSerializing [{_storage.items.Count}]");
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
            PUtil.LogDebug($"[SubStorageSaver] OnDeserialized Count:[{savedItems.Count}] _storage: [{_storage}]");
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
            plantablePlot.occupyingObjectRelativePosition = new Vector3(0f, 1f, 0f);

            plantablePlot.SetFertilizationFlags(true, false);

            go.AddOrGet<DualHeadReceptacleMarker>();
        }
    }
}