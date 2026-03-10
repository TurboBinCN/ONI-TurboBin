using Klei.AI;
using MutantContainmentProject.MutanterEffect;
using MutantContainmentProject.Skills;
using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    public class MutanterAttackSystem : KMonoBehaviour
    {
        private List<IMutanterAttackBehavior> _availableBehaviors = new List<IMutanterAttackBehavior>();
        private Dictionary<IMutanterAttackBehavior, float> _behaviorLastExecutionTimes = new Dictionary<IMutanterAttackBehavior, float>();

        private Effects _effects;
        private EmotionMonitor.StatesInstance _emotionMonitorSMI;

        private bool _isContained = false;
        public bool IsContained { get => _isContained; }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            _effects = GetComponent<Effects>();
            _emotionMonitorSMI = gameObject.GetSMI<EmotionMonitor.StatesInstance>();
            InitializeBehaviors();

            // 初始化时检查当前的收容状态
            if (_effects != null && _effects.HasEffect(MutanterEffects.MUTANTER_CONTAINED_EFFECT))
            {
                _isContained = true;
                TbbDebuger.LogDebug($"[MutanterAttackSystem] {gameObject.name} initialized with containment effect, IsContained = true");
            }

            // 使用Klei原生事件系统订阅事件
            Subscribe((int)MutanterGameHashes.MutanterContained, OnContained);
            Subscribe((int)MutanterGameHashes.MutanterBreachContained, OnBreachContained);
        }

        protected override void OnCleanUp()
        {
            // 使用Klei原生事件系统取消订阅
            Unsubscribe((int)MutanterGameHashes.MutanterContained, OnContained);
            Unsubscribe((int)MutanterGameHashes.MutanterBreachContained, OnBreachContained);
            base.OnCleanUp();
        }

        private void OnContained(object data)
        {
            GameObject mutanterObj = data as GameObject;
            if (mutanterObj == gameObject)
            {
                _isContained = true;
                TbbDebuger.LogDebug($"[MutanterAttackSystem] {gameObject.name} received MutanterContained event");
            }
        }

        private void OnBreachContained(object data)
        {
            GameObject mutanterObj = data as GameObject;
            if (mutanterObj == gameObject)
            {
                _isContained = false;
                TbbDebuger.LogDebug($"[MutanterAttackSystem] {gameObject.name} received MutanterBreachContained event");
            }
        }

        /// <summary>
        /// 初始化攻击行为列表
        /// </summary>
        private void InitializeBehaviors()
        {
            _availableBehaviors.Clear();
            _behaviorLastExecutionTimes.Clear();

            // 获取 KPrefabID 来检查标签
            var kPrefabID = GetComponent<KPrefabID>();
            if (kPrefabID != null)
            {
                //物理攻击
                if (kPrefabID.HasTag(MutanterTags.PhysicalAttack))
                    _availableBehaviors.Add(new MeleeAttack());
                // 心理攻击
                if (kPrefabID.HasTag(MutanterTags.PsychologicalAttack))
                {
                    _availableBehaviors.Add(new PsychologicalAttack());
                }

                // 侵蚀攻击
                if (kPrefabID.HasTag(MutanterTags.ErosionAttack))
                {
                    _availableBehaviors.Add(new ErosionAttack());
                }

                // 灵魂攻击
                if (kPrefabID.HasTag(MutanterTags.SoulAttack))
                {
                    _availableBehaviors.Add(new SoulAttack());
                }
            }
            //默认添加物理攻击标签
            //if(_availableBehaviors.Count == 0) _availableBehaviors.Add(new MeleeAttack());
        }

        /// <summary>
        /// 尝试执行攻击行为，考虑效果状态
        /// </summary>
        /// <param name="target">攻击目标</param>
        /// <returns>是否成功执行攻击</returns>
        public bool TryExecuteAttack(GameObject target)
        {
            if (target == null)
                return false;

            // 检查攻击限制
            if (!CanExecuteAnyAttack())
            {
                return false;
            }

            // 获取理智值
            float insanityValue = _emotionMonitorSMI?.INSANITYValue ?? 100f;

            // 执行攻击
            return ExecuteAttackInternal(target, insanityValue);
        }

        /// <summary>
        /// 尝试执行多目标攻击，考虑效果状态
        /// </summary>
        /// <param name="targets">目标列表</param>
        /// <returns>是否成功执行攻击</returns>
        public bool TryExecuteAttack(List<KPrefabID> targets)
        {
            if (targets == null || targets.Count == 0)
                return false;

            // 检查攻击限制
            if (!CanExecuteAnyAttack())
            {
                return false;
            }

            // 获取理智值
            float insanityValue = _emotionMonitorSMI?.INSANITYValue ?? 100f;

            // 执行多目标攻击
            bool success = false;
            foreach (var target in targets)
            {
                if (target != null && target.gameObject != null)
                {
                    if (ExecuteAttackInternal(target.gameObject, insanityValue))
                    {
                        success = true;
                    }
                }
            }
            return success;
        }

        /// <summary>
        /// 检查是否可以执行任何攻击
        /// </summary>
        /// <returns>是否可以执行攻击</returns>
        private bool CanExecuteAnyAttack()
        {
            // 检查是否有收容效果，如果有则限制攻击
            if (_effects != null && _effects.HasEffect(MutanterEffects.MUTANTER_CONTAINED_EFFECT))
            {
                TbbDebuger.LogDebug("[MutanterAttackSystem] Attack restricted due to containment effect");
                return false;
            }

            // 检查是否有攻击限制效果
            if (_effects != null && _effects.HasEffect(MutanterEffects.MUTANTER_ATTACK_RESTRICTED_EFFECT))
            {
                TbbDebuger.LogDebug("[MutanterAttackSystem] Attack restricted due to attack restricted effect");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 内部执行攻击逻辑
        /// </summary>
        /// <param name="target">攻击目标</param>
        /// <param name="insanityValue">理智值</param>
        /// <returns>是否成功执行攻击</returns>
        private bool ExecuteAttackInternal(GameObject target, float insanityValue)
        {
            // 选择行为的逻辑
            IMutanterAttackBehavior selectedBehavior = SelectBehavior(target, insanityValue);

            if (selectedBehavior != null)
            {
                // 评估效果影响
                float effectImpact = EvaluateEffectImpact(selectedBehavior);
                bool success = selectedBehavior.Execute(this, target, effectImpact);
                if (success)
                {
                    _behaviorLastExecutionTimes[selectedBehavior] = Time.time; // 更新执行时间
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 评估效果对攻击行为的影响
        /// </summary>
        /// <param name="behavior">攻击行为</param>
        /// <returns>影响因子 (0-1)</returns>
        private float EvaluateEffectImpact(IMutanterAttackBehavior behavior)
        {
            float impact = 1.0f;

            if (_effects == null)
                return impact;

            // 攻击增强效果：提高攻击效果
            if (_effects.HasEffect(MutanterEffects.MUTANTER_ATTACK_ENHANCED_EFFECT))
            {
                impact *= 1.5f;
            }

            // 意志效果：小幅提高攻击效果
            if (_effects.HasEffect(MutanterEffects.MUTANTER_WILLED_EFFECT))
            {
                impact *= 1.2f;
            }

            return Mathf.Clamp01(impact);
        }

        /// <summary>
        /// 评估攻击能力，根据当前效果状态
        /// </summary>
        /// <returns>攻击能力评估值 (0-1)</returns>
        public float EvaluateAttackCapability()
        {
            float capability = 1.0f;

            if (_effects == null)
                return capability;

            // 收容效果：大幅降低攻击能力
            if (_effects.HasEffect(MutanterEffects.MUTANTER_CONTAINED_EFFECT))
            {
                capability *= 0.1f;
            }

            // 攻击限制效果：降低攻击能力
            if (_effects.HasEffect(MutanterEffects.MUTANTER_ATTACK_RESTRICTED_EFFECT))
            {
                capability *= 0.3f;
            }

            // 攻击增强效果：提高攻击能力
            if (_effects.HasEffect(MutanterEffects.MUTANTER_ATTACK_ENHANCED_EFFECT))
            {
                capability *= 1.5f;
            }

            // 意志效果：小幅提高攻击能力
            if (_effects.HasEffect(MutanterEffects.MUTANTER_WILLED_EFFECT))
            {
                capability *= 1.2f;
            }

            return Mathf.Clamp01(capability);
        }

        /// <summary>
        /// 检查是否可以执行特定类型的攻击
        /// </summary>
        /// <param name="attackType">攻击类型</param>
        /// <returns>是否可以执行</returns>
        public bool CanExecuteAttack(string attackType)
        {
            return CanExecuteAnyAttack();
        }

        /// <summary>
        /// 检查是否免疫即死攻击
        /// </summary>
        /// <returns>是否免疫即死攻击</returns>
        public bool IsImmuneToInstantKill()
        {
            // 已收容效果免疫即死攻击
            if (_effects != null && _effects.HasEffect(MutanterEffects.MUTANTER_CONTAINED_EFFECT))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 根据当前状态、目标和可用行为，选择一个要执行的行为
        /// </summary>
        /// <param name="target">当前攻击目标</param>
        /// <param name="insanityValue">理智值，用于决定攻击类型</param>
        /// <returns>选中的攻击行为，如果无合适行为则返回 null</returns>
        private IMutanterAttackBehavior SelectBehavior(GameObject target, float insanityValue)
        {
            List<IMutanterAttackBehavior> candidates = new List<IMutanterAttackBehavior>();

            foreach (var behavior in _availableBehaviors)
            {
                // 检查冷却时间
                if (_behaviorLastExecutionTimes.TryGetValue(behavior, out float lastTime))
                {
                    if (Time.time - lastTime < behavior.GetCooldown())
                    {
                        continue; // 跳过仍在冷却中的行为
                    }
                }

                // 检查行为自身条件
                if (behavior.CanExecute(this, target))
                {
                    candidates.Add(behavior);
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            // 根据理智值选择攻击类型
            IMutanterAttackBehavior selectedBehavior = null;

            if (insanityValue < 20f)
            {
                // 理智值低，使用物理攻击
                selectedBehavior = candidates.Find(b => b is MeleeAttack);
            }
            else if (insanityValue < 40f)
            {
                // 理智值较低，使用心理攻击
                selectedBehavior = candidates.Find(b => b is PsychologicalAttack);
            }
            else if (insanityValue < 60f)
            {
                // 理智值中等，使用侵蚀攻击
                selectedBehavior = candidates.Find(b => b is ErosionAttack);
            }
            else
            {
                // 理智值高，使用灵魂攻击
                selectedBehavior = candidates.Find(b => b is SoulAttack);
            }

            // 如果没有找到对应类型的攻击，随机选择一个
            if (selectedBehavior == null)
            {
                int randomIndex = UnityEngine.Random.Range(0, candidates.Count);
                selectedBehavior = candidates[randomIndex];
            }

            return selectedBehavior;
        }

        /// <summary>
        /// 添加一个新的攻击行为到可用列表中
        /// </summary>
        /// <param name="behavior">要添加的行为实例</param>
        public void AddBehavior(IMutanterAttackBehavior behavior)
        {
            if (!_availableBehaviors.Contains(behavior))
            {
                _availableBehaviors.Add(behavior);
                TbbDebuger.LogDebug($"[MutanterAttackSystem] Added behavior: {behavior.GetType().Name}");
            }
        }

        /// <summary>
        /// 移除一个攻击行为
        /// </summary>
        /// <param name="behavior">要移除的行为实例</param>
        public void RemoveBehavior(IMutanterAttackBehavior behavior)
        {
            if (_availableBehaviors.Remove(behavior))
            {
                _behaviorLastExecutionTimes.Remove(behavior); // 同步移除其冷却记录
                TbbDebuger.LogDebug($"[MutanterAttackSystem] Removed behavior: {behavior.GetType().Name}");
            }
        }

        // ==================== 攻击效果执行方法 ====================

        /// <summary>
        /// 执行生命值攻击
        /// </summary>
        /// <param name="target">攻击目标</param>
        /// <param name="damage">伤害值</param>
        /// <returns>是否成功执行</returns>
        public bool ExecuteHealthAttack(GameObject target, float damage)
        {
            if (!CanExecuteAnyAttack())
                return false;

            if (target == null)
                return false;

            var health = target.GetComponent<Health>();
            if (health != null)
            {
                // 计算物理防御影响
                float physicalDefenseFactor = 1f;
                var attributes = target.GetAttributes();
                if (attributes != null)
                {
                    var defenseAttribute = attributes.Get(MutanterAttributes.AttributeDefenseID);
                    if (defenseAttribute != null)
                    {
                        // 获取物理防御转换器
                        var physicalDefenseConverter = Db.Get().AttributeConverters.Get(MutanterAttributeConverters.AttributePhysicalDefenseConverterID);
                        if (physicalDefenseConverter != null)
                        {
                            var converterInstance = physicalDefenseConverter.Lookup(target);
                            if (converterInstance != null)
                            {
                                float physicalDefenseValue = converterInstance.Evaluate();
                                physicalDefenseFactor = Mathf.Max(0.1f, 1f - physicalDefenseValue);
                            }
                        }
                    }
                }

                float effectiveDamage = damage * physicalDefenseFactor;
                health.Damage(effectiveDamage);
                TbbDebuger.LogDebug($"[MutanterAttackSystem] Health attack: {target.name} took {effectiveDamage} damage (original: {damage}, defense factor: {physicalDefenseFactor})");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 执行压力值攻击
        /// </summary>
        /// <param name="target">攻击目标</param>
        /// <param name="stressAmount">压力值增加量</param>
        /// <returns>是否成功执行</returns>
        public bool ExecuteStressAttack(GameObject target, float stressAmount)
        {
            if (!CanExecuteAnyAttack())
                return false;

            if (target == null)
                return false;

            var amounts = target.GetAmounts();
            if (amounts != null)
            {
                var stressAmountComp = amounts.Get(Db.Get().Amounts.Stress);
                if (stressAmountComp != null)
                {
                    // 计算精神防御影响
                    float mentalDefenseFactor = 1f;
                    var attributes = target.GetAttributes();
                    if (attributes != null)
                    {
                        var defenseAttribute = attributes.Get(MutanterAttributes.AttributeDefenseID);
                        if (defenseAttribute != null)
                        {
                            // 获取精神防御转换器
                            var mentalDefenseConverter = Db.Get().AttributeConverters.Get(MutanterAttributeConverters.AttributeMentalDefenseConverterID);
                            if (mentalDefenseConverter != null)
                            {
                                var converterInstance = mentalDefenseConverter.Lookup(target);
                                if (converterInstance != null)
                                {
                                    float mentalDefenseValue = converterInstance.Evaluate();
                                    mentalDefenseFactor = Mathf.Max(0.1f, 1f - mentalDefenseValue);
                                }
                            }
                        }
                    }

                    float effectiveStressAmount = stressAmount * mentalDefenseFactor;
                    stressAmountComp.value = Mathf.Min(stressAmountComp.value + effectiveStressAmount, 100f);
                    TbbDebuger.LogDebug($"[MutanterAttackSystem] Stress attack: {target.name} stress increased to {stressAmountComp.value}%, effective amount: {effectiveStressAmount}");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 执行效果攻击
        /// </summary>
        /// <param name="target">攻击目标</param>
        /// <param name="effectId">效果ID</param>
        /// <param name="duration">效果持续时间</param>
        /// <returns>是否成功执行</returns>
        public bool ExecuteEffectAttack(GameObject target, string effectId)
        {
            if (!CanExecuteAnyAttack())
                return false;

            if (target == null)
                return false;

            var effects = target.GetComponent<Effects>();
            if (effects != null)
            {
                effects.Add(effectId, true);
                TbbDebuger.LogDebug($"[MutanterAttackSystem] Effect attack: {target.name} received effect {effectId}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 执行综合攻击（同时影响生命值和压力值）
        /// </summary>
        /// <param name="target">攻击目标</param>
        /// <param name="damage">伤害值</param>
        /// <param name="stressAmount">压力值增加量</param>
        /// <returns>是否成功执行</returns>
        public bool ExecuteCombinedAttack(GameObject target, float damage, float stressAmount)
        {
            if (!CanExecuteAnyAttack())
                return false;

            bool healthSuccess = ExecuteHealthAttack(target, damage);
            bool stressSuccess = ExecuteStressAttack(target, stressAmount);
            return healthSuccess || stressSuccess;
        }
    }
}
