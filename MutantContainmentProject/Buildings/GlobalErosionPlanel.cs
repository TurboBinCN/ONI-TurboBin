using TBB.He.TbbLib.Debuger;

namespace MutantContainmentProject.Buildings
{
    public class GlobalErosionPlanel : KMonoBehaviour, ISim1000ms
    {
        private MeterController m_erosionMeter;
        private string currentMeterTarget = "meter_target";

        protected override void OnSpawn()
        {
            base.OnSpawn();
            InitializeMeter();
        }

        private void InitializeMeter()
        {
            m_erosionMeter = new MeterController(GetComponent<KBatchedAnimController>(), "meter_target", "meter", Meter.Offset.UserSpecified, Grid.SceneLayer.BuildingFront, System.Array.Empty<string>());
        }

        public void Sim1000ms(float dt)
        {
            UpdateMeter();
        }

        private void UpdateMeter()
        {
            var erosionManager = GlobalErosionManager.Instance;
            if (erosionManager == null) return;

            var currentLevel = erosionManager.CurrentErosionLevel;
            float percentage = 0f;
            string targetSymbol = "meter_target";
            string targetAnimation = "meter";

            // 根据不同等级设置不同的动画目标、动画名称和百分比
            switch (currentLevel)
            {
                case GlobalErosionManager.ErosionLevel.Safe:
                    targetAnimation = "meter_s"; // 4帧3格的动画
                    break;
                case GlobalErosionManager.ErosionLevel.Alert:
                    targetAnimation = "meter"; // 6帧4格的动画
                    break;
                case GlobalErosionManager.ErosionLevel.Crisis:
                    targetAnimation = "meter_l"; // 9帧8格的动画
                    break;
                case GlobalErosionManager.ErosionLevel.Disaster:
                    targetAnimation = "meter_l"; // 9帧8格的动画
                    break;
            }
            percentage = erosionManager.PercentageToNextLevel;

            if (targetAnimation != currentMeterTarget)
            {
                currentMeterTarget = targetAnimation;
                m_erosionMeter.Unlink();
                DestroyImmediate(m_erosionMeter.gameObject);
                m_erosionMeter = new MeterController(GetComponent<KBatchedAnimController>(), targetSymbol, targetAnimation, Meter.Offset.UserSpecified, Grid.SceneLayer.BuildingFront, System.Array.Empty<string>());
            }

            TbbDebuger.LogDebug($"Updating Global Erosion Meter: Level={currentLevel}, Percentage={percentage:P2}");
            // 更新 meter 显示
            m_erosionMeter.SetPositionPercent(percentage);
        }
    }
}