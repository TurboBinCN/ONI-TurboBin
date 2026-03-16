using MutantContainmentProject.Buildings;
using MutantContainmentProject.MutanterComponent;
using System.Collections.Generic;
using UnityEngine;

namespace MutantContainmentProject.Mutanters
{
    public class DawnConfusionConfig : IEntityConfig, IHasDlcRestrictions
    {
        public static string ID = "MUTANTER_DAWN_CONFUSION";
        public static readonly string TRAIT_ID = "MutanterDawnConfusionTrait";
        public static readonly string KANIM_NAME = "dawn_doubt_kanim";
        public static readonly string KANIM_BUILD_NAME = "dawn_doubt_kanim";
        public static readonly string KANIM_EMOTES_NAME = "chameleo_emotes_kanim";

        public GameObject CreatePrefab()
        {
            string name = STRINGS.ENTITY.MUTANTER.MUTANTER_DAWN_CONFUSION.NAME;
            string desc = STRINGS.ENTITY.MUTANTER.MUTANTER_DAWN_CONFUSION.DESCRIPTION;

            GameObject prefab = BaseMutanter.BaseGameObject(ID, name, desc, 1, 2, KANIM_NAME, KANIM_NAME, KANIM_EMOTES_NAME, null, 233.15f, 293.15f, 173.15f, 373.15f);

            BaseMutanter.ExtendMutanterMove(prefab, "WalkerNavGrid1x2");

            BaseMutanter.ExtendTraitsToBaseMutanter(prefab, TRAIT_ID, name, 30);

            // 安全措施偏好值
            Dictionary<SecureAction, float[]> secureActionPreferences = new Dictionary<SecureAction, float[]>
            {
                // 本能操作（对应勇气技能）
                [SecureAction.Instinct] = new float[] { 60f, 60f, 60f },
                // 洞察操作（对应防御技能）
                [SecureAction.Reconnaissance] = new float[] { 10f, 20f, 30f },
                // 自律操作（对应沟通技能）
                [SecureAction.Communicate] = new float[] { 5f, 15f, 25f },
                // 压迫操作（对应正义技能）
                [SecureAction.Intimidation] = new float[] { 20f, 40f, 50f }
            };

            BaseMutanter.ExtendToBaseMutanter(prefab, MutanterDangerLevel.Euclid, faction: FactionManager.FactionID.Pest, attackTags: new List<Tag> { MutanterTags.PhysicalAttack, MutanterTags.PsychologicalAttack }, secureActionPreferences: secureActionPreferences, canBeCaptured: false, canBeDefeated: true);

            // 添加产出物
            BaseMutanter.AddProductToMutanter(prefab, SimHashes.Iron.CreateTag(), 1000f, 0.8f);
            BaseMutanter.AddProductToMutanter(prefab, SimHashes.Steel.CreateTag(), 500f, 0.4f);

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