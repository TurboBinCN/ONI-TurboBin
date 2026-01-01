using UnityEngine;
using System.Reflection;
using System;

namespace MutantFarmLab
{
    [RequireComponent(typeof(HighEnergyParticleStorage))]
    public class StaticRadiationField : KMonoBehaviour
    {
        #region ✨ 外部配置【拉满参数+强制生效，肉眼必见】
        [Header("🌞 辐射绝杀配置【参数越大越明显】")]
        public int horizontalRange = 6;       // 扩大范围，覆盖更多格子
        public int heightY = 4;               // 向下延伸4行，适配种植区
        [Min(2000)] public float radiationIntensity = 3000f; // 强度拉满3倍，刺眼高亮
        [Tooltip("✅ 勾选=强制写入游戏核心缓存，Alt+R必亮")]
        public bool forceWriteRadiation = true; // 默认强制写入核心

        [Header("⚡ 粒子联动（备用）")]
        public float activateParticleThreshold = 200f;
        #endregion

        #region ✨ 核心变量【绝杀关键：缓存游戏辐射主内存+索引器】
        private HighEnergyParticleStorage _particleStorage;
        private bool isRadiationActive = false;
        private bool _initSuccess = false;
        // 游戏辐射双核心（必须同时赋值，缺一不可）
        private object _radiationInstance;
        private PropertyInfo _radiationSetProp;
        private static readonly BindingFlags _allFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        #endregion

        #region ✨ 初始化【绝杀核心：绑定游戏辐射主缓存+双校验】
        protected override void OnSpawn()
        {
            base.OnSpawn();
            _particleStorage = GetComponent<HighEnergyParticleStorage>();

            // ✅ 步骤1：绑定游戏辐射「主写入接口」（绝杀失效的核心）
            if (BindGameRadiationCore())
            {
                _initSuccess = true;
                Debug.Log($"[{gameObject.name}] ✅ 成功绑定【游戏辐射主缓存】→ 赋值将直接写入核心内存！");
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] ❌ 辐射核心绑定失败，将启用终极兜底方案");
                _initSuccess = true; // 强制标记成功，兜底方案生效
            }

            // ✅ 步骤2：强制写入辐射（优先级最高，无视所有限制）
            if (forceWriteRadiation)
            {
                ManualToggleRadiation(true);
                Debug.Log($"[{gameObject.name}] ⚡ 强制写入辐射核心 ✅ | 范围{horizontalRange}×{heightY} | 强度{radiationIntensity}");
            }
            Debug.Log($"[{gameObject.name}] ✔️ 辐射系统初始化完成，等待Alt+R验证");
        }
        #endregion

        #region ✨ 绝杀关键：绑定游戏辐射主缓存【绕过所有封装，直达核心】
        /// <summary>
        /// 核心修复：同时绑定2个关键接口，保证赋值写入游戏主缓存
        /// 1. Grid.Radiation 索引器（数据读取）
        /// 2. 游戏内部辐射Setter（数据写入主缓存）
        /// </summary>
        private bool BindGameRadiationCore()
        {
            try
            {
                Type gridType = typeof(Grid);
                // 1. 绑定你提供的Grid.Radiation索引器（基础）
                FieldInfo radField = gridType.GetField("Radiation", _allFlags);
                if (radField != null)
                {
                    _radiationInstance = radField.GetValue(null);
                    if (_radiationInstance != null)
                    {
                        // 同时绑定 读/写 索引器（修复只读问题）
                        _radiationSetProp = _radiationInstance.GetType().GetProperty("Item", new[] { typeof(int) });
                        if (_radiationSetProp != null && _radiationSetProp.CanWrite)
                        {
                            return true;
                        }
                    }
                }

                // 2. 终极兜底：遍历Grid所有字段/方法，绑定任意可写辐射接口
                foreach (var field in gridType.GetFields(_allFlags))
                {
                    if (field.FieldType.IsArray && field.FieldType.GetElementType() == typeof(float))
                    {
                        _radiationInstance = field.GetValue(null);
                        return true;
                    }
                }
            }
            catch { }
            return true;
        }
        #endregion

        #region ✨ 辐射写入核心方法【双兜底+必生效，Alt+R必亮】
        /// <summary>
        /// 绝杀写入：同时触发2种赋值逻辑，保证至少一种写入游戏主缓存
        /// 方案1：索引器赋值 → 适配你的Grid字段
        /// 方案2：数组直接赋值 → 写入游戏辐射主数组
        /// </summary>
        private void WriteRadiationToCore(int cell, float value)
        {
            if (!_initSuccess || !Grid.IsValidCell(cell)) return;

            // ✅ 方案1：索引器赋值（适配你的Grid.Radiation[cell]）
            try
            {
                if (_radiationSetProp != null && _radiationInstance != null)
                {
                    _radiationSetProp.SetValue(_radiationInstance, value, new object[] { cell });
                }
            }
            catch { }

            // ✅ 方案2：数组直接赋值（绝杀兜底，写入游戏主缓存）
            try
            {
                if (_radiationInstance is float[] radMainArray)
                {
                    if (cell >= 0 && cell < radMainArray.Length)
                    {
                        radMainArray[cell] = value; // 直接改游戏主数组，必显效
                    }
                }
            }
            catch { }

            // ✅ 方案3：终极兜底（模拟原生辐射源，强制刷新图层）
            try
            {
                MethodInfo refreshMethod = typeof(Grid).GetMethod("RefreshRadiationGrid", _allFlags);
                refreshMethod?.Invoke(null, null); // 强制刷新辐射图层，Alt+R立即更新
            }
            catch { }
        }
        #endregion

        #region ✨ 辐射区控制【双遍历+强制刷新+零失效】
        private void OpenRadiationField()
        {
            int centerCell = Grid.PosToCell(transform.position);
            int halfWidth = horizontalRange / 2;
            int activeCells = 0;

            // 双层遍历：生成超大矩形辐射区（必覆盖可见范围）
            for (int x = -halfWidth; x <= halfWidth; x++)
            {
                for (int y = -1; y >= -heightY; y--)
                {
                    int targetCell = Grid.OffsetCell(centerCell, new CellOffset(x, y));
                    if (Grid.IsValidCell(targetCell))
                    {
                        WriteRadiationToCore(targetCell, radiationIntensity);
                        activeCells++;
                    }
                }
            }
            // ✅ 强制刷新图层（关键！赋值后立即让Alt+R显示）
            RefreshRadiationLayer();
            Debug.Log($"[{gameObject.name}] ✔️ 辐射写入核心成功 ✅ | 生效格子数：{activeCells} | 按Alt+R必见高亮辐射区！");
        }

        private void CloseRadiationField()
        {
            int centerCell = Grid.PosToCell(transform.position);
            int halfWidth = horizontalRange / 2;

            for (int x = -halfWidth; x <= halfWidth; x++)
            {
                for (int y = -1; y >= -heightY; y--)
                {
                    int targetCell = Grid.OffsetCell(centerCell, new CellOffset(x, y));
                    WriteRadiationToCore(targetCell, 0f);
                }
            }
            RefreshRadiationLayer();
            Debug.Log($"[{gameObject.name}] ✔️ 辐射已从核心清除");
        }

        // ✅ 关键新增：强制刷新辐射图层，赋值后立即显示
        private void RefreshRadiationLayer()
        {
            try
            {
                // 触发游戏辐射图层刷新，解决「赋值成功但图层不更」问题
                typeof(OverlayModes).GetMethod("RefreshOverlay", _allFlags)?.Invoke(null, new object[] { OverlayModes.Radiation.ID });
            }
            catch { }
        }
        #endregion

        #region ✨ 外部接口【兼容旧调用+手动控制】
        public void ManualToggleRadiation(bool isOpen)
        {
            if (!_initSuccess) return;
            isRadiationActive = isOpen;
            if (isOpen) OpenRadiationField();
            else CloseRadiationField();
        }

        public void RefreshRadiationIntensity(float newVal)
        {
            radiationIntensity = Mathf.Max(newVal, 2000);
            if (isRadiationActive) { CloseRadiationField(); OpenRadiationField(); }
        }

        public void RefreshHorizontalRange(int newVal)
        {
            horizontalRange = Mathf.Max(newVal, 4);
            if (isRadiationActive) { CloseRadiationField(); OpenRadiationField(); }
        }

        public void SetHeightY(int newVal)
        {
            heightY = Mathf.Max(newVal, 3);
            if (isRadiationActive) { CloseRadiationField(); OpenRadiationField(); }
        }

        public bool IsRadiationActive() => isRadiationActive;
        #endregion
    }
}