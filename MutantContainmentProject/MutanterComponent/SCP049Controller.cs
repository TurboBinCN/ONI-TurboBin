using Klei.AI;
using MutantContainmentProject.Mutanters;
using System.Collections.Generic;
using System.Linq;
using TBB.He.TbbLib.Debuger;
using UnityEngine;
using static Health;

namespace MutantContainmentProject.MutanterComponent
{
    // 用于存储墓碑引用的组件
    public class GraveReference : KMonoBehaviour
    {
        public Grave grave;
    }

    public class SCP049Controller : KMonoBehaviour
    {
        private EmotionMonitor.StatesInstance _emotionMonitorSMI;

        private EmotionMonitor.StatesInstance EmotionMonitorInstance => _emotionMonitorSMI ??= gameObject.GetSMI<EmotionMonitor.StatesInstance>();


        // SCP-049-2相关
        public List<GameObject> deadBodies = new();

        protected override void OnSpawn()
        {
            base.OnSpawn();
            // 注册事件监听
            _emotionMonitorSMI = gameObject.GetSMI<EmotionMonitor.StatesInstance>();
        }

        protected override void OnCleanUp()
        {
            base.OnCleanUp();
        }

        private void CheckForPlague()
        {
            // 使用EmotionMonitor中的threaters列表
            var threaters = EmotionMonitorInstance.GetThreaters();
            foreach (var threater in threaters)
            {
                if (threater != null && threater.gameObject != gameObject)
                {
                    if (IsInfectedWithPlague(threater.gameObject))
                    {
                        // 对感染"瘟疫"的生物发起攻击
                    }
                }
            }
        }

        private bool IsInfectedWithPlague(GameObject creature)
        {
            // SCP-049对"瘟疫"的定义与基金会不同，这里可以实现自定义的判断逻辑
            // 例如：随机判断，或者基于生物的状态、属性等
            return UnityEngine.Random.value < 0.3f; // 30%的概率被判定为感染"瘟疫"
        }

        public void PerformFlawedRecovery()
        {
            // 使用EmotionMonitor中的threaters列表
            var threaters = EmotionMonitorInstance.GetThreaters();
            foreach (var threater in threaters)
            {
                if (threater != null && threater.gameObject != gameObject)
                {
                    if (IsSickOrInjured(threater.gameObject))
                    {
                        // 执行"治愈"之手
                        PerformHealing(threater.gameObject);
                        break;
                    }
                }
            }
        }
        public bool CheckCanFlawedRecovery()
        {
            var threaters = EmotionMonitorInstance.GetThreaters();
            // 检查是否有任何需要"治愈"之手的生物
            return threaters.Any(threater => IsSickOrInjured(threater.gameObject));
        }
        private bool IsSickOrInjured(GameObject minion)
        {
            // 检查是否进入可被救援状态
            var health = minion.GetComponent<Health>();
            if (health != null && (health.State == HealthState.Incapacitated || health.State == HealthState.Dead))
            {
                return false;
            }

            // 检查是否生病
            var sicknesses = minion.GetComponent<Sicknesses>();
            if (sicknesses != null)
            {
                // 检查是否有任何疾病
                foreach (var sickness in sicknesses)
                {
                    return true;
                }
            }

            // 检查是否受伤
            if (health != null && health.hitPoints < health.maxHitPoints)
            {
                return true;
            }

            return false;
        }

        private void PerformHealing(GameObject target)
        {
            Debug.Log($"[SCP049] Performing healing on {target.name}");

            // 清空所有疾病
            var sicknesses = target.GetComponent<Sicknesses>();
            if (sicknesses != null)
            {
                List<SicknessInstance> sicknessInstances = new();
                foreach (var sickness in sicknesses)
                {
                    sicknessInstances.Add(sickness);
                }

                foreach (var sicknessInstance in sicknessInstances)
                {
                    sicknessInstance.Cure();
                }
            }

            // 恢复所有生命值
            var health = target.GetComponent<Health>();
            if (health != null)
            {
                health.hitPoints = health.maxHitPoints;
            }

            // 压力值直接满值
            var amounts = target.GetAmounts();
            if (amounts != null)
            {
                var stressAmount = amounts.Get(Db.Get().Amounts.Stress);
                if (stressAmount != null)
                {
                    stressAmount.value = 100f;
                }
            }

            TbbDebuger.LogDebug($"[SCP049] Healing completed for {target.name}: all diseases and injuries cleared, stress maxed");
        }
        public bool CheckCanRevivedZombie()
        {
            // 清理无效的尸体
            deadBodies.RemoveAll(body => body == null || (!body.HasTag(GameTags.Dead) && !body.HasTag(GameTags.Corpse)));
            // 通过EmotionMonitor检查周围的小人是否死亡
            var threaters = EmotionMonitorInstance.GetThreaters();
            foreach (var threater in threaters)
            {
                if (threater != null && threater.gameObject != gameObject)
                {
                    if (threater.HasTag(GameTags.Corpse))
                    {
                        if (!deadBodies.Contains(threater.gameObject))
                        {
                            deadBodies.Add(threater.gameObject);
                        }
                    }
                }
            }
            var buildings = EmotionMonitorInstance.GetBuildings();
            List<Grave> gravesToRemove = new();
            foreach (var building in buildings)
            {
                Grave grave = null;
                if (building.HasTag(GraveConfig.ID)) grave = building.GetComponent<Grave>();
                if (grave != null && !string.IsNullOrEmpty(grave.graveName))
                {
                    // 直接创建一个临时的尸体对象来表示墓碑中的尸体
                    GameObject graveCorpse = new($"GraveCorpse_{grave.graveName}");
                    graveCorpse.AddComponent<KPrefabID>();
                    graveCorpse.GetComponent<KPrefabID>().AddTag(GameTags.Corpse);

                    // 存储墓碑引用，以便后续销毁
                    graveCorpse.AddComponent<GraveReference>().grave = grave;

                    if (!deadBodies.Contains(graveCorpse))
                    {
                        deadBodies.Add(graveCorpse);
                        gravesToRemove.Add(grave);
                    }
                }
            }
            return deadBodies.Count > 0;
        }
        public void PerformRevivedZombie()
        {
            // 对尸体进行手术
            if (deadBodies.Count > 0)
            {
                TbbDebuger.LogDebug($"[SCP049] 复活 {deadBodies[0].name}");
                PerformSurgery(deadBodies[0]);
                deadBodies.RemoveAt(0);
            }
        }

        private void PerformSurgery(GameObject body)
        {
            if (body == null || (!body.HasTag(GameTags.Dead) && !body.HasTag(GameTags.Corpse)))
                return;

            TbbDebuger.LogDebug($"[SCP049] Performing surgery on {body.name}");

            // 确定生成位置：优先使用尸体或墓碑的位置
            Vector3 spawnPosition = body.transform.position;

            // 检查是否是墓碑中的尸体
            GraveReference graveRef = body.GetComponent<GraveReference>();
            if (graveRef != null && graveRef.grave != null)
            {
                // 使用墓碑的位置
                spawnPosition = graveRef.grave.transform.position;

                // 销毁墓碑
                Util.KDestroyGameObject(graveRef.grave.gameObject);
            }

            // 移除尸体
            Util.KDestroyGameObject(body);

            // 生成SCP-049-2实例
            GameObject scp049_2 = SpawnSCP049_2(spawnPosition);

            if (scp049_2 != null)
            {
                Debug.Log($"[SCP049] Created SCP-049-2 instance at {spawnPosition}");
            }
        }

        private GameObject SpawnSCP049_2(Vector3 position)
        {
            // 检查SCP049_2的配置是否存在
            var prefab = Assets.GetPrefab(SCP049_2Config.ID);
            if (prefab == null)
            {
                Debug.LogWarning($"[SCP049] SCP049_2 prefab not found: {SCP049_2Config.ID}");
                return null;
            }

            // 生成SCP-049-2实例
            GameObject instance = GameUtil.KInstantiate(prefab, position, Grid.SceneLayer.Creatures);
            instance.SetActive(true);

            return instance;
        }

    }

}
