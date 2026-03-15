using MutantContainmentProject.Buildings;
using MutantContainmentProject.MutanterComponent;
using System.Collections.Generic;
using UnityEngine;

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

            // 添加SCP-939特有的组件
            prefab.AddOrGet<SCP939Controller>();

            // 添加动画调试组件
            //prefab.AddOrGet<MutanterAnimationDebugger>();

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