using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    public class BaseItemsMutanter
    {
        // 创建物品类型畸变体的基础游戏对象
        public static GameObject CreateItemsMutanter(string id, string name, string description, float mass, string anim_file, string initial_anim, Grid.SceneLayer scene_layer, EntityTemplates.CollisionShape collision_shape, float width, float height, bool is_pickupable = true)
        {
            // 创建松散实体
            GameObject prefab = EntityTemplates.CreateLooseEntity(
                id,
                name,
                description,
                mass,
                is_pickupable,
                Assets.GetAnim(anim_file),
                initial_anim,
                scene_layer,
                collision_shape,
                width,
                height,
                is_pickupable
            );

            // 添加畸变体标签
            prefab.AddOrGet<KPrefabID>().AddTag(MutanterTags.Mutanter);
            prefab.AddOrGet<KPrefabID>().AddTag(GameTags.IndustrialProduct);
            // 添加耐久度组件
            prefab.AddOrGet<Durability>();

            return prefab;
        }

        // 添加产出物到物品类型畸变体
        public static void AddProductToItemsMutanter(GameObject template, Tag productId, float baseAmount, float successRateMultiplier)
        {
            MutanterProductComponent productComponent = template.GetComponent<MutanterProductComponent>();
            TbbDebuger.LogDebug($"productComponent[{productComponent}]");
            if (productComponent != null)
            {
                // 获取畸变体ID
                string mutanterId = template.GetComponent<KPrefabID>().PrefabID().Name;

                // 添加到静态数据库
                MutanterProductComponent.AddProductToDatabase(mutanterId, new MutanterProductComponent.Product(productId, baseAmount, successRateMultiplier));

                // 同时添加到当前实例（用于预览）
                productComponent.AddProduct(new MutanterProductComponent.Product(productId, baseAmount, successRateMultiplier));
            }
        }

        // 重载方法，支持 string 类型的产品 ID
        public static void AddProductToItemsMutanter(GameObject template, string productId, float baseAmount, float successRateMultiplier)
        {
            AddProductToItemsMutanter(template, new Tag(productId), baseAmount, successRateMultiplier);
        }

        // 扩展物品类型畸变体的功能
        public static void ExtendItemsMutanter(GameObject prefab, MutanterDangerLevel dangerLevel)
        {
            // 可以根据危险等级添加不同的组件
            switch (dangerLevel)
            {
                case MutanterDangerLevel.Safe:
                case MutanterDangerLevel.Euclid:
                    // 低危物品，添加基础组件
                    break;
                case MutanterDangerLevel.Keter:
                case MutanterDangerLevel.Thaumiel:
                    // 中高危物品，添加更多组件
                    break;
                case MutanterDangerLevel.Neutralized:
                    // 灾难级物品，添加所有可能的组件
                    break;
            }

            // 可以在这里添加更多扩展功能
        }
    }
}