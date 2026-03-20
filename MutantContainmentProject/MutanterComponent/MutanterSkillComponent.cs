using System.Collections.Generic;
using System.Linq;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    public class MutanterSkillComponent : KMonoBehaviour
    {
        public struct SkillData
        {
            public string name;
            public Tag damageType;
            public float damage;
            public int range;
            public float cooldown;
            public string animation;
            public float lastUseTime;
            public bool isFirstUse;
        }

        [SerializeField]
        public List<SkillData> skills = new List<SkillData>();
        private MutanterAttackSystem attackSystem;
        private Navigator navigator;

        // 静态技能数据库
        private static Dictionary<string, List<SkillData>> _mutanterSkillsDatabase = new Dictionary<string, List<SkillData>>();

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            attackSystem = GetComponent<MutanterAttackSystem>();
            navigator = GetComponent<Navigator>();
            
            // 尝试从数据库加载技能
            string mutanterId = gameObject.GetComponent<KPrefabID>().PrefabID().Name;
            if (_mutanterSkillsDatabase.ContainsKey(mutanterId))
            {
                skills = new List<SkillData>(_mutanterSkillsDatabase[mutanterId]);
            }
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

        public bool TryExecuteSkill(GameObject target, out SkillData? usedSkill)
        {
            usedSkill = null;
            if (target == null || attackSystem == null || skills.Count == 0)
                return false;

            // 计算距离
            int targetCell = Grid.PosToCell(target.transform.position);
            int currentCell = Grid.PosToCell(gameObject.transform.position);
            float distance = Mathf.Abs( Grid.CellToPos2D(targetCell).x - Grid.CellToPos2D(currentCell).x);
            TbbDebuger.LogDebug($"距离: {distance}");
            // 选择合适的技能
            int selectedSkillIndex = -1;

            // 按距离和伤害排序技能
            for (int i = 0; i < skills.Count; i++)
            {
                var skill = skills[i];
                if (distance <= skill.range && Time.time - skill.lastUseTime >= skill.cooldown)
                {
                    if (selectedSkillIndex == -1 || skills[i].damage > skills[selectedSkillIndex].damage)
                    {
                        selectedSkillIndex = i;
                    }
                }
            }

            TbbDebuger.LogDebug($"选择技能索引: {selectedSkillIndex}");
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

        private void ExecuteSkill(int skillIndex, GameObject target)
        {
            if (skillIndex < 0 || skillIndex >= skills.Count)
                return;

            var skill = skills[skillIndex];
            
            // 执行攻击
            attackSystem.TryExecuteAttack(target, skill.damage, skill.damageType);

            // 播放动画
            var animController = gameObject.GetComponent<KBatchedAnimController>();
            if (animController != null && !string.IsNullOrEmpty(skill.animation))
            {
                animController.Play(skill.animation, KAnim.PlayMode.Once);
            }

            // 更新技能冷却时间
            var updatedSkill = skill;
            updatedSkill.lastUseTime = Time.time;
            if (updatedSkill.isFirstUse)
            {
                updatedSkill.isFirstUse = false;
                // 初次使用后，冷却时间保持配置值不变
            }
            skills[skillIndex] = updatedSkill;

            TbbDebuger.LogDebug($"[MutanterSkillComponent] Executed skill: {skill.name} with damage: {skill.damage}, damageType: {skill.damageType}");
        }
    }
}