using KSerialization;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace MutantFarmLab.mutantplants
{
    internal class OilEnrichedMutantComponent : KMonoBehaviour
    {
        public static CellOffset OUTPUT_CONDUIT_CELL_OFFSET = new(0, 0);
        public ElementConsumer co2Consumer;
        private static readonly List<Storage.StoredItemModifier> storedItemModifiers = new()
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

            co2Consumer = gameObject.AddComponent<ElementConsumer>();
            co2Consumer.storage = storage;
            co2Consumer.showInStatusPanel = true;
            co2Consumer.storeOnConsume = true;
            co2Consumer.elementToConsume = SimHashes.CarbonDioxide;
            co2Consumer.configuration = ElementConsumer.Configuration.Element;
            co2Consumer.consumptionRadius = 2;
            co2Consumer.EnableConsumption(true);
            co2Consumer.sampleCellOffset = new Vector3(0f, 0f);
            co2Consumer.consumptionRate = PlantMutationRegister.OIL_ENRICH_CARBONGAS_MOD;

            ConduitDispenser conduitDispenser = gameObject.AddOrGet<ConduitDispenser>();
            conduitDispenser.noBuildingOutputCellOffset = OUTPUT_CONDUIT_CELL_OFFSET;
            conduitDispenser.conduitType = ConduitType.Liquid;
            conduitDispenser.alwaysDispense = true;
            conduitDispenser.SetOnState(true);

            EntityCellVisualizer entityCellVisualizer = gameObject.AddOrGet<EntityCellVisualizer>();
            entityCellVisualizer.AddPort(EntityCellVisualizer.Ports.LiquidOut, OUTPUT_CONDUIT_CELL_OFFSET, entityCellVisualizer.Resources.liquidIOColours.output.connected);

            //气压
            var pressureVulnerable = gameObject.GetComponent<PressureVulnerable>();
            if (pressureVulnerable != null)
            {
                pressureVulnerable.pressureWarning_High = PlantMutationRegister.OIL_ENRICH_AIRPRESS_RANGE_MOD;
                pressureVulnerable.pressureLethal_High = pressureVulnerable.pressureWarning_High * 1.5f;
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
