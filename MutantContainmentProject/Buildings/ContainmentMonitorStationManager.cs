using System.Collections.Generic;
using UnityEngine;

namespace MutantContainmentProject.Buildings
{
    public static class ContainmentMonitorStationManager
    {
        private static List<KPrefabID> containmentStations = new List<KPrefabID>();

        public static void RegisterStation(GameObject station)
        {
            KPrefabID kPrefabID = station.GetComponent<KPrefabID>();
            if (kPrefabID != null && !containmentStations.Contains(kPrefabID))
            {
                containmentStations.Add(kPrefabID);
            }
        }

        public static void UnregisterStation(GameObject station)
        {
            KPrefabID kPrefabID = station.GetComponent<KPrefabID>();
            if (kPrefabID != null)
            {
                containmentStations.Remove(kPrefabID);
            }
        }

        public static List<KPrefabID> GetAllStations()
        {
            // 清理无效的实例
            containmentStations.RemoveAll(kPrefabID => kPrefabID == null || kPrefabID.gameObject == null || !kPrefabID.gameObject.activeSelf);
            return containmentStations;
        }

        public static void Clear()
        {
            containmentStations.Clear();
        }
    }
}