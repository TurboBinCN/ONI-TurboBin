using MutantContainmentProject.Buildings;
using System;
using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.Room
{
    public static class DepartmentRoom
    {
        public static readonly string CATEGORY_ID = "ControlDepartmentCategory";
        public static readonly string ROOMTYPE_ID = "ControlDepartmentRoom";
        public static RoomType ControlDepartment;

        public static void Register()
        {
            RegisterControlDepartmentRoomType();
        }

        public static void RegisterControlDepartmentRoomType()
        {
            // --- 1. 注册控制部房间分类 ---
            var controlDepartmentCategory = new RoomTypeCategory(
                id: CATEGORY_ID,
                name: STRINGS.ROOMS.CATEGORY.CONTROL_DEPARTMENT.CATE_NAME,
                colorName: "mutanter_containment_room", // TODO: 定义颜色或使用现有颜色名，例如 "roomNone"
                icon: "ui_room_hospital" // TODO: 提供图标路径，例如 "ui_room_food"
            );

            if (!Db.Get().RoomTypeCategories.Exists(CATEGORY_ID))
                Db.Get().RoomTypeCategories.Add(controlDepartmentCategory);

            // --- 2. 定义控制部房间的主要约束条件 ---
            // 要求房间内有至少一个ControlDepartmentConsole
            var primaryBuildingConstraint = new RoomConstraints.Constraint(
                building_criteria: (KPrefabID building) => building.IsPrefabID(ControlDepartmentConsoleConfig.ID),
                room_criteria: null,
                times_required: 1,
                name: STRINGS.ROOMS.CRITERIA.CONTROL_DEPARTMENT_CONSOLE.NAME,
                description: STRINGS.ROOMS.CRITERIA.CONTROL_DEPARTMENT_CONSOLE.DESCRIPTION
            );

            // --- 3. 定义灯光约束条件 ---
            // 要求房间内有至少一个光源
            var lightingConstraint = new RoomConstraints.Constraint(
                building_criteria: (KPrefabID building) => building.HasTag(GameTags.LightSource),
                room_criteria: null,
                times_required: 1,
                name: STRINGS.ROOMS.CRITERIA.CONTROL_DEPARTMENT_LIGHT.NAME,
                description: STRINGS.ROOMS.CRITERIA.CONTROL_DEPARTMENT_LIGHT.DESCRIPTION
            );

            // --- 4. 获取控制部房间分类 ---
            var controlDepartmentRoomCategory = Db.Get().RoomTypeCategories.Get(CATEGORY_ID);
            if (controlDepartmentRoomCategory == null)
            {
                TbbDebuger.LogWarning($"[ControlDepartmentRoom] 没有找到 Room 分类 '{CATEGORY_ID}'.不能创建新的RoomType.");
                return;
            }

            // --- 5. 创建控制部房间类型 ---
            ControlDepartment = new RoomType(
                id: ROOMTYPE_ID,
                name: STRINGS.ROOMS.CATEGORY.CONTROL_DEPARTMENT.NAME,
                description: STRINGS.ROOMS.CATEGORY.CONTROL_DEPARTMENT.DESCRIPTION,
                tooltip: STRINGS.ROOMS.CATEGORY.CONTROL_DEPARTMENT.TOOLTIP,
                effect: "",
                category: controlDepartmentRoomCategory,
                primary_constraint: primaryBuildingConstraint,
                additional_constraints: new RoomConstraints.Constraint[]
                {
                    RoomConstraints.MINIMUM_SIZE_12,
                    RoomConstraints.MAXIMUM_SIZE_64,
                    lightingConstraint
                },
                display_details: new RoomDetails.Detail[]
                {
                    RoomDetails.SIZE,
                    RoomDetails.BUILDING_COUNT,
                    RoomDetails.CREATURE_COUNT
                },
                priority: 0,
                upgrade_paths: null,
                single_assignee: false,
                priority_building_use: false
            );

            // --- 6. 将控制部房间类型注册到数据库 ---
            if (!Db.Get().RoomTypes.Exists(ROOMTYPE_ID))
            {
                Db.Get().RoomTypes.Add(ControlDepartment);
                TbbDebuger.LogDebug($"Added Room Type: {ControlDepartment.Id}");
            }
        }
    }
}