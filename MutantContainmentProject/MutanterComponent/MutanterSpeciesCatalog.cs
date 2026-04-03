using KSerialization;
using System.Collections.Generic;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    public class MutanterSpeciesCatalog : KMonoBehaviour
    {
        private static MutanterSpeciesCatalog _instance;
        [Serialize]
        private Dictionary<Tag, int> discoveredMutanters = new();
        [Serialize]
        private Dictionary<Tag, int> maxMutanterCounts = new();

        public static MutanterSpeciesCatalog Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<MutanterSpeciesCatalog>();
                    if (_instance == null)
                    {
                        GameObject go = new("MutanterSpeciesCatalog");
                        _instance = go.AddComponent<MutanterSpeciesCatalog>();
                    }
                }
                return _instance;
            }
        }
        public MutanterSpeciesCatalog()
        {
        }
        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            _instance = this;
        }
        protected override void OnSpawn()
        {
            base.OnSpawn();
            this.EnsureMutanterSpecies();
        }

        private void EnsureMutanterSpecies()
        {
            foreach (GameObject gameObject in Assets.GetPrefabsWithTag(MutanterTags.Mutanter))
            {
                KPrefabID kPrefabID = gameObject.GetComponent<KPrefabID>();
                Tag speciesID = kPrefabID.PrefabID();
                if (!this.discoveredMutanters.ContainsKey(speciesID))
                {
                    this.discoveredMutanters[speciesID] = 0;
                }
                if (!this.maxMutanterCounts.ContainsKey(speciesID))
                {
                    this.maxMutanterCounts[speciesID] = int.MaxValue; // 默认无限制
                }
            }
        }
        public void SetMaxMutanterCount(Tag speciesID, int maxCount)
        {
            this.maxMutanterCounts[speciesID] = maxCount;
        }
        public int GetMaxMutanterCount(Tag speciesID)
        {
            // 首先检查是否在maxMutanterCounts中设置了值
            if (maxMutanterCounts.TryGetValue(speciesID, out int maxCount))
            {
                return maxCount;
            }
            
            // 然后尝试从MutanterColonyComponent中读取
            GameObject prefab = Assets.GetPrefab(speciesID.Name);
            if (prefab != null)
            {
                MutanterColonyComponent colonyComponent = prefab.GetComponent<MutanterColonyComponent>();
                if (colonyComponent != null)
                {
                    return colonyComponent.MaxColonySize;
                }
            }
            
            // 默认无限制
            return int.MaxValue;
        }
        public bool CanSpawnMutanter(Tag speciesID)
        {
            int currentCount = GetMutanterSpeciesCount(speciesID);
            int maxCount = GetMaxMutanterCount(speciesID);
            return currentCount < maxCount;
        }
        public void RegisterMutanterSpecies(Tag speciesID)
        {
            if (!this.discoveredMutanters.ContainsKey(speciesID))
            {
                this.discoveredMutanters[speciesID] = 1;
            }
            else
            {
                if (CanSpawnMutanter(speciesID))
                {
                    this.discoveredMutanters[speciesID]++;
                }
            }
        }
        public int GetMutanterSpeciesCount(Tag speciesID)
        {
            if (discoveredMutanters.TryGetValue(speciesID, out int count))
            {
                return count;
            }
            return 0;
        }
        public int GetMutanterSpeciesCount()
        {
            int count = 0;
            foreach (var entry in discoveredMutanters)
            {
                if (entry.Value >= 1)
                {
                    count++;
                }
            }
            return count;
        }

        public bool IsMutanterSpeciesExists(Tag speciesID)
        {
            return discoveredMutanters.ContainsKey(speciesID) && discoveredMutanters[speciesID] >= 1;
        }
    }
}
