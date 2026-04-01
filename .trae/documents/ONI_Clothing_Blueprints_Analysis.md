# Oxygen Not Included 服装蓝图实现原理分析

## 核心架构

Oxygen Not Included (ONI) 的服装蓝图系统采用了模块化的设计，主要由以下几个核心组件组成：

### 1. 蓝图管理系统

**Blueprints 类** (Blueprints.cs)：
- 作为整个蓝图系统的入口点和管理器
- 维护两个主要的蓝图集合：`all`（所有蓝图）和 `skinsRelease`（皮肤蓝图）
- 包含多个皮肤蓝图提供者，如 `Blueprints_U51AndBefore`、`Blueprints_DlcPack2` 等
- 通过单例模式提供全局访问点

**BlueprintCollection 类** (BlueprintCollection.cs)：
- 存储不同类型的蓝图数据结构
- 包含多个列表，如 `clothingItems`（服装项目）、`outfits`（服装套装）等
- 提供蓝图添加和后处理功能
- 负责过滤不符合DLC要求的蓝图

### 2. 蓝图提供者系统

**BlueprintProvider 类** (BlueprintProvider.cs)：
- 抽象基类，定义了添加各种蓝图的方法
- 提供 `AddClothing()` 方法用于添加服装蓝图
- 提供 `AddOutfit()` 方法用于添加服装套装
- 包含 `ClothingType` 枚举，定义了所有服装类型

**具体实现类**：
- `Blueprints_Default`：提供默认蓝图
- `Blueprints_U51AndBefore`、`Blueprints_DlcPack2` 等：提供特定版本和DLC的皮肤蓝图

### 3. 服装蓝图数据结构

**ClothingItemInfo 类** (ClothingItemInfo.cs)：
- 表示单个服装项目的信息
- 包含ID、名称、描述、稀有度、动画文件等属性
- 关联到特定的服装类型和套装类型

**ClothingOutfitResource 类** (ClothingOutfitResource.cs)：
- 表示服装套装
- 包含套装ID、名称、包含的服装项目列表等
- 关联到特定的套装类型（普通服装、太空服、喷气服）

## 服装类型体系

ONI 的服装系统包含多种类型，通过 `BlueprintProvider.ClothingType` 枚举定义：

| 服装类型 | 枚举值 | 描述 |
|---------|-------|------|
| DupeTops | 1 | 复制人上衣 |
| DupeBottoms | 2 | 复制人裤子 |
| DupeGloves | 3 | 复制人手套 |
| DupeShoes | 4 | 复制人鞋子 |
| DupeHats | 5 | 复制人帽子 |
| DupeAccessories | 6 | 复制人配件 |
| AtmoSuitHelmet | 7 | 太空服头盔 |
| AtmoSuitBody | 8 | 太空服身体 |
| AtmoSuitGloves | 9 | 太空服手套 |
| AtmoSuitBelt | 10 | 太空服腰带 |
| AtmoSuitShoes | 11 | 太空服鞋子 |
| JetSuitHelmet | 18 | 喷气服头盔 |
| JetSuitBody | 19 | 喷气服身体 |
| JetSuitGloves | 20 | 喷气服手套 |
| JetSuitShoes | 21 | 喷气服鞋子 |

## 套装类型体系

服装套装分为三种主要类型，通过 `BlueprintProvider.OutfitType` 枚举定义：

| 套装类型 | 枚举值 | 描述 |
|---------|-------|------|
| Clothing | 0 | 普通服装套装 |
| AtmoSuit | 2 | 太空服套装 |
| JetSuit | 3 | 喷气服套装 |

## 蓝图初始化流程

1. **系统启动**：游戏启动时，`Blueprints.Get()` 方法被调用
2. **初始化单例**：如果 `Blueprints.instance` 为 null，则创建新实例
3. **添加默认蓝图**：调用 `AddBlueprintsFrom<Blueprints_Default>()` 添加默认蓝图
4. **添加皮肤蓝图**：遍历 `skinsReleaseProviders`，添加各提供者的皮肤蓝图
5. **合并蓝图**：将 `skinsRelease` 中的蓝图添加到 `all` 中
6. **后处理**：对 `skinsRelease` 和 `all` 执行 `PostProcess()`，移除不符合DLC要求的蓝图

## 服装蓝图的添加过程

1. **定义蓝图**：通过继承 `BlueprintProvider` 类并实现 `SetupBlueprints()` 方法
2. **添加服装**：使用 `AddClothing()` 方法添加单个服装项目
3. **添加套装**：使用 `AddOutfit()` 方法添加服装套装
4. **设置属性**：为每个服装项目设置ID、名称、描述、稀有度、动画文件等
5. **DLC限制**：可选择性地设置服装的DLC要求

## 蓝图的使用

1. **获取蓝图**：通过 `Blueprints.Get().all` 获取所有可用蓝图
2. **过滤蓝图**：根据服装类型、套装类型、DLC可用性等条件过滤
3. **应用蓝图**：玩家可以通过游戏内界面选择和应用服装蓝图
4. **视觉表现**：蓝图的动画文件用于在游戏中显示服装的外观

## 代码示例分析

### 添加服装蓝图的示例

```csharp
// 在 BlueprintProvider 子类中添加服装
protected void AddClothing(
    BlueprintProvider.ClothingType clothingType,
    PermitRarity rarity,
    string permitId,
    string animFile)
{
    this.blueprintCollection.clothingItems.Add(new ClothingItemInfo(
        permitId,
        (string) Strings.Get($"STRINGS.BLUEPRINTS.{permitId.ToUpper()}.NAME"),
        (string) Strings.Get($"STRINGS.BLUEPRINTS.{permitId.ToUpper()}.DESC"),
        (PermitCategory) clothingType,
        rarity,
        animFile,
        this.requiredDlcIds,
        this.forbiddenDlcIds
    ));
}
```

### 添加服装套装的示例

```csharp
// 在 BlueprintProvider 子类中添加服装套装
protected void AddOutfit(
    BlueprintProvider.OutfitType outfitType,
    string outfitId,
    string[] permitIdList)
{
    this.blueprintCollection.outfits.Add(new ClothingOutfitResource(
        outfitId,
        permitIdList,
        (string) Strings.Get($"STRINGS.BLUEPRINTS.{outfitId.ToUpper()}.NAME"),
        (ClothingOutfitUtility.OutfitType) outfitType,
        this.requiredDlcIds,
        this.forbiddenDlcIds
    ));
}
```

## 技术特点

1. **模块化设计**：通过抽象基类和具体实现类的分离，实现了蓝图系统的可扩展性
2. **DLC支持**：内置了DLC限制机制，可以根据玩家拥有的DLC显示或隐藏相应的蓝图
3. **资源管理**：通过动画文件关联，实现了服装的视觉表现
4. **单例模式**：使用单例模式确保蓝图系统的全局访问和一致性
5. **分层结构**：从管理类到数据结构，形成了清晰的分层架构

## 总结

ONI 的服装蓝图系统是一个设计良好的模块化系统，通过以下步骤实现：

1. **核心管理**：由 `Blueprints` 类统一管理所有蓝图
2. **数据组织**：使用 `BlueprintCollection` 存储不同类型的蓝图
3. **蓝图定义**：通过 `BlueprintProvider` 子类定义具体蓝图
4. **类型体系**：建立了完善的服装类型和套装类型体系
5. **DLC支持**：内置DLC限制机制，确保内容的正确显示
6. **视觉表现**：通过动画文件实现服装的视觉效果

这种设计不仅满足了游戏内服装定制的需求，也为Mod开发者提供了扩展服装系统的可能性。通过继承 `BlueprintProvider` 类，Mod开发者可以添加新的服装蓝图，丰富游戏内容。

## 应用场景

1. **游戏内服装定制**：玩家可以通过蓝图系统为复制人选择不同的服装
2. **DLC内容管理**：根据玩家拥有的DLC显示相应的服装内容
3. **Mod开发**：Mod开发者可以通过扩展蓝图系统添加新的服装
4. **季节性活动**：游戏可以通过蓝图系统推出限时服装

通过这种灵活的蓝图系统，ONI 实现了丰富多样的服装定制功能，增强了游戏的可玩性和个性化体验。