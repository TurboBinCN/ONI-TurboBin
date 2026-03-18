using MutantContainmentProject.Buildings;
using MutantContainmentProject.MutanterComponent;
using System.Collections.Generic;
using UnityEngine;

namespace MutantContainmentProject.Mutanters
{
    public class CosmicBugConfig : IEntityConfig, IHasDlcRestrictions
    {
        public static string ID = "MUTANTER_COSMIC_BUG";
        public static readonly string TRAIT_ID = "MutanterCosmicBugTrait";
        public static readonly string KANIM_NAME = "cosmic_bug_kanim";
        public static readonly string KANIM_BUILD_NAME = "cosmic_bug_kanim";
        public static readonly string KANIM_EMOTES_NAME = "chameleo_emotes_kanim";

        public GameObject CreatePrefab()
        {
            string name = STRINGS.ENTITY.MUTANTER.MUTANTER_COSMIC_BUG.NAME;
            string desc = STRINGS.ENTITY.MUTANTER.MUTANTER_COSMIC_BUG.DESCRIPTION;

            GameObject prefab = BaseMutanter.BaseGameObject(ID, name, desc, 2, 2, KANIM_NAME, KANIM_NAME, KANIM_EMOTES_NAME, null, 233.15f, 293.15f, 173.15f, 373.15f);

            BaseMutanter.ExtendMutanterMove(prefab, "WalkerNavGrid2x2");

            BaseMutanter.ExtendTraitsToBaseMutanter(prefab, TRAIT_ID, name, 40);

            // 安全措施偏好值 - 无收容偏好
            Dictionary<SecureAction, float[]> secureActionPreferences = new Dictionary<SecureAction, float[]>
            {
                // 本能操作（对应勇气技能）
                [SecureAction.Instinct] = new float[] { 50f, 50f, 50f },
                // 洞察操作（对应防御技能）
                [SecureAction.Reconnaissance] = new float[] { 50f, 50f, 50f },
                // 自律操作（对应沟通技能）
                [SecureAction.Communicate] = new float[] { 50f, 50f, 50f },
                // 压迫操作（对应正义技能）
                [SecureAction.Intimidation] = new float[] { 50f, 50f, 50f }
            };

            BaseMutanter.ExtendToBaseMutanter(prefab, MutanterDangerLevel.Euclid, faction: FactionManager.FactionID.Pest, attackTags: new List<Tag> { MutanterTags.ErosionAttack }, secureActionPreferences: secureActionPreferences, canBeCaptured: false, canBeDefeated: true);

            // 添加死后造成伤害的组件
            var deathDamageComponent = prefab.AddComponent<DeathDamage>();
            deathDamageComponent.attackTag = MutanterTags.PhysicalAttack;
            deathDamageComponent.damageAmount = 5f;

            // 添加产出物
            //BaseMutanter.AddProductToMutanter(prefab, SimHashes..CreateTag(), 500f, 0.8f);
            BaseMutanter.AddProductToMutanter(prefab, SimHashes.SlimeMold.CreateTag(), 300f, 0.5f);

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
