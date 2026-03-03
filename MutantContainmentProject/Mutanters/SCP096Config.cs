using HarmonyLib;
using MutantContainmentProject.MutanterComponent;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MutantContainmentProject.Mutanters
{
    public class SCP096Config : IEntityConfig, IHasDlcRestrictions
    {
        public static string ID = "MUTANTER_SCP096";
        public static readonly string TRAIT_ID = "MutanterSCP096Trait";
        //public static readonly string KANIM_NAME = "chameleo_kanim";
        public static readonly string KANIM_NAME = "SCP096_kanim";
        public static readonly string KANIM_BUILD_NAME = "chameleo_build_kanim";
        public static readonly string KANIM_EMOTES_NAME = "chameleo_emotes_kanim";


        public GameObject CreatePrefab()
        {
            string name = STRINGS.ENTITY.MUTANTER.MUTANTER_SCP096.NAME;
            string desc = STRINGS.ENTITY.MUTANTER.MUTANTER_SCP096.DESCRIPTION;

            GameObject prefab = BaseMutanter.BaseGameObject(ID, name, desc,1,4, KANIM_NAME, KANIM_NAME, KANIM_EMOTES_NAME, null, 233.15f, 293.15f, 173.15f, 373.15f);

            BaseMutanter.ExtendMutanterMove(prefab, "WalkerNavGrid1x4");

            BaseMutanter.ExtendTraitsToBaseMutanter(prefab, TRAIT_ID, name, 25);

            BaseMutanter.ExtendToBaseMutanter(prefab, MutanterDangerLevel.Euclid, faction: FactionManager.FactionID.Pest, attackTags: new List<Tag> { MutanterTags.PhysicalAttack });
            prefab.AddOrGetDef<MutanterChaseMonitor.Def>();

            // 添加产出物
            BaseMutanter.AddProductToMutanter(prefab, SimHashes.Gold.CreateTag(), 1000f, 0.8f);
            BaseMutanter.AddProductToMutanter(prefab, SimHashes.Diamond.CreateTag(), 1000f, 0.4f);

            return prefab;
        }

        public string[] GetRequiredDlcIds() => DlcManager.AVAILABLE_ALL_VERSIONS;
        public string[] GetForbiddenDlcIds() => null;
        public string[] GetAnyRequiredDlcIds() => null;
        public string[] GetDlcIds() => DlcManager.AVAILABLE_ALL_VERSIONS;

        public void OnPrefabInit(GameObject inst) { }

        public void OnSpawn(GameObject inst) { }
    }
    [HarmonyPatch(typeof(GameNavGrids), MethodType.Constructor, new Type[] { typeof(Pathfinding) })]
    public static class GameNavGrids_ctor_Patch
    {
        public static void Postfix(GameNavGrids __instance, Pathfinding pathfinding)
        {
            NavGrid WalkerGrid1x4 = CreateHighWalkerNavigation(__instance, pathfinding, "WalkerNavGrid1x4", new CellOffset[4]
            {
                new CellOffset(0, 0),
                new CellOffset(0, 1),
                new CellOffset(0, 2),
                new CellOffset(0, 3)
            });
        }
        private static NavGrid CreateHighWalkerNavigation(
            GameNavGrids __instance,
            Pathfinding pathfinding,
            string id,
            CellOffset[] bounding_offsets)
        {
            NavGrid.Transition[] originalTransitions = new NavGrid.Transition[]
            {
                new NavGrid.Transition(NavType.Floor, NavType.Floor, 1, 0, NavAxis.NA, true, true, true, 1, "", new CellOffset[0], new CellOffset[0], new NavOffset[0], new NavOffset[0], true),
                new NavGrid.Transition(NavType.Floor, NavType.Floor, 1, 1, NavAxis.NA, false, false, true, 1, "", new CellOffset[1]
                {
                    new CellOffset(0, 1)
                }, new CellOffset[0], new NavOffset[0], new NavOffset[0], true),
                new NavGrid.Transition(NavType.Floor, NavType.Floor, 2, 0, NavAxis.NA, false, false, true, 1, "", new CellOffset[2]
                {
                    new CellOffset(1, 0),
                    new CellOffset(1, -1)
                }, new CellOffset[0], new NavOffset[0], new NavOffset[0], true),
                new NavGrid.Transition(NavType.Floor, NavType.Floor, 1, -4, NavAxis.NA, false, false, true, 1, "", new CellOffset[2]
                {
                    new CellOffset(1, 0),
                    new CellOffset(1, -1)
                }, new CellOffset[0], new NavOffset[0], new NavOffset[0], true),
                new NavGrid.Transition(NavType.Floor, NavType.Floor, 1, 2, NavAxis.NA, false, false, true, 1, "", new CellOffset[2]
                {
                    new CellOffset(0, 1),
                    new CellOffset(0, 2)
                }, new CellOffset[0], new NavOffset[0], new NavOffset[0], true)
            };
            
            // 使用正确的 Traverse 语法调用私有方法
            NavGrid.Transition[] transitions = Traverse.Create(__instance)
                .Method("MirrorTransitions", originalTransitions)
                .GetValue<NavGrid.Transition[]>();
            NavGrid.NavTypeData[] nav_type_data = new NavGrid.NavTypeData[1]
            {
                new NavGrid.NavTypeData()
                {
                    navType = NavType.Floor,
                    idleAnim = (HashedString) "idle_loop"
                }
            };
            NavGrid nav_grid = new NavGrid(id, transitions, nav_type_data, bounding_offsets, new NavTableValidator[1]
            {
                (NavTableValidator) new GameNavGrids.FloorValidator(false)
            }, 2, 3, transitions.Length);
            pathfinding.AddNavGrid(nav_grid);
            return nav_grid;
        }
    }
}
