using Klei.AI;
using MutantContainmentProject.MutanterComponent;
using TbbLib.UI;
using UnityEngine;

namespace MutantContainmentProject.Mutanters
{
    public class SCP173Config : IEntityConfig, IHasDlcRestrictions
    {
        public static string ID = "MUTANTER_SCP173";
        public static readonly string TRAIT_ID = "MutanterSCP173Trait";

        private static string[] traits = new string[] { TRAIT_ID, "Regeneration" };
        public GameObject CreatePrefab()
        {
            string name = STRINGS.ENTITY.MUTANTER.MUTANTER_SCP173.NAME;
            string desc = STRINGS.ENTITY.MUTANTER.MUTANTER_SCP173.DESCRIPTION;

            GameObject prefab = BaseMutanter.BaseGameObject(ID, name, desc, "chameleo_kanim", null, 233.15f, 293.15f, 173.15f, 373.15f);

            BaseMutanter.ExtendMutanterToDangerLevel(prefab, MutanterDangerLevel.TETH);
            BaseMutanter.ExtendMutanterMove(prefab, "DreckoNavGrid");

            Trait trait = Db.Get().CreateTrait(TRAIT_ID, name, name, null, false, null, true, true);
            trait.Add(new AttributeModifier(Db.Get().Amounts.HitPoints.maxAttribute.Id, 25f, name, false, false, true));
            trait.Add(new AttributeModifier(Db.Get().Amounts.Age.maxAttribute.Id, 9999, name, false, false, true));

            BaseMutanter.ExtendTraitsToBaseMutanter(prefab,traits);
            BaseMutanter.ExtendThreatToBaseMutanter(prefab);

            return prefab;
        }


        public string[] GetRequiredDlcIds() => DlcManager.AVAILABLE_ALL_VERSIONS;
        public string[] GetForbiddenDlcIds() => null;
        public string[] GetAnyRequiredDlcIds() => null;
        public string[] GetDlcIds() => DlcManager.AVAILABLE_ALL_VERSIONS;

        public void OnPrefabInit(GameObject inst)
        {
        }

        public void OnSpawn(GameObject inst)
        {
        }
    }
}
