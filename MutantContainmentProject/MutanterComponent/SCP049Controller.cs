using Klei.AI;
using MutantContainmentProject.MutanterEffect;
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
    
    public class SCP049Controller : KMonoBehaviour, ISim1000ms
    {
        private MutanterAttackSystem _attackSystem;

        private MutanterAttackSystem attackSystem{
            get{
                if(_attackSystem == null){
                    _attackSystem = gameObject.GetComponent<MutanterAttackSystem>();
                }
                return _attackSystem;
            }
        }

        private EmotionMonitor.StatesInstance _emotionMonitorSMI;

        private EmotionMonitor.StatesInstance emotionMonitorInstance{
            get{
                if(_emotionMonitorSMI == null){
                    _emotionMonitorSMI = gameObject.GetSMI<EmotionMonitor.StatesInstance>();
                }
                return _emotionMonitorSMI;
            }
        }

        private float checkInterval = 1f;
        private float lastCheckTime = 0f;
        private float contactKillRange = 1f;
        private float healCooldown = 10f;
        private float lastHealTime = 0f;

        // SCP-049-2相关
        private float surgeryTime = 5f;
        private float lastSurgeryTime = 0f;
        private List<GameObject> deadBodies = new();

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
        public void Sim1000ms(float dt)
        {
            Update();
        }
        private void Update()
        {
            // 定期检查周围环境
            if (Time.time - lastCheckTime >= checkInterval)
            {
                lastCheckTime = Time.time;
                CheckForPlague();
                CheckForDeadBodies();
                CheckForSickOrInjured();
            }

            // 检查是否有生物在接触范围内
            CheckForContact();
        }

        private void CheckForPlague()
        {
            // 使用EmotionMonitor中的threaters列表
            var threaters = emotionMonitorInstance.GetThreaters();
            foreach (var threater in threaters)
            {
                if (threater != null && threater.gameObject != gameObject)
                {
                    if (IsInfectedWithPlague(threater.gameObject))
                    {
                        // 对感染"瘟疫"的生物发起攻击
                        AttackTarget(threater.gameObject);
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

        private void CheckForSickOrInjured()
        {
            // 检查是否在冷却期
            if (Time.time - lastHealTime < healCooldown)
                return;

            // 使用EmotionMonitor中的threaters列表
            var threaters = emotionMonitorInstance.GetThreaters();
            foreach (var threater in threaters)
            {
                if (threater != null && threater.gameObject != gameObject)
                {
                    if (IsSickOrInjured(threater.gameObject))
                    {
                        // 执行"治愈"之手
                        PerformHealing(threater.gameObject);
                        lastHealTime = Time.time;
                        break;
                    }
                }
            }
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
                List<SicknessInstance> sicknessInstances = new List<SicknessInstance>();
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

            Debug.Log($"[SCP049] Healing completed for {target.name}: all diseases and injuries cleared, stress maxed");
        }

        private void AttackTarget(GameObject target)
        {
            // 尝试执行攻击行为
            attackSystem.TryExecuteAttack(target);
        }

        private void CheckForContact()
        {
            // 使用EmotionMonitor中的threaters列表
            var threaters = emotionMonitorInstance.GetThreaters();
            foreach (var threater in threaters)
            {
                if (threater != null && threater.gameObject != gameObject)
                {
                    if (Vector3.Distance(transform.position, threater.transform.position) <= contactKillRange)
                    {
                        // 皮肤接触，瞬间终止生物的所有生理机能
                        InstantKill(threater.gameObject);
                    }
                }
            }
        }

        private void InstantKill(GameObject target)
        {
            // 使用攻击系统执行即死攻击
            var health = target.GetComponent<Health>();
            if (health != null)
            {
                // 计算需要的伤害值（确保杀死目标）
                float damage = health.hitPoints;
                if (attackSystem.ExecuteHealthAttack(target, damage))
                {
                    TbbDebuger.LogDebug($"[SCP049] Instantly killed {target.name} via skin contact");
                    
                    // 将尸体添加到列表中，以便后续进行手术
                    if (!deadBodies.Contains(target))
                    {
                        deadBodies.Add(target);
                    }
                }
            }
        }

        private void CheckForDeadBodies()
        {
            // 清理无效的尸体
            deadBodies.RemoveAll(body => body == null || (!body.HasTag(GameTags.Dead) && !body.HasTag(GameTags.Corpse)));

            // 通过EmotionMonitor检查周围的小人是否死亡
            var threaters = emotionMonitorInstance.GetThreaters();
            foreach (var threater in threaters)
            {
                if (threater != null && threater.gameObject != gameObject)
                {
                    var health = threater.gameObject.GetComponent<Health>();
                    if (health != null && health.State == HealthState.Dead)
                    {
                        if (!deadBodies.Contains(threater.gameObject))
                        {
                            deadBodies.Add(threater.gameObject);
                        }
                    }
                }
            }

            // 直接检查所有墓碑
            List<Grave> gravesToRemove = new List<Grave>();
            foreach (var grave in Components.Graves.Items)
            {
                if (grave != null && !string.IsNullOrEmpty(grave.graveName))
                {
                    // 直接创建一个临时的尸体对象来表示墓碑中的尸体
                    GameObject graveCorpse = new GameObject($"GraveCorpse_{grave.graveName}");
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
            
            // 移除已处理的墓碑
            foreach (var grave in gravesToRemove)
            {
                if (grave != null)
                {
                    Components.Graves.Remove(grave);
                }
            }

            // 对尸体进行手术
            if (deadBodies.Count > 0 && Time.time - lastSurgeryTime >= surgeryTime)
            {
                lastSurgeryTime = Time.time;
                PerformSurgery(deadBodies[0]);
                deadBodies.RemoveAt(0);
            }
        }

        private void PerformSurgery(GameObject body)
        {
            if (body == null || (!body.HasTag(GameTags.Dead) && !body.HasTag(GameTags.Corpse)))
                return;

            Debug.Log($"[SCP049] Performing surgery on {body.name}");

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
