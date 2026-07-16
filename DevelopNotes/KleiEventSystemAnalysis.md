# Klei事件系统与GameStateMachine分析

## 1. 核心组件

### 1.1 KMonoBehaviour
- **作用**：Klei游戏中所有MonoBehaviour的基类
- **事件机制**：
  - 提供 `Subscribe` 方法订阅事件
  - 提供 `Unsubscribe` 方法取消订阅
  - 事件类型使用 `GameHashes` 枚举定义

### 1.2 GameStateMachine
- **作用**：状态机系统，管理游戏对象的状态转换
- **核心概念**：
  - **State**：状态定义，包含进入、退出、更新等回调
  - **StatesInstance**：状态机实例，管理具体对象的状态
  - **UpdateRate**：更新频率（如 SIM_1000ms）
  - **Transition**：状态转换条件

### 1.3 事件系统工作原理
1. **事件定义**：使用 `GameHashes` 枚举定义事件类型
2. **事件发布**：通过 `Game.Instance.EventManager` 发布事件
3. **事件订阅**：通过 `Subscribe` 方法订阅事件
4. **事件处理**：事件触发时执行回调函数

## 2. 原生事件系统使用示例

### 2.1 订阅事件
```csharp
// 在StatesInstance的构造函数中
Subscribe((int)GameHashes.CreatureLowDecor, (_) => _highDecor = false);
Subscribe((int)GameHashes.CreatureHighDecor, (_) => _highDecor = true);
```

### 2.2 取消订阅
```csharp
// 在OnCleanUp方法中
Unsubscribe((int)GameHashes.CreatureLowDecor);
Unsubscribe((int)GameHashes.CreatureHighDecor);
```

### 2.3 发布事件
```csharp
// 发布事件
Game.Instance.EventManager.Trigger((int)GameHashes.CreatureLowDecor, gameObject);
```

## 3. 我们的实现分析

### 3.1 现有实现
- **MutanterEvent**：自定义事件枚举
- **MutanterEventManager**：自定义事件管理器
- **事件流程**：
  1. MutanterSecurableMonitor发布事件
  2. 各组件订阅并处理事件

### 3.2 与原生系统的对比
- **原生系统**：使用 `GameHashes` 枚举和 `Game.Instance.EventManager`
- **我们的实现**：使用 `MutanterEvent` 枚举和 `MutanterEventManager`
- **设计理念**：保持一致，都是基于发布-订阅模式

## 4. 优势
1. **解耦**：组件间通过事件通信，减少直接依赖
2. **实时响应**：事件触发立即执行，无需轮询
3. **可扩展**：易于添加新的事件类型
4. **灵活性**：支持多个订阅者监听同一事件

## 5. 优化建议
1. **使用原生事件系统**：利用Klei已有的事件机制
2. **统一事件定义**：在GameHashes中添加自定义事件
3. **简化事件管理**：减少自定义事件管理器的复杂性
4. **提高性能**：利用原生事件系统的优化
