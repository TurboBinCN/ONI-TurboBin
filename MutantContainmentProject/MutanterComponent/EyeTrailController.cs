using TBB.He.TbbLib.Debuger;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace MutantContainmentProject.MutanterComponent
{
    public class EyeTrailController : KMonoBehaviour, ISimEveryTick
    {
        [Header("拖尾配置")]
        public string eyeSymbolName = "snapto_eye";

        private Vector3 eyeMarkerWorldPosition;
        private bool isSkillActive = false;
        private KBatchedAnimController animController;
        private GameObject trailInstance;
        private ParticleSystem particleSystem;
        private Texture2D particleTexture;

        public KBatchedAnimController AnimController
        {
            get
            {
                if (animController == null)
                {
                    animController = gameObject.GetComponent<KBatchedAnimController>();
                }
                return animController;
            }
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();

            TbbDebuger.LogDebug($"[EyeTrailController] OnSpawn 开始，游戏对象：{gameObject?.name}");

            if (AnimController == null)
            {
                TbbDebuger.LogError($"在[{gameObject?.name}]上未找到KBatchedAnimController组件！");
                return;
            }
            TbbDebuger.LogDebug("[EyeTrailController] 找到KBatchedAnimController组件");

            // 尝试找到眼睛标记点
            if (!FindEyeMarker())
            {
                TbbDebuger.LogWarning($"在[{gameObject?.name}]上未找到标记点：{eyeSymbolName}，使用默认位置");
                // 使用对象中心作为默认位置
                Vector3 defaultPosition = AnimController.transform.position;
                defaultPosition.z = Grid.GetLayerZ(Grid.SceneLayer.FXFront);
                eyeMarkerWorldPosition = defaultPosition;
            }

            // 生成粒子纹理
            particleTexture = GenerateCircleTexture();
            TbbDebuger.LogDebug("[EyeTrailController] 生成粒子纹理完成");

            TbbDebuger.LogDebug($"[EyeTrailController] 红眼拖尾初始化完成，初始位置：{eyeMarkerWorldPosition}");
        }
        
        // 检查组件是否被正确添加
        public void DebugCheck()
        {
            TbbDebuger.LogDebug($"[EyeTrailController] DebugCheck - 游戏对象：{gameObject?.name}");
            TbbDebuger.LogDebug($"[EyeTrailController] AnimController: {AnimController != null}");
            TbbDebuger.LogDebug($"[EyeTrailController] 标记点位置：{eyeMarkerWorldPosition}");
            TbbDebuger.LogDebug($"[EyeTrailController] 拖尾实例：{trailInstance != null}");
            TbbDebuger.LogDebug($"[EyeTrailController] 技能激活状态：{isSkillActive}");
        }

        protected override void OnCleanUp()
        {
            DeactivateRedEyeTrail();
            if (particleTexture != null)
            {
                Destroy(particleTexture);
                particleTexture = null;
            }
            animController = null;
            base.OnCleanUp();
        }

        private bool FindEyeMarker()
        {
            bool isFound = false;
            Matrix4x4 symbolMatrix = AnimController.GetSymbolTransform(eyeSymbolName, out isFound);

            if (!isFound)
            {
                TbbDebuger.LogWarning($"[EyeTrailController] 未找到标记点：{eyeSymbolName}，使用默认位置");
                // 使用对象中心作为默认位置
                eyeMarkerWorldPosition = AnimController.transform.position;
                eyeMarkerWorldPosition.z = Grid.GetLayerZ(Grid.SceneLayer.FXFront);
                return false; // 找不到标记点，返回false
            }

            Vector3 localPos = symbolMatrix.GetColumn(3);
            eyeMarkerWorldPosition = AnimController.transform.TransformPoint(localPos);
            eyeMarkerWorldPosition.z = Grid.GetLayerZ(Grid.SceneLayer.FXFront);
            
            TbbDebuger.LogDebug($"[EyeTrailController] 找到标记点：{eyeSymbolName}，位置：{eyeMarkerWorldPosition}");
            
            return true; // 找到标记点，返回true
        }

        public void ActivateRedEyeTrail()
        {
            TbbDebuger.LogDebug("[EyeTrailController] 开始激活红眼拖尾（粒子系统）");

            if (AnimController == null)
            {
                TbbDebuger.LogWarning("[EyeTrailController] 动画控制器为空，无法激活拖尾");
                return;
            }

            // 确保没有已存在的拖尾实例
            DeactivateRedEyeTrail();

            // 尝试找到眼睛标记点
            bool found = FindEyeMarker();
            if (!found)
            {
                TbbDebuger.LogWarning($"在[{gameObject?.name}]上未找到标记点：{eyeSymbolName}，使用默认位置");
                // 使用对象中心作为默认位置
                Vector3 defaultPosition = AnimController.transform.position;
                defaultPosition.z = Grid.GetLayerZ(Grid.SceneLayer.FXFront);
                eyeMarkerWorldPosition = defaultPosition;
            }
            
            // 创建拖尾实例游戏对象
            trailInstance = new GameObject("EyeTrailFX");
            trailInstance.transform.position = eyeMarkerWorldPosition;
            trailInstance.transform.localScale = Vector3.one;
            trailInstance.SetActive(true);
            
            // 确保拖尾在正确的渲染层
            Vector3 pos = trailInstance.transform.position;
            pos.z = Grid.GetLayerZ(Grid.SceneLayer.FXFront);
            trailInstance.transform.position = pos;
            TbbDebuger.LogDebug($"[EyeTrailController] 拖尾实例已设置到FXFront层，Z位置：{trailInstance.transform.position.z}");
            TbbDebuger.LogDebug($"[EyeTrailController] 拖尾位置：{eyeMarkerWorldPosition}");
            
            // 添加粒子系统组件
            particleSystem = trailInstance.AddComponent<ParticleSystem>();
            TbbDebuger.LogDebug("[EyeTrailController] 添加ParticleSystem组件");
            
            // 配置粒子系统 - 使用与manualMiner类似的配置
            ParticleSystem.MainModule mainModule = particleSystem.main;
            mainModule.startColor = Color.red;
            mainModule.startSize = 0.5f; // 适中的粒子大小
            mainModule.startSpeed = 2.0f; // 初始速度
            mainModule.maxParticles = 1000; // 更多的粒子数量
            mainModule.duration = 3.0f; // 持续时间
            mainModule.loop = true; // 循环模式
            mainModule.playOnAwake = true; // 唤醒时播放
            mainModule.gravityModifier = 0f; // 无重力
            mainModule.simulationSpace = ParticleSystemSimulationSpace.Local; // 本地空间，粒子会跟随父物体移动
            mainModule.startLifetime = 1.5f; // 延长生命周期
            
            // 启用发射模块，设置发射速率
            ParticleSystem.EmissionModule emissionModule = particleSystem.emission;
            emissionModule.enabled = true;
            emissionModule.rateOverTime = 50f; // 较高的发射速率
            
            // 配置形状模块，设置方向发射
            ParticleSystem.ShapeModule shapeModule = particleSystem.shape;
            shapeModule.shapeType = ParticleSystemShapeType.Cone;
            shapeModule.radius = 0.1f;
            shapeModule.angle = 45f; // 发射角度
            shapeModule.rotation = new Vector3(0, 0, 0); // 发射方向
            
            // 配置大小随时间变化
            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0.0f, 0.5f);
            sizeCurve.AddKey(0.2f, 1f);
            sizeCurve.AddKey(1f, 0.0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
            
            // 配置颜色随时间变化
            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.red, 0.0f),
                    new GradientColorKey(Color.red, 0.5f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0.0f),
                    new GradientAlphaKey(0.5f, 0.5f),
                    new GradientAlphaKey(0.0f, 1f)
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
            
            // 启用拖尾模块
            ParticleSystem.TrailModule trails = particleSystem.trails;
            trails.enabled = true;
            trails.ratio = 1.0f; // 所有粒子都产生拖尾
            trails.lifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.5f); // 延长拖尾生命周期
            trails.minVertexDistance = 0.01f; // 最小顶点距离，使拖尾更平滑
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1.5f, new AnimationCurve(
                new Keyframe(0.0f, 1f),
                new Keyframe(1f, 0.0f)
            )); // 增加拖尾宽度
            trails.inheritParticleColor = true;
            trails.colorOverTrail = new ParticleSystem.MinMaxGradient(
                new Gradient() {
                    colorKeys = new GradientColorKey[] {
                        new GradientColorKey(Color.red, 0.0f),
                        new GradientColorKey(new Color(1f, 0.5f, 0f), 0.5f), // 使用RGB值代替Color.orange
                        new GradientColorKey(Color.yellow, 1.0f)
                    },
                    alphaKeys = new GradientAlphaKey[] {
                        new GradientAlphaKey(1f, 0.0f),
                        new GradientAlphaKey(0.7f, 0.5f),
                        new GradientAlphaKey(0.0f, 1f)
                    }
                }
            ); // 添加颜色渐变效果
            
            // 启用速度模块，确保粒子有速度
            ParticleSystem.VelocityOverLifetimeModule velocityModule = particleSystem.velocityOverLifetime;
            velocityModule.enabled = true;
            velocityModule.speedModifier = new ParticleSystem.MinMaxCurve(2f, 4f);
            
            // 配置渲染模块
            ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                // 使用与manualMiner相同的材质和纹理
                Material particleMaterial = new Material(Shader.Find("Sprites/Default"));
                if (particleTexture != null)
                {
                    particleMaterial.mainTexture = particleTexture;
                }
                renderer.material = particleMaterial;
                // 使用与manualMiner相同的排序层
                renderer.sortingLayerName = "Place";
                renderer.sortingOrder = 120;
                TbbDebuger.LogDebug("[EyeTrailController] 已设置粒子系统材质和排序层");
            }
            
            // 开始播放粒子系统
            particleSystem.Play();
            TbbDebuger.LogDebug("[EyeTrailController] 粒子系统已开始播放");

            isSkillActive = true;
            TbbDebuger.LogDebug($"[EyeTrailController] 红眼拖尾（粒子系统）已激活，位置：{eyeMarkerWorldPosition}");
        }

        public void DeactivateRedEyeTrail()
        {
            if (trailInstance != null)
            {
                // 停止粒子系统
                if (particleSystem != null)
                {
                    particleSystem.Stop();
                }
                Destroy(trailInstance);
                trailInstance = null;
                particleSystem = null;
            }
            isSkillActive = false;
            TbbDebuger.LogDebug("[EyeTrailController] 红眼拖尾（粒子系统）已关闭");
        }



        // 立即更新粒子系统位置
        public void UpdateParticlePosition()
        {
            if (!isSkillActive || AnimController == null || trailInstance == null)
            {
                return;
            }
            
            bool flag = false;
            Matrix4x4 symbolMatrix = AnimController.GetSymbolTransform(eyeSymbolName, out flag);
            if (flag)
            {
                Vector3 localPos = symbolMatrix.GetColumn(3);
                eyeMarkerWorldPosition = AnimController.transform.TransformPoint(localPos);
                eyeMarkerWorldPosition.z = Grid.GetLayerZ(Grid.SceneLayer.FXFront);
            }
            
            if (trailInstance != null)
            {
                trailInstance.transform.position = eyeMarkerWorldPosition;
            }
        }

        // 生成圆形粒子纹理
        private Texture2D GenerateCircleTexture()
        {
            int size = 64;
            Texture2D circleTexture = new Texture2D(size, size);
            Color[] colors = new Color[size * size];
            float center = (float)size * 0.5f;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = 1f - Mathf.Clamp01(Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / (center - 2f));
                    float alpha = distance * distance;
                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            
            circleTexture.SetPixels(colors);
            circleTexture.Apply();
            return circleTexture;
        }

        // 每帧生成粒子
        public void SimEveryTick(float dt)
        {
            if (!isSkillActive || AnimController == null || trailInstance == null || particleSystem == null)
            {
                return;
            }

            // 实时更新眼睛标记点位置
            bool found = FindEyeMarker();
            if (found)
            {
                TbbDebuger.LogDebug($"[EyeTrailController] 每帧更新时找到标记点：{eyeSymbolName}，位置：{eyeMarkerWorldPosition}");
            }
            else
            {
                TbbDebuger.LogWarning($"[EyeTrailController] 每帧更新时未找到标记点：{eyeSymbolName}，使用最后已知位置：{eyeMarkerWorldPosition}");
            }

            // 更新拖尾实例的位置
            if (trailInstance != null)
            {
                trailInstance.transform.position = eyeMarkerWorldPosition;
            }

            // 手动发射粒子
            SpawnParticles();
        }

        // 手动发射粒子
        private void SpawnParticles()
        {
            if (!isSkillActive || particleSystem == null || trailInstance == null)
            {
                return;
            }

            // 禁用手动发射，依赖emission模块自动发射
            // 这样粒子会在trailInstance的本地空间中发射，跟随trailInstance的位置移动
        }
    }
}