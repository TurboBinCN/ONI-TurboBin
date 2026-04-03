using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent.VFXController
{
    [VFXAttribute("AerosolVFX")]
    public class AerosolVFXController : KMonoBehaviour, IVFXController, ISim1000ms
    {
        private GameObject MistInstance;
        GameObject FogPrefab;
        private int currentLODLevel;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            FogPrefab = MutantContainmentProjectMod.MutantContainmentProject.ModAssetBundle.LoadAsset<GameObject>("TheFixerWhiteFog");
            if (FogPrefab == null) TbbDebuger.LogError($"加载迷雾预制体失败");
        }
        public void Activate(GameObject target = null)
        {
            CreateMistInstance();
        }

        public void Deactivate()
        {
            if (MistInstance != null)
            {
                MistInstance.SetActive(false);
                Destroy(MistInstance);
            }
        }

        public List<KPrefabID> GetAttackTargets()
        {
            var emotionManager = gameObject.GetSMI<EmotionMonitor.StatesInstance>();
            if (emotionManager != null) return emotionManager.GetThreaters();
            return new List<KPrefabID>();
        }

        public int GetCurrentLODLevel()
        {
            return currentLODLevel;
        }

        public void SetLODLevel(int level)
        {
            currentLODLevel = level;
        }

        public void UpdateLOD(float distance)
        {
            int newLODLevel = 0;
            if (distance > 20f)
            {
                newLODLevel = 1; // 低细节
            }
            else
            {
                newLODLevel = 0; // 高细节
            }

            if (newLODLevel != currentLODLevel)
            {
                SetLODLevel(newLODLevel);
            }
        }

        private void CreateMistInstance()
        {
            if (MistInstance == null)
            {
                TbbDebuger.LogDebug($"创建迷雾实例 {transform.position}");
                Vector3 position = transform.position;
                MistInstance = Util.KInstantiate(FogPrefab);
                position.y += 2f;
                MistInstance.transform.position = position;
                MistInstance.transform.localScale = new Vector3(10f, 4f, 0);

                // 设置颜色为粉红色
                var renderer = MistInstance.GetComponent<SpriteRenderer>();
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.SetColor("_Color", new Color(1f, 0.5f, 0.5f));
                }

                MistInstance.SetActive(true);
            }
        }

        public void Sim1000ms(float dt)
        {
            if (MistInstance == null) return;

            //if (GetCurrentLODLevel() > 0) MistInstance.SetActive(false);
            // 更新LOD
            if (CameraController.Instance != null)
            {
                float distance = Vector3.Distance(transform.position, CameraController.Instance.transform.position);
                UpdateLOD(distance);
            }
        }
    }
}
