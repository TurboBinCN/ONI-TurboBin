using MutantContainmentProject.Buildings;
using MutantContainmentProject.MutanterComponent;
using System.Collections.Generic;
using UnityEngine;
using static MutantContainmentProject.MutanterComponent.MutanterSkillComponent;

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

            BaseMutanter.ExtendToBaseMutanter(prefab, MutanterDangerLevel.Euclid, faction: FactionManager.FactionID.Pest, attackTags: new List<Tag>(), secureActionPreferences: secureActionPreferences, canBeCaptured: false, canBekilled: true);

            // 添加MoveImmediately组件
            prefab.AddOrGet<MoveImmediately>();

            // 添加CircusJokerBehavior组件
            prefab.AddOrGet<CircusJokerBehavior>();

            // 添加ChoreProvider组件
            prefab.AddOrGet<ChoreProvider>();

            // 添加技能攻击组件
            var skillComponent = prefab.AddOrGet<MutanterSkillComponent>();
            var skills = new List<SkillData>{
                //死亡攻击
                new() {
                    name = "DeathAttack",
                    isPassiveSkill = true,
                    cooldown = 0f,
                    animation = "death",
                    lastUseTime = 0f,
                    isFirstUse = true,
                    VFXName = "CircusJokerDeathDamangeVFX",
                    attackEffectors = new List<AttackEffectorData>{
                        new(){
                            attackEffectorName = "BasicAttackBounsApply",
                            damageType = MutanterTags.PhysicalAttack,
                            damageAmount = Random.Range(30f, 50f)
                        }
                    },
                    triggers = new List<TriggerData>()
                    {
                        new(){
                            triggerName = "DeathTrigger",
                            properties = new Dictionary<string, object>()
                        }
                    }
                }
            };

            // 设置技能并添加到数据库
            skillComponent.AddSkillsToDb(skills);

            // 配置攻击策略
            var strategyManager = prefab.AddOrGet<AttackStrategyManager>();
            
            // 只启用基础攻击策略
            strategyManager.SetStrategyEnabled(AttackStrategyManager.StrategyType.BasicAttack, true);
            strategyManager.SetStrategyEnabled(AttackStrategyManager.StrategyType.SkillAttack, true);

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