using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent.VFXController
{
    [VFXAttribute("CastMagicVFX")]
    public class CastMagicVFXController : KMonoBehaviour, IVFXController, ISimEveryTick
    {
        private GameObject trailInstance;
        private TrailRenderer TrailRendererInstance;

        private float facingDirection;
        private float tickStep;
        private const float defaultTickStep = 0.5f; // 默认每帧移动距离
        private const float castDuration = 2f; // 魔法持续时间
        private const float minPlayDuration = 2f; // 最短播放时长
        private float playTime; // 播放计时器
        private bool hasTarget; // 是否有目标

        private List<KPrefabID> targetList = new();

        private Facing facing;

        private Facing FacingCom => facing ??= GetComponent<Facing>();
        override protected void OnSpawn()
        {
            base.OnSpawn();
            GameObject prefab = MutantContainmentProjectMod.MutantContainmentProject.ModAssetBundle.LoadAsset<GameObject>("MagicCastEffect");
            trailInstance = Util.KInstantiate(prefab);
            if (trailInstance != null)
            {
                trailInstance.transform.SetParent(gameObject.transform);
                trailInstance.transform.localPosition = Vector3.zero;
                trailInstance.transform.localScale = new Vector3(0f, 5f, 0f);
                trailInstance.transform.localRotation = Quaternion.identity;

                TrailRendererInstance = trailInstance.GetComponent<TrailRenderer>();
                if(TrailRendererInstance!= null){ 
                    TrailRendererInstance.emitting = false; 
                    TrailRendererInstance.textureScale = new Vector2(0.5f, 0.5f);
                }
                trailInstance.SetActive(true);
            }
            else{
                TbbDebuger.LogWarning("CastMagicVFXController:CastMagicVFX初始化失败");
            }
        }
        public void Activate(GameObject target = null)
        {
            TbbDebuger.LogDebug($"CastMagicVFXController:激活 {target?.name ?? "无目标"} [{TrailRendererInstance?.emitting}]");
            if(TrailRendererInstance == null) return;
            targetList.Clear();
            if(target?.GetComponent<KPrefabID>() != null) targetList.Add(target?.GetComponent<KPrefabID>());
            facingDirection = FacingCom?.GetFacing() == true ? -1f : 1f;
            playTime = 0f; // 重置播放计时器
            hasTarget = target != null;

            var position = transform.position;
            position.y += 1f;

            if (target != null)
            {
                float step = (target.transform.position.x - transform.position.x) / castDuration;
                tickStep = Mathf.Abs(step) < defaultTickStep ? defaultTickStep : step;
            }
            else
            {
                // 无目标时，水平飞出一段距离
                tickStep = defaultTickStep;
            }

            trailInstance.transform.position = position; 
            TrailRendererInstance.emitting = true;
            trailInstance.SetActive(true);
        }

        public void Deactivate()
        {
            TrailRendererInstance.emitting = false;
            trailInstance?.SetActive(false);
        }

        public List<KPrefabID> GetAttackTargets()
        {
            return targetList;
        }

        public int GetCurrentLODLevel()
        {
            return 0;
        }

        public void SetLODLevel(int level)
        {
        }

        public void UpdateLOD(float distance)
        {
        }

        public void SimEveryTick(float dt)
        {
            if (TrailRendererInstance?.emitting != true || trailInstance == null) return;

            // 累计播放时间
            playTime += dt;

            // 更新位置
            var position = trailInstance.transform.position;
            if (hasTarget)
            {
                // 有目标时，tickStep已经包含方向信息，直接使用
                position.x += tickStep * dt;
            }
            else
            {
                // 无目标时，使用facingDirection控制方向
                position.x += facingDirection * tickStep * dt;
            }
            trailInstance.transform.position = position;

            // 检查是否达到最短播放时长
            if (playTime >= minPlayDuration)
            {
                Deactivate();
            }
        }
    }
}
