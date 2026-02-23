using MutantContainmentProject.MutanterComponent;
using rail;
using System;
using System.Collections.Generic;
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
                    HashSet<int> boundaryCells = new HashSet<int>();
                    
                    // 遍历房间的所有内部单元格，找到其相邻的边界单元格
                    foreach (int cell in room.cavity.cells)
                    {
                        if (!Grid.IsValidCell(cell))
                            continue;
                        
                        // 检查四个方向的相邻单元格
                        int[] adjacentCells = new int[]
                        {
                            Grid.XYToCell(Grid.CellToXY(cell).x - 1, Grid.CellToXY(cell).y), // 左
                            Grid.XYToCell(Grid.CellToXY(cell).x + 1, Grid.CellToXY(cell).y), // 右
                            Grid.XYToCell(Grid.CellToXY(cell).x, Grid.CellToXY(cell).y - 1), // 下
                            Grid.XYToCell(Grid.CellToXY(cell).x, Grid.CellToXY(cell).y + 1)  // 上
                        };
                        
                        // 添加相邻的边界单元格到集合中
                        foreach (int adjCell in adjacentCells)
                        {
                            if (Grid.IsValidCell(adjCell) && IsCavityBoundary(adjCell))
                            {
                                boundaryCells.Add(adjCell);
                            }
                        }
                    }
                    
                    // 检查所有边界单元格是否是收容砖或门
                    foreach (int boundaryCell in boundaryCells)
                    {
                        if (!flag)
                            break;
                        
                        // 检查该单元格是否有门
                        if (Grid.HasDoor[boundaryCell])
                            continue; // 门是有效的
                        
                        // 检查该单元格是否有收容砖（只检查FoundationTile层，因为ContainmentTileConfig是FoundationTile）
                        bool hasContainmentTile = false;
                        
                        // 检查地面层的对象
                        GameObject foundationObj = Grid.Objects[boundaryCell, (int)ObjectLayer.FoundationTile];
                        TbbDebuger.LogDebug($"Checking prefabID: {foundationObj?.GetComponent<KPrefabID>()?.name}");
                        if (foundationObj != null && foundationObj.GetComponent<KPrefabID>()?.HasTag(MutanterTags.MutanterBuildings) == true)
                        {
                            hasContainmentTile = true;
                        }
                        
                        if (!hasContainmentTile)
                        {
                            flag = false;
                        }
                    }
                    
                    return flag;
                },
                name: STRINGS.BUILDINGS.PREFABS.CONTAINMENTTILE.NAME,
                description: "收容室的所有外墙必须是收容砖或门"
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
                    containmentWallConstraint
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
                colorName: "roomBathroom", // TODO: 定义颜色或使用现有颜色名，例如 "roomNone"
                icon: "ui_room_hospital" // TODO: 提供图标路径，例如 "ui_room_food"
            );

            if (!Db.Get().RoomTypeCategories.Exists(CATEGORY_ID))
                Db.Get().RoomTypeCategories.Add(containmentCategory);
        }
    }
}
