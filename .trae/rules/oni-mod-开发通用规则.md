---
alwaysApply: true
---

# ONI Mods 开发规则

## 项目概况
- ONI mod 项目根目录：E:\oni_mods

## 编译规则
- 进入项目目录之后运行 `dotnet build -v d` 命令会自动发布项目到mod目录
## UI 相关开发说明
- ONI 是基于Unity 6引擎开发的，UI相关逻辑应该遵从了Unity的UI逻辑

## 源码与调试相关目录
- ONI 源码目录: E:\oni_mods\OxygenNotIncludedCode
  - 0Harmony: 当前ONI使用的Harmony源码
  - Assembly-CSharp: 当前ONI使用的Assembly-CSharp源码
  - Assembly-CSharp-firstpass: 当前ONI使用的Assembly-CSharp-firstpass源码
  - StreamingAssets: 当前ONI使用的资源文件
  - UnityEngine.CoreModule: 当前ONI使用的UnityEngine.CoreModule源码
- ONI 日志文件: C:\Users\Administrator\AppData\LocalLow\Klei\Oxygen Not Included\Player.log
- ONI 总结文档目录: E:\oni_mods\ONI-TurboBin\DevelopNotes
- ONI 发布目录: C:\Users\Administrator\Documents\Klei\OxygenNotIncluded\mods\dev

## Log 使用原则
- 初次开发必须添加 log 文件调试，确保 mod 正常运行
- log 需要有开关控制（日志级别、是否输出到游戏、是否写入文件）
- 当不确定开发内容是否有效时，必须添加 log 进行调试验证
- Log模块在每个项目中可能不同，常用的模块有本项目中的 TbbDebuger 或者共用库E:\oni_mods\ONI-TurboBin\TbbLib\Debuger\TbbDebuger.cs

## Log 分析流程
1. 设置合适的日志级别（DEBUG 用于详细调试）
2. 运行游戏触发问题场景
3. 查看 Player.log 分析调用链
4. 根据日志定位问题位置并修复

## 调试时机判断
- 新增功能或修改核心逻辑时，必须添加 log
- 函数入口/出口添加 log 记录参数和返回值
- 状态变化（实体创建、销毁、配置变更）添加 log
- 异常处理添加 ERROR 级别 log
- 边界条件添加 WARN 级别 log
- save/load 过程添加 log 记录数据恢复

## TbbLib 共用库说明
- 目录: E:\oni_mods\ONI-TurboBin\TbbLib
- Debuger 模块: TbbDebuger.cs - 提供多级别日志输出
- Module 模块: TbbModule.cs - 资源加载与卸载管理
- Extension 模块: TbbHarmonyExtension.cs - Harmony 补丁辅助方法
- Utils 模块: TbbUtils.cs - 通用工具方法

## 经验总结引用规则

### 1. 开发前必须查阅经验总结
- 开发前必须先阅读 [DevelopNotes](file:///e:/oni_mods/ONI-TurboBin/DevelopNotes) 目录下的相关经验总结文档
- 重点关注 [oni mod开发经验.md](file:///e:/oni_mods/ONI-TurboBin/DevelopNotes/oni%20mod开发经验.md) 中的已知问题和解决方案
- 避免重复踩坑，优先使用已验证的解决方案

### 2. 遇到问题必须记录经验总结
- 当遇到新的开发问题或 bug 时，必须在 [oni mod开发经验.md](file:///e:/oni_mods/ONI-TurboBin/DevelopNotes/oni%20mod开发经验.md) 中记录完整的经验总结
- 经验总结必须包含：问题描述、根因分析、解决方案、经验教训
- 引用相关文件时使用 `[文件名](file:///绝对路径)` 格式

### 3. 经验总结文档管理
- 系统分析文档：`{SystemName}Analysis.md` - 分析游戏原生系统的实现机制
- 实现方案文档：`实现{功能描述}.md` - 记录新功能的实现计划和设计方案
- 问题排查文档：`{问题描述}_排查.md` - 记录问题排查过程
- 经验总结文档：`oni mod开发经验.md` - 汇总开发过程中积累的经验和最佳实践

### 4. 规则升级流程
- 当某个经验被多次验证有效后，可以从经验总结文档中提炼为正式开发规则
- 正式规则应标注验证次数和适用场景
- 规则变更应在经验总结文档中记录变更历史