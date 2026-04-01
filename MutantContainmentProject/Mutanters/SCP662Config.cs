using MutantContainmentProject.MutanterComponent;
using UnityEngine;

namespace MutantContainmentProject.Mutanters
{
    public class SCP662Config : IEntityConfig
    {
        public static string ID = "MUTANTER_SCP662";
        public static readonly string KANIM_NAME = "SCP662_kanim";

        public GameObject CreatePrefab()
        {
            string name = STRINGS.ENTITY.MUTANTER.MUTANTER_SCP662.NAME;
            string desc = STRINGS.ENTITY.MUTANTER.MUTANTER_SCP662.DESCRIPTION;

            // 创建物品类型畸变体
            GameObject prefab = BaseItemsMutanter.CreateItemsMutanter(
                ID,
                name,
                desc,
                1f, // 质量
                KANIM_NAME,
                "object", // 初始动画
                Grid.SceneLayer.Front, // 场景层
                EntityTemplates.CollisionShape.RECTANGLE, // 碰撞形状
                0.8f, // 宽度
                0.6f, // 高度
                true // 可拾取
            );

            // 扩展物品类型畸变体功能
            BaseItemsMutanter.ExtendItemsMutanter(prefab, MutanterDangerLevel.Safe);

            // 添加可在基座上展示的标签
            prefab.AddOrGet<KPrefabID>().AddTag(GameTags.PedestalDisplayable);

            prefab.AddOrGet<SCP662BellController>();

            // 配置攻击策略
            var strategyManager = prefab.AddOrGet<AttackStrategyManager>();
            strategyManager.SetStrategyEnabled(AttackStrategyManager.StrategyType.BasicAttack, false);
            strategyManager.SetStrategyEnabled(AttackStrategyManager.StrategyType.SkillAttack, false);

            return prefab;
        }

        public void OnPrefabInit(GameObject inst)
        {
        }

        public void OnSpawn(GameObject inst)
        {
        }

        public string[] GetDlcIds() => DlcManager.AVAILABLE_ALL_VERSIONS;
    }
}