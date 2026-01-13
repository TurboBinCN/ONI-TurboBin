using STRINGS;
using TUNING;
using UnityEngine;

namespace MutantFarmLab.mutantplants
{
    public class RadiationResistSeedConfig : IEntityConfig
    {
        public const string ID = "RADSEED";
        public static string Name = UI.FormatAsLink(STRINGS.ELEMENT.RADSEED.NAME, ID.ToUpper());
        public static string Description = STRINGS.ELEMENT.RADSEED.DESC;

        public GameObject CreatePrefab()
        {
            GameObject looseEntity = EntityTemplates.CreateLooseEntity(
                ID,
                Name,
                Description,
                1f,
                false,
                Assets.GetAnim("radiation_seed_kanim"),
                "object",
                Grid.SceneLayer.Front,
                EntityTemplates.CollisionShape.CIRCLE,
                0.25f, 0.25f,
                true,
                0,
                SimHashes.Carbon,
                null
            );

            EdiblesManager.FoodInfo foodInfo = new EdiblesManager.FoodInfo(
                ID,
                500000f,
                FOOD.FOOD_QUALITY_GOOD,
                FOOD.DEFAULT_PRESERVE_TEMPERATURE,
                FOOD.DEFAULT_ROT_TEMPERATURE,
                float.PositiveInfinity,
                false,
                DlcManager.AVAILABLE_ALL_VERSIONS,
                null
            );

            foodInfo.AddEffects(new System.Collections.Generic.List<string>
            {
                FoodEffectRegister.RAD_CLEAR_ID,
                FoodEffectRegister.RAD_IMMUNE_ID
            });

            GameObject foodEntity = EntityTemplates.ExtendEntityToFood(looseEntity, foodInfo);

            foodEntity.AddOrGet<RadSeedEatWorkable>();

            return foodEntity;
        }

        public string[] GetDlcIds() => DlcManager.AVAILABLE_ALL_VERSIONS;
        public void OnPrefabInit(GameObject inst) { }
        public void OnSpawn(GameObject inst) { }
    }
}