using KSerialization;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    public class MutanterColonyComponent : KMonoBehaviour
    {
        [Serialize]
        private Tag speciesID;
        [Serialize]
        private int maxColonySize = int.MaxValue;
        [Serialize]
        private string colonyName;

        public Tag SpeciesID
        {
            get { return speciesID; }
            set { speciesID = value; }
        }

        public int MaxColonySize
        {
            get { return maxColonySize; }
            set { maxColonySize = value; }
        }

        public string ColonyName
        {
            get { return colonyName; }
            set { colonyName = value; }
        }

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            speciesID = gameObject.GetComponent<KPrefabID>().PrefabID();
        }

        public void SetColonyParameters(int maxSize, string name = null)
        {
            maxColonySize = maxSize;
            if (!string.IsNullOrEmpty(name))
            {
                colonyName = name;
            }
        }
    }
}