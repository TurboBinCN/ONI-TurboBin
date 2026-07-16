# NavGrid 分析与 1x4 高小动物创建方案

## 1. NavGrid 系统分析

### 1.1 NavGrid 结构
NavGrid 是 ONI 中控制生物移动的核心导航系统，主要由以下组件组成：

- **id**：导航网格的唯一标识符
- **transitions**：定义生物在不同导航类型间的移动规则
- **nav_type_data**：定义不同导航类型的行为数据
- **bounding_offsets**：定义生物的边界偏移量，决定生物大小
- **validators**：验证导航网格的有效性
- **update_range_x/y**：更新范围
- **max_links_per_cell**：每个单元格的最大链接数

### 1.2 现有导航网格
游戏中已存在多种导航网格，包括：
- `WalkerGrid1x1`：1x1 大小的步行生物
- `WalkerGrid1x2`：1x2 大小的步行生物
- `WalkerGrid2x2`：2x2 大小的步行生物
- 以及其他类型如飞行、游泳、挖掘等导航网格

## 2. 1x4 高小动物创建分析

### 2.1 可行性分析
基于现有代码结构，创建 1x4 高的小动物是完全可行的。主要需要：

1. **定义新的导航网格**：创建一个类似 `WalkerGrid1x2` 的新导航网格
2. **设置正确的边界偏移**：使用 4 个 CellOffset 来定义 1x4 的大小
3. **调整移动规则**：确保生物能正确在不同高度间移动
4. **在生物定义中引用**：将新导航网格分配给目标生物

### 2.2 实现方案

#### 2.2.1 在 GameNavGrids.cs 中添加新导航网格

```csharp
// 在 GameNavGrids 类中添加新的导航网格字段
public NavGrid WalkerGrid1x4;

// 在构造函数中初始化
this.WalkerGrid1x4 = this.CreateWalkerNavigation(pathfinding, "WalkerNavGrid1x4", new CellOffset[4]
{
    new CellOffset(0, 0),
    new CellOffset(0, 1),
    new CellOffset(0, 2),
    new CellOffset(0, 3)
});
```

#### 2.2.2 调整移动规则

现有的 `CreateWalkerNavigation` 方法已经包含了处理垂直移动的规则，特别是：
- 1, 1：向上移动 1 格
- 1, -1：向下移动 1 格
- 1, 2：向上移动 2 格
- 1, -2：向下移动 2 格

对于 1x4 高的生物，这些规则基本足够，但可能需要添加更多的垂直移动选项，例如：

```csharp
// 在 CreateWalkerNavigation 方法中添加更多垂直移动规则
new NavGrid.Transition(NavType.Floor, NavType.Floor, 1, 3, NavAxis.NA, false, false, true, 1, "", new CellOffset[3]
{
    new CellOffset(0, 1),
    new CellOffset(0, 2),
    new CellOffset(0, 3)
}, new CellOffset[0], new NavOffset[0], new NavOffset[0], true),
new NavGrid.Transition(NavType.Floor, NavType.Floor, 1, -3, NavAxis.NA, false, false, true, 1, "", new CellOffset[3]
{
    new CellOffset(1, 0),
    new CellOffset(1, -1),
    new CellOffset(1, -2)
}, new CellOffset[0], new NavOffset[0], new NavOffset[0], true)
```

#### 2.2.3 调整更新范围

对于更高的生物，可能需要增加 `update_range_y` 参数，确保导航网格能正确更新：

```csharp
// 在创建 NavGrid 时调整 update_range_y
NavGrid nav_grid = new NavGrid(id, transitions, nav_type_data, bounding_offsets, new NavTableValidator[1]
{
    (NavTableValidator) new GameNavGrids.FloorValidator(false)
}, 2, 5, transitions.Length); // 将 update_range_y 从 3 增加到 5
```

## 3. 实现步骤

1. **修改 GameNavGrids.cs**：
   - 添加 `WalkerGrid1x4` 字段
   - 在构造函数中初始化
   - 确保移动规则支持 1x4 高的生物

2. **在生物定义中引用**：
   - 在目标生物的定义中，将其导航网格设置为 `WalkerNavGrid1x4`

3. **测试和调整**：
   - 测试生物的移动行为
   - 调整移动规则和更新范围以获得最佳效果

## 4. 注意事项

- **碰撞检测**：确保新生物能正确与环境交互，特别是在狭窄空间中
- **动画适配**：可能需要为新生物创建或调整动画，以适应其高度
- **性能影响**：更高的生物可能会增加导航计算的复杂度，需要监控性能
- **路径查找**：确保路径查找算法能正确处理高生物的移动需求

## 5. 结论

基于现有的 NavGrid 系统，创建 1x4 高的小动物是完全可行的。通过适当的配置和调整，可以实现一个能在游戏世界中正常移动的高生物。这种生物可以为游戏增加新的玩法和策略元素。