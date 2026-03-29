using MutantContainmentProject.Buildings;
using MutantContainmentProject.MutanterComponent;
using System.Collections.Generic;
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

            GameObject prefab = BaseMutanter.BaseGameObject(ID, name, desc, 1, 2, KANIM_NAME, KANIM_NAME, KANIM_EMOTES_NAME, null, 233.15f, 293.15f, 173.15f, 373.15f);

            BaseMutanter.ExtendMutanterMove(prefab, "WalkerNavGrid1x2");

            BaseMutanter.ExtendTraitsToBaseMutanter(prefab, TRAIT_ID, name, 25);

            // 安全措施偏好值
            Dictionary<SecureAction, float[]> secureActionPreferences = new Dictionary<SecureAction, float[]>
            {
                // 本能操作（对应勇气技能）
                [SecureAction.Instinct] = new float[] { 70f, 70f, 70f },
                // 洞察操作（对应防御技能）
                [SecureAction.Reconnaissance] = new float[] { 5f, 15f, 25f },
                // 自律操作（对应沟通技能）
                [SecureAction.Communicate] = new float[] { 10f, 30f, 40f },
                // 压迫操作（对应正义技能）
                [SecureAction.Intimidation] = new float[] { 15f, 35f, 45f }
            };

            BaseMutanter.ExtendToBaseMutanter(prefab, MutanterDangerLevel.Euclid, faction: FactionManager.FactionID.Pest, attackTags: new List<Tag> { MutanterTags.PsychologicalAttack }, secureActionPreferences: secureActionPreferences);

            // 添加产出物
            BaseMutanter.AddProductToMutanter(prefab, SimHashes.Gold.CreateTag(), 1000f, 0.8f);
            BaseMutanter.AddProductToMutanter(prefab, SimHashes.Diamond.CreateTag(), 1000f, 0.4f);
            
            // 配置攻击策略
            var strategyManager = prefab.AddOrGet<AttackStrategyManager>();
            strategyManager.SetStrategyEnabled(AttackStrategyManager.StrategyType.SkillAttack, false);
            strategyManager.SetStrategyEnabled(AttackStrategyManager.StrategyType.BasicAttack, true);
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
