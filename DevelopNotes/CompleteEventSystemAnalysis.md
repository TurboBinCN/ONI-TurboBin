# 完整事件系统分析

## 1. 全局事件系统

### 1.1 核心组件

#### EventBase
- **位置**：`Assembly-CSharp\EventBase.cs`
- **作用**：事件的基类，定义了事件的基本属性和方法
- **关键属性**：
  - `hash`：事件的哈希值，用于快速查找
- **关键方法**：
  - `GetDescription(EventInstanceBase ev)`：获取事件描述

#### EventInstanceBase
- **位置**：`Assembly-CSharp\EventInstanceBase.cs`
- **作用**：事件实例的基类
- **关键属性**：
  - `frame`：事件发生的帧号
  - `eventHash`：事件的哈希值
  - `ev`：对应的EventBase对象

#### GameplayEvent
- **位置**：`Assembly-CSharp\GameplayEvent.cs`
- **作用**：游戏玩法事件的抽象基类
- **关键属性**：
  - `numTimesAllowed`：事件允许触发的次数
  - `allowMultipleEventInstances`：是否允许多个事件实例
  - `basePriority`：基础优先级
  - `preconditions`：前置条件列表
  - `minionFilters`：小人过滤器列表
  - `successEvents`：成功时触发的事件
  - `failureEvents`：失败时触发的事件
- **关键方法**：
  - `IsAllowed()`：检查事件是否允许触发
  - `CalculatePriority()`：计算事件优先级
  - `CreateInstance(int worldId)`：创建事件实例
  - `GetSMI(GameplayEventManager manager, GameplayEventInstance eventInstance)`：获取状态机实例

#### GameplayEventManager
- **位置**：`Assembly-CSharp\GameplayEventManager.cs`
- **作用**：事件管理器，负责事件的注册、触发和管理
- **关键属性**：
  - `Instance`：单例实例
  - `activeEvents`：活跃事件列表
  - `pastEvents`：历史事件计数
  - `sleepTimers`：事件睡眠计时器
- **关键方法**：
  - `StartNewEvent(GameplayEvent eventType, int worldId = -1)`：启动新事件
  - `RemoveActiveEvent(GameplayEventInstance eventInstance)`：移除活跃事件
  - `IsGameplayEventActive(GameplayEvent eventType)`：检查事件是否活跃
  - `GetActiveEventsOfType<T>(ref List<GameplayEventInstance> results)`：获取特定类型的活跃事件

### 1.2 运作流程

1. **事件注册**：
   - 开发者通过继承GameplayEvent创建具体的事件类型
   - 在构造函数中设置事件的优先级、重要性等属性
   - 通过链式调用添加前置条件、优先级提升、小人过滤器等

2. **事件触发**：
   - GameplayEventManager.StartNewEvent()方法创建并启动事件实例
   - 首先检查事件是否允许触发（通过IsAllowed()方法）
   - 创建事件实例并添加到活跃事件列表
   - 增加历史事件计数

3. **事件执行**：
   - 事件实例通过状态机（StateMachine）管理事件的生命周期
   - 状态机处理事件的各个阶段，如开始、进行中、结束等
   - 事件执行过程中可能会产生通知、效果等

4. **事件结束**：
   - 事件完成后，状态机会调用OnStop回调
   - 回调中将事件实例从活跃事件列表中移除
   - 可能会触发后续事件（成功或失败事件）

### 1.3 关键特性

- **事件优先级系统**：基础优先级 + 优先级提升 = 计算优先级
- **事件前置条件**：必需前置条件和非必需前置条件
- **事件睡眠机制**：通过睡眠计时器控制事件的冷却时间
- **事件过滤**：通过小人过滤器选择适合的小人参与事件
- **事件标签系统**：通过标签对事件进行分类
- **事件链式触发**：事件成功或失败后可以触发后续事件

## 2. GameObject级别事件系统

### 2.1 核心入口点

#### KMonoBehaviour
- **位置**：`Assembly-CSharp-firstpass\KMonoBehaviour.cs`
- **作用**：所有游戏组件的基类，提供完整的事件系统接口
- **关键方法**：
  - `Subscribe(int hash, Action<object> handler)` - 订阅事件
  - `Trigger(int hash, object data = null)` - 触发事件
  - `BoxingTrigger<T>(int hash, T data)` - 优化的事件触发，减少垃圾回收

#### KPrefabID
- **位置**：`Assembly-CSharp-firstpass\KPrefabID.cs`
- **作用**：每个GameObject的核心组件，管理标签和事件
- **关键特性**：
  - 继承自KMonoBehaviour，因此拥有完整的事件系统功能
  - 每个GameObject通常都有一个KPrefabID组件
  - 提供标签管理和事件触发功能

#### StateMachine.Instance
- **位置**：`Assembly-CSharp\StateMachine.cs`
- **作用**：状态机实例，通过GetMaster()获取目标对象的事件系统
- **关键方法**：
  - `Subscribe(int hash, System.Action<object> handler)` - 转发到目标对象的事件系统
  - `Trigger(int hash, object data = null)` - 转发到目标对象的KPrefabID

#### KObject
- **作用**：每个GameObject对应的内部管理对象
- **关键特性**：
  - 包含EventSystem实例
  - 通过`GetOrCreateEventSystem()`方法获取事件系统
  - 是事件系统的实际存储和处理者

#### EventSystem2
- **位置**：`Assembly-CSharp\EventSystem2Syntax\`
- **作用**：新一代类型安全的事件系统
- **关键特性**：
  - 使用泛型和接口确保类型安全
  - 提供更清晰的事件定义和处理方式
  - 示例实现：KMonoBehaviour2

### 2.2 运作流程

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

## 3. 两种事件系统的对比

| 特性 | 全局事件系统 | GameObject级别事件系统 |
|------|-------------|------------------------|
| 作用范围 | 全局，影响整个游戏 | 局部，仅影响特定GameObject |
| 管理方式 | 集中管理，通过GameplayEventManager | 分散管理，每个GameObject独立 |
| 事件类型 | 游戏玩法事件，如流星雨、派对等 | 组件级事件，如选择、激活、状态变化等 |
| 触发机制 | 通过状态机和事件管理器 | 通过KPrefabID直接触发 |
| 注册方式 | 事件定义和前置条件 | 直接订阅特定事件 |
| 生命周期 | 由状态机管理，通常较长 | 由组件生命周期决定，通常较短 |
| 复杂性 | 较高，包含前置条件、优先级等 | 较低，直接订阅和触发 |
| 使用场景 | 游戏玩法事件、剧情事件 | 组件间通信、状态变化通知 |

## 4. 代码示例

### 4.1 全局事件系统使用

#### 事件定义
```csharp
public class CustomGlobalEvent : GameplayEvent
{
    public CustomGlobalEvent() : base("CustomGlobalEvent", 100, 5)
    {
        // 添加前置条件
        AddPrecondition(new GameplayEventPrecondition(() => GameUtil.GetCurrentTimeInCycles() > 10));
        
        // 添加优先级提升
        AddPriorityBoost(new GameplayEventPrecondition(() => Components.LiveMinionIdentities.Items.Count > 5), 20);
        
        // 添加小人过滤器
        AddMinionFilter(new GameplayEventMinionFilter((minion) => minion.GetComponent<MinionIdentity>().HasTrait("CouchPotato")));
        
        // 设置成功和失败事件
        TrySpawnEventOnSuccess("SuccessEvent");
        TrySpawnEventOnFailure("FailureEvent");
    }
    
    public override StateMachine.Instance GetSMI(GameplayEventManager manager, GameplayEventInstance eventInstance)
    {
        // 返回事件的状态机实例
        return new CustomGlobalEventSMI(manager, eventInstance);
    }
}
```

#### 事件触发
```csharp
// 触发事件
GameplayEvent customEvent = Db.Get().GameplayEvents.Get("CustomGlobalEvent");
if (customEvent.IsAllowed())
{
    GameplayEventManager.Instance.StartNewEvent(customEvent);
}
```

### 4.2 GameObject级别事件系统使用

#### 传统事件系统
```csharp
// 订阅事件
int handlerId = this.Subscribe(GameHashes.OnSelected, OnSelected);

// 触发事件
this.Trigger(GameHashes.OnSelected, this);

// 取消订阅
this.Unsubscribe(handlerId);

// 事件处理方法
private void OnSelected(object data)
{
    // 处理选择事件
}
```

#### 新一代EventSystem2
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

## 5. 最佳实践

### 5.1 全局事件系统

1. **合理设置优先级**：根据事件的重要性设置合适的优先级
2. **使用前置条件**：通过前置条件控制事件的触发时机
3. **避免过度使用**：全局事件可能影响整个游戏，应谨慎使用
4. **合理设置冷却时间**：通过睡眠计时器避免事件过于频繁触发
5. **使用状态机管理复杂逻辑**：对于复杂事件，使用状态机管理其生命周期

### 5.2 GameObject级别事件系统

1. **使用BoxingTrigger**：对于值类型参数，使用BoxingTrigger减少垃圾回收
2. **及时取消订阅**：在组件销毁时取消订阅事件，避免内存泄漏
3. **使用事件哈希常量**：使用GameHashes中定义的常量，避免硬编码
4. **合理设计事件数据**：对于复杂事件，设计清晰的事件数据结构
5. **考虑使用EventSystem2**：对于新代码，考虑使用类型安全的EventSystem2

## 6. 总结

游戏中的事件系统分为全局级别和GameObject级别，它们各有特点和适用场景：

1. **全局事件系统**：
   - 适用于影响整个游戏的大型事件
   - 提供了丰富的前置条件、优先级和链式触发机制
   - 通过状态机管理复杂的事件逻辑

2. **GameObject级别事件系统**：
   - 适用于组件间的通信和状态变化通知
   - 提供了简单直接的订阅和触发机制
   - 支持类型安全的EventSystem2

两种事件系统相互补充，共同构成了游戏中完整的事件处理机制。开发者应根据具体场景选择合适的事件系统，并遵循最佳实践，以确保代码的可维护性和性能。