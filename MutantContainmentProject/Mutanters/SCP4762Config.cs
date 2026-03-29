using MutantContainmentProject.Buildings;
using MutantContainmentProject.MutanterComponent;
using System.Collections.Generic;
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
            BaseMutanter.ExtendToBaseMutanter(prefab, MutanterDangerLevel.Safe, faction: FactionManager.FactionID.Prey, attackTags: null, secureActionPreferences: secureActionPreferences);

            prefab.AddOrGetDef<MutanterColdIceMonitor.Def>();

            BaseMutanter.AddProductToMutanter(prefab, SimHashes.Ice.CreateTag(), 2000f, 0.9f);
            BaseMutanter.AddProductToMutanter(prefab, SimHashes.Snow.CreateTag(), 1500f, 0.8f);

            // 配置攻击策略
            var strategyManager = prefab.AddOrGet<AttackStrategyManager>();
            
            // 禁用所有攻击策略（安全级别，无攻击能力）
            strategyManager.SetStrategyEnabled(AttackStrategyManager.StrategyType.BasicAttack, false);
            strategyManager.SetStrategyEnabled(AttackStrategyManager.StrategyType.SkillAttack, false);

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