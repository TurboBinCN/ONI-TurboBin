---
alwaysApply: true
---
{
  "alwaysApply": false,
  "description": "ONI (Oxygen Not Included) mod 开发规则，当处理 .cs 文件时生效",
  "globs": ["**/*.cs"],
  "priority": 100
}

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