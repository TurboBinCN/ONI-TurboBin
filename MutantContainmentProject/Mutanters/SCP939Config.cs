using MutantContainmentProject.MutanterComponent;
using System.Collections.Generic;
using UnityEngine;

namespace MutantContainmentProject.Mutanters
{
    public class SCP939Config : IEntityConfig, IHasDlcRestrictions
    {
        public static string ID = "MUTANTER_SCP939";
        public static readonly string TRAIT_ID = "MutanterSCP939Trait";
        public static readonly string KANIM_NAME = "SCP173_kanim";
        public static readonly string KANIM_EMOTES_NAME = "chameleo_emotes_kanim";


        public GameObject CreatePrefab()
        {
            string name = STRINGS.ENTITY.MUTANTER.MUTANTER_SCP939.NAME;
            string desc = STRINGS.ENTITY.MUTANTER.MUTANTER_SCP939.DESCRIPTION;

            GameObject prefab = BaseMutanter.BaseGameObject(ID, name, desc, 1, 2, KANIM_NAME, KANIM_NAME, KANIM_EMOTES_NAME, null, 233.15f, 293.15f, 173.15f, 373.15f);

            BaseMutanter.ExtendMutanterMove(prefab, "WalkerNavGrid1x2");

            BaseMutanter.ExtendTraitsToBaseMutanter(prefab, TRAIT_ID, name, 50);

            BaseMutanter.ExtendToBaseMutanter(prefab, MutanterDangerLevel.Keter, faction: FactionManager.FactionID.Pest, attackTags: new List<Tag> { MutanterTags.PhysicalAttack, MutanterTags.PsychologicalAttack });

            // 添加SCP-939特有的组件
            prefab.AddOrGet<SCP939Controller>();

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