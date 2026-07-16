using HarmonyLib;
using KSerialization;
using MutantFarmLab.mutantplants;
using MutantFarmLab.tbbLibs;
using System;
using System.Collections.Generic;
using System.Reflection;
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
                DropAll();
                base.OnCleanUp();
            }
        }
        public static GameObject Init(GameObject parentGo, Vector3 centerOffset = default)
        {
            GameObject SubGameObject;

            SubGameObject = new GameObject(storageName);
            SubGameObject.SetActive(false);
            SubGameObject.transform.SetParent(parentGo.transform, false);
            SubGameObject.transform.localPosition = centerOffset;

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
            PlantablePlot plantablePlot = subGo.GetComponent<PlantablePlot>();
            if (plantablePlot == null)
                return;

            Rotatable rotatable = go.GetComponent<Rotatable>();
            if (rotatable == null)
            {
                plantablePlot.occupyingObjectRelativePosition.y = 1f;
                return;
            }

            go.GetComponent<KPrefabID>().prefabSpawnFn += delegate (GameObject inst)
            {
                if (inst == null)
                    return;

                Rotatable component = inst.GetComponent<Rotatable>();
                if (component == null)
                    return;

                switch (component.GetOrientation())
                {
                    case Orientation.Neutral:
                    case Orientation.FlipH:
                        plantablePlot.occupyingObjectRelativePosition.y = 1f;
                        break;
                    case Orientation.R90:
                    case Orientation.R270:
                    case Orientation.R180:
                    case Orientation.FlipV:
                        plantablePlot.occupyingObjectRelativePosition.y = -1f;
                        break;
                    case Orientation.NumRotations:
                        break;
                    default:
                        plantablePlot.occupyingObjectRelativePosition.y = 1f;
                        break;
                }
            };
        }

        public static Type FindType(string typeName)
        {
            Type type = Type.GetType(typeName);
            if (type != null)
                return type;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null)
                    return type;
            }
            return null;
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
    [HarmonyPatch]
    public static class WideFarmTileConfig_DoPostConfigureComplete_Patches
    {
        public static bool Prepare()
        {
            Type type = PlantablePlotGameObject.FindType("WideFarmTileConfig");
            bool result = type != null;
            if (!result)
            return result;
        }

        public static MethodBase TargetMethod()
        {
            Type type = PlantablePlotGameObject.FindType("WideFarmTileConfig");
            if (type == null)
                return null;
            return type.GetMethod("DoPostConfigureComplete", BindingFlags.Public | BindingFlags.Instance);
        }

        [HarmonyPostfix]
        public static void Postfix(ref GameObject go)
        {
            if (!PlantMutationRegister.DUAL_HEAD_ENABLED) return;

            var building = go.GetComponent<Building>();
            Vector3 centerOffset = Vector3.zero;
            if (building != null && building.Def != null)
            {
                centerOffset = new Vector3((building.Def.WidthInCells - 1) * 0.5f, (building.Def.HeightInCells - 1) * 0.5f, 0f);
            }

            var sub = PlantablePlotGameObject.Init(go, centerOffset);
            var plantablePlot = sub.AddOrGet<PlantablePlot>();
            
            var originalPlot = go.GetComponent<PlantablePlot>();
            if (originalPlot != null)
            {
                Vector3 adjustedOffset = originalPlot.occupyingObjectRelativePosition - centerOffset;
                plantablePlot.occupyingObjectRelativePosition = adjustedOffset;
            }
            else
            {
                plantablePlot.occupyingObjectRelativePosition.y = 1f;
            }

            plantablePlot.SetFertilizationFlags(true, true);

            go.AddOrGet<DualHeadReceptacleMarker>();

            PlantablePlotGameObject.SetUpFarmPlotTags(go, sub);
        }
    }
    [HarmonyPatch]
    public static class LargeBackwallFarmConfig_DoPostConfigureComplete_Patches
    {
        public static bool Prepare()
        {
            Type type = PlantablePlotGameObject.FindType("LargeBackwallFarmConfig");
            bool result = type != null;
            if (!result)
            return result;
        }

        public static MethodBase TargetMethod()
        {
            Type type = PlantablePlotGameObject.FindType("LargeBackwallFarmConfig");
            if (type == null)
                return null;
            return type.GetMethod("DoPostConfigureComplete", BindingFlags.Public | BindingFlags.Instance);
        }

        [HarmonyPostfix]
        public static void Postfix(ref GameObject go)
        {
            if (!PlantMutationRegister.DUAL_HEAD_ENABLED) return;

            var building = go.GetComponent<Building>();
            Vector3 centerOffset = Vector3.zero;
            if (building != null && building.Def != null)
            {
                centerOffset = new Vector3((building.Def.WidthInCells - 1) * 0.5f, (building.Def.HeightInCells - 1) * 0.5f, 0f);
            }
            else
            {
                TbbDebuger.LogWarning($"[双头株] building或building.Def为null, go.name={go.name}");
            }

            var sub = PlantablePlotGameObject.Init(go, centerOffset);
            
            var originalPlot = go.GetComponent<PlantablePlot>();
            var plantablePlot = sub.GetComponent<PlantablePlot>();
            
            if (originalPlot != null)
            {
                Vector3 adjustedOffset = originalPlot.occupyingObjectRelativePosition - centerOffset;
                plantablePlot.occupyingObjectRelativePosition = adjustedOffset;
            }
            else
            {
                Vector3 defaultOffset = new Vector3(0.49f, 0.0f, -0.5f) - centerOffset;
                plantablePlot.occupyingObjectRelativePosition = defaultOffset;
            }
            
            plantablePlot.AddDepositTag(GameTags.BackwallSeed);
            plantablePlot.SetReceptacleDirection(SingleEntityReceptacle.ReceptacleDirection.Top);
            plantablePlot.SetFertilizationFlags(true, false);
            
            // 设置标记用于按钮状态检测
            var marker = go.AddOrGet<DualHeadReceptacleMarker>();
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
                TbbDebuger.LogWarning($"{ex.Message}\n{ex.StackTrace}");
                return false;
            }
            return true;
        }
    }
}