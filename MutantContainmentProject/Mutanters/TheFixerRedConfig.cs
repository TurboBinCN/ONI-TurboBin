using MutantContainmentProject.Buildings;
using MutantContainmentProject.MutanterComponent;
using System.Collections.Generic;
using UnityEngine;
using static MutantContainmentProject.MutanterComponent.MutanterSkillComponent;

namespace MutantContainmentProject.Mutanters
{
    public class TheFixerRedConfig : IEntityConfig, IHasDlcRestrictions
    {
        public static string ID = "MUTANTER_THE_FIXER_RED";
        public static readonly string TRAIT_ID = "MutanterTheFixerRedTrait";
        public static readonly string KANIM_NAME = "the_fixer_red_kanim";

        public GameObject CreatePrefab()
        {
            string name = STRINGS.ENTITY.MUTANTER.MUTANTER_THE_FIXER_RED.NAME;
            string desc = STRINGS.ENTITY.MUTANTER.MUTANTER_THE_FIXER_RED.DESCRIPTION;

            GameObject prefab = BaseMutanter.BaseGameObject(ID, name, desc, 1, 2, KANIM_NAME, KANIM_NAME, KANIM_NAME, null, 233.15f, 293.15f, 173.15f, 373.15f);

            BaseMutanter.ExtendMutanterMove(prefab, "WalkerNavGrid1x2");

            BaseMutanter.ExtendTraitsToBaseMutanter(prefab, TRAIT_ID, name, 25);

            // 安全措施偏好值
            Dictionary<SecureAction, float[]> secureActionPreferences = new()
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

            BaseMutanter.ExtendToBaseMutanter(prefab, MutanterDangerLevel.Keter, faction: FactionManager.FactionID.Pest, attackTags: new List<Tag> { MutanterTags.PhysicalAttack }, secureActionPreferences: secureActionPreferences, canBeCaptured: false, canBekilled: true);


            // 添加技能攻击组件
            var skillComponent = prefab.AddOrGet<MutanterSkillComponent>();
            // 近距离切割攻击
            var skills = new List<SkillData>{
                new() {
                    name = "Slash",
                    isPassiveSkill = false,
                    cooldown = 2f,
                    animation = "attack_once_3",
                    lastUseTime = 0f,
                    isFirstUse = true,
                    attackEffectors = new List<AttackEffectorData>{
                        new(){
                            attackEffectorName = "BasicAttackBounsApply",
                            damageType = MutanterTags.PhysicalAttack,
                            damageAmount = Random.Range(5f, 6f)
                        }
                    },
                    triggers = new List<TriggerData> {
                        new() {
                            triggerName = "DistanceTrigger",
                            properties = new Dictionary<string, object> {
                                { "Range", 3 }
                            }
                        }
                    }
                },
                // 远距离手炮攻击
                new() {
                    name = "HandCannon",
                    isPassiveSkill = false,
                    cooldown = 3f,
                    animation = "attack_once",
                    lastUseTime = 0f,
                    isFirstUse = true,
                    attackEffectors = new List<AttackEffectorData>{
                        new(){
                            attackEffectorName = "BasicAttackBounsApply",
                            damageType = MutanterTags.PhysicalAttack,
                            damageAmount = Random.Range(14f, 17f)
                        }
                    },
                    triggers = new List<TriggerData> {
                        new() {
                            triggerName = "DistanceTrigger",
                            properties = new Dictionary<string, object> {
                                { "Range", 9 }
                            }
                        }
                    }
                },
                // 回旋斩击
                new() {
                    name = "SpinSlash",
                    isPassiveSkill = false,
                    cooldown = 19f,
                    animation = "attack_once_2",
                    lastUseTime = 0f,
                    VFXName = "EyeTrailVFX",
                    //extraAnimationEffectId = typeof(EyeTrailController).Name,
                    isFirstUse = true,
                    attackEffectors = new List<AttackEffectorData>{
                        new(){
                            attackEffectorName = "BasicAttackBounsApply",
                            damageType = MutanterTags.PhysicalAttack,
                            damageAmount = Random.Range(25f, 30f)
                        }
                    },
                    triggers = new List<TriggerData> {
                        new() {
                            triggerName = "DistanceTrigger",
                            properties = new Dictionary<string, object> {
                                { "Range", 3 }
                            }
                        }
                    }
                },
                // 激光攻击
                new() {
                    name = "Laser",
                    isPassiveSkill = false,
                    cooldown = 45f,
                    animation = "attack_once_4",
                    lastUseTime = 0f,
                    VFXName = "LaserBeamVFX",
                    //extraAnimationEffectId = typeof(LaserBeamController).Name, // 使用激光束效果
                    isFirstUse = true,
                    attackEffectors = new List<AttackEffectorData>{
                        new(){
                            attackEffectorName = "BasicAttackBounsApply",
                            damageType = MutanterTags.PhysicalAttack,
                            damageAmount = Random.Range(70f, 100f)
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
                //死亡攻击
                new() {
                    name = "DeathAttack",
                    isPassiveSkill = true,
                    cooldown = 0f,
                    animation = "death",
                    lastUseTime = 0f,
                    VFXName = "FixerRedDeathDamageVFX",
                    //extraAnimationEffectId = typeof(FixerRedDeathDamageController).Name,
                    isFirstUse = true,
                    attackEffectors = new List<AttackEffectorData>{
                        new(){
                            attackEffectorName = "BasicAttackBounsApply",
                            damageType = MutanterTags.PhysicalAttack,
                            damageAmount = Random.Range(70f, 100f)
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

            prefab.GetComponent<KPrefabID>().prefabSpawnFn += delegate (GameObject inst)
            {
                var animController = inst.GetComponent<KBatchedAnimController>();
                animController.SetSymbolVisiblity("snapto_gun_base", is_visible: false);
                animController.SetSymbolVisiblity("snapto_gun_end", is_visible: false);
                animController.SetSymbolVisiblity("snapto_eye", is_visible: false);
            };
            //禁用基础攻击能力
            prefab.AddOrGet<MutanterCombatManager>().AbilityOfBasicAttaction = false;
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