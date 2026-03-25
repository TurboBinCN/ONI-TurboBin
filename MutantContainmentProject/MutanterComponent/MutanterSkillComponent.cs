using System;
using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    public class MutanterSkillComponent : KMonoBehaviour
    {
        // 额外动画效果接口
        public interface IExtraAnimationEffect
        {
            void Activate();
            void Deactivate();
            List<KPrefabID> GetAttackTargets();
        }

        public struct SkillData
        {
            public string name;
            public Tag damageType;
            public bool isPassiveSkill;
            public float damage;
            public int range;
            public float cooldown;
            public string animation;
            public float animationDuration;
            public float lastUseTime;
            public bool isFirstUse;
            public string extraAnimationEffectId; // 额外动画效果ID
        }

        [SerializeField]
        public List<SkillData> skills = new();
        private MutanterAttackSystem attackSystem;

        // 静态技能数据库
        private static Dictionary<string, List<SkillData>> _mutanterSkillsDatabase = new();

        private LaserBeamController laserBeamController;
        public LaserBeamController LaserBeamController => laserBeamController ??= GetComponent<LaserBeamController>();

        private WhiteMistController whiteMistController;
        public WhiteMistController WhiteMistControllerInstancce => whiteMistController ??= GetComponent<WhiteMistController>();

        // 效果组件映射
        private Dictionary<string, IExtraAnimationEffect> effectComponents = new();

        private static Dictionary<Type, Type> EffectsCom = new();
        protected override void OnSpawn()
        {
            base.OnSpawn();
            attackSystem = GetComponent<MutanterAttackSystem>();

            string mutanterId = gameObject.GetComponent<KPrefabID>().PrefabID().Name;
            if (_mutanterSkillsDatabase.ContainsKey(mutanterId))
            {
                skills = new List<SkillData>(_mutanterSkillsDatabase[mutanterId]);
            }
            InitializeEffects();
        }

        private void InitializeEffects()
        {
            foreach (var kvp in EffectsCom)
            {
                try
                {
                    var effectController = gameObject.GetComponent(kvp.Key) ?? gameObject.AddComponent(kvp.Key);
                    if (effectController != null)
                    {
                        var effect = (IExtraAnimationEffect)Activator.CreateInstance(kvp.Value, new object[] { effectController });
                        string key = kvp.Key.Name;
                        effectComponents[key] = effect;
                    }
                }
                catch (Exception e)
                {
                    TbbDebuger.LogError($"初始化效果时出错: {e.Message}\n{e.StackTrace}");
                }
            }
        }

        public void RegisterEffectComponents<TEffectController, TAnimationEffect>()
        where TEffectController : KMonoBehaviour
        where TAnimationEffect : IExtraAnimationEffect
        {
            if (!EffectsCom.ContainsKey(typeof(TEffectController)))
                EffectsCom.Add(typeof(TEffectController), typeof(TAnimationEffect));
        }
        private IExtraAnimationEffect GetExtraAnimationEffect(string effectId)
        {
            if (effectId != null && effectComponents.TryGetValue(effectId, out var effect))
            {
                return effect;
            }
            return null;
        }

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
        }

        // 添加技能到数据库
        public static void AddSkillsToDatabase(string mutanterId, List<SkillData> skills)
        {
            _mutanterSkillsDatabase[mutanterId] = new List<SkillData>(skills);
        }

        // 添加单个技能
        public void AddSkill(SkillData skill)
        {
            skills.Add(skill);
        }
        public bool TryExecuteSkill(string extraAnimationEffectId, float damageAmount = 0, bool playAnimation = true)
        {
            if (extraAnimationEffectId == null || skills.Count == 0)
                return false;
            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i].extraAnimationEffectId == extraAnimationEffectId)
                {
                    // 执行技能攻击
                    ExecuteSkill(i, null, damageAmount, playAnimation);
                    return true;
                }
            }
            return false;
        }
        public bool TryExecuteSkill(GameObject target, out SkillData? usedSkill)
        {
            usedSkill = null;
            if (target == null || attackSystem == null || skills.Count == 0)
                return false;

            // 检查生命值，确保只有在生命值大于0时才尝试执行技能
            var health = gameObject.GetComponent<Health>();
            if (health != null && health.hitPoints <= 0f)
            {
                return false;
            }

            // 计算距离
            int targetCell = Grid.PosToCell(target.transform.position);
            int currentCell = Grid.PosToCell(gameObject.transform.position);
            float distance = Mathf.Abs(Grid.CellToPos2D(targetCell).x - Grid.CellToPos2D(currentCell).x);
            //TbbDebuger.LogDebug($"距离: {distance} 总技能数量:[{skills.Count}]");
            // 选择合适的技能
            int selectedSkillIndex = -1;

            //distance = 10;//调试用，不要修改
            // 筛选非被动技能
            var activeSkills = new List<SkillData>();
            for (int i = 0; i < skills.Count; i++)
            {
                if (!skills[i].isPassiveSkill)
                {
                    activeSkills.Add(skills[i]);
                }
            }

            // 按距离和伤害排序技能
            for (int i = 0; i < activeSkills.Count; i++)
            {
                var skill = activeSkills[i];
                if (distance <= skill.range && Time.time - skill.lastUseTime >= skill.cooldown)
                {
                    // 找到原始技能索引
                    int originalIndex = skills.FindIndex(s => s.name == skill.name && s.damage == skill.damage);
                    if (selectedSkillIndex == -1 || skill.damage > skills[selectedSkillIndex].damage)
                    {
                        selectedSkillIndex = originalIndex;
                    }
                }
            }
            //TbbDebuger.LogDebug($"选择技能索引: {selectedSkillIndex} 技能名称：[{skills[selectedSkillIndex].name}]");
            if (selectedSkillIndex != -1)
            {

                ExecuteSkill(selectedSkillIndex, target);
                usedSkill = skills[selectedSkillIndex];
                return true;
            }

            return false;
        }

        public bool TryExecuteSkill(GameObject target)
        {
            return TryExecuteSkill(target, out _);
        }

        private SkillData? GetAvailableSkill(string skillName, int distance)
        {
            foreach (var skill in skills)
            {
                if (skill.name == skillName && distance <= skill.range && Time.time - skill.lastUseTime >= skill.cooldown)
                {
                    return skill;
                }
            }
            return null;
        }

        private void ExecuteSkill(int skillIndex, GameObject target, float damageAmount = 0, bool playAnimation = true)
        {
            // 检查生命值，确保只有在生命值大于0时才执行技能
            var health = gameObject.GetComponent<Health>();
            if (health != null && health.hitPoints <= 0f)
            {
                return;
            }

            if (skillIndex < 0 || skillIndex >= skills.Count)
                return;

            var skill = skills[skillIndex];

            var combatManager = gameObject.GetComponent<MutanterCombatManager>();
            combatManager?.SetAttacking(true);

            float damage = damageAmount == 0 ? skill.damage : damageAmount;
            if (target != null)
            {
                attackSystem.TryExecuteAttack(target, damage, skill.damageType);
            }
            else
            {
                //没有Target的情况分两种，一种是被动技能，一种是主动技能，主动技能没有目标，说明出错了
                var extraEffect = GetExtraAnimationEffect(skill.extraAnimationEffectId);

                if (extraEffect != null)
                {
                    List<KPrefabID> extralDamageTargets = extraEffect.GetAttackTargets();
                    if (extralDamageTargets.Count > 0)
                    {
                        foreach (var kPrefabID in extralDamageTargets)
                        {
                            attackSystem.TryExecuteAttack(kPrefabID.gameObject, damage, skill.damageType);
                        }
                    }
                }
            }
            if (playAnimation)
            {
                // 播放动画
                var animController = gameObject.GetComponent<KBatchedAnimController>();
                if (animController != null && !string.IsNullOrEmpty(skill.animation))
                {
                    // 获取额外动画效果
                    var extraEffect = GetExtraAnimationEffect(skill.extraAnimationEffectId);
                    // 激活额外动画效果
                    if (extraEffect != null)
                    {
                        extraEffect.Activate();
                    }


                    System.Action onComplete = null;
                    onComplete = () =>
                    {
                        // 停用额外动画效果
                        if (extraEffect != null)
                        {
                            extraEffect.Deactivate();
                            List<KPrefabID> extralDamageTargets = extraEffect.GetAttackTargets();
                            if (extralDamageTargets.Count > 0)
                            {
                                // 处理额外伤害目标
                                foreach (var target in extralDamageTargets)
                                {
                                    attackSystem.TryExecuteAttack(target.gameObject, damage, skill.damageType);
                                }
                            }
                        }
                        combatManager?.SetAttacking(false);
                    };
                    if (skill.animationDuration > 0f)
                    {
                        combatManager?.PlayAnimation(skill.animation, skill.animationDuration, onComplete);
                    }
                    else
                    {
                        combatManager?.PlayAnimation(skill.animation, KAnim.PlayMode.Once, onComplete);
                    }
                }
                else
                {
                    combatManager?.SetAttacking(false);
                }
            }

            // 更新技能冷却时间
            var updatedSkill = skill;
            updatedSkill.lastUseTime = Time.time;
            if (updatedSkill.isFirstUse)
            {
                updatedSkill.isFirstUse = false;
            }
            skills[skillIndex] = updatedSkill;

            TbbDebuger.LogDebug($"[MutanterSkillComponent] 执行攻击: {skill.name} 伤害: {skill.damage}, 攻击属性: {skill.damageType}");
        }

    }
}