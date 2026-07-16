# GameObject级别事件系统分析

## 核心入口点

### 1. KMonoBehaviour
- **位置**：`Assembly-CSharp-firstpass\KMonoBehaviour.cs`
- **作用**：所有游戏组件的基类，提供完整的事件系统接口
- **关键方法**：
  - `Subscribe(int hash, Action<object> handler)` - 订阅事件
  - `Trigger(int hash, object data = null)` - 触发事件
  - `BoxingTrigger<T>(int hash, T data)` - 优化的事件触发，减少垃圾回收

### 2. KPrefabID
- **位置**：`Assembly-CSharp-firstpass\KPrefabID.cs`
- **作用**：每个GameObject的核心组件，管理标签和事件
- **关键特性**：
  - 继承自KMonoBehaviour，因此拥有完整的事件系统功能
  - 每个GameObject通常都有一个KPrefabID组件
  - 提供标签管理和事件触发功能

### 3. StateMachine.Instance
- **位置**：`Assembly-CSharp\StateMachine.cs`
- **作用**：状态机实例，通过GetMaster()获取目标对象的事件系统
- **关键方法**：
  - `Subscribe(int hash, System.Action<object> handler)` - 转发到目标对象的事件系统
  - `Trigger(int hash, object data = null)` - 转发到目标对象的KPrefabID

### 4. KObject
- **作用**：每个GameObject对应的内部管理对象
- **关键特性**：
  - 包含EventSystem实例
  - 通过`GetOrCreateEventSystem()`方法获取事件系统
  - 是事件系统的实际存储和处理者

### 5. EventSystem2
- **位置**：`Assembly-CSharp\EventSystem2Syntax\`
- **作用**：新一代类型安全的事件系统
- **关键特性**：
  - 使用泛型和接口确保类型安全
  - 提供更清晰的事件定义和处理方式
  - 示例实现：KMonoBehaviour2

## 事件系统的工作流程

1. **事件注册**：
   - 通过KMonoBehaviour或KPrefabID的Subscribe方法注册事件监听器
   - 监听器存储在KObject的EventSystem实例中

2. **事件触发**：
   - 通过Trigger方法触发事件
   - EventSystem查找对应哈希值的所有监听器
   - 按顺序调用所有注册的回调函数

3. **事件传播**：
   - 事件在单个GameObject内部传播
   - 可以通过状态机进行更复杂的事件处理
   - 支持跨GameObject的事件订阅

## 代码示例

### 从KMonoBehaviour订阅和触发事件
```csharp
// 订阅事件
int handlerId = this.Subscribe(GameHashes.OnSelected, OnSelected);

// 触发事件
this.Trigger(GameHashes.OnSelected, this);

// 取消订阅
this.Unsubscribe(handlerId);
```

### 从KPrefabID订阅和触发事件
```csharp
// 获取KPrefabID组件
KPrefabID prefabId = gameObject.GetComponent<KPrefabID>();

// 订阅事件
int handlerId = prefabId.Subscribe(GameHashes.OnSelected, OnSelected);

// 触发事件
prefabId.Trigger(GameHashes.OnSelected, gameObject);

// 取消订阅
prefabId.Unsubscribe(handlerId);
```

### 从StateMachine实例触发事件
```csharp
// 在状态机中触发事件
this.Trigger(GameHashes.OnComplete, result);
```

### 新一代EventSystem2使用
```csharp
// 定义事件数据结构
private struct ObjectDestroyedEvent : IEventData
{
    public bool parameter;
}

// 订阅事件
Subscribe<MyComponent, ObjectDestroyedEvent>(OnObjectDestroyed);

// 触发事件
Trigger<ObjectDestroyedEvent>(new ObjectDestroyedEvent() { parameter = true });

// 事件处理方法
private void OnObjectDestroyed(MyComponent component, ObjectDestroyedEvent evt)
{
    // 处理事件
}
```

## 与全局事件系统的区别

| 特性 | 全局事件系统 | GameObject级别事件系统 |
|------|-------------|------------------------|
| 作用范围 | 全局，影响整个游戏 | 局部，仅影响特定GameObject |
| 管理方式 | 集中管理，通过GameplayEventManager | 分散管理，每个GameObject独立 |
| 事件类型 | 游戏玩法事件，如流星雨、派对等 | 组件级事件，如选择、激活、状态变化等 |
| 触发机制 | 通过状态机和事件管理器 | 通过KPrefabID直接触发 |
| 注册方式 | 事件定义和前置条件 | 直接订阅特定事件 |

## 关键特性

1. **哈希值事件系统**：使用整数哈希值作为事件标识符，提高性能
2. **类型安全的事件**：新一代EventSystem2提供类型安全的事件处理
3. **状态机集成**：事件系统与状态机紧密集成，实现复杂的行为逻辑
4. **优化的内存管理**：通过装箱/拆箱优化减少垃圾回收
5. **灵活的订阅机制**：支持多种订阅方式，包括跨GameObject的事件订阅

## 总结

GameObject级别事件系统是一个灵活、高效的组件间通信机制，通过以下方式运作：

1. **去中心化设计**：每个GameObject独立管理自己的事件，减少全局依赖
2. **高效的哈希系统**：使用整数哈希值快速查找和触发事件
3. **类型安全的演进**：新一代EventSystem2提供类型安全的事件处理
4. **与状态机集成**：事件系统与状态机紧密配合，实现复杂的行为逻辑
5. **优化的内存管理**：通过装箱/拆箱优化减少垃圾回收

这种事件系统设计不仅满足了游戏中组件间通信的需求，也为mod开发者提供了扩展游戏功能的强大工具。它与全局游戏事件系统相互补充，共同构成了游戏中完整的事件处理机制。