# MutanterChaseMonitor 与 MutanterStateMachine 集成方案

## 问题背景
MutanterChaseMonitor 是一个可选的组件，负责处理畸变体的追击逻辑，而 MutanterStateMachine 负责管理所有动画的切换，特别是攻击动画。目前两个组件相互独立，导致动画控制和追击逻辑分离，可能产生不一致的行为。

## 解决方案
基于对系统中其他组件（如植物生长状态机）交互方式的分析，采用事件驱动和直接引用相结合的方式实现两个状态机的协调。

## 实现步骤

### 1. 创建自定义事件枚举
由于 GameHashes 是系统枚举，需要创建自定义枚举来定义追击相关的事件。

### 2. 修改 MutanterChaseMonitor
- 添加事件触发，在进入 chasing 状态时触发自定义事件
- 在停止追击时触发自定义事件
- 添加对 MutanterStateMachine 状态的检查

### 3. 修改 MutanterStateMachine
- 添加对自定义事件的监听
- 当接收到追击开始事件时，进入 attackStates
- 当接收到追击停止事件时，退出 attackStates
- 添加对 MutanterChaseMonitor 状态的检查

### 4. 实现状态协调
- MutanterChaseMonitor 在 UpdateChase 中检查 MutanterStateMachine 的状态
- MutanterStateMachine 在 attackStates 中检查 MutanterChaseMonitor 的状态
- 确保两个状态机的状态变化能够相互感知

## 优势
1. **保持可选性**：MutanterChaseMonitor 可以根据需要选择性挂载
2. **职责分离**：MutanterStateMachine 负责动画控制，MutanterChaseMonitor 负责追击逻辑
3. **灵活性**：两个状态机可以独立演化，同时保持协调
4. **符合系统设计**：这种交互方式符合游戏中其他组件的实现模式

## 技术实现

### 自定义事件枚举
```csharp
public class MutanterEvents
{
    public const string MutanterChaseStart = "MutanterChaseStart";
    public const string MutanterChaseStop = "MutanterChaseStop";
}
```

### MutanterChaseMonitor 修改
- 在进入 chasing 状态时触发 MutanterChaseStart 事件
- 在停止追击时触发 MutanterChaseStop 事件
- 添加对 MutanterStateMachine 状态的检查

### MutanterStateMachine 修改
- 添加对 MutanterChaseStart 和 MutanterChaseStop 事件的监听
- 当接收到 ChaseStart 事件时，进入 attackStates
- 当接收到 ChaseStop 事件时，退出 attackStates
- 添加对 MutanterChaseMonitor 状态的检查

## 预期效果
- MutanterChaseMonitor 作为可选组件，可以根据需要挂载
- 当 MutanterChaseMonitor 启动追击时，MutanterStateMachine 自动进入攻击状态，播放攻击动画
- 当 MutanterChaseMonitor 停止追击时，MutanterStateMachine 自动退出攻击状态
- 两个状态机的状态变化保持同步，确保动画和追击行为的一致性
