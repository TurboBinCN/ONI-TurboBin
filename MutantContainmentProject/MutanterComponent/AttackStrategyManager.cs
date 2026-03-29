using System.Collections.Generic;
using System.Linq;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    public interface IAttackStrategy
    {
        bool CanExecute(GameObject target);
        bool Execute(GameObject target);
        float GetPriority(GameObject target);
    }

    public class SkillAttackStrategy : IAttackStrategy
    {
        private MutanterSkillComponent skillComponent;
        private MutanterCombatManager combatManager;
        private float priority;

        public SkillAttackStrategy(MutanterSkillComponent skillComponent, MutanterCombatManager combatManager, float priority = 2.0f)
        {
            this.skillComponent = skillComponent;
            this.combatManager = combatManager;
            this.priority = priority;
        }

        public bool CanExecute(GameObject target)
        {
            if (skillComponent == null) return false;
            
            if (skillComponent.skills == null) return false;
            
            if (skillComponent.skills.Count == 0) return false;

            // 检查是否有冷却完成的技能
            foreach (var skill in skillComponent.skills)
            {
                if (!skill.isPassiveSkill)
                {
                    float timeSinceLastUse = Time.time - skill.lastUseTime;
                    if (timeSinceLastUse >= skill.cooldown) return true;
                }
            }
            return false;
        }

        public bool Execute(GameObject target)
        {
            return skillComponent.TryExecuteSkill(target);
        }

        public float GetPriority(GameObject target)
        {
            return priority;
        }
    }

    public class BasicAttackStrategy : IAttackStrategy
    {
        private MutanterAttackSystem attackSystem;
        private float priority;

        public BasicAttackStrategy(MutanterAttackSystem attackSystem, float priority = 1.0f)
        {
            this.attackSystem = attackSystem;
            this.priority = priority;
        }

        public bool CanExecute(GameObject target)
        {
            if (attackSystem == null)
            {
                return false;
            }
            
            // 尝试执行攻击，看看是否有可用的攻击行为
            // 注意：这里只是检查，不会实际执行攻击
            // 由于MutanterAttackSystem没有提供直接检查的方法，我们需要通过尝试执行来判断
            // 但为了避免实际执行攻击，我们可以创建一个临时的目标检查
            // 或者直接检查攻击系统是否有可用的攻击行为
            // 这里简化处理，假设如果攻击系统存在，就认为可以执行基础攻击
            // 实际的冷却检查会在MutanterAttackSystem内部进行
            return true;
        }

        public bool Execute(GameObject target)
        {
            return attackSystem.TryExecuteAttack(target);
        }

        public float GetPriority(GameObject target)
        {
            return priority;
        }
    }

    public class AttackStrategyManager : KMonoBehaviour
    {
        public enum StrategyType
        {
            SkillAttack,
            BasicAttack
        }
        
        private List<IAttackStrategy> strategies = new();
        private MutanterCombatManager combatManager;
        private MutanterSkillComponent skillComponent;
        private MutanterAttackSystem attackSystem;
        
        // 使用Unity可序列化的字段存储策略配置
        [SerializeField]
        private float skillAttackPriority = 2.0f;
        
        [SerializeField]
        private float basicAttackPriority = 1.0f;
        
        [SerializeField]
        private bool skillAttackEnabled = true;
        
        [SerializeField]
        private bool basicAttackEnabled = true;

        public void SetStrategyPriority(StrategyType strategyType, float priority)
        {
            switch (strategyType)
            {
                case StrategyType.SkillAttack:
                    skillAttackPriority = priority;
                    break;
                case StrategyType.BasicAttack:
                    basicAttackPriority = priority;
                    break;
            }
        }
        
        public void SetStrategyEnabled(StrategyType strategyType, bool enabled)
        {
            switch (strategyType)
            {
                case StrategyType.SkillAttack:
                    skillAttackEnabled = enabled;
                    break;
                case StrategyType.BasicAttack:
                    basicAttackEnabled = enabled;
                    break;
            }
            // 重新初始化策略列表，确保启用状态的变化被反映
            Initialize();
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            // 当组件被实例化时，重新初始化策略列表，确保使用正确的策略配置
            Initialize();
        }

        public void Initialize()
        {
            combatManager = GetComponent<MutanterCombatManager>();
            skillComponent = GetComponent<MutanterSkillComponent>();
            attackSystem = GetComponent<MutanterAttackSystem>();

            // 清空现有策略
            strategies.Clear();

            TbbDebuger.LogDebug($"[AttackStrategyManager] 初始化策略，技能攻击启用: {skillAttackEnabled}，基础攻击启用: {basicAttackEnabled}");

            // 注册策略
            if (skillAttackEnabled && skillComponent != null)
            {
                // 即使skills为空，也注册技能攻击策略，因为技能可能在稍后被添加
                strategies.Add(new SkillAttackStrategy(skillComponent, combatManager, skillAttackPriority));
                TbbDebuger.LogDebug($"[AttackStrategyManager] 注册技能攻击策略，优先级: {skillAttackPriority}");
            }

            if (basicAttackEnabled && attackSystem != null)
            {
                strategies.Add(new BasicAttackStrategy(attackSystem, basicAttackPriority));
                TbbDebuger.LogDebug($"[AttackStrategyManager] 注册基础攻击策略，优先级: {basicAttackPriority}");
            }
        }

        public bool ExecuteAttack(GameObject target)
        {
            // 按优先级排序策略
            var sortedStrategies = strategies
                .Where(s => s.CanExecute(target))
                .OrderByDescending(s => s.GetPriority(target))
                .ToList();
            TbbDebuger.LogDebug($"[AttackStrategyManager] strategies：{strategies.Count} 执行攻击策略: {string.Join(", ", sortedStrategies.Select(s => s.GetType().Name))}");
            foreach (var strategy in sortedStrategies)
            {
                if (strategy.Execute(target))
                {
                    return true;
                }
            }

            return false;
        }
        
        public bool HasAnyAvailableStrategy(GameObject target)
        {
            // 检查是否有任何可用的攻击策略
            foreach (var strategy in strategies)
            {
                if (strategy.CanExecute(target))
                {
                    return true;
                }
            }
            return false;
        }
    }
}