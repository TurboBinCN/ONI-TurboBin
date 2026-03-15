using MutantContainmentProject.MutanterComponent;
using UnityEngine;

namespace MutantContainmentProject.Mutanters
{
    public class SCP4762Config : IEntityConfig, IHasDlcRestrictions
    {
        public static string ID = "MUTANTER_SCP4762";
        public static readonly string TRAIT_ID = "MutanterSCP4762Trait";
        public static readonly string KANIM_NAME = "scp4762_kanim";
        public static readonly string KANIM_BUILD_NAME = "chameleo_build_kanim";
        public static readonly string KANIM_EMOTES_NAME = "chameleo_emotes_kanim";

        public GameObject CreatePrefab()
        {
            string name = STRINGS.ENTITY.MUTANTER.MUTANTER_SCP4762.NAME;
            string desc = STRINGS.ENTITY.MUTANTER.MUTANTER_SCP4762.DESCRIPTION;

            GameObject prefab = BaseMutanter.BaseGameObject(ID, name, desc, 1, 2, KANIM_NAME, KANIM_NAME, KANIM_EMOTES_NAME, null, 198.15f, 373.15f, 173.15f, 373.15f);

            BaseMutanter.ExtendMutanterMove(prefab, "WalkerNavGrid1x2", moveSpeed: 1);

            BaseMutanter.ExtendTraitsToBaseMutanter(prefab, TRAIT_ID, name, 350);

            BaseMutanter.ExtendToBaseMutanter(prefab, MutanterDangerLevel.Safe, faction: FactionManager.FactionID.Prey, attackTags: null);

            prefab.AddOrGetDef<MutanterColdIceMonitor.Def>();

            BaseMutanter.AddProductToMutanter(prefab, SimHashes.Ice.CreateTag(), 2000f, 0.9f);
            BaseMutanter.AddProductToMutanter(prefab, SimHashes.Snow.CreateTag(), 1500f, 0.8f);

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