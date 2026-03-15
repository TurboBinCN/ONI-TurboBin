using MutantContainmentProject.Buildings;
using MutantContainmentProject.MutanterComponent;
using System.Collections.Generic;
using UnityEngine;

namespace MutantContainmentProject.Mutanters
{
    public class SCP049Config : IEntityConfig, IHasDlcRestrictions
    {
        public static string ID = "MUTANTER_SCP049";
        public static readonly string TRAIT_ID = "MutanterSCP049Trait";
        public static readonly string KANIM_NAME = "SCP049_kanim";
        public static readonly string KANIM_EMOTES_NAME = "chameleo_emotes_kanim";


        public GameObject CreatePrefab()
        {
            string name = STRINGS.ENTITY.MUTANTER.MUTANTER_SCP049.NAME;
            string desc = STRINGS.ENTITY.MUTANTER.MUTANTER_SCP049.DESCRIPTION;

            GameObject prefab = BaseMutanter.BaseGameObject(ID, name, desc, 1, 2, KANIM_NAME, KANIM_NAME, KANIM_EMOTES_NAME, null, 233.15f, 293.15f, 173.15f, 373.15f);

            BaseMutanter.ExtendMutanterMove(prefab, "WalkerNavGrid1x2");

            BaseMutanter.ExtendTraitsToBaseMutanter(prefab, TRAIT_ID, name, 50);
            // 安全措施偏好值
            Dictionary<SecureAction, float[]> secureActionPreferences = new Dictionary<SecureAction, float[]>
            {
                // 本能操作（对应勇气技能）
                [SecureAction.Instinct] = new float[] { 20f, 30f, 30f },
                // 洞察操作（对应防御技能）
                [SecureAction.Reconnaissance] = new float[] { 50f, 50f, 50f },
                // 自律操作（对应沟通技能）
                [SecureAction.Communicate] = new float[] { 70f, 70f, 70f },
                // 压迫操作（对应正义技能）
                [SecureAction.Intimidation] = new float[] { 40f, 50f, 60f }
            };
            BaseMutanter.ExtendToBaseMutanter(prefab, MutanterDangerLevel.Euclid, faction: FactionManager.FactionID.Pest, attackTags: new List<Tag> { MutanterTags.PhysicalAttack }, secureActionPreferences: secureActionPreferences);

            // 添加SCP-049特有的组件
            prefab.AddOrGet<SCP049Controller>();

            // 添加产出物
            BaseMutanter.AddProductToMutanter(prefab, IntermediateBoosterConfig.ID, 1000f, 0.8f);//免疫增补剂
            BaseMutanter.AddProductToMutanter(prefab, BasicBoosterConfig.ID, 20f, 0.6f);//维生素咀嚼胶囊
            BaseMutanter.AddProductToMutanter(prefab, AntihistamineConfig.ID, 1000f, 0.8f);//抗敏药
            BaseMutanter.AddProductToMutanter(prefab, BasicCureConfig.ID, 20f, 0.6f);// 治疗药片
            BaseMutanter.AddProductToMutanter(prefab, IntermediateCureConfig.ID, 20f, 0.6f);//医疗包
            BaseMutanter.AddProductToMutanter(prefab, AdvancedCureConfig.ID, 1000f, 0.8f);//血清瓶
            BaseMutanter.AddProductToMutanter(prefab, BasicCureConfig.ID, 20f, 0.6f);// 治疗药片

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
