using Klei.AI;
using MutantContainmentProject.MutanterComponent;
using System;
using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.Buildings
{
    public class CorrosionManager : KMonoBehaviour, ISim1000ms
    {
        // 腐蚀状态枚举
        public enum CorrosionState
        {
            Stable = 0,      // 稳定状态
            Warning = 1,     // 腐蚀预警
            HighCorrosion = 2, // 高腐蚀
            Overflow = 3     // 溢流突破
        }

        // 管理失误类型
        public enum ManagementError
        {
            ContainmentFailure,   // 管控失败
            CorrosionFull,        // 腐蚀值满100
            Unattended,           // 10周期无人管控
            FacilityDamaged       // 设施损坏未修复
        }

        // 腐蚀值相关
        [SerializeField]
        private float corrosionValue = 0f; // 0-100
        [SerializeField]
        private float corrosionUpdateTimer = 0f;
        [SerializeField]
        private bool isOperationInProgress = false; // 标记是否有操作正在进行中
        [SerializeField]
        private bool hasReportedCorrosionFull = false; // 标记是否已经报告过腐蚀值满值的情况
        [SerializeField]
        private CorrosionState previousCorrosionState = CorrosionState.Stable;

        // 腐蚀值阈值
        private const float CORROSION_WARNING_THRESHOLD = 30f;
        private const float CORROSION_HIGH_THRESHOLD = 70f;
        private const float CORROSION_OVERFLOW_THRESHOLD = 100f;

        // 腐蚀值变化速率
        private const float CORROSION_DECREASE_STABLE = 0.01f; // 每周期+6
        private const float CORROSION_INCREASE_NORMAL = 0.015f; // 每周期+9
        private const float CORROSION_INCREASE_FAST = 0.02f; // 每周期+12
        private const float CORROSION_INCREASE_UNATTENDED = 0.05f; // 每周期+30
        // 管控成功/失败的腐蚀值变化
        private const float CORROSION_CHANGE_SUCCESS = -10f;
        private const float CORROSION_CHANGE_FAILURE = 15f;

        // 收容等级基数
        private static Dictionary<MutanterDangerLevel, float> DANGER_LEVEL_MULTIPLIERS = new ()
        {
            { MutanterDangerLevel.Safe, 1f },
            { MutanterDangerLevel.Euclid, 1.5f },
            { MutanterDangerLevel.Keter, 2f },
            { MutanterDangerLevel.Thaumiel, 2.5f },
            { MutanterDangerLevel.Neutralized, 3f }
        };
        //全局侵蚀等级基数
        private static Dictionary<GlobalErosionManager.ErosionLevel, float> EROSION_LEVEL_MULTIPLIERS = new ()
        {
            { GlobalErosionManager.ErosionLevel.Safe, 1f },
            { GlobalErosionManager.ErosionLevel.Alert, 1.5f },
            { GlobalErosionManager.ErosionLevel.Crisis, 2f },
            { GlobalErosionManager.ErosionLevel.Disaster, 2.5f }
        };
        public float CorrosionValue
        {
            get { return corrosionValue; }
            set { corrosionValue = Mathf.Clamp(value, 0f, 100f); }
        }

        public CorrosionState CurrentCorrosionState
        {
            get
            {
                if (corrosionValue >= CORROSION_OVERFLOW_THRESHOLD)
                    return CorrosionState.Overflow;
                else if (corrosionValue >= CORROSION_HIGH_THRESHOLD)
                    return CorrosionState.HighCorrosion;
                else if (corrosionValue >= CORROSION_WARNING_THRESHOLD)
                    return CorrosionState.Warning;
                else
                    return CorrosionState.Stable;
            }
        }

        private MeterController m_corrosionMeter;

        private MutanterSecurableMonitor.Instance securableMonitor;
        private MutanterSecurableMonitor.Instance SecurableMonitor             {
            get
            {
                if (securableMonitor == null)
                {
                    securableMonitor = gameObject.GetSMI<MutanterSecurableMonitor.Instance>();
                }
                return securableMonitor;
            }
        }
        protected override void OnSpawn()
        {
            base.OnSpawn();
            m_corrosionMeter = new MeterController(GetComponent<KBatchedAnimController>(), "meter_target", "meter", Meter.Offset.UserSpecified, Grid.SceneLayer.TileFront, Array.Empty<string>());
            // 使用自定义插值函数，实现5级动画的正确映射
            m_corrosionMeter.interpolateFunction = (percentage, frames) => {
                if (frames <= 1 || percentage <= 0f) return 0f;
                if (percentage >= 1f) return 1f;
                
                // 5级动画的映射：
                // 0-20% → 第1-2帧 (0-0.2)
                // 20-40% → 第2-3帧 (0.2-0.4)
                // 40-60% → 第3-4帧 (0.4-0.6)
                // 60-80% → 第4-5帧 (0.6-0.8)
                // 80-100% → 第5-6帧 (0.8-1.0)
                if (percentage < 0.2f) {
                    return percentage * 5f / (float)frames;
                } else if (percentage < 0.4f) {
                    return (1f + (percentage - 0.2f) * 5f) / (float)frames;
                } else if (percentage < 0.6f) {
                    return (2f + (percentage - 0.4f) * 5f) / (float)frames;
                } else if (percentage < 0.8f) {
                    return (3f + (percentage - 0.6f) * 5f) / (float)frames;
                } else {
                    return (4f + (percentage - 0.8f) * 5f) / (float)frames;
                }
            };
            Subscribe((int)MutanterGameHashes.MutanterBreachContained, OnBreachContained);
            // 更新腐蚀值显示
            UpdateCorrosionMeter();
            // 发布当前腐蚀等级事件
            CheckCorrosionStateChange();
        }

        private void OnBreachContained(object obj)
        {
            corrosionValue = 0;
        }

        // 实现ISim1000ms接口
        public void Sim1000ms(float dt)
        {
            UpdateCorrosion(dt);
        }

        // 更新腐蚀值
        public void UpdateCorrosion(float dt)
        {
            corrosionUpdateTimer += dt;

            // 每1000ms更新一次
            if (corrosionUpdateTimer >= 1f)
            {
                UpdateCorrosionValue(dt);
                corrosionUpdateTimer = 0f;
            }

        }

        // 更新腐蚀值
        private void UpdateCorrosionValue(float dt)
        {
            // 如果有操作正在进行中，不更新腐蚀值
            if (isOperationInProgress)
            {
                TbbDebuger.LogDebug($"[腐蚀管理] 操作进行中，跳过腐蚀值更新");
                return;
            }

            // 检查是否有控制部抑制效果
            bool hasControlSuppressionEffect = false;
            Effects effects = gameObject.GetComponent<Effects>();
            if (effects != null && effects.HasEffect(MutanterEffect.MutanterEffects.MUTANTER_CONTROL_SUPPRESSION_EFFECT))
            {
                hasControlSuppressionEffect = true;
                // 如果腐蚀值已经达到或超过高腐蚀阈值，不继续增长
                if (CorrosionValue >= CORROSION_HIGH_THRESHOLD)
                {
                    TbbDebuger.LogDebug($"[腐蚀管理] 有控制部抑制效果，腐蚀值已达到高腐蚀阈值，停止增长");
                    return;
                }
            }

            float corrosionChange = 0f;

            // 基础腐蚀速率
            float baseRate = CORROSION_DECREASE_STABLE;

            float corrosionLevelFactor = Mathf.Floor(CorrosionValue / 3);
            if (corrosionLevelFactor > 3)
                baseRate = CORROSION_INCREASE_FAST;
            else if(corrosionLevelFactor > 2)
                baseRate = CORROSION_INCREASE_NORMAL;
            else
                baseRate = CORROSION_DECREASE_STABLE;
            // 根据全局侵蚀等级调整
            baseRate *= EROSION_LEVEL_MULTIPLIERS[GlobalErosionManager.Instance.CurrentErosionLevel];
            // 根据收容等级调整
            MutanterDangerLevel dangerLevel = GetComponent<MutanterColonyComponent>()?.DangerLevel ?? MutanterDangerLevel.Safe;
            baseRate *= DANGER_LEVEL_MULTIPLIERS[dangerLevel];

            // 是否被收容
            if (!SecurableMonitor?.IsSecured() == true)
            {
                corrosionChange += CORROSION_INCREASE_UNATTENDED;
            }
            // 应用基础速率
            corrosionChange += baseRate;

            // 应用腐蚀变化
            CorrosionValue += corrosionChange;
            
            // 如果有控制部抑制效果，确保腐蚀值不超过高腐蚀阈值
            if (hasControlSuppressionEffect && CorrosionValue > CORROSION_HIGH_THRESHOLD)
            {
                CorrosionValue = CORROSION_HIGH_THRESHOLD;
                TbbDebuger.LogDebug($"[腐蚀管理] 有控制部抑制效果，腐蚀值被限制在高腐蚀阈值: {CORROSION_HIGH_THRESHOLD}");
            }
            
            // 检查腐蚀状态变化
            CheckCorrosionStateChange();
            
            // 更新腐蚀值显示
            UpdateCorrosionMeter();
        }

        // 标记操作开始
        public void StartOperation()
        {
            isOperationInProgress = true;
            TbbDebuger.LogDebug($"[腐蚀管理] 操作开始");
        }

        // 标记操作结束
        public void EndOperation()
        {
            isOperationInProgress = false;
            TbbDebuger.LogDebug($"[腐蚀管理] 操作结束");
        }


        // 检查腐蚀状态变化
        private void CheckCorrosionStateChange()
        {
            CorrosionState currentState = CurrentCorrosionState;
            KSelectable selectable = gameObject.GetComponent<KSelectable>();

            // 检查状态是否变化
            if (currentState != previousCorrosionState)
            {
                // 触发腐蚀等级变化事件
                BoxingTrigger((int)MutanterGameHashes.CorrosionLevelChanged, currentState);
                previousCorrosionState = currentState;
            }

            // 先移除所有腐蚀相关的状态项
            if (selectable != null)
            {
                selectable.RemoveStatusItem(ContainmentMonitorBuildingStatusItems.Instance.CorrosionWarning);
                selectable.RemoveStatusItem(ContainmentMonitorBuildingStatusItems.Instance.HighCorrosionWarning);
                selectable.RemoveStatusItem(ContainmentMonitorBuildingStatusItems.Instance.CorrosionOverflow);
            }

            if (currentState == CorrosionState.Overflow)
            {
                HandleOverflow();
                // 只报告一次腐蚀值满值的情况
                if (!hasReportedCorrosionFull)
                {
                    HandleManagementError(ManagementError.CorrosionFull);
                    hasReportedCorrosionFull = true;
                }
                // 显示溢流突破状态项
                if (selectable != null)
                {
                    selectable.AddStatusItem(ContainmentMonitorBuildingStatusItems.Instance.CorrosionOverflow);
                }
            }
            else if (currentState == CorrosionState.HighCorrosion)
            {
                // 重置腐蚀值满值标记
                hasReportedCorrosionFull = false;
                // 触发高腐蚀警报
                TriggerHighCorrosionAlert();
                // 显示高腐蚀预警状态项
                if (selectable != null)
                {
                    selectable.AddStatusItem(ContainmentMonitorBuildingStatusItems.Instance.HighCorrosionWarning, corrosionValue);
                }
            }
            else if (currentState == CorrosionState.Warning)
            {
                // 重置腐蚀值满值标记
                hasReportedCorrosionFull = false;
                // 显示腐蚀预警状态项
                if (selectable != null)
                {
                    selectable.AddStatusItem(ContainmentMonitorBuildingStatusItems.Instance.CorrosionWarning, corrosionValue);
                }
            }
            else if (currentState == CorrosionState.Stable)
            {
                // 重置腐蚀值满值标记
                hasReportedCorrosionFull = false;
            }
        }

        // 处理管理失误
        public void HandleManagementError(ManagementError error)
        {
            // 调用全局侵蚀管理器处理失误
            GlobalErosionManager.Instance.HandleManagementError((GlobalErosionManager.ManagementError)error);
        }

        // 处理溢流
        private void HandleOverflow()
        {
            TbbDebuger.LogDebug($"[腐蚀管理] 触发溢流突破");
            // 触发突破效果
            TriggerBreakout();
        }

        // 触发高腐蚀警报
        private void TriggerHighCorrosionAlert()
        {
            TbbDebuger.LogDebug($"[腐蚀管理] 高腐蚀警报，腐蚀值: {corrosionValue}");
            // 这里需要实现警报逻辑
        }

        // 触发突破
        private void TriggerBreakout()
        {
            TbbDebuger.LogDebug($"[腐蚀管理] 畸变体突破收容");
            SecurableMonitor.GoOutOfContainment();
        }

        // 处理管控成功
        public void HandleContainmentSuccess()
        {
            CorrosionValue -= CORROSION_CHANGE_SUCCESS;
            TbbDebuger.LogDebug($"[腐蚀管理] 管控成功，腐蚀值减少10，当前值: {corrosionValue}");

            // 如果侵蚀等级≥2，减少1点点数
            if (GlobalErosionManager.Instance.CurrentErosionLevel >= GlobalErosionManager.ErosionLevel.Alert)
            {
                GlobalErosionManager.Instance.ReduceErosionPoints(1);
            }
            
            // 更新腐蚀值显示
            UpdateCorrosionMeter();
        }

        // 处理管控失败
        public void HandleContainmentFailure()
        {
            CorrosionValue += CORROSION_CHANGE_FAILURE;
            TbbDebuger.LogDebug($"[腐蚀管理] 管控失败，腐蚀值增加15，当前值: {corrosionValue}");

            // 累计管理失误
            HandleManagementError(ManagementError.ContainmentFailure);
            
            // 更新腐蚀值显示
            UpdateCorrosionMeter();
        }

        // 更新腐蚀值显示
        private void UpdateCorrosionMeter()
        {
            if (m_corrosionMeter != null)
            {
                // 计算腐蚀值百分比 (0-100% 映射到 0-1)
                float percentage = corrosionValue / 100f;
                m_corrosionMeter.SetPositionPercent(Mathf.Clamp01(percentage));
                //TbbDebuger.LogDebug($"[腐蚀管理] 更新腐蚀值显示，腐蚀值: {corrosionValue}, 百分比: {percentage}");
            }
        }
    }
}