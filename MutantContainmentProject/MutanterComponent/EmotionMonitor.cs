using Klei.AI;
using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;
using TBB.He.TbbLib.UI;
using TBB.He.TbbLib.Utils;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    /**
     * 2. 情绪/理智监控器 (EmotionMonitor)
        功能: 替代或扩展 ThreatMonitor，持续追踪畸变体的情绪状态（如恐惧、愤怒、兴奋、绝望）。
        这是一个数值系统（如 0-100 的“理智值”）。
        触发源:
        * 环境:灯光、装饰度
        * 其他creature: 复制人、仿生人
        * 其他Plant: 植物
        * 安全控制措施：词条
        * Effect：收容->随时间变化
        * 特定事件
        作用: 输出的理智状态是 MutanterStateMachine 切换状态的关键输入。
     */
    public class EmotionMonitor : GameStateMachine<EmotionMonitor, EmotionMonitor.StatesInstance, IStateMachineTarget, EmotionMonitor.Def>
    {
        // --- 常量定义 ---
        public const float MAX_INSANITY = 100f;
        public const float MIN_INSANITY = 0f;

        public const int PROBE_RANGE = 10;
        public List<KPrefabID> threaters = new();
        public List<KPrefabID> buildings = new();
        public List<KPrefabID> plants = new();
        public List<KPrefabID> creatures = new();

        public override void InitializeStates(out BaseState default_state)
        {
            default_state = root;

            root
                .Enter(smi =>
                {
                    smi.INSANITYValue = MAX_INSANITY;
                })
                .Update((smi, dt) => CalculateNewINSANITY(smi, dt), UpdateRate.SIM_1000ms);
        }

        private void UpdateThreateArea(StatesInstance smi, float dt)
        {
            float insanityValue = smi.INSANITYValue; // 获取值一次，提高可读性和效率

            if (insanityValue >= 75f)
            {
                smi.tbbRangeVisualizer.SetHightlightColor(Color.white);
            }
            else if (insanityValue >= 50f)
            {
                smi.tbbRangeVisualizer.SetHightlightColor(Color.yellow);
            }
            else
            {
                smi.tbbRangeVisualizer.SetHightlightColor(Color.red);
            }
        }

        private void CalculateNewINSANITY(StatesInstance smi, float dt)
        {
            SpaceProbe(smi, dt);
            float newINSANITY = smi.INSANITYValue;

            TbbDebuger.LogDebug($"[EmotionMonitor] threatercount:[{threaters.Count}]");
            newINSANITY -= EvaluateThreaters(smi);

            // 仅当配置允许时才计算环境影响
            if (smi.def.considerEnvironmentalFactors)
            {
                newINSANITY += EvaluateEnvironment(smi);
            }

            // 仅当配置允许时才计算生物影响
            newINSANITY += EvaluateNearbyCreatures(smi);

            // 仅当配置允许时才计算植物影响
            if (smi.def.considerPlantFactors)
            {
                newINSANITY += EvaluateNearbyPlants(smi);
            }

            // 安全控制措施影响
            // 固定Effect影响
            newINSANITY += EvaluateEffects(smi);

            // 随时间自然衰减或恢复
            newINSANITY += smi.def.timeBasedINSANITYDriftPerSecond * dt;

            newINSANITY = Mathf.Clamp(newINSANITY, MIN_INSANITY, MAX_INSANITY);
            smi.INSANITYValue = newINSANITY;

            UpdateThreateArea(smi, dt);
            TbbDebuger.LogDebug($"[EmotionMonitor] {smi.master.name} 理智值更新: {smi.INSANITYValue:F2}");
        }

        private float EvaluateEnvironment(StatesInstance smi)
        {
            float impact = 0f;
            // --- 仅在需要时评估光照 ---
            if (smi.def.considerLighting)
            {
                var pos = smi.master.transform.GetPosition();
                var lightProvider = smi.gameObject.GetComponent<IlluminationVulnerable>();
                if (lightProvider == null) return impact;
                if (lightProvider != null)
                {
                    float lightIntensity = lightProvider.LightIntensityThreshold;
                    float optimalLight = smi.def.optimalLightLevel;
                    float lightDelta = Mathf.Abs(lightIntensity - optimalLight);
                    impact -= lightDelta * smi.def.lightIntensityImpactFactor;
                }
            }

            // --- 仅在需要时评估装饰度 ---
            if (smi.def.considerDecor)
            {
                if (!smi.HighDecor) impact += smi.def.decorImpactFactor;
            }

            return impact;
        }

        private void SpaceProbe(StatesInstance smi, float dt)
        {
            var BaseCell = Grid.PosToCell(smi);
            //根据probelayer找到对应的植物、小人、建筑、动物等
            if (smi.tbbRangeVisualizer == null) return;
            List<int> list_cells = TbbLimitedRoomSpaceBuilder.BuildRoom(BaseCell, 10);
            TbbDebuger.LogDebug($"[畸变收容所]SpaceProbe cell count:[{list_cells.Count}]");
            //用于可视化显示威胁区域
            smi.tbbRangeVisualizer.SetTargetCells(list_cells);
            creatures.Clear();
            threaters.Clear();
            plants.Clear();
            buildings.Clear();
            for (int i = 0; i < list_cells.Count; i++)
            {
                //小动物
                GameObject obj_gameobject = null;
                if ((obj_gameobject = Grid.Objects[list_cells[i], (int)ObjectLayer.Pickupables]) != null && obj_gameobject.GetComponent<KPrefabID>() != smi.gameObject.GetComponent<KPrefabID>())
                    creatures.Add(obj_gameobject.GetComponent<KPrefabID>());
                //小人
                if ((obj_gameobject = Grid.Objects[list_cells[i], (int)ObjectLayer.Minion]) != null)
                    threaters.Add(obj_gameobject.GetComponent<KPrefabID>());
                //植物
                if ((obj_gameobject = Grid.Objects[list_cells[i], (int)ObjectLayer.Plants]) != null)
                    plants.Add(obj_gameobject.GetComponent<KPrefabID>());
                //建筑
                if ((obj_gameobject = Grid.Objects[list_cells[i], (int)ObjectLayer.Building]) != null)
                    buildings.Add(obj_gameobject.GetComponent<KPrefabID>());
            }

        }
        private float EvaluateNearbyCreatures(StatesInstance smi)
        {
            float impact = 0f;
            var pos = smi.master.transform.GetPosition();
            var creatures = Components.LiveMinionIdentities.Items;
            //逻辑还没想好
            return impact;
        }
        private float EvaluateThreaters(StatesInstance smi)
        {
            float impact = 0f;
            if (threaters.Count > 0)
            {
                impact += 5f;
            }
            return impact;
        }
        private float EvaluateNearbyPlants(StatesInstance smi)
        {
            float impact = 0f;
            //逻辑还没想好
            return impact;
        }

        private float EvaluateEffects(StatesInstance smi)
        {
            float impact = 0f;
            var effects = smi.master.gameObject.GetComponent<Effects>();
            if (effects != null)
            {
            }
            return impact;
        }
        public class StatesInstance : GameInstance
        {
            private bool _highDecor = false;
            public bool HighDecor { get => _highDecor; }
            public float INSANITYValue;
            public TbbRangeVisualizer tbbRangeVisualizer;
            public float GetINSANITY() => INSANITYValue;

            public StatesInstance(IStateMachineTarget master, Def def) : base(master, def)
            {
                if (smi.def.considerDecor)
                {
                    //master.gameObject.AddOrGetDef<CreatureDecorMonitor.Def>()
                    //    .DecorValueTreshold = smi.def.DecorValueTreshold;

                    Subscribe((int)GameHashes.CreatureLowDecor, (_) => _highDecor = false);
                    Subscribe((int)GameHashes.CreatureHighDecor, (_) => _highDecor = true);
                }
                tbbRangeVisualizer = master.gameObject.GetComponent<TbbRangeVisualizer>();
            }
            protected override void OnCleanUp()
            {
                Unsubscribe((int)GameHashes.CreatureLowDecor);
                Unsubscribe((int)GameHashes.CreatureHighDecor);
                base.OnCleanUp();
            }
        }
        public class Def : BaseDef
        {
            public float INSANITYCalculationInterval = 5f;

            // --- 环境因素总开关 ---
            [Tooltip("是否考虑环境因素(灯光,装饰度)对理智的影响?")]
            public bool considerEnvironmentalFactors = true;

            // --- 环境因素子项开关 ---
            [Tooltip("是否考虑灯光对理智的影响? (需启用总开关)")]
            public bool considerLighting = true;
            [Tooltip("是否考虑装饰度对理智的影响? (需启用总开关)")]
            public bool considerDecor = true;
            public float DecorValueTreshold = 80f;

            // --- 环境因素参数 ---
            public float optimalLightLevel = 50f;
            public float lightIntensityImpactFactor = 0.1f;
            public float decorImpactFactor = 0.05f;

            // --- 生物因素参数 (总是启用) ---
            public float creatureInteractionRange = 10f;
            public float cloneOrAndroidProximityImpact = -2f;

            // --- 植物因素总开关 ---
            [Tooltip("是否考虑附近植物对理智的影响?")]
            public bool considerPlantFactors = true;

            // --- 植物因素参数 ---
            public float plantInteractionRange = 8f;
            public float plantProximityImpact = 0.5f;

            // --- 时间流逝影响 ---
            public float timeBasedINSANITYDriftPerSecond = -0.01f;

            //public override void Configure()
            //{
            //    base.Configure();
            //    AddState(base.smi.sm.baseState);
            //}
        }
    }
}
