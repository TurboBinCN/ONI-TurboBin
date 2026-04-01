using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MutantContainmentProject.Buildings
{
    public static class ContainmentMonitorStationManager
    {
        private static List<KPrefabID> containmentStations = new List<KPrefabID>();

        public static void RegisterStation(GameObject station)
        {
            KPrefabID kPrefabID = station.GetComponent<KPrefabID>();
            if (kPrefabID != null && !containmentStations.Any(x => x.GetInstanceID() == kPrefabID.GetInstanceID()))
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
            containmentStations.ForEach(kPrefabID =>
            {
                if (kPrefabID == null || kPrefabID.gameObject == null || !kPrefabID.gameObject.activeSelf)
                {
                     containmentStations.Remove(kPrefabID);
                }
            });
            return containmentStations;
        }

        public static void Clear()
        {
            containmentStations.Clear();
        }
    }
}