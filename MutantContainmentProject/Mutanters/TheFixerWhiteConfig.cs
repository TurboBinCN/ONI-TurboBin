using MutantContainmentProject.Buildings;
using MutantContainmentProject.MutanterComponent;
using System.Collections.Generic;
using UnityEngine;
using static MutantContainmentProject.MutanterComponent.MutanterSkillComponent;

namespace MutantContainmentProject.Mutanters
{
    public class TheFixerWhiteConfig : IEntityConfig, IHasDlcRestrictions
    {
        public static string ID = "MUTANTER_THE_FIXER_WHITE";
        public static readonly string TRAIT_ID = "MutanterTheFixerWhiteTrait";
        public static readonly string KANIM_NAME = "the_fixer_white_kanim";

        public GameObject CreatePrefab()
        {
            string name = STRINGS.ENTITY.MUTANTER.MUTANTER_THE_FIXER_WHITE.NAME;
            string desc = STRINGS.ENTITY.MUTANTER.MUTANTER_THE_FIXER_WHITE.DESCRIPTION;

            GameObject prefab = BaseMutanter.BaseGameObject(ID, name, desc, 1, 2, KANIM_NAME, KANIM_NAME, KANIM_NAME, null, 233.15f, 293.15f, 173.15f, 373.15f);

            BaseMutanter.ExtendMutanterMove(prefab, "WalkerNavGrid1x2");

            BaseMutanter.ExtendTraitsToBaseMutanter(prefab, TRAIT_ID, name, 200);

            // 安全措施偏好值
            Dictionary<SecureAction, float[]> secureActionPreferences = new()
            {
                // 本能操作（对应勇气技能）
                [SecureAction.Instinct] = new float[] { 10f, 20f, 30f },
                // 洞察操作（对应防御技能）
                [SecureAction.Reconnaissance] = new float[] { 5f, 15f, 25f },
                // 自律操作（对应沟通技能）
                [SecureAction.Communicate] = new float[] { 5f, 15f, 25f },
                // 压迫操作（对应正义技能）
                [SecureAction.Intimidation] = new float[] { 5f, 15f, 25f }
            };

            BaseMutanter.ExtendToBaseMutanter(prefab, MutanterDangerLevel.Keter, faction: FactionManager.FactionID.Pest, attackTags: new List<Tag> { MutanterTags.PsychologicalAttack }, secureActionPreferences: secureActionPreferences, canBeCaptured: false, canBekilled: true);


            // 添加技能攻击组件
            var skillComponent = prefab.AddOrGet<MutanterSkillComponent>();

            // 精神激光攻击
            var skills = new List<SkillData>{
                new() {
                    name = "MentalLaser",
                    isPassiveSkill = false,
                    cooldown = Random.Range(20f, 30f),
                    animation = "attack_skill_1",
                    lastUseTime = 0f,
                    VFXName = "FixerWhiteStraightLaserVFX",
                    isFirstUse = true,
                    attackEffectors = new List<AttackEffectorData>{
                        new(){ 
                            attackEffectorName = "BasicAttackBounsApply",
                            damageType = MutanterTags.PsychologicalAttack,
                            damageAmount = Random.Range(10f, 12f)
                        }
                    },
                    triggers = new List<TriggerData> {
                        new() {
                            triggerName = "DistanceTrigger",
                            properties = new Dictionary<string, object> {
                                { "Range", 15 }
                            }
                        }
                    }
                },
                // 240°激光横扫
                new() {
                    name = "LaserSweep",
                    isPassiveSkill = false,
                    cooldown = Random.Range(40f, 45f),
                    animation = "attack_skill_2",
                    lastUseTime = 0f,
                    VFXName = "FixerWhiteLaserSweepVFX",
                    isFirstUse = true,
                    attackEffectors = new List<AttackEffectorData>{
                        new(){
                            attackEffectorName = "BasicAttackBounsApply",
                            damageType = MutanterTags.PsychologicalAttack,
                            damageAmount = Random.Range(10f, 12f)
                        }
                    },
                    triggers = new List<TriggerData> {
                        new() {
                            triggerName = "DistanceTrigger",
                            properties = new Dictionary<string, object> {
                                { "Range", 15 }
                            }
                        }
                    }
                },
                // 祈祷反伤
                new() {
                    name = "Prayer",
                    isPassiveSkill = true,
                    cooldown = 0f,
                    animation = "attack_skill_pray",
                    animationDuration = 10f,
                    lastUseTime = 0f,
                    isFirstUse = true,
                    attackEffectors = new List<AttackEffectorData>{
                        new(){
                            attackEffectorName = "BasicAttackBounsApply",
                            damageType = MutanterTags.PsychologicalAttack,
                            damageAmount = 0
                        },
                        new(){
                            attackEffectorName = "DamageReflection",
                            damageType = Tag.Invalid,
                            damageAmount = 0
                        },
                        new(){
                            attackEffectorName = "FrozenHitPoints",
                            damageType = Tag.Invalid,
                            damageAmount = 0
                        }
                    },
                    triggers = new List<TriggerData> {
                        new() {
                            triggerName = "HealthChangeTrigger",
                            properties = new Dictionary<string, object>()
                            {
                                { "ChangeDelta", 0.3 }
                            }
                        }
                    }
                }
            };

            // 设置技能并添加到数据库
            skillComponent.AddSkillsToDb(skills);

            // 配置攻击策略
            var strategyManager = prefab.AddOrGet<AttackStrategyManager>();
            strategyManager.SetStrategyEnabled(AttackStrategyManager.StrategyType.SkillAttack, true);
            strategyManager.SetStrategyEnabled(AttackStrategyManager.StrategyType.BasicAttack, false);
            strategyManager.SetStrategyPriority(AttackStrategyManager.StrategyType.SkillAttack, 2.0f);

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