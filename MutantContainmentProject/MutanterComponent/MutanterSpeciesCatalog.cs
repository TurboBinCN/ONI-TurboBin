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

        public static MutanterSpeciesCatalog Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<MutanterSpeciesCatalog>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("MutanterSpeciesCatalog");
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
            }
        }
        public void RegisterMutanterSpecies(Tag speciesID)
        {
            if (!this.discoveredMutanters.ContainsKey(speciesID))
            {
                this.discoveredMutanters[speciesID] = 1;
            }
            else
            {
                this.discoveredMutanters[speciesID]++;
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
            return discoveredMutanters.Count;
        }

        public bool IsMutanterSpeciesExists(Tag speciesID)
        {
            return discoveredMutanters.ContainsKey(speciesID);
        }
    }
}
