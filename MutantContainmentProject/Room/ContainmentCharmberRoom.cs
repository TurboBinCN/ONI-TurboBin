using MutantContainmentProject.MutanterComponent;
using TBB.He.TbbLib.Debuger;
using UnityEngine;


namespace MutantContainmentProject.Room
{
    public static class ContainmentCharmberRoom
    {
        public static readonly string CATEGORY_ID = "MutanterContainmentCategory";
        public static readonly string ROOMTYPE_ID = "MutanterContainmentChamber";
        public static RoomType ContainmentChamber;
        public static void Register()
        {
            RegisterRoomCategory();
            RegisterRoomType();
        }
        private static void RegisterRoomType()
        {

            var primaryBuildingConstraint = new RoomConstraints.Constraint(
                building_criteria: (KPrefabID building) => building.HasTag(MutanterTags.MutanterBuildings),
                room_criteria: null, // 不需要房间级别的判断，由 building_criteria 决定
                times_required: 1, // 需要至少1个
                name: STRINGS.ROOMS.CRITERIA.CONTAINMENTMONITOR.NAME,
                description: STRINGS.ROOMS.CRITERIA.CONTAINMENTMONITOR.DESCRIPTION
            );

            // --- 3. 定义房间效果列表 ---
            // TODO: 创建并添加限制畸变体行为的 Effect ID
            //List<string> effects = new List<string>();
            //effects.Add("TODO_ContainmentModifier"); // 占位符 Effect ID

            // --- 4. 获取分类 ---
            var containmentCategory = Db.Get().RoomTypeCategories.Get(CATEGORY_ID);
            if (containmentCategory == null)
            {
                TbbDebuger.LogWarning($"[ContainmentCharmberRoom] 没有找到 Room 分类 '{CATEGORY_ID}'.不能创建新的RoomType.");
                return;
            }

            // --- 添加外墙约束 --- 检查所有外墙是否为收容砖或门
            var containmentWallConstraint = new RoomConstraints.Constraint(
                building_criteria: null,
                room_criteria: (global::Room room) =>
                {
                    bool flag = true;
                    var cavity = room.cavity;

                    // 遍历房间内的所有单元格，检查每个单元格的相邻单元格是否为边界
                    for (int y = cavity.minY; flag && y <= cavity.maxY; y++)
                    {
                        for (int x = cavity.minX; flag && x <= cavity.maxX; x++)
                        {
                            int cell = Grid.XYToCell(x, y);
                            // 只检查房间内的单元格
                            if (cavity.cells.Contains(cell))
                            {
                                // 检查四个方向的相邻单元格
                                int[] adjacentCells = new int[]
                                {
                                    Grid.XYToCell(x + 1, y), // 右
                                    Grid.XYToCell(x - 1, y), // 左
                                    Grid.XYToCell(x, y + 1), // 上
                                    Grid.XYToCell(x, y - 1)  // 下
                                };

                                foreach (int adjCell in adjacentCells)
                                {
                                    // 检查相邻单元格是否有效且不在房间内
                                    if (Grid.IsValidCell(adjCell) && !cavity.cells.Contains(adjCell))
                                    {
                                        // 这是一个边界单元格，检查是否为有效边界
                                        if (!CheckBoundaryCell(adjCell))
                                        {
                                            flag = false;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    return flag;

                    bool CheckBoundaryCell(int cell)
                    {
                        // 检查该单元格是否有门
                        if (Grid.HasDoor[cell])
                            return true; // 门是有效的

                        // 检查该单元格是否有收容砖（只检查FoundationTile层，因为ContainmentTileConfig是FoundationTile）
                        GameObject foundationObj = Grid.Objects[cell, (int)ObjectLayer.FoundationTile];
                        return foundationObj != null && foundationObj.GetComponent<KPrefabID>()?.HasTag(MutanterTags.MutanterBuildings) == true;
                    }
                },
                name: STRINGS.ROOMS.CRITERIA.CONTAINMENTMONITOREXTERIOR.NAME,
                description: STRINGS.ROOMS.CRITERIA.CONTAINMENTMONITOREXTERIOR.DESCRIPTION
            );

            // --- 添加畸变体数量约束 --- 检查房间内只能有1只畸变体
            var mutanterCountConstraint = new RoomConstraints.Constraint(
                building_criteria: null,
                room_criteria: (global::Room room) =>
                {
                    int mutanterCount = 0;

                    // 遍历房间内的所有生物
                    foreach (KPrefabID creature in room.creatures)
                    {
                        if (creature != null && creature.HasTag(MutanterTags.Mutanter))
                        {
                            mutanterCount++;
                            if (mutanterCount > 1)
                                break; // 超过1只，直接返回false
                        }
                    }

                    return mutanterCount == 1;
                },
                name: STRINGS.ROOMS.CRITERIA.MUTANTER_COUNT.NAME,
                description: STRINGS.ROOMS.CRITERIA.MUTANTER_COUNT.DESCRIPTION
            );

            // --- 5. 创建房间类型 ---
            ContainmentChamber = new RoomType(
                id: ROOMTYPE_ID, // 唯一的房间类型ID
                name: STRINGS.ROOMS.CATEGORY.MUTANTER_CONTAINER.NAME,
                description: STRINGS.ROOMS.CATEGORY.MUTANTER_CONTAINER.DESCRIPTION,
                tooltip: STRINGS.ROOMS.CATEGORY.MUTANTER_CONTAINER.TOOLTIP,
                effect: "", // Effect 描述会在 GetRoomEffectsString 中处理
                category: containmentCategory,
                primary_constraint: primaryBuildingConstraint,
                additional_constraints: new RoomConstraints.Constraint[]
                {
                    RoomConstraints.MINIMUM_SIZE_12,
                    RoomConstraints.MAXIMUM_SIZE_64,
                    containmentWallConstraint,
                    mutanterCountConstraint
                },
                display_details: new RoomDetails.Detail[]
                    {
                        RoomDetails.SIZE,
                        RoomDetails.BUILDING_COUNT,
                        RoomDetails.CREATURE_COUNT,
                        RoomDetails.PLANT_COUNT
                    },
                priority: 0,
                upgrade_paths: null, // 无升级路径
                single_assignee: false, // 不是单人专用
                priority_building_use: false // 不优先占用建筑
                                             //effects: effects.ToArray() // 应用的效果数组
            );
            // --- 6. 将房间类型注册到数据库 ---
            if (!Db.Get().RoomTypes.Exists(ROOMTYPE_ID))
            {
                Db.Get().RoomTypes.Add(ContainmentChamber);
                TbbDebuger.LogDebug($"Added Room Type: {ContainmentChamber.Id}");
            }
        }

        private static bool IsCavityBoundary(int cell)
        {
            // 参考RoomProber.IsCavityBoundary的实现
            return (Grid.BuildMasks[cell] & (Grid.BuildFlags.Solid | Grid.BuildFlags.Foundation)) != 0 || Grid.HasDoor[cell];
        }

        public static void RegisterRoomCategory()
        {

            // --- 创建房间类型分类 ---
            var containmentCategory = new RoomTypeCategory(
                id: CATEGORY_ID, // 唯一的分类ID
                name: STRINGS.ROOMS.CATEGORY.MUTANTER_CONTAINER.CATE_NAME,
                colorName: "mutanter_containment_room", // TODO: 定义颜色或使用现有颜色名，例如 "roomNone"
                icon: "ui_room_hospital" // TODO: 提供图标路径，例如 "ui_room_food"
            );

            if (!Db.Get().RoomTypeCategories.Exists(CATEGORY_ID))
                Db.Get().RoomTypeCategories.Add(containmentCategory);
        }
    }
}
