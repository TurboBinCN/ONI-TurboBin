using MutantContainmentProject.MutanterComponent;
using TBB.He.TbbLib.Debuger;

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
                    RoomConstraints.MAXIMUM_SIZE_64
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
