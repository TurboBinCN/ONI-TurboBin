using Klei.AI;
using MutantContainmentProject.MutanterEffect;
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
            if (smi.MutanterStateMachineDef == null) return;
            if (insanityValue <= smi.MutanterStateMachineDef.sanityThresholdToAttack)
            {
                smi.tbbRangeVisualizer.SetHightlightColor(Color.red);
            }
            else if (insanityValue <= smi.MutanterStateMachineDef.sanityThresholdToHostile)
            {
                smi.tbbRangeVisualizer.SetHightlightColor(Color.yellow);
            }
            else if (insanityValue <= smi.MutanterStateMachineDef.sanityThresholdToAgitate)
            {
                smi.tbbRangeVisualizer.SetHightlightColor(Color.white);
            }
            else
            {
                smi.tbbRangeVisualizer.SetHightlightColor(new Color(0f, 1f, 0.8f, 1f));
            }
        }

        private void CalculateNewINSANITY(StatesInstance smi, float dt)
        {
            smi.SpaceProbe(smi, dt);
            float positiveImpact = 0f;
            float negativeImpact = 0f;

            // 处理所有影响因素，分离正负值
            AddImpact(EvaluateThreaters(smi), ref positiveImpact, ref negativeImpact);

            if (smi.def.considerEnvironmentalFactors)
                AddImpact(EvaluateEnvironment(smi), ref positiveImpact, ref negativeImpact);

            AddImpact(EvaluateNearbyCreatures(smi), ref positiveImpact, ref negativeImpact);

            if (smi.def.considerPlantFactors)
                AddImpact(EvaluateNearbyPlants(smi), ref positiveImpact, ref negativeImpact);

            AddImpact(smi.def.timeBasedINSANITYDriftPerSecond * dt, ref positiveImpact, ref negativeImpact);

            // 应用Effect影响：对正值加成、负值削弱
            float effectImpact = EvaluateEffects(smi);
            if (effectImpact > 0)
            {
                positiveImpact *= (1 + effectImpact);
                negativeImpact *= (1 - effectImpact);
            }

            // 计算总影响并更新理智值
            float totalImpact = positiveImpact + negativeImpact;
            smi.INSANITYValue = Mathf.Clamp(smi.INSANITYValue + totalImpact, MIN_INSANITY, MAX_INSANITY);

            UpdateThreateArea(smi, dt);
            //TbbDebuger.LogDebug($"[EmotionMonitor] {smi.master.name} 理智值更新: {smi.INSANITYValue:F2}");
        }

        // 辅助方法：分离并累加正负影响
        private void AddImpact(float impact, ref float positiveImpact, ref float negativeImpact)
        {
            if (impact > 0)
                positiveImpact += impact;
            else
                negativeImpact += impact;
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
            if (smi.GetThreaters().Count > 0)
            {
                impact += 2f;
                var effects = smi.master.gameObject.GetComponent<Effects>();
                if (effects != null && effects.HasEffect(MutanterEffects.MUTANTER_CONTAINED_EFFECT))
                {
                    impact *= 0.5f;
                }
            }
            return -impact;
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
                if (effects.HasEffect(MutanterEffects.MUTANTER_CONTAINED_EFFECT))
                {
                    impact += 0.1f;
                }
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
            private MutanterStateMachine.Def mutanterStateMachineDef;
            public MutanterStateMachine.Def MutanterStateMachineDef
            {
                get
                {
                    if (mutanterStateMachineDef == null) mutanterStateMachineDef = gameObject.GetDef<MutanterStateMachine.Def>();
                    return mutanterStateMachineDef;
                }
            }

            private bool _isContained = false;
            public bool IsContained { get => _isContained; }

            // 实例级别的列表，每个实例都有自己的列表
            private List<KPrefabID> threaters = new();
            private List<KPrefabID> buildings = new();
            private List<KPrefabID> plants = new();
            private List<KPrefabID> creatures = new();
            public StatesInstance(IStateMachineTarget master, Def def) : base(master, def)
            {
                if (def.considerDecor)
                {
                    Subscribe((int)GameHashes.CreatureLowDecor, (_) => _highDecor = false);
                    Subscribe((int)GameHashes.CreatureHighDecor, (_) => _highDecor = true);
                }
                tbbRangeVisualizer = master.gameObject.GetComponent<TbbRangeVisualizer>();
                mutanterStateMachineDef = gameObject.GetComponent<MutanterStateMachine.Def>();

                // 初始化时检查当前的收容状态
                Effects effects = gameObject.GetComponent<Effects>();
                if (effects != null && effects.HasEffect(MutanterEffect.MutanterEffects.MUTANTER_CONTAINED_EFFECT))
                {
                    _isContained = true;
                    TbbDebuger.LogDebug($"[EmotionMonitor] {gameObject.name} initialized with containment effect, IsContained = true");
                }

                // 使用Klei原生事件系统订阅事件
                Subscribe((int)MutanterGameHashes.MutanterContained, OnContained);
                Subscribe((int)MutanterGameHashes.MutanterBreachContained, OnBreachContained);
            }

            private void OnContained(object data)
            {
                GameObject mutanterObj = data as GameObject;
                if (mutanterObj == gameObject)
                {
                    _isContained = true;
                    TbbDebuger.LogDebug($"[EmotionMonitor] {gameObject.name} received MutanterContained event");
                }
            }

            private void OnBreachContained(object data)
            {
                GameObject mutanterObj = data as GameObject;
                if (mutanterObj == gameObject)
                {
                    _isContained = false;
                    TbbDebuger.LogDebug($"[EmotionMonitor] {gameObject.name} received MutanterBreachContained event");
                }
            }

            protected override void OnCleanUp()
            {
                Unsubscribe((int)GameHashes.CreatureLowDecor);
                Unsubscribe((int)GameHashes.CreatureHighDecor);
                // 使用Klei原生事件系统取消订阅
                Unsubscribe((int)MutanterGameHashes.MutanterContained, OnContained);
                Unsubscribe((int)MutanterGameHashes.MutanterBreachContained, OnBreachContained);
                base.OnCleanUp();
            }

            public List<KPrefabID> GetThreaters()
            {
                return threaters;
            }
            public List<KPrefabID> GetBuildings()
            {
                return buildings;
            }
            public void SpaceProbe(StatesInstance smi, float dt)
            {
                var BaseCell = Grid.PosToCell(smi);
                //根据probelayer找到对应的植物、小人、建筑、动物等
                if (smi.tbbRangeVisualizer == null || smi.MutanterStateMachineDef == null) return;
                List<int> list_cells = TbbLimitedRoomSpaceBuilder.BuildRoom(BaseCell, smi.MutanterStateMachineDef.threatenRange);
                //用于可视化显示威胁区域
                smi.tbbRangeVisualizer.SetTargetCells(list_cells);
                smi.creatures.Clear();
                smi.threaters.Clear();
                smi.plants.Clear();
                smi.buildings.Clear();
                for (int i = 0; i < list_cells.Count; i++)
                {
                    //小动物
                    GameObject obj_gameobject = null;
                    if ((obj_gameobject = Grid.Objects[list_cells[i], (int)ObjectLayer.Pickupables]) != null && obj_gameobject.GetComponent<KPrefabID>() != smi.gameObject.GetComponent<KPrefabID>() && !smi.creatures.Contains(obj_gameobject.GetComponent<KPrefabID>()))
                        smi.creatures.Add(obj_gameobject.GetComponent<KPrefabID>());
                    //小人
                    if ((obj_gameobject = Grid.Objects[list_cells[i], (int)ObjectLayer.Minion]) != null && !smi.threaters.Contains(obj_gameobject.GetComponent<KPrefabID>()))
                        smi.threaters.Add(obj_gameobject.GetComponent<KPrefabID>());
                    //植物
                    if ((obj_gameobject = Grid.Objects[list_cells[i], (int)ObjectLayer.Plants]) != null && !smi.plants.Contains(obj_gameobject.GetComponent<KPrefabID>()))
                        smi.plants.Add(obj_gameobject.GetComponent<KPrefabID>());
                    //建筑
                    if ((obj_gameobject = Grid.Objects[list_cells[i], (int)ObjectLayer.Building]) != null && !smi.buildings.Contains(obj_gameobject.GetComponent<KPrefabID>()))
                    {
                        smi.buildings.Add(obj_gameobject.GetComponent<KPrefabID>());
                    }
                }
            }
        }
        public class Def : BaseDef
        {
            public float INSANITYCalculationInterval = 5f;

            // --- 环境因素总开关 ---
            public bool considerEnvironmentalFactors = true;

            // --- 环境因素子项开关 ---
            public bool considerLighting = true;
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
            public bool considerPlantFactors = true;

            // --- 植物因素参数 ---
            public float plantInteractionRange = 8f;
            public float plantProximityImpact = 0.5f;

            // --- 时间流逝影响 ---
            public float timeBasedINSANITYDriftPerSecond = 0.01f;

            // --- 威胁范围 ---
            public int threatenRange = 10;

            //public override void Configure()
            //{
            //    base.Configure();
            //    AddState(base.smi.sm.baseState);
            //}
        }
    }
}
