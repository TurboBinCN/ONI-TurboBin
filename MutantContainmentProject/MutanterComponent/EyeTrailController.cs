using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;
using UnityEngine;
using static MutantContainmentProject.MutanterComponent.MutanterSkillComponent;

namespace MutantContainmentProject.MutanterComponent
{
    public class EyeTrailController : KMonoBehaviour, ISimEveryTick
    {
        public static string ID = "EyeTrailController";
        [Header("拖尾配置")]
        public string eyeSymbolName = "snapto_eye";

        private bool isSkillActive = false;
        private GameObject trailInstance;
        private ParticleSystem particleSystem;
        private Facing facing;
        public Facing FacingCom => facing ??= GetComponent<Facing>();

        // 椭圆轨迹参数
        private float ellipseAngle = 0f; // 椭圆轨迹的当前角度
        private float ellipseSpeed = 200f; // 椭圆轨迹的旋转速度
        private float ellipseWidth = 3f; // 椭圆的宽度（半长轴）
        private float ellipseHeight = 1f; // 椭圆的高度（半短轴）
        private float ellipseTilt = 10f; // 椭圆的倾斜角度（度）
        private float facingDirection = 1f; // 畸变体的朝向（1=右, -1=左）


        protected override void OnSpawn()
        {
            base.OnSpawn();

            GameObject prefab = MutantContainmentProjectMod.MutantContainmentProject.ModAssetBundle.LoadAsset<GameObject>("TheFixerRedEyeTrail");
            trailInstance = Util.KInstantiate(prefab);
            if (trailInstance != null)
            {
                trailInstance.transform.SetParent(gameObject.transform);
                trailInstance.transform.localPosition = Vector3.zero;
                trailInstance.transform.localScale = Vector3.one;
                trailInstance.transform.localRotation = Quaternion.identity;

                particleSystem ??= trailInstance.GetComponent<ParticleSystem>();
                particleSystem?.Stop();
                isSkillActive = false;
                trailInstance.SetActive(true);
            }
        }

        protected override void OnCleanUp()
        {
            DeactivateRedEyeTrail();
            base.OnCleanUp();
        }

        public void ActivateRedEyeTrail()
        {
            TbbDebuger.LogDebug($"激活红眼拖尾");
            // 更新畸变体的朝向
            facingDirection = FacingCom?.GetFacing() == true ? -1f : 1f; // Facing组件的GetFacing()返回true表示向左

            // 根据畸变体朝向设置粒子系统的旋转和位置
            if (trailInstance != null)
            {
                float rotationAngle = facingDirection > 0 ? 0f : 180f;
                trailInstance.transform.rotation = Quaternion.Euler(0, rotationAngle, 0);
            }

            particleSystem ??= trailInstance?.GetComponent<ParticleSystem>();
            if (particleSystem != null && trailInstance != null)
            {
                particleSystem.Play();
                isSkillActive = true;
                trailInstance.SetActive(true);
            }
        }

        public void DeactivateRedEyeTrail()
        {
            if (trailInstance != null && particleSystem != null)
            {
                particleSystem.Stop();
                trailInstance.SetActive(false);
            }
            isSkillActive = false;
        }
        // 每帧生成粒子
        public void SimEveryTick(float dt)
        {
            if (!isSkillActive || trailInstance == null || particleSystem == null) return;

            // 每帧更新畸变体的朝向
            float currentFacingDirection = FacingCom?.GetFacing() == true ? -1f : 1f;
            TbbDebuger.LogDebug($"每帧生成粒子，当前朝向：{currentFacingDirection}，当前位置：{trailInstance.transform.position}");
            if (currentFacingDirection != facingDirection)
            {
                facingDirection = currentFacingDirection;
            }
            if (trailInstance != null)
            {
                float rotationAngle = facingDirection > 0 ? 0f : 180f;
                trailInstance.transform.rotation = Quaternion.Euler(0, rotationAngle, 0);
            }

            // 计算椭圆轨迹上的位置
            ellipseAngle += ellipseSpeed * dt;
            if (ellipseAngle > 360f)
            {
                ellipseAngle -= 360f;
            }

            // 转换倾斜角度为弧度
            float tiltRad = ellipseTilt * Mathf.Deg2Rad;

            // 计算椭圆上的点（基于极坐标）
            float angleRad = ellipseAngle * Mathf.Deg2Rad;
            float x = ellipseWidth * Mathf.Cos(angleRad);
            float y = ellipseHeight * Mathf.Sin(angleRad);

            // 应用倾斜变换
            float tiltedX = x * Mathf.Cos(tiltRad) - y * Mathf.Sin(tiltRad);
            float tiltedY = x * Mathf.Sin(tiltRad) + y * Mathf.Cos(tiltRad);

            // 根据畸变体朝向调整椭圆的水平位置
            tiltedX *= facingDirection;

            // 计算椭圆中心位置（以游戏对象位置为焦点）
            Vector3 ellipseCenter = gameObject.transform.position;
            ellipseCenter.y += 1f;
            ellipseCenter.z = Grid.GetLayerZ(Grid.SceneLayer.FXFront);

            // 计算最终位置
            Vector3 ellipsePosition = new Vector3(ellipseCenter.x + tiltedX, ellipseCenter.y + tiltedY, ellipseCenter.z);

            // 更新拖尾实例的位置和旋转
            if (trailInstance != null)
            {
                trailInstance.transform.position = ellipsePosition;
            }
        }

    }
    public class EyeTrailEffect : IExtraAnimationEffect
    {
        private EyeTrailController trail;

        public EyeTrailEffect(EyeTrailController trail)
        {
            this.trail = trail;
        }

        public void Activate()
        {
            trail?.ActivateRedEyeTrail();
        }

        public void Deactivate()
        {
            trail?.DeactivateRedEyeTrail();
        }

        public List<KPrefabID> GetAttackTargets()
        {
            return new List<KPrefabID>();
        }
    }
}