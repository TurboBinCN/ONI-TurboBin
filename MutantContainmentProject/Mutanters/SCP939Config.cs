using MutantContainmentProject.Buildings;
using MutantContainmentProject.MutanterComponent;
using System;
using System.Collections.Generic;
using UnityEngine;
using static MutantContainmentProject.MutanterComponent.MutanterSkillComponent;

namespace MutantContainmentProject.Mutanters
{
    public class SCP939Config : IEntityConfig, IHasDlcRestrictions
    {
        public static string ID = "MUTANTER_SCP939";
        public static readonly string TRAIT_ID = "MutanterSCP939Trait";
        public static readonly string KANIM_NAME = "SCP939_kanim";
        public static readonly string KANIM_EMOTES_NAME = "chameleo_emotes_kanim";


        public GameObject CreatePrefab()
        {
            string name = STRINGS.ENTITY.MUTANTER.MUTANTER_SCP939.NAME;
            string desc = STRINGS.ENTITY.MUTANTER.MUTANTER_SCP939.DESCRIPTION;

            GameObject prefab = BaseMutanter.BaseGameObject(ID, name, desc, 2, 2, KANIM_NAME, KANIM_NAME, KANIM_EMOTES_NAME, null, 233.15f, 293.15f, 173.15f, 373.15f);
            KBoxCollider2D kboxCollider2D = prefab.AddOrGet<KBoxCollider2D>();
            kboxCollider2D.offset = (Vector2)new Vector2f(0.0f, kboxCollider2D.offset.y);
            prefab.GetComponent<KBatchedAnimController>().Offset = new Vector3(0.0f, 0.0f, 0.0f);

            BaseMutanter.ExtendMutanterMove(prefab, "WalkerNavGrid2x2");

            BaseMutanter.ExtendTraitsToBaseMutanter(prefab, TRAIT_ID, name, 50);
            // 安全措施偏好值
            Dictionary<SecureAction, float[]> secureActionPreferences = new Dictionary<SecureAction, float[]>
            {
                // 本能操作（对应勇气技能）
                [SecureAction.Instinct] = new float[] { 70f, 70f, 70f },
                // 洞察操作（对应防御技能）
                [SecureAction.Reconnaissance] = new float[] { 50f, 50f, 50f },
                // 自律操作（对应沟通技能）
                [SecureAction.Communicate] = new float[] { 0f, 0f, 0f },
                // 压迫操作（对应正义技能）
                [SecureAction.Intimidation] = new float[] { 40f, 50f, 60f }
            };
            BaseMutanter.ExtendToBaseMutanter(prefab, MutanterDangerLevel.Keter, faction: FactionManager.FactionID.Pest, attackTags: new List<Tag> { MutanterTags.PhysicalAttack, MutanterTags.PsychologicalAttack }, secureActionPreferences: secureActionPreferences);

            // 添加技能攻击组件
            var skillComponent = prefab.AddOrGet<MutanterSkillComponent>();
            var skills = new List<SkillData>{new() {
                    name = "BasicErosionAttack",
                    isPassiveSkill = false,
                    cooldown = 2f,
                    animation = "attack_once",
                    lastUseTime = 0f,
                    isFirstUse = true,
                    attackEffectors = new List<AttackEffectorData>{
                        new(){
                            attackEffectorName = "BasicAttackBounsApply",
                            damageType = MutanterTags.ErosionAttack,
                            damageAmount = 3f
                        }
                    },
                    triggers = new List<TriggerData> {
                        new() {
                            triggerName = "DistanceTrigger",
                            properties = new Dictionary<string, object> {
                                { "Range", 2 }
                            }
                        }
                    }
                },
                new() {
                    name = "ReleaseAerosol",
                    isPassiveSkill = true,
                    cooldown = 100f,
                    animation = "attack_aerosol",
                    animationDuration = 10f,
                    lastUseTime = 0f,
                    isFirstUse = true,
                    VFXName = "AerosolVFX",
                    attackEffectors = new List<AttackEffectorData>{
                        new(){
                            attackEffectorName = "SpecialAttact",
                            kMonoBehaviours = new List<Type>(){ typeof(SCP939Amnesia) },
                        }
                    },
                    triggers = new List<TriggerData> {
                        new() {
                            triggerName = "CyclicCheckTrigger",
                            properties = new Dictionary<string, object> {},
                            conditionCallbackMethods = new Dictionary<string, Func<GameObject, bool>> {
                                { "ReleaseAerosolCondition", (_) => true}
                            }
                        }
                    }
                }
            };
            skillComponent.AddSkillsToDb(skills);

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