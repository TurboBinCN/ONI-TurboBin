using MutantContainmentProject.Buildings;
using MutantContainmentProject.MutanterComponent;
using System.Collections.Generic;
using UnityEngine;

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
            skillComponent.RegisterEffectComponents<LaserBeamController, LaserBeamEffect>();
            skillComponent.RegisterEffectComponents<EyeTrailController, EyeTrailEffect>();
            skillComponent.RegisterEffectComponents<FixerRedDeathDamageController, FixerRedDeathDamageEffect>();
            // 近距离切割攻击
            var skills = new List<MutanterSkillComponent.SkillData>{
                new() {
                    name = "Slash",
                    damageType = MutanterTags.PhysicalAttack,
                    isPassiveSkill = false,
                    damage = Random.Range(5f, 6f),
                    range = 3,
                    cooldown = 2f,
                    animation = "attack_once_3",
                    lastUseTime = 0f,
                    isFirstUse = true

                },
                // 远距离手炮攻击
                new() {
                    name = "HandCannon",
                    damageType = MutanterTags.PhysicalAttack,
                    isPassiveSkill = false,
                    damage = Random.Range(14f, 17f),
                    range = 9,
                    cooldown = 3f,
                    animation = "attack_once",
                    lastUseTime = 0f,
                    isFirstUse = true
                },
                // 回旋斩击
                new() {
                    name = "SpinSlash",
                    damageType = MutanterTags.PhysicalAttack,
                    isPassiveSkill = false,
                    damage = Random.Range(25f, 30f),
                    range = 3,
                    cooldown = 19f,
                    animation = "attack_once_2",
                    lastUseTime = 0f,
                    extraAnimationEffectId = typeof(EyeTrailController).Name,
                    isFirstUse = true
                },
                // 激光攻击
                new() {
                    name = "Laser",
                    damageType = MutanterTags.PhysicalAttack,
                    isPassiveSkill = false,
                    damage = Random.Range(70f, 100f),
                    range = 15,
                    cooldown = 45f,
                    animation = "attack_once_4",
                    lastUseTime = 0f,
                    extraAnimationEffectId = typeof(LaserBeamController).Name, // 使用激光束效果
                    isFirstUse = true
                },
                //死亡攻击
                new() {
                    name = "DeathAttack",
                    damageType = MutanterTags.PhysicalAttack,
                    isPassiveSkill = false,
                    damage = Random.Range(70f, 100f),
                    range = 0,
                    cooldown = 0f,
                    animation = "death",
                    lastUseTime = 0f,
                    extraAnimationEffectId = typeof(FixerRedDeathDamageController).Name,
                    isFirstUse = true
                }
            };

            // 设置技能并添加到数据库
            skillComponent.skills = skills;
            MutanterSkillComponent.AddSkillsToDatabase(ID, skills);

            prefab.AddOrGet<LaserBeamController>();
            prefab.GetComponent<KPrefabID>().prefabSpawnFn += delegate (GameObject inst)
            {
                var animController = inst.GetComponent<KBatchedAnimController>();
                animController.SetSymbolVisiblity("snapto_gun_base", is_visible: false);
                animController.SetSymbolVisiblity("snapto_gun_end", is_visible: false);
                animController.SetSymbolVisiblity("snapto_eye", is_visible: false);
            };

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