using Klei.AI;
using MutantContainmentProject.MutanterComponent.Effector;
using MutantContainmentProject.MutanterComponent.Triggers;
using MutantContainmentProject.MutanterComponent.VFXController;
using System;
using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    /**
     * 参考 DevelopNote\畸变体战斗系统架构
     */
    public class MutanterSkillComponent : KMonoBehaviour
    {
        public struct TriggerData
        {
            public string triggerName;
            public Dictionary<string, object> properties; // 触发器属性
            public Dictionary<string, Func<GameObject, bool>> conditionCallbackMethods;
        }
        public struct AttackEffectorData
        {
            public string attackEffectorName;
            public Tag damageType;
            public float damageAmount;
            //应用特殊效果
            public List<Effect> effects;
            //应用组件
            public List<Type> kMonoBehaviours;
            //回调函数
            public List<System.Action<GameObject>> callbackMethods;
        }
        public struct SkillData
        {
            public string name;
            public bool isPassiveSkill;
            public float cooldown;
            public List<TriggerData> triggers; // 触发器数据列表
            public List<AttackEffectorData> attackEffectors;//攻击效果数据列表
            public float lastUseTime;
            public bool isFirstUse;
            //表现层设定: 基础动画 攻击特效
            public string animation;
            public float animationDuration;
            public string VFXName;
        }

        // 静态技能数据库
        private static Dictionary<Tag, List<SkillData>> MutantersSkillDb = new();

        public List<SkillData> skills = new();

        private MutanterAttackSystem attackSystem;
        private MutanterAttackSystem AttackSystem => attackSystem ??= GetComponent<MutanterAttackSystem>();

        private SkillTriggerManager triggerManager;
        private SkillTriggerManager TriggerManager => triggerManager ??= GetComponent<SkillTriggerManager>();

        private MutanterCombatManager combatManager;
        private MutanterCombatManager CombatManager => combatManager ??= GetComponent<MutanterCombatManager>();

        private SkillEffectorManager effectorManager;
        private SkillEffectorManager EffectorManager => effectorManager ??= GetComponent<SkillEffectorManager>();

        private VFXManager vFXManager;
        private VFXManager VFXManagerInstancce => vFXManager ??= GetComponent<VFXManager>();

        protected override void OnSpawn()
        {
            base.OnSpawn();

            string mutanterId = gameObject.GetComponent<KPrefabID>().PrefabID().Name;

            skills = MutantersSkillDb.TryGetValue(mutanterId, out var skillData) ? skillData : new List<SkillData>();
            // 执行被动触发器
            TriggerManager?.ExecutePassiveTriggers(gameObject, skills);
            EffectorManager?.LoadEffectors(gameObject, skills);

        }

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
        }
        public void AddSkillsToDb(List<SkillData> skills)
        {
            string mutanterId = gameObject.GetComponent<KPrefabID>().PrefabID().Name;
            if (!MutantersSkillDb.ContainsKey(mutanterId))
            {
                MutantersSkillDb.Add(mutanterId, skills);
            }
        }
        public void AddSkill(SkillData skill)
        {
            skills.Add(skill);
        }
        public bool IsSkillCooldown(string skillName) {
            if (skillName == null || skills.Count == 0)
                return false;
            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i].name == skillName)
                {
                    // 添加冷却时间检查
                    var skill = skills[i];
                    if (!skill.isFirstUse && Time.time - skill.lastUseTime < skill.cooldown)
                        return false;
                    return true;
                }
            }
            return false;
        }
        public bool TryExecuteSkill(string skillName, GameObject target = null, float damageAmount = 0)
        {
            if (skillName == null || skills.Count == 0)
                return false;
            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i].name == skillName)
                {
                    // 添加冷却时间检查
                    var skill = skills[i];
                    if (!skill.isPassiveSkill && !skill.isFirstUse && Time.time - skill.lastUseTime < skill.cooldown)
                        return false;
                    //执行攻击之前停止移动
                    gameObject?.GetComponent<Navigator>()?.Stop();
                    //改变攻击朝向
                    FaceTarget(target);
                    // 执行技能攻击
                    ExecuteSkill(i, target, damageAmount);
                    return true;
                }
            }
            return false;
        }
        private void FaceTarget(GameObject target)
        {
            if (target == null) return;
            Vector3 targetPos = target.transform.position;
            GetComponent<Facing>()?.Face(targetPos);
        }

        private void ExecuteSkill(int skillIndex, GameObject target, float damageAmount = 0)
        {
            if (skillIndex < 0 || skillIndex >= skills.Count) return;
            TbbDebuger.LogDebug($"[MutanterSkillComponent] {gameObject.name} ExecuteSkill, 技能索引 = {skillIndex}");
            var skill = skills[skillIndex];

            TbbDebuger.LogDebug($"[MutanterSkillComponent] {gameObject.name} 设置攻击状态为true");
            CombatManager?.SetAttacking(true);
            //表现层逻辑
            var animController = gameObject.GetComponent<KBatchedAnimController>();
            TbbDebuger.LogDebug($"[MutanterSkillComponent] {gameObject.name} 动画控制器: {animController != null}, 动画名称: {skill.animation}");
            EffectorManager?.ApplyEffectorsBefore(skill);
            if (animController != null && !string.IsNullOrEmpty(skill.animation))
            {
                TbbDebuger.LogDebug($"[MutanterSkillComponent] {gameObject.name} ExecuteSkill, 攻击动画 = {skill.animation}");
                //攻击特效
                var attackVFX = VFXManagerInstancce.GetVFXController(skill);
                TbbDebuger.LogDebug($"[MutanterSkillComponent] {gameObject.name} 攻击特效: {attackVFX != null}");
                attackVFX?.Activate(target);

                void onComplete()
                {
                    TbbDebuger.LogDebug($"[MutanterSkillComponent] {gameObject.name} ExecuteSkill, 攻击动画完成");
                    if (attackVFX != null)
                    {
                        attackVFX.Deactivate();
                        List<KPrefabID> aoeTargets = attackVFX.GetAttackTargets();
                        TbbDebuger.LogDebug($"[MutanterSkillComponent] {gameObject.name} ExecuteSkill, 攻击特效目标 = {aoeTargets.Count}");
                        if (aoeTargets.Count > 0)
                        {
                            foreach (var aoeTarget in aoeTargets)
                            {
                                //处理AOE伤害，碰撞判断对象伤害
                                EffectorManager?.ApplyEffectorsAfter(aoeTarget?.gameObject, skill);
                            }
                        }
                        else
                        {
                            EffectorManager?.ApplyEffectorsAfter(target?.gameObject, skill);
                        }
                    }
                    else
                    {
                        EffectorManager?.ApplyEffectorsAfter(target, skill);
                    }
                    TbbDebuger.LogDebug($"[MutanterSkillComponent] {gameObject.name} 设置攻击状态为false");
                    CombatManager?.SetAttacking(false);

                    // 通知状态机攻击完成
                    var stateMachine = gameObject.GetSMI<MutanterStateMachine.StatesInstance>();
                    TbbDebuger.LogDebug($"[MutanterSkillComponent] {gameObject.name} ExecuteSkill, 攻击完成, 当前状态 = {stateMachine?.GetStatus()}");
                    stateMachine?.OnAttackComplete();

                    // 动画完成后解锁状态机并继续处理队列
                    TbbDebuger.LogDebug($"[MutanterSkillComponent] {gameObject.name} 解锁状态机并处理下一个技能");
                    CombatManager?.UnlockStateMachineAfterAnimation();
                }

                if (skill.animationDuration > 0f)
                {
                    TbbDebuger.LogDebug($"[MutanterSkillComponent] {gameObject.name} 播放动画(带持续时间): {skill.animation}, 持续时间: {skill.animationDuration}");
                    CombatManager?.PlayAnimation(skill.animation, skill.animationDuration, onComplete);
                }
                else
                {
                    TbbDebuger.LogDebug($"[MutanterSkillComponent] {gameObject.name} 播放动画(一次性): {skill.animation}");
                    CombatManager?.PlayAnimation(skill.animation, KAnim.PlayMode.Once, onComplete);
                }
            }
            else
            {
                TbbDebuger.LogDebug($"[MutanterSkillComponent] {gameObject.name} 没有动画，直接设置攻击状态为false");
                CombatManager?.SetAttacking(false);

                // 没有动画时直接解锁状态机并继续处理队列
                TbbDebuger.LogDebug($"[MutanterSkillComponent] {gameObject.name} 没有动画，直接解锁状态机并处理下一个技能");
                CombatManager?.UnlockStateMachineAfterAnimation();
            }
            // 更新技能冷却时间
            var updatedSkill = skill;
            updatedSkill.lastUseTime = Time.time;
            if (updatedSkill.isFirstUse)
            {
                updatedSkill.isFirstUse = false;
            }
            skills[skillIndex] = updatedSkill;
            TbbDebuger.LogDebug($"[MutanterSkillComponent] {gameObject.name} 技能执行完成，更新冷却时间，动画异步执行中。");
        }

    }
}