using MutantContainmentProject.Buildings;
using MutantContainmentProject.MutanterComponent;
using System.Collections.Generic;
using UnityEngine;

namespace MutantContainmentProject.Mutanters
{
    public class CircusJokerConfig : IEntityConfig, IHasDlcRestrictions
    {
        public static string ID = "MUTANTER_CIRCUS_JOKER";
        public static readonly string TRAIT_ID = "MutanterCircusJokerTrait";
        public static readonly string KANIM_NAME = "circus_joker_kanim";

        public GameObject CreatePrefab()
        {
            string name = STRINGS.ENTITY.MUTANTER.MUTANTER_CIRCUS_JOKER.NAME;
            string desc = STRINGS.ENTITY.MUTANTER.MUTANTER_CIRCUS_JOKER.DESCRIPTION;

            GameObject prefab = BaseMutanter.BaseGameObject(ID, name, desc, 1, 2, KANIM_NAME, KANIM_NAME, KANIM_NAME, null, 233.15f, 293.15f, 173.15f, 373.15f);

            BaseMutanter.ExtendMutanterMove(prefab, "WalkerNavGrid1x2");

            BaseMutanter.ExtendTraitsToBaseMutanter(prefab, TRAIT_ID, name, 25);

            // 安全措施偏好值
            Dictionary<SecureAction, float[]> secureActionPreferences = new Dictionary<SecureAction, float[]>
            {
                // 本能操作（对应勇气技能）
                [SecureAction.Instinct] = new float[] { 50f, 50f, 50f },
                // 洞察操作（对应防御技能）
                [SecureAction.Reconnaissance] = new float[] { 30f, 30f, 30f },
                // 自律操作（对应沟通技能）
                [SecureAction.Communicate] = new float[] { 10f, 10f, 10f },
                // 压迫操作（对应正义技能）
                [SecureAction.Intimidation] = new float[] { 10f, 10f, 10f }
            };

            BaseMutanter.ExtendToBaseMutanter(prefab, MutanterDangerLevel.Euclid, faction: FactionManager.FactionID.Pest, attackTags: new List<Tag>(), secureActionPreferences: secureActionPreferences);

            // 添加MoveImmediately组件
            prefab.AddOrGet<MoveImmediately>();

            // 添加DeathDamage组件
            var deathDamage = prefab.AddOrGet<DeathDamage>();
            deathDamage.damageAmount = 12.5f;
            deathDamage.damageRadius = 5f;

            // 添加CircusJokerBehavior组件
            prefab.AddOrGet<CircusJokerBehavior>();

            // 添加ChoreProvider组件
            prefab.AddOrGet<ChoreProvider>();

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