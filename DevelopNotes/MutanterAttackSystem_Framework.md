# 畸变体攻击系统框架

## 1. 系统概述

畸变体攻击系统是一个统一的攻击管理框架，负责处理所有畸变体的攻击行为，包括攻击类型选择、攻击限制、攻击执行等功能。该系统通过效果系统实现攻击能力的限制和增强，提供了灵活的攻击行为管理机制。

## 2. 核心组件

### 2.1 MutanterAttackSystem

**文件路径**：`MutanterComponent/MutanterAttackSystem.cs`

**功能**：
- 统一管理所有攻击行为
- 处理攻击限制（基于效果）
- 提供统一的攻击执行方法
- 根据理智值选择合适的攻击类型
- 管理攻击冷却时间

**主要方法**：
- `TryExecuteAttack(GameObject target)` - 尝试执行攻击
- `TryExecuteAttack(List<KPrefabID> targets)` - 尝试执行多目标攻击
- `ExecuteHealthAttack(GameObject target, float damage)` - 执行生命值攻击
- `ExecuteStressAttack(GameObject target, float stressAmount)` - 执行压力值攻击
- `ExecuteEffectAttack(GameObject target, string effectId, float duration)` - 执行效果攻击
- `ExecuteCombinedAttack(GameObject target, float damage, float stressAmount)` - 执行综合攻击

### 2.2 攻击行为接口与实现

**接口**：`IMutanterAttackBehavior`

**实现类**：
- **MeleeAttack** - 物理攻击
- **PsychologicalAttack** - 心理攻击（增加压力）
- **ErosionAttack** - 侵蚀攻击（同时减少生命值和增加压力）
- **SoulAttack** - 灵魂攻击（按百分比扣减生命值）

**共同方法**：
- `GetTag()` - 获取攻击标签
- `GetCooldown()` - 获取攻击冷却时间
- `CanExecute(IStateMachineTarget attacker, GameObject target)` - 检查是否可以执行攻击
- `Execute(IStateMachineTarget attacker, GameObject target)` - 执行攻击
- `Execute(IStateMachineTarget attacker, GameObject target, float effectImpact)` - 执行攻击（带效果影响）

### 2.3 辅助组件

- **MutanterChaseMonitor** - 追击监控器，负责追踪和攻击目标
- **SCP049Controller** - SCP-049的控制器，使用攻击系统执行特殊攻击
- **BaseMutanter** - 基础畸变体设置，负责挂载攻击系统

## 3. 工作流程

### 3.1 初始化流程

1. 畸变体在创建时通过 `BaseMutanter.ExtendToBaseMutanter` 挂载 `MutanterAttackSystem`
2. `MutanterAttackSystem` 在 `OnSpawn` 时初始化：
   - 获取 `Effects` 组件
   - 获取 `EmotionMonitor.StatesInstance`
   - 调用 `InitializeBehaviors()` 初始化攻击行为
3. `InitializeBehaviors()` 根据 KPrefabID 上的标签加载对应的攻击行为

### 3.2 攻击执行流程

1. 当畸变体需要攻击时，调用 `MutanterAttackSystem.TryExecuteAttack`
2. 攻击系统检查当前效果状态：
   - 检查是否有 `MUTANTER_CONTAINED_EFFECT`
   - 检查是否有 `MUTANTER_ATTACK_RESTRICTED_EFFECT`
3. 如果攻击被限制，返回 false
4. 获取畸变体的理智值
5. 调用 `SelectBehavior` 根据理智值选择合适的攻击行为
6. 评估效果影响（如 `MUTANTER_ATTACK_ENHANCED_EFFECT`）
7. 执行选中的攻击行为
8. 更新攻击行为的最后执行时间

### 3.3 攻击选择逻辑

- 理智值 < 20f：使用物理攻击
- 理智值 < 40f：使用心理攻击
- 理智值 < 60f：使用侵蚀攻击
- 理智值 ≥ 60f：使用灵魂攻击

如果没有找到对应类型的攻击，随机选择一个可用的攻击行为。

## 4. 技术特点

### 4.1 统一的攻击管理

- 所有攻击行为通过 `MutanterAttackSystem` 统一管理
- 攻击系统内部处理攻击限制，避免重复代码
- 提供统一的攻击执行接口，简化攻击调用

### 4.2 基于标签的攻击行为挂载

- 通过标签系统（如 `MutanterTags.PhysicalAttack`）挂载攻击行为
- 灵活可扩展，易于添加新的攻击类型
- 默认添加物理攻击作为 fallback

### 4.3 效果-based的攻击限制

- 通过效果系统（如 `MUTANTER_CONTAINED_EFFECT`）限制攻击能力
- 支持攻击增强效果（如 `MUTANTER_ATTACK_ENHANCED_EFFECT`）
- 效果影响评估，调整攻击效果

### 4.4 懒加载的攻击系统获取方式

- 所有组件都采用懒加载属性模式获取 `MutanterAttackSystem` 实例
- 提高代码一致性和可维护性
- 减少不必要的组件获取操作

### 4.5 多种攻击类型

- 支持物理、心理、侵蚀、灵魂四种攻击类型
- 每种攻击类型有不同的伤害机制和效果
- 攻击行为模块化，易于扩展

### 4.6 降级处理

- 当攻击系统不可用时，攻击行为会降级为直接执行攻击
- 提高系统的可靠性和容错性

## 5. 代码结构

```
MutanterComponent/
├── MutanterAttackSystem.cs        # 统一攻击管理系统
├── IMutanterAttackBehavior.cs     # 攻击行为接口
├── MeleeAttack.cs                 # 物理攻击实现
├── PsychologicalAttack.cs         # 心理攻击实现
├── ErosionAttack.cs               # 侵蚀攻击实现
├── SoulAttack.cs                  # 灵魂攻击实现
├── MutanterChaseMonitor.cs        # 追击监控器
├── SCP049Controller.cs            # SCP-049控制器
└── BaseMutanter.cs                # 基础畸变体设置
```

## 6. 攻击限制效果

| 效果名称 | 效果ID | 作用 |
|---------|-------|------|
| 已收容效果 | MUTANTER_CONTAINED_EFFECT | 限制攻击行为，免疫即死攻击 |
| 攻击限制效果 | MUTANTER_ATTACK_RESTRICTED_EFFECT | 限制攻击行为 |
| 攻击增强效果 | MUTANTER_ATTACK_ENHANCED_EFFECT | 提高攻击效果 |
| 意志效果 | MUTANTER_WILLED_EFFECT | 小幅提高攻击效果 |

## 7. 攻击标签

| 标签名称 | 作用 |
|---------|------|
| PhysicalAttack | 启用物理攻击 |
| PsychologicalAttack | 启用心理攻击 |
| ErosionAttack | 启用侵蚀攻击 |
| SoulAttack | 启用灵魂攻击 |

## 8. 后续扩展建议

### 8.1 添加更多攻击类型

- 可以通过实现 `IMutanterAttackBehavior` 接口添加新的攻击类型
- 例如：毒素攻击、辐射攻击、冰冻攻击等

### 8.2 增强攻击效果

- 为攻击行为添加更多的效果选项
- 例如：持续伤害、debuff效果、状态效果等

### 8.3 优化攻击AI

- 根据目标类型、距离等因素优化攻击选择逻辑
- 例如：对不同类型的目标使用不同的攻击类型
- 添加攻击优先级系统

### 8.4 添加攻击动画和音效

- 为不同的攻击类型添加对应的动画和音效
- 提高游戏的视觉和听觉体验

### 8.5 实现攻击组合系统

- 允许畸变体组合使用多种攻击类型
- 创建更复杂的攻击模式

## 9. 测试与调试

### 9.1 日志记录

- 攻击系统在关键操作时会记录调试日志
- 可以通过查看 player.log 文件监控攻击系统的运行状态

### 9.2 测试场景

- 测试不同理智值下的攻击类型选择
- 测试效果对攻击的限制和增强
- 测试攻击冷却和距离限制
- 测试多目标攻击

## 10. 总结

畸变体攻击系统是一个灵活、可扩展的攻击管理框架，通过统一的攻击系统和模块化的攻击行为，实现了多种攻击类型的管理和执行。系统通过效果系统实现攻击能力的限制和增强，提供了丰富的攻击行为选择。

该框架具有良好的可扩展性，可以轻松添加新的攻击类型和效果，满足不同畸变体的攻击需求。同时，系统的模块化设计和统一的接口，提高了代码的可维护性和可读性。