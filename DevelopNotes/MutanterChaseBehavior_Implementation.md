# MutanterChaseBehavior 实现方案

## 1. 概述

MutanterChaseBehavior 是一个可选的组件，用于控制畸变体的追击行为。当 EmotionMonitor 的 threaters 列表有内容时，畸变体将顺序追击列表中的小人，直到效果持续时间结束或目标不可达。

## 2. 核心功能

### 2.1 触发条件
- EmotionMonitor 的 threaters 列表有内容
- 不存在 MUTANTER_CONTAINED_EFFECT 效果

### 2.2 追击逻辑
- 顺序追击 threaters 列表中的小人
- 当目标不可达时，转向列表中的下一位
- 循环追击直到效果结束

### 2.3 效果系统
- 追击开始时设置 MUTANTER_CHASE_EFFECT 效果
- 效果持续时间为 3*600s（1800秒）
- 效果结束时停止追击

### 2.4 攻击系统
- 使用 MutanterAttackBehaviors 进行攻击
- 攻击结束判定：目标压力值满值或生命值归0

### 2.5 状态管理
- 使用 GameStateMachine 管理追击状态
- 与 MutanterStateMachine 集成，控制攻击动画

## 3. 技术实现

### 3.1 状态机设计

#### 3.1.1 MutanterChaseBehavior 状态机

```csharp
public class MutanterChaseBehavior : GameStateMachine<MutanterChaseBehavior, MutanterChaseBehavior.Instance, IStateMachineTarget, MutanterChaseBehavior.Definition>
{
    public State root;
    public State idle;
    public State chasing;
    public State attacking;
    public State stopping;
}
```

#### 3.1.2 状态流转
- **root** → **idle**：初始状态
- **idle** → **chasing**：当 threaters 列表有内容且无 MUTANTER_CONTAINED_EFFECT 时
- **chasing** → **attacking**：当接近目标时
- **attacking** → **chasing**：攻击结束后
- **chasing** → **stopping**：当效果结束或 threaters 列表为空时
- **stopping** → **idle**：停止追击后
- **chasing** → **idle**：当检测到 MUTANTER_CONTAINED_EFFECT 时

### 3.2 事件系统

创建 MutanterEvents.cs 文件，定义自定义事件：

```csharp
public class MutanterEvents
{
    public static readonly Event<MutanterChaseBehavior.Instance> MutanterChaseStart = new Event<MutanterChaseBehavior.Instance>();
    public static readonly Event<MutanterChaseBehavior.Instance> MutanterChaseStop = new Event<MutanterChaseBehavior.Instance>();
}
```

### 3.3 枚举类型

由于 GameHashes 是系统枚举类型，需要自建枚举来定义事件和状态：

```csharp
public class MutanterHashes
{
    public static HashChanger<MutanterChaseBehavior.State> chaseState = new HashChanger<MutanterChaseBehavior.State>();
}
```

### 3.4 关键组件

#### 3.4.1 Navigator 集成
- 使用 Navigator 组件进行路径查找
- 调用 Navigator.CanReach() 检查目标可达性
- 使用 Navigator.GoTo() 进行移动

#### 3.4.2 目标管理
- 使用 storedThreaters 列表存储追击目标
- 按顺序追击列表中的目标
- 当目标不可达时，移除当前目标并选择下一个

### 3.5 与 MutanterStateMachine 集成

MutanterStateMachine 订阅 MutanterChaseStart 和 MutanterChaseStop 事件：

```csharp
[OnEvent(MutanterEvents.MutanterChaseStart)]
private void OnChaseStart(MutanterChaseBehavior.Instance smi)
{
    GoToAttackState();
}

[OnEvent(MutanterEvents.MutanterChaseStop)]
private void OnChaseStop(MutanterChaseBehavior.Instance smi)
{
    ExitAttackState();
}
```

## 4. 文件结构

### 4.1 新增文件
- `MutanterComponent/MutanterChaseBehavior.cs`：追击行为状态机
- `MutanterComponent/MutanterEvents.cs`：自定义事件定义

### 4.2 修改文件
- `MutanterEffect/MutanterEffects.cs`：添加 MUTANTER_CHASE_EFFECT
- `MutantContainmentProjectMod.cs`：初始化 MUTANTER_CHASE_EFFECT
- `Strings.cs`：添加英语字符串
- `translations/zh.hjson`：添加中文翻译
- `MutanterComponent/MutanterStateMachine.cs`：集成追击事件

## 5. 实现步骤

1. 创建 MutanterEvents.cs 文件，定义自定义事件
2. 创建 MutanterChaseBehavior.cs 文件，实现追击状态机
3. 修改 MutanterEffects.cs，添加 MUTANTER_CHASE_EFFECT
4. 修改 MutantContainmentProjectMod.cs，初始化效果
5. 修改 Strings.cs，添加英语字符串
6. 修改 translations/zh.hjson，添加中文翻译
7. 修改 MutanterStateMachine.cs，集成追击事件
8. 测试追击行为是否正常工作

## 6. 注意事项

- MutanterChaseBehavior 是可选组件，可根据畸变体类型选择性挂载
- 确保与 MutanterStateMachine 的集成正确，以控制攻击动画
- 正确处理 Navigator 的使用，避免导航冲突
- 确保 MUTANTER_CONTAINED_EFFECT 能正确阻止追击行为
- 测试不同场景下的追击行为，确保稳定性