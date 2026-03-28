using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent.Effector
{
    [SkillEffector("BasicAttackBounsApply", 10)]
    public class BasicAttackBounsApplyEffctor : KMonoBehaviour, ISkillEffector
    {
        public string EffectorName => "BasicAttackBounsApply";

        public int Priority => 10;

        private MutanterAttackSystem attackSystem;
        private MutanterAttackSystem AttackSystem => attackSystem ??= GetComponent<MutanterAttackSystem>();
        protected override void OnSpawn()
        {
            base.OnSpawn();

        }
        protected override void OnCleanUp()
        {
            base.OnCleanUp();
        }

        public bool ApplyEffectorAfter(GameObject target, MutanterSkillComponent.SkillData skillData)
        {
            if (AttackSystem == null) return false;
            var effectorData = skillData.attackEffectors.FirstOrDefault(effector=>effector.attackEffectorName == EffectorName);
            if (effectorData.IsNullOrDestroyed()) return false;
            
            //基础伤害应用
            AttackSystem.TryExecuteAttack(target, effectorData.damageAmount, effectorData.damageType);
            //碰撞伤害应用
            return true;
        }

        public bool ApplyEffectorsBefore()
        {
            return true;
        }
    }
}
