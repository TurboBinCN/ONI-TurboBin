using MutantContainmentProject.Buildings;
using MutantContainmentProject.MutanterComponent;
using MutantContainmentProject.MutanterComponent.VFXController;
using System.Collections.Generic;
using UnityEngine;
using static MutantContainmentProject.MutanterComponent.MutanterSkillComponent;

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

            BaseMutanter.ExtendToBaseMutanter(prefab, MutanterDangerLevel.Euclid, faction: FactionManager.FactionID.Pest, attackTags: new List<Tag> { MutanterTags.ErosionAttack }, secureActionPreferences: secureActionPreferences, canBeCaptured: false, canBekilled: true);

            // 添加产出物
            BaseMutanter.AddProductToMutanter(prefab, "Meat", 500f, 0.8f);
            BaseMutanter.AddProductToMutanter(prefab, SimHashes.SlimeMold.CreateTag(), 300f, 0.5f);

            // 添加技能攻击组件
            var skillComponent = prefab.AddOrGet<MutanterSkillComponent>();
            var skills = new List<SkillData>{
                new() {
                    name = "BasicAttack",
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
                //死亡攻击
                new() {
                    name = "DeathAttack",
                    isPassiveSkill = true,
                    cooldown = 0f,
                    animation = "death",
                    lastUseTime = 0f,
                    isFirstUse = true,
                    VFXName = "CosmicBugDeathDamangeVFX",
                    //extraAnimationEffectId = typeof(CosmicBugDeathDamangeVFXController).Name,
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
