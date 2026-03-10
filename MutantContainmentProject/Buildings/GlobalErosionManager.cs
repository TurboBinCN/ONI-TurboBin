using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.Buildings
{
    public class GlobalErosionManager : KMonoBehaviour, ISim1000ms
    {
        // 全局侵蚀等级枚举
        public enum ErosionLevel
        {
            Safe = 1,      // 安全
            Alert = 2,     // 警戒
            Crisis = 3,    // 危机
            Disaster = 4   // 灾难
        }

        // 管理失误类型
        public enum ManagementError
        {
            ContainmentFailure,   // 管控失败
            CorrosionFull,        // 腐蚀值满100
            Unattended,           // 10周期无人管控
            FacilityDamaged       // 设施损坏未修复
        }

        // 全局侵蚀等级相关
        [SerializeField]
        private static ErosionLevel currentErosionLevel = ErosionLevel.Safe;
        [SerializeField]
        private static int erosionPoints = 0;
        private static float randomOverflowTimer = 0f;

        // 侵蚀等级阈值
        private const int EROSION_LEVEL_1_TO_2 = 3;
        private const int EROSION_LEVEL_2_TO_3 = 5;
        private const int EROSION_LEVEL_3_TO_4 = 8;

        // 随机溢流触发概率
        private const float RANDOM_OVERFLOW_CHANCE_LEVEL1 = 0.05f; // 5%
        private const float RANDOM_OVERFLOW_CHANCE_LEVEL2 = 0.15f; // 15%
        private const float RANDOM_OVERFLOW_CHANCE_LEVEL3 = 0.3f;  // 30%
        private const float RANDOM_OVERFLOW_CHANCE_LEVEL4 = 1.0f;  // 100%

        public ErosionLevel CurrentErosionLevel
        {
            get { return currentErosionLevel; }
        }

        public float PercentageToNextLevel
        {
            get
            {
                switch (currentErosionLevel)
                {
                    case ErosionLevel.Safe:
                        return (float)erosionPoints / EROSION_LEVEL_1_TO_2;
                    case ErosionLevel.Alert:
                        return (float)(erosionPoints - EROSION_LEVEL_1_TO_2) / EROSION_LEVEL_2_TO_3;
                    case ErosionLevel.Crisis:
                        return (float)(erosionPoints - EROSION_LEVEL_1_TO_2 - EROSION_LEVEL_2_TO_3) / EROSION_LEVEL_3_TO_4;
                    case ErosionLevel.Disaster:
                        return 1f; // 已经是最高等级
                    default:
                        return 0f;
                }
            }
        }
        public int ErosionPoints
        {
            get { return erosionPoints; }
        }
        private static GlobalErosionManager _instance;
        public static GlobalErosionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GlobalErosionManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GlobalErosionManager");
                        _instance = go.AddComponent<GlobalErosionManager>();
                    }
                }
                return _instance;
            }
        }
        protected override void OnSpawn()
        {
            base.OnSpawn();
            TbbDebuger.LogDebug($"[GlobalErosionManager] 全局侵蚀管理器初始化");
        }

        protected override void OnCleanUp()
        {
            base.OnCleanUp();
            TbbDebuger.LogDebug($"[GlobalErosionManager] 全局侵蚀管理器清理");
        }

        // 实现ISim1000ms接口
        public void Sim1000ms(float dt)
        {
            UpdateErosion(dt);
        }

        // 更新侵蚀等级
        private void UpdateErosion(float dt)
        {
            randomOverflowTimer += dt;

            if (randomOverflowTimer >= 60f) // 每周期检查一次
            {
                CheckRandomOverflow();
                randomOverflowTimer = 0f;
            }
        }

        // 检查随机溢流
        private void CheckRandomOverflow()
        {
            float chance = 0f;
            switch (currentErosionLevel)
            {
                case ErosionLevel.Safe:
                    chance = RANDOM_OVERFLOW_CHANCE_LEVEL1;
                    break;
                case ErosionLevel.Alert:
                    chance = RANDOM_OVERFLOW_CHANCE_LEVEL2;
                    break;
                case ErosionLevel.Crisis:
                    chance = RANDOM_OVERFLOW_CHANCE_LEVEL3;
                    break;
                case ErosionLevel.Disaster:
                    chance = RANDOM_OVERFLOW_CHANCE_LEVEL4;
                    break;
            }

            if (UnityEngine.Random.value < chance)
            {
                TriggerRandomOverflow();
            }
        }

        // 处理管理失误
        public void HandleManagementError(ManagementError error)
        {
            erosionPoints++;
            TbbDebuger.LogDebug($"[全局侵蚀管理] 管理失误: {error}, 侵蚀点数: {erosionPoints}");
            UpdateErosionLevel();
        }

        // 更新侵蚀等级
        private static void UpdateErosionLevel()
        {
            ErosionLevel newLevel = currentErosionLevel;

            switch (currentErosionLevel)
            {
                case ErosionLevel.Safe:
                    if (erosionPoints >= EROSION_LEVEL_1_TO_2)
                        newLevel = ErosionLevel.Alert;
                    break;
                case ErosionLevel.Alert:
                    if ((erosionPoints - EROSION_LEVEL_1_TO_2) >= EROSION_LEVEL_2_TO_3)
                        newLevel = ErosionLevel.Crisis;
                    break;
                case ErosionLevel.Crisis:
                    if ((erosionPoints - EROSION_LEVEL_1_TO_2 - EROSION_LEVEL_2_TO_3) >= EROSION_LEVEL_3_TO_4)
                        newLevel = ErosionLevel.Disaster;
                    break;
            }

            if (newLevel != currentErosionLevel)
            {
                currentErosionLevel = newLevel;
                TbbDebuger.LogDebug($"[全局侵蚀管理] 侵蚀等级升级: {currentErosionLevel}");
                TriggerErosionLevelChange();

                // 等级升级时触发局部溢流
                if (currentErosionLevel >= ErosionLevel.Alert)
                {
                    TriggerLevelUpOverflow();
                }
            }
        }

        // 触发随机溢流
        private void TriggerRandomOverflow()
        {
            TbbDebuger.LogDebug($"[全局侵蚀管理] 触发随机溢流");
            // 这里需要实现具体的溢流逻辑
        }

        // 触发等级升级溢流
        private static void TriggerLevelUpOverflow()
        {
            TbbDebuger.LogDebug($"[全局侵蚀管理] 等级升级触发溢流");
            // 这里需要实现具体的溢流逻辑
        }

        // 触发侵蚀等级变化
        private static void TriggerErosionLevelChange()
        {
            TbbDebuger.LogDebug($"[全局侵蚀管理] 侵蚀等级变化: {currentErosionLevel}");
            // 这里需要实现等级变化逻辑
        }

        // 减少侵蚀点数
        public void ReduceErosionPoints(int amount)
        {
            erosionPoints = Mathf.Max(0, erosionPoints - amount);
            TbbDebuger.LogDebug($"[全局侵蚀管理] 减少侵蚀点数，当前点数: {erosionPoints}");
        }
    }
}