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
        }
        public struct AttackEffectorData
        {
            public string attackEffectorName;
            public Tag damageType;
            public float damageAmount;
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
            //public string extraAnimationEffectId; // 额外动画效果ID
        }

        // 静态技能数据库
        private static Dictionary<Tag, List<SkillData>> MutantersSkillDb = new();

        public List<SkillData> skills = new();

        private MutanterAttackSystem attackSystem;
        private MutanterAttackSystem AttackSystem => attackSystem ??= GetComponent<MutanterAttackSystem>();

        private WhiteMistController whiteMistController;
        public WhiteMistController WhiteMistControllerInstancce => whiteMistController ??= GetComponent<WhiteMistController>();

        private SkillTriggerManager triggerManager;
        private SkillTriggerManager TriggerManager => triggerManager ??= GetComponent<SkillTriggerManager>();

        private MutanterCombatManager combatManager;
        private MutanterCombatManager CombatManager => combatManager ??= GetComponent<MutanterCombatManager>();
        private SkillEffectorManager effectorManager;
        private SkillEffectorManager EffectorManager => effectorManager ??= GetComponent<SkillEffectorManager>();
        private VFXManager vFXManager;
        private VFXManager VFXManagerInstancce=> vFXManager ??= GetComponent<VFXManager>();
        protected override void OnSpawn()
        {
            base.OnSpawn();

            string mutanterId = gameObject.GetComponent<KPrefabID>().PrefabID().Name;

            skills = MutantersSkillDb.TryGetValue(mutanterId, out var skillData)? skillData: new List<SkillData>();
            // 执行被动触发器
            TriggerManager?.ExecutePassiveTriggers(gameObject, skills);
            EffectorManager?.LoadEffectors(gameObject,skills);
        }

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
        }
        public void AddSkillsToDb(List<SkillData> skills) {
            string mutanterId = gameObject.GetComponent<KPrefabID>().PrefabID().Name;
            if (!MutantersSkillDb.ContainsKey(mutanterId))
            {
                MutantersSkillDb.Add(mutanterId,skills);
            }
        }
        public void AddSkill(SkillData skill)
        {
            skills.Add(skill);
        }
        //
        public bool TryExecuteSkill(string skillName, float damageAmount = 0)
        {
            if (skillName == null || skills.Count == 0)
                return false;
            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i].name == skillName)
                {
                    // 执行技能攻击
                    ExecuteSkill(i, null, damageAmount);
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
        public bool TryExecuteSkill(GameObject target, out SkillData? usedSkill)
        {
            usedSkill = null;
            FaceTarget(target);
            if (target == null || AttackSystem == null || skills.Count == 0) return false;

            // 协调多个触发器，选择最佳技能
            int selectedSkillIndex = CoordinateTriggers(target);

            if (selectedSkillIndex != -1)
            {
                ExecuteSkill(selectedSkillIndex, target);
                usedSkill = skills[selectedSkillIndex];
                return true;
            }

            return false;
        }

        /// <summary>
        /// 协调多个触发器之间的触发逻辑，选择最佳技能
        /// </summary>
        /// <param name="target">目标</param>
        /// <returns>选中的技能索引，-1表示没有选中</returns>
        private int CoordinateTriggers(GameObject target)
        {
            if (TriggerManager != null)
            {
                return TriggerManager.SelectSkill(gameObject, target, skills);
            }

            return -1;
        }

        public bool TryExecuteSkill(GameObject target)
        {
            return TryExecuteSkill(target, out _);
        }

        private void ExecuteSkill(int skillIndex, GameObject target, float damageAmount = 0)
        {
            if (skillIndex < 0 || skillIndex >= skills.Count) return;

            var skill = skills[skillIndex];

            CombatManager?.SetAttacking(true);
            //表现层逻辑
            var animController = gameObject.GetComponent<KBatchedAnimController>();
            EffectorManager?.ApplyEffectorsBefore(skill);
            if (animController != null && !string.IsNullOrEmpty(skill.animation))
            {
                //攻击特效
                var attackVFX = VFXManagerInstancce.GetVFXController(skill.VFXName);
                attackVFX?.Activate();

                void onComplete()
                {
                    if (attackVFX != null)
                    {
                        attackVFX.Deactivate();
                        List<KPrefabID> extralDamageTargets = attackVFX.GetAttackTargets();
                        if (extralDamageTargets.Count > 0)
                        {
                            foreach (var target in extralDamageTargets)
                            {
                                //处理AOE伤害，碰撞判断对象伤害
                                EffectorManager?.ApplyEffectorsAfter(target?.gameObject, skill);
                            }
                        }
                    }
                    else
                    {
                        EffectorManager?.ApplyEffectorsAfter(target,skill);
                    }
                    CombatManager?.SetAttacking(false);
                }

                if (skill.animationDuration > 0f)
                {
                    CombatManager?.PlayAnimation(skill.animation, skill.animationDuration, onComplete);
                }
                else
                {
                    CombatManager?.PlayAnimation(skill.animation, KAnim.PlayMode.Once, onComplete);
                }
            }
            else
            {
                CombatManager?.SetAttacking(false);
            }

            // 更新技能冷却时间
            var updatedSkill = skill;
            updatedSkill.lastUseTime = Time.time;
            if (updatedSkill.isFirstUse)
            {
                updatedSkill.isFirstUse = false;
            }
            skills[skillIndex] = updatedSkill;
        }

    }
}