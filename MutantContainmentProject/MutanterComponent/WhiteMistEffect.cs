using Klei.AI;
using MutantContainmentProject.MutanterEffect;
using System;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    public class WhiteMistEffect : KMonoBehaviour
    {
        private float duration;
        private float startTime;
        private float damageInterval = 0.3f;
        private float lastDamageTime;
        private MutanterAttackSystem attackSystem;

        public static WhiteMistEffect CreateMist(Vector3 position, float duration = 5f, MutanterAttackSystem attackSystem = null)
        {
            GameObject mistObj = new GameObject("WhiteMist");
            mistObj.transform.position = position;
            WhiteMistEffect mist = mistObj.AddComponent<WhiteMistEffect>();
            mist.duration = duration;
            mist.startTime = Time.time;
            mist.lastDamageTime = Time.time;
            mist.attackSystem = attackSystem;
            return mist;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
        }

        private void Update()
        {
            if (Time.time - startTime > duration)
            {
                Destroy(gameObject);
                return;
            }

            if (Time.time - lastDamageTime > damageInterval)
            {
                ApplyMistEffects();
                lastDamageTime = Time.time;
            }
        }

        private void ApplyMistEffects()
        {
            // 对范围内的单位造成精神伤害和减速效果
            int centerCell = Grid.PosToCell(transform.position);
            int centerX, centerY;
            Grid.CellToXY(centerCell, out centerX, out centerY);

            // 遍历2格范围内的所有单元格
            for (int x = centerX - 2; x <= centerX + 2; x++)
            {
                for (int y = centerY - 2; y <= centerY + 2; y++)
                {
                    int cell = Grid.XYToCell(x, y);
                    if (Grid.IsValidCell(cell))
                    {
                        // 检查距离是否在2格范围内（曼哈顿距离）
                        int distance = Math.Abs(x - centerX) + Math.Abs(y - centerY);
                        if (distance <= 2)
                        {
                            // 检查该单元格是否有角色或可拾取物品
                            GameObject character = Grid.Objects[cell, (int)ObjectLayer.Minion];
                            if (character != null)
                            {
                                ApplyEffectsToObject(character);
                            }

                            GameObject pickupable = Grid.Objects[cell, (int)ObjectLayer.Pickupables];
                            if (pickupable != null)
                            {
                                ApplyEffectsToObject(pickupable);
                            }
                        }
                    }
                }
            }
        }

        private void ApplyEffectsToObject(GameObject obj)
        {
            if (obj == gameObject)
                return;

            // 造成4-6点精神伤害
            float damage = UnityEngine.Random.Range(4f, 6f);

            // 使用攻击系统执行精神攻击
            if (attackSystem != null)
            {
                attackSystem.TryExecuteAttack(obj, damage, MutanterTags.PsychologicalAttack);
            }
            else
            {
                // 降级处理：直接增加压力值
                var amounts = obj.GetComponent<Amounts>();
                if (amounts != null)
                {
                    var stressAmountComp = amounts.Get(Db.Get().Amounts.Stress);
                    if (stressAmountComp != null)
                    {
                        stressAmountComp.value = Mathf.Min(stressAmountComp.value + damage, 100f);
                    }
                }
            }

            // 应用减速效果
            var effects = obj.GetComponent<Effects>();
            if (effects != null)
            {
                effects.Add(MutanterEffects.WHITE_MIST_SLOW_EFFECT, true);
            }
        }
    }
}