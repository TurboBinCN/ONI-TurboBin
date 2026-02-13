using System;
using System.Collections.Generic;
using System.Linq;
using TBBHe.TbbLib.Debuger;

namespace MutantContainmentProject.Buildings
{
    public enum SecureAction
    {
        None = 0,
        Instinct = 1,
        Reconnaissance = 2,
        Communicate = 3,
        Intimidation = 4
    }
    public class SecureActionTerm
    {
        public string Name { get; set; } // 词条名称，如 "检查生命体征"
        public SecureAction ActionType { get; set; } // 所属行动类型
        public float BaseWeight { get; set; } // 基础权重，用于计算或排序

        // 关联的能力列表。Key: 能力名称 (e.g., "Research", "Medicine")，Value: 该能力对当前词条权重的影响系数
        // 例如，"Research": 1.2f 表示 Research 技能每提升一级，该词条权重增加 20% 或者对成功率有 20% 的贡献
        public Dictionary<string, float> RelatedSkills { get; set; }

        public SecureActionTerm(string name, SecureAction actionType, float baseWeight)
        {
            Name = name;
            ActionType = actionType;
            BaseWeight = baseWeight;
            RelatedSkills = new Dictionary<string, float>();
        }

        /// <summary>
        /// 添加关联技能及其影响系数
        /// </summary>
        /// <param name="skillName">技能名称</param>
        /// <param name="coefficient">影响系数 (e.g., 1.0f for 100%, 0.5f for 50%)</param>
        public void AddRelatedSkill(string skillName, float coefficient)
        {
            RelatedSkills[skillName] = coefficient;
        }

        /// <summary>
        /// 根据小人的技能等级计算当前词条的加权权重或成功率
        /// </summary>
        /// <param name="duplicantSkills">小人的技能字典 (Key: 技能名, Value: 等级)</param>
        /// <returns>计算后的权重或成功率</returns>
        public float CalculateWeight(Dictionary<string, int> duplicantSkills)
        {
            float calculatedWeight = BaseWeight;

            foreach (var skillPair in RelatedSkills)
            {
                string skillName = skillPair.Key;
                float coefficient = skillPair.Value;

                if (duplicantSkills.TryGetValue(skillName, out int skillLevel))
                {
                    // 示例计算方式：基础权重 + (技能等级 * 影响系数)
                    // 也可以采用其他方式，如：基础权重 * (1 + 技能等级 * 影响系数 / 10)
                    calculatedWeight += skillLevel * coefficient;
                }
                // 如果小人没有该技能，其贡献为 0 (不影响 calculatedWeight)
            }

            return Math.Max(0, calculatedWeight); // 确保权重不为负
        }
    }
    public class SecureTermDb
    {
        // 存储所有词条，以词条名称为 Key，便于快速查找
        private Dictionary<string, SecureActionTerm> allTermsByName = new();
        // 按照行动类型分类存储词条名称，便于按类型检索
        private Dictionary<SecureAction, List<string>> termsByActionType = new();
        private static SecureTermDb _instance;
        public static SecureTermDb Instance { get 
            {
                if (_instance == null) new SecureTermDb();
                return _instance; 
            } 
        }
        public SecureTermDb() {
            _instance = this;
            InitializeData();
        }
        public void AddTerm(SecureActionTerm term)
        {
            if (allTermsByName.ContainsKey(term.Name))
            {
                TbbDebuger.LogWarning($"警告: 词条 '{term.Name}' 已存在，将被覆盖。");
            }

            allTermsByName[term.Name] = term;

            if (!termsByActionType.ContainsKey(term.ActionType))
            {
                termsByActionType[term.ActionType] = new List<string>();
            }
            termsByActionType[term.ActionType].Add(term.Name);
        }
        public SecureActionTerm GetTermByName(string name)
        {
            allTermsByName.TryGetValue(name, out SecureActionTerm term);
            return term;
        }
        public List<SecureActionTerm> GetTermsByActionType(SecureAction actionType)
        {
            List<SecureActionTerm> result = new List<SecureActionTerm>();
            if (termsByActionType.TryGetValue(actionType, out List<string> names))
            {
                foreach (var name in names)
                {
                    result.Add(allTermsByName[name]); // 安全起见，再次检查是否存在
                }
            }
            return result;
        }
        /// <summary>
        /// 根据行动类型和小人的技能，随机选择一个词条
        /// </summary>
        /// <param name="actionType">行动类型</param>
        /// <param name="duplicantSkills">小人的技能字典</param>
        /// <returns>选中的词条，如果该类型下没有词条则返回 null</returns>
        public SecureActionTerm SelectRandomTermByActionType(SecureAction actionType)
        {
            var availableTerms = GetTermsByActionType(actionType);
            if (availableTerms.Count == 0)
            {
                return null; // 该类型下没有词条
            }

            // 计算每个词条的加权权重
            var weightedTerms = availableTerms.Select(term => new { Term = term, Weight = term.BaseWeight }).ToList();

            // 使用加权随机选择算法
            float totalWeight = weightedTerms.Sum(x => x.Weight);
            if (totalWeight <= 0)
            {
                // 如果所有词条计算后权重都为 0 或负数，可以选择一个默认策略，比如随机选择或返回第一个
                TbbDebuger.LogWarning($"警告: 在 {actionType} 类型下，所有词条对当前小人计算出的总权重为 0 或负数。");
                return availableTerms[UnityEngine.Random.Range(0, availableTerms.Count)]; // 随机选择一个
                                                                                          // return availableTerms[0]; // 或者返回第一个
            }

            float randomPoint = UnityEngine.Random.Range(0, totalWeight);

            float currentWeight = 0;
            foreach (var item in weightedTerms)
            {
                currentWeight += item.Weight;
                if (randomPoint <= currentWeight)
                {
                    return item.Term;
                }
            }

            // Should not happen if weights are positive, but just in case.
            return weightedTerms.LastOrDefault()?.Term;
        }
        public void InitializeData()
        {
            // Instinct Actions
            foreach (var field in typeof(STRINGS.SECURE_ACTION.ACTION_INSTINCT).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            {
                if (field.FieldType == typeof(LocString))
                {
                    var locString = (LocString)field.GetValue(null);
                    AddTerm(new SecureActionTerm(locString, SecureAction.Instinct, 1.0f)); // Assuming base weight of 1.0
                }
            }

            // Reconnaissance Actions
            foreach (var field in typeof(STRINGS.SECURE_ACTION.ACTION_RECONNAISSANCE).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            {
                if (field.FieldType == typeof(LocString))
                {
                    var locString = (LocString)field.GetValue(null);
                    AddTerm(new SecureActionTerm(locString, SecureAction.Reconnaissance, 1.0f));
                }
            }

            // Communicate Actions
            foreach (var field in typeof(STRINGS.SECURE_ACTION.ACTION_COMMUNICATE).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            {
                if (field.FieldType == typeof(LocString))
                {
                    var locString = (LocString)field.GetValue(null);
                    AddTerm(new SecureActionTerm(locString, SecureAction.Communicate, 1.0f));
                }
            }

            // Intimidation Actions
            foreach (var field in typeof(STRINGS.SECURE_ACTION.ACTION_INTIMIDATION).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            {
                if (field.FieldType == typeof(LocString))
                {
                    var locString = (LocString)field.GetValue(null);
                    AddTerm(new SecureActionTerm(locString, SecureAction.Intimidation, 1.0f));
                }
            }
        }
    }
}
