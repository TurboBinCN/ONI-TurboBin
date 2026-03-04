using MutantContainmentProject.MutanterComponent;
using System.Collections.Generic;
using TUNING;
using UnityEngine;

namespace MutantContainmentProject.Mutanters
{
    public class SCP662_1Config : IEntityConfig, IHasDlcRestrictions
    {
        public static string ID = "MUTANTER_SCP662_1";
        public static readonly string TRAIT_ID = "MutanterSCP662_1Trait";
        public static readonly string KANIM_NAME = "scp662_1_kanim";
        public static readonly string KANIM_EMOTES_NAME = "chameleo_emotes_kanim";

        public GameObject CreatePrefab()
        {
            string name = STRINGS.ENTITY.MUTANTER.MUTANTER_SCP662_1.NAME;
            string desc = STRINGS.ENTITY.MUTANTER.MUTANTER_SCP662_1.DESCRIPTION;

            GameObject prefab = BaseMutanter.BaseGameObject(ID, name, desc, 1, 2, KANIM_NAME, KANIM_NAME, KANIM_EMOTES_NAME, null, 233.15f, 293.15f, 173.15f, 373.15f);

            BaseMutanter.ExtendMutanterMove(prefab, "WalkerNavGrid1x2");

            BaseMutanter.ExtendTraitsToBaseMutanter(prefab, TRAIT_ID, name, 15);

            BaseMutanter.ExtendToBaseMutanter(prefab, MutanterDangerLevel.Safe, considerDecor: false, useEmotionMonitor: false, faction: FactionManager.FactionID.Pest, attackTags: null);

            var storage = prefab.AddOrGet<Storage>();
            storage.storageFilters = STORAGEFILTERS.FOOD;
            storage.allowClearable = false;
            storage.allowItemRemoval = false;
            var treeFilterable = prefab.AddOrGet<TreeFilterable>();
            treeFilterable.copySettingsEnabled = false;

            return prefab;
        }

        public string[] GetRequiredDlcIds() => DlcManager.AVAILABLE_ALL_VERSIONS;
        public string[] GetForbiddenDlcIds() => null;
        public string[] GetAnyRequiredDlcIds() => null;
        public string[] GetDlcIds() => DlcManager.AVAILABLE_ALL_VERSIONS;

        public void OnPrefabInit(GameObject inst) { }

        public void OnSpawn(GameObject inst) { }
    }
}