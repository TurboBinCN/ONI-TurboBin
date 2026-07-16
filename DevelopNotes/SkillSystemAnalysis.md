# 技能系统、任务系统与经验系统工作流转分析

## 1. 系统架构分析

### 1.1 技能系统结构

* **SkillGroup**：技能组，用于分类技能，如Mining、Building等

* **Skill**：具体技能，如Mining I、Mining II等

* **SkillPerk**：技能带来的具体效果

* **Attribute**：属性，如Strength、Caring等

* **AttributeLevel**：属性等级，通过经验升级

### 1.2 任务系统结构

* **ChoreGroup**：任务组，如Dig、Build等，每个任务组关联一个属性

* **ChoreType**：任务类型，定义任务的基本信息

* **Chore**：具体的任务实例

* **Workable**：可工作的对象，如建筑、资源等

### 1.3 经验获取机制

* 小人执行任务时，通过Workable组件获得经验

* Workable组件的`skillExperienceSkillGroup`属性指定该任务属于哪个技能组

* 经验通过`MinionResume.AddExperienceWithAptitude`方法添加

* 经验会根据小人对该技能组的aptitude（天赋）进行加成

## 2. 核心概念及其作用

### 2.1 技能 (Skill)

* **作用**：定义小人可以学习的具体技能，如挖掘、烹饪、研究等

* **核心属性**：

  * 技能组归属 (`skillGroup`)

  * 技能等级 (`tier`)

  * 解锁的帽子和徽章 (`hat`, `badge`)

  * 前置技能要求 (`priorSkills`)

  * 技能效果 (`perks`)

* **调用时机**：

  * 小人学习技能时 (`MinionResume.MasterSkill()`)

  * 计算技能点数和士气时 (`MinionResume.UpdateMorale()`)

  * 检查技能学习条件时 (`MinionResume.CanMasterSkill()`)

### 2.2 技能Perk (SkillPerk)

* **作用**：实现技能的具体效果，如属性提升、特殊能力等

* **核心属性**：

  * 应用效果回调 (`OnApply`)

  * 移除效果回调 (`OnRemove`)

  * 是否影响所有小人 (`affectAll`)

* **调用时机**：

  * 学习技能时 (`MinionResume.ApplySkillPerksForSkill()`)

  * 遗忘技能时 (`MinionResume.RemoveSkillPerksForSkill()`)

  * 小人状态变化时

### 2.3 属性 (Attribute)

* **作用**：定义小人的各种能力数值，如挖掘速度、力量、学习能力等

* **核心属性**：

  * 基础值 (`BaseValue`)

  * 是否可训练 (`IsTrainable`)

  * 在UI中显示方式 (`ShowInUI`)

  * 相关的属性转换器 (`converters`)

* **调用时机**：

  * 计算工作效率时 (`Workable.GetEfficiencyMultiplier()`)

  * 小人属性界面显示时

  * 技能效果应用时

### 2.4 属性转换器 (AttributeConverter)

* **作用**：将基础属性值转换为实际游戏效果，如将防御属性转换为伤害减免百分比

* **核心属性**：

  * 关联的属性 (`attribute`)

  * 转换系数 (`multiplier`)

  * 基础值 (`baseValue`)

  * 格式化器 (`formatter`)

* **计算方式**：
  * 使用公式：`multiplier * attributeValue + baseValue`
  * 例如：防御属性每级减少5%物理伤害，那么multiplier=0.05

* **调用时机**：
  * 计算实际效果时 (`AttributeConverter.Evaluate()`)
  * 显示转换后的属性值时
  * 工作效率计算时

* **在项目中的应用**：
  * 物理防御转换器：将防御属性转换为物理伤害减免
  * 精神防御转换器：将防御属性转换为精神伤害减免
  * 收容速度转换器：将自律属性转换为收容工作速度加成
  * 安全措施成功率转换器：将自律属性转换为安全措施成功率加成
  * 攻击伤害转换器：将正义属性转换为对畸变体的攻击伤害加成
  * 攻击速度转换器：将正义属性转换为对畸变体的攻击速度加成

### 2.5 技能组 (SkillGroup)

* **作用**：对技能进行分类，决定经验获取的归属

* **核心属性**：

  * 关联的任务组 (`choreGroupID`)

  * 相关属性 (`relevantAttributes`)

  * 图标 (`choreGroupIcon`, `archetypeIcon`)

* **调用时机**：

  * 小人获得经验时 (`MinionResume.AddExperienceWithAptitude()`)

  * 选人界面显示时

  * 技能学习条件检查时

### 2.6 任务类型 (ChoreType)

* **作用**：定义具体的工作任务，如挖掘、搬运、烹饪等

* **核心属性**：

  * 所属任务组 (`groups`)

  * 优先级 (`priority`, `explicitPriority`)

  * 相关状态项 (`statusItem`)

* **调用时机**：

  * 任务生成时

  * ChoreDriver选择任务时

  * 小人执行任务时

### 2.7 任务组 (ChoreGroup)

* **作用**：对任务类型进行分类，影响小人的工作优先级

* **核心属性**：

  * 关联的属性 (`attribute`)

  * 图标 (`sprite`)

  * 默认个人优先级 (`default_personal_priority`)

* **调用时机**：

  * 任务类型注册时

  * 小人设置工作优先级时

  * 计算工作效率时

## 3. 系统关联与运作机制

### 3.1 技能与属性的关联

* 技能通过SkillPerk修改小人的属性

* 技能组关联特定的属性，影响经验获取

* 工作效率计算基于属性值

### 3.2 技能与任务的关联

* 技能组关联任务组

* 任务类型属于特定任务组

* 工作对象(Workable)指定技能组，决定经验归属

### 3.3 Chore系统运作机制

1. **任务生成**：各种游戏系统生成Chore对象
2. **任务消费**：

   * ChoreConsumer管理小人的任务优先级

   * 基于任务组的默认优先级和用户设置的优先级
3. **任务执行**：

   * ChoreDriver从ChoreConsumer获取最高优先级任务

   * 小人执行任务，获得经验

   * 经验基于Workable指定的技能组分配
4. **经验处理**：

   * MinionResume处理经验，计算技能点数

   * 达到阈值时获得技能点

   * 小人可以学习新技能

### 3.4 经验获取流程

1. 小人执行工作任务
2. Workable完成工作时，调用AddExperienceWithAptitude
3. 根据技能组和小人天赋计算经验
4. 经验累积，达到阈值时获得技能点
5. 小人使用技能点学习技能
6. 技能学习后应用SkillPerk效果

### 3.5 任务优先级系统

* 任务类型有基础优先级

* 任务组有默认个人优先级

* 用户可以调整小人的任务组优先级

* ChoreDriver根据优先级选择任务

## 4. 关键调用链

### 4.1 技能学习

```
MinionResume.MasterSkill() → ApplySkillPerksForSkill() → SkillPerk.OnApply()
```

### 4.2 经验获取

```
Workable.CompleteWork() → MinionResume.AddExperienceWithAptitude() → AddExperience() → OnSkillPointGained()
```

### 4.3 工作效率计算

```
Workable.GetEfficiencyMultiplier() → AttributeConverter.Evaluate() → 属性值计算
```

### 4.4 任务选择

```
ChoreDriver.Sim200ms() → ChoreConsumer.GetNextChore() → 基于优先级选择任务
```

### 4.5 任务执行

```
ChoreDriver.StartChore() → Workable.StartWork() → Workable.WorkTick() → Workable.CompleteWork()
```

## 5. 项目中的技能实现

### 5.1 新增技能

* **Bravery（勇气）**：增加生命值

* **MentalResistance（精神抗性）**：减少精神攻击带来的压力增长

* **Discipline（自律）**：增加成功率和工作速度

* **Righteousness（正义）**：增加攻击伤害

### 5.2 技能实现结构

1. **MutanterAttributes.cs**：定义技能相关的属性
2. **MutanterSkillGroups.cs**：创建技能组
3. **MutanterChoreGroups.cs**：创建任务组
4. **MutanterChoreTypes.cs**：创建任务类型
5. **MutanterSkillPerks.cs**：定义技能效果
6. **MutanterSkills.cs**：定义具体技能

### 5.3 技能组排除特质

* 在`SkillGroupExclusionsPatch.cs`中为新技能组添加到`DUPLICANTSTATS.ARCHETYPE_TRAIT_EXCLUSIONS`字典

* 确保技能组在选人界面中能正确显示和使用

## 6. 技能-属性-角色映射

| 技能组              | 技能   | 对应属性       | 属性转换器效果                              | 建议角色名称             | 角色定位                  |
| ---------------- | ---- | ---------- | ------------------------------------ | ------------------ | --------------------- |
| Bravery          | 勇气   | 勇气（生命值）    | -                                    | 守护者 (Guardian)     | 负责防御和承受伤害，适合前线作战      |
| MentalResistance | 精神抗性 | 精神抗性（压力管理） | -                                    | 心灵守卫 (Psyche Ward) | 负责抵抗精神攻击，适合处理畸变体的心理影响 |
| Defense          | 防御   | 防御         | 物理防御：每级减少5%物理伤害<br>精神防御：每级减少7%精神伤害 | 守护者 (Guardian)     | 负责防御和承受伤害，适合前线作战      |
| Discipline       | 自律   | 自律         | 收容速度：每级增加6%收容速度<br>安全措施成功率：每级增加8%安全措施成功率 | 执行官 (Enforcer)     | 负责高效完成任务，适合需要精准操作的工作  |
| Righteousness    | 正义   | 正义         | 攻击伤害：每级增加10%攻击伤害<br>攻击速度：每级增加8%攻击速度 | 裁决者 (Judicator)    | 负责对畸变体的直接攻击，适合战斗      |

