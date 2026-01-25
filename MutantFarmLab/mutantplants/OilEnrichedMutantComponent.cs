using KSerialization;
using MutantFarmLab.tbbLibs;
using PeterHan.PLib.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static STRINGS.BUILDING.STATUSITEMS;

namespace MutantFarmLab.mutantplants
{
    internal class OilEnrichedMutantComponent : KMonoBehaviour
    {
        public static CellOffset OUTPUT_CONDUIT_CELL_OFFSET = new CellOffset(0, 0);
        private static readonly List<Storage.StoredItemModifier> storedItemModifiers = new List<Storage.StoredItemModifier>
        {
            Storage.StoredItemModifier.Hide,
            Storage.StoredItemModifier.Preserve,
            Storage.StoredItemModifier.Insulate,
            Storage.StoredItemModifier.Seal
        };
        protected override void OnSpawn()
        {
            base.OnSpawn();
            Storage storage = gameObject.AddOrGet<Storage>();
            storage.allowItemRemoval = false;
            storage.showInUI = true;
            storage.capacityKg = 2000f;
            storage.SetDefaultStoredItemModifiers(storedItemModifiers);

            var elementConsumer = gameObject.AddOrGet<ElementConsumer>();
            elementConsumer.storage = storage;
            elementConsumer.showInStatusPanel = true;
            elementConsumer.storeOnConsume = true;
            elementConsumer.elementToConsume = SimHashes.CarbonDioxide;
            elementConsumer.configuration = ElementConsumer.Configuration.Element;
            elementConsumer.consumptionRadius = 2;
            elementConsumer.EnableConsumption(true);
            elementConsumer.sampleCellOffset = new Vector3(0f, 0f);
            elementConsumer.consumptionRate = PlantMutationRegister.OIL_ENRICH_CARBONGAS_MOD;

            ConduitDispenser conduitDispenser = gameObject.AddOrGet<ConduitDispenser>();
            conduitDispenser.noBuildingOutputCellOffset = OUTPUT_CONDUIT_CELL_OFFSET;
            conduitDispenser.conduitType = ConduitType.Liquid;
            conduitDispenser.alwaysDispense = true;
            conduitDispenser.SetOnState(true);

            EntityCellVisualizer entityCellVisualizer = gameObject.AddOrGet<EntityCellVisualizer>();
            entityCellVisualizer.AddPort(EntityCellVisualizer.Ports.LiquidOut, OUTPUT_CONDUIT_CELL_OFFSET, entityCellVisualizer.Resources.liquidIOColours.output.connected);

            //气压
            var pressureVulnerable = gameObject.GetComponent<PressureVulnerable>();
            if(pressureVulnerable != null)
            {
                pressureVulnerable.pressureWarning_High = PlantMutationRegister.OIL_ENRICH_AIRPRESS_RANGE_MOD;
                pressureVulnerable.pressureLethal_High = pressureVulnerable.pressureWarning_High*1.5f;
                pressureVulnerable.pressureWarning_Low = 0;
                pressureVulnerable.pressureLethal_Low = 0;
            }
            gameObject.AddOrGet<DynamicStorageSaver>();

            gameObject.AddOrGet<OilEnrichedStates>();
        }

    }
    [SerializationConfig(MemberSerialization.OptIn)]
    public class DynamicStorageSaver : KMonoBehaviour, ISaveLoadable
    {
        public class ItemElement
        {
            public SimHashes id;
            public float Mass;
            public float Temperature;
        }
        [Serialize]
        private List<ItemElement> savedItems = new();

        private Storage dynamicStorage;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            dynamicStorage = gameObject.AddOrGet<Storage>();
        }
        [OnSerializing]
        public void Serialize()
        {
            savedItems.Clear();
            if (dynamicStorage.items.Count <= 0) return;
            foreach (var item in dynamicStorage.items)
            {
                PrimaryElement primary = item.GetComponent<PrimaryElement>();
                if (primary != null)
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
        public void Deserialize()
        {
            RestoreItems();
        }

        private void RestoreItems()
        {
            if (savedItems.Count <= 0 || dynamicStorage == null) return;

            foreach (var item in savedItems)
            {
                GameObject itemGo = Util.KInstantiate(Assets.GetPrefab(item.id.CreateTag()));
                itemGo.SetActive(true);
                dynamicStorage.Store(itemGo);
                dynamicStorage.AddToPrimaryElement(item.id, item.Mass, item.Temperature);
            }
            savedItems.Clear();
        }
    }
}
