# 实现畸变体熔融金属生成SMI

## 1. 需求分析

* 实现一个新的SMI（状态机实例），用于控制畸变体产生熔融金属的行为

* 该功能与"1.1.2 畸变体熔融物产出威胁"相关

* 需要集成到现有的畸变体系统中

## 2. 实现计划

### 2.1 创建新的SMI类

* 创建 `MutanterMoltenMetalMonitor.cs` 文件

* 实现一个状态机，包含以下状态：

  * `idle`：默认状态，不产生熔融金属

  * `generating`：生成熔融金属的状态

  * `discharging`：释放熔融金属的状态

### 2.2 实现核心逻辑

* 在 `generating` 状态中，累积能量/温度，获取EmotionMonitor实例，根据实例获取理智值INSANITYValue，根据持续时间能量与温度，

* 当达到阈值时，切换到 `discharging` 状态

* 在 `discharging` 状态中，生成熔融金属实体

* 可配置参数：生成速率、阈值、金属类型等

### 2.3 集成到畸变体系统

* 修改 `BaseMutanter.cs`，在 `ExtendToBaseMutanter` 方法中添加新的SMI

* 确保与现有的状态机（如MutanterStateMachine、EmotionMonitor）协同工作

### 2.4 配置和平衡

* 添加必要的配置选项

* 确保功能平衡，不会导致游戏过于简单或困难

## 3. 文件修改

* `MutanterComponent/MutanterMoltenMetalMonitor.cs`（新建）

* `MutanterComponent/BaseMutanter.cs`（修改）

## 4. 测试和验证

* 确保新功能正常工作

* 验证与现有系统的兼容性

* 测试不同配置下的行为

