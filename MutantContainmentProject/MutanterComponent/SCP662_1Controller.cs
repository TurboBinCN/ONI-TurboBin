using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject
{
    // Controller for SCP-662-1 instance to handle filter changes
    public class SCP6621InstanceController : KMonoBehaviour
    {
        private TreeFilterable treeFilterable;
        private Storage storage;
        private HashSet<Tag> selectedTags = new();

        protected override void OnSpawn()
        {
            base.OnSpawn();

            // Get TreeFilterable and Storage components from SCP-662-1
            treeFilterable = base.GetComponent<TreeFilterable>();
            storage = base.GetComponent<Storage>();

            if (treeFilterable != null)
            {
                treeFilterable.OnFilterChanged += OnFilterChanged;
                selectedTags = treeFilterable.GetTags();
            }

            // Discover all items
            DicoveryAllItems();
        }

        protected override void OnCleanUp()
        {
            base.OnCleanUp();
            if (treeFilterable != null)
            {
                treeFilterable.OnFilterChanged -= OnFilterChanged;
            }

        }
        private void DicoveryAllItems()
        {
            var allFood = EdiblesManager.GetAllFoodTypes();

            foreach (EdiblesManager.FoodInfo food in allFood)
            {
                try
                {
                    if ((double)food.CaloriesPerUnit == 0.0)
                        DiscoveredResources.Instance.Discover(food.Id.ToTag(), GameTags.CookingIngredient);
                    else
                        DiscoveredResources.Instance.Discover(food.Id.ToTag(), GameTags.Edible);
                }
                catch (System.Exception e)
                {
                    TbbDebuger.LogWarning("Unpossible to load " + food.Id + e.Message + e.StackTrace);
                }
            }
        }
        private void OnFilterChanged(HashSet<Tag> tags)
        {
            // Update selectedTags to the latest tags
            selectedTags = new HashSet<Tag>(tags);

            // Only process if there are selected tags
            if (selectedTags != null && selectedTags.Count > 0 && storage != null)
            {
                // Generate and store selected elements
                GenerateAndStoreElements();

                // Destroy SCP-662-1 after generating items
                base.gameObject.DeleteObject();
            }
        }

        private void GenerateAndStoreElements()
        {
            if (storage == null || selectedTags == null || selectedTags.Count == 0)
            {
                return;
            }

            // Generate and store each selected element
            foreach (Tag tag in selectedTags)
            {
                // Create the element item
                GameObject item = Util.KInstantiate(Assets.GetPrefab(tag), base.transform.position);
                if (item != null)
                {
                    item.SetActive(true);
                    // Set mass to 100kg
                    PrimaryElement primaryElement = item.GetComponent<PrimaryElement>();
                    if (primaryElement != null)
                    {
                        primaryElement.Mass = 100f;
                    }

                    // Add the item to storage
                    storage.Store(item);
                }
            }

            // Drop all items from storage
            storage.DropAll(false, false, Vector3.zero, true, null);
        }
    }
}