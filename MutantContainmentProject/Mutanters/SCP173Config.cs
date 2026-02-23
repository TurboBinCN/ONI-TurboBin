using Klei.AI;
using MutantContainmentProject.MutanterComponent;
using UnityEngine;

namespace MutantContainmentProject.Mutanters
{
    public class SCP173Config : IEntityConfig, IHasDlcRestrictions
    {
        public static string ID = "MUTANTER_SCP173";
        public static readonly string TRAIT_ID = "MutanterSCP173Trait";
        //public static readonly string KANIM_NAME = "chameleo_kanim";
        public static readonly string KANIM_NAME = "SCP173_kanim";
        public static readonly string KANIM_BUILD_NAME = "chameleo_build_kanim";
        public static readonly string KANIM_EMOTES_NAME = "chameleo_emotes_kanim";

        public GameObject CreatePrefab()
        {
            string name = STRINGS.ENTITY.MUTANTER.MUTANTER_SCP173.NAME;
            string desc = STRINGS.ENTITY.MUTANTER.MUTANTER_SCP173.DESCRIPTION;

            GameObject prefab = BaseMutanter.BaseGameObject(ID, name, desc, KANIM_NAME, KANIM_NAME, KANIM_EMOTES_NAME, null, 233.15f, 293.15f, 173.15f, 373.15f);

            BaseMutanter.ExtendMutanterMove(prefab, "DreckoNavGrid");

            BaseMutanter.ExtendTraitsToBaseMutanter(prefab, TRAIT_ID, name,25);

            BaseMutanter.ExtendToBaseMutanter(prefab, MutanterDangerLevel.TETH,faction:FactionManager.FactionID.Pest);

            // 添加产出物
            BaseMutanter.AddProductToMutanter(prefab, SimHashes.Gold.CreateTag(), 1000f, 0.8f);
            BaseMutanter.AddProductToMutanter(prefab, SimHashes.Diamond.CreateTag(), 1000f, 0.4f);

            return prefab;
        }


        public string[] GetRequiredDlcIds() => DlcManager.AVAILABLE_ALL_VERSIONS;
        public string[] GetForbiddenDlcIds() => null;
        public string[] GetAnyRequiredDlcIds() => null;
        public string[] GetDlcIds() => DlcManager.AVAILABLE_ALL_VERSIONS;

        public void OnPrefabInit(GameObject inst)
        {
        }

        public void OnSpawn(GameObject inst)
        {
        }
    }
}
