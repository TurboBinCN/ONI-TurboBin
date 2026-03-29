using MutantContainmentProject.Buildings;
using MutantContainmentProject.MutanterComponent;
using System.Collections.Generic;
using UnityEngine;

namespace MutantContainmentProject.Mutanters
{
    public class BugDuskConfig : IEntityConfig, IHasDlcRestrictions
    {
        public static string ID = "MUTANTER_BUG_DUSK";
        public static readonly string TRAIT_ID = "MutanterBugDuskTrait";
        public static readonly string KANIM_NAME = "bugdusk_kanim";
        public static readonly string KANIM_BUILD_NAME = "bugdusk_kanim";
        public static readonly string KANIM_EMOTES_NAME = "chameleo_emotes_kanim";

        public GameObject CreatePrefab()
        {
            string name = STRINGS.ENTITY.MUTANTER.MUTANTER_BUG_DUSK.NAME;
            string desc = STRINGS.ENTITY.MUTANTER.MUTANTER_BUG_DUSK.DESCRIPTION;

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

            BaseMutanter.ExtendToBaseMutanter(prefab, MutanterDangerLevel.Euclid, faction: FactionManager.FactionID.Pest, attackTags: new List<Tag> { MutanterTags.PhysicalAttack }, secureActionPreferences: secureActionPreferences, canBeCaptured: false, canBekilled: true);

            // 添加产出物
            BaseMutanter.AddProductToMutanter(prefab, SimHashes.ToxicSand.CreateTag(), 500f, 0.8f);
            BaseMutanter.AddProductToMutanter(prefab, SimHashes.SlimeMold.CreateTag(), 300f, 0.5f);
            
            // 配置攻击策略
            var strategyManager = prefab.AddOrGet<AttackStrategyManager>();
            strategyManager.SetStrategyEnabled(AttackStrategyManager.StrategyType.BasicAttack, true);
            strategyManager.SetStrategyEnabled(AttackStrategyManager.StrategyType.SkillAttack, false);
            strategyManager.SetStrategyPriority(AttackStrategyManager.StrategyType.BasicAttack, 1.0f);

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