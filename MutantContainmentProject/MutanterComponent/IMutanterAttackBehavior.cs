using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    public interface IMutanterAttackBehavior
    {
        /// <summary>
        /// 执行攻击行为
        /// </summary>
        /// <param name="attacker">攻击者实例</param>
        /// <param name="target">攻击目标</param>
        /// <returns>是否成功执行</returns>
        bool Execute(IStateMachineTarget attacker, GameObject target);

        /// <summary>
        /// 获取该攻击行为的标签，用于区分不同类型的行为
        /// </param>
        Tag GetTag();

        /// <summary>
        /// 获取冷却时间
        /// </param>
        float GetCooldown();

        /// <summary>
        /// 检查是否可以执行此行为
        /// </param>
        bool CanExecute(IStateMachineTarget attacker, GameObject target);
    }
}
