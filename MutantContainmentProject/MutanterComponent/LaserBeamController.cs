using TBB.He.TbbLib.Debuger;
using UnityEngine;
using static MutantContainmentProject.MutanterComponent.MutanterSkillComponent;

namespace MutantContainmentProject.MutanterComponent
{
    public class LaserBeamEffect : IExtraAnimationEffect
    {
        private LaserBeamController laserBeam;

        public LaserBeamEffect(LaserBeamController laserBeam)
        {
            this.laserBeam = laserBeam;
        }

        public void Activate()
        {
            laserBeam?.ActiveParticle();
        }

        public void Deactivate()
        {
            laserBeam?.DeactiveParticle();
        }
    }
    public class LaserBeamController : KMonoBehaviour, ISimEveryTick
    {
        public static string ID = "LaserBeam";
        [Header("激光束配置")]
        public string gunBaseSymbolName = "snapto_gun_base";
        public string gunEndSymbolName = "snapto_gun_end";
        public float beamLength = 15f;
        public float beamWidth = 0.3f;
        public Color beamColor = Color.red;

        private Vector3 gunBasePosition;
        private Vector3 gunEndPosition;
        private Vector3 beamDirection;
        private float beamDistance;
        private bool isSkillActive = false;
        private GameObject beamInstance;
        private ParticleSystem particleSystem;
        private Texture2D particleTexture;

        private Facing facing;
        public Facing FacingCom => facing ??= gameObject.GetComponent<Facing>();

        private KBatchedAnimController animController;
        public KBatchedAnimController AnimController => animController ??= gameObject.GetComponent<KBatchedAnimController>();

        protected override void OnSpawn()
        {
            base.OnSpawn();

            if (AnimController == null)
            {
                TbbDebuger.LogError($"在[{gameObject?.name}]上未找到KBatchedAnimController组件！");
                return;
            }

            // 生成粒子纹理
            particleTexture = GenerateCircleTexture();

            // 创建并配置光束实例和粒子系统
            CreateBeamInstance();
        }

        protected override void OnCleanUp()
        {
            DeactiveParticle();
            if (particleTexture != null)
            {
                Destroy(particleTexture);
                particleTexture = null;
            }
            animController = null;
            base.OnCleanUp();
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

        // 创建并配置光束实例和粒子系统
        private void CreateBeamInstance()
        {
            if (beamInstance == null)
            {
                // 创建光束实例游戏对象
                beamInstance = new GameObject("LaserBeamFX");
                beamInstance.transform.position = Vector3.zero;
                beamInstance.transform.localScale = Vector3.one;
                beamInstance.SetActive(false);

                // 确保光束在正确的渲染层
                Vector3 pos = beamInstance.transform.position;
                pos.z = Grid.GetLayerZ(Grid.SceneLayer.FXFront);
                beamInstance.transform.position = pos;

                // 添加粒子系统组件
                particleSystem = beamInstance.AddComponent<ParticleSystem>();

                // 配置粒子系统
                ParticleSystem.MainModule mainModule = particleSystem.main;
                mainModule.startColor = beamColor;
                mainModule.startSize = beamWidth * 3f;
                mainModule.startSpeed = 50f;
                mainModule.maxParticles = 10000;
                mainModule.duration = 0f;
                mainModule.loop = true;
                mainModule.playOnAwake = false;
                mainModule.gravityModifier = 0f;
                mainModule.simulationSpace = ParticleSystemSimulationSpace.Local;
                mainModule.startLifetime = beamLength / 5f;

                // 配置发射模块
                ParticleSystem.EmissionModule emissionModule = particleSystem.emission;
                emissionModule.enabled = true;
                emissionModule.rateOverTime = 1000f;

                // 配置形状模块
                ParticleSystem.ShapeModule shapeModule = particleSystem.shape;
                shapeModule.shapeType = ParticleSystemShapeType.Cone;
                shapeModule.radius = 0.01f;
                shapeModule.angle = 0.5f;
                shapeModule.rotation = new Vector3(0, 0, 0);

                // 配置速度模块
                ParticleSystem.VelocityOverLifetimeModule velocityModule = particleSystem.velocityOverLifetime;
                velocityModule.enabled = false; // 禁用速度模块，使用初始速度

                // 配置大小随时间变化
                ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particleSystem.sizeOverLifetime;
                sizeOverLifetime.enabled = true;
                AnimationCurve sizeCurve = new AnimationCurve();
                sizeCurve.AddKey(0.0f, 1f);
                sizeCurve.AddKey(1f, 0.5f);
                sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

                // 配置颜色随时间变化
                ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
                colorOverLifetime.enabled = true;
                Gradient gradient = new Gradient();
                // 大幅增加颜色亮度，使激光束更明显
                Color brightColor = new Color(beamColor.r * 2f, beamColor.g * 2f, beamColor.b * 2f, 1f);
                Color midColor = new Color(beamColor.r * 1.5f, beamColor.g * 1.5f, beamColor.b * 1.5f, 0.8f);
                gradient.SetKeys(
                    new GradientColorKey[] {
                        new(brightColor, 0.0f),
                        new(midColor, 0.3f),
                        new(beamColor, 0.6f),
                        new(new Color(beamColor.r, beamColor.g, beamColor.b, 0.5f), 1.0f)
                    },
                    new GradientAlphaKey[] {
                        new(1f, 0.0f),
                        new(0.9f, 0.3f),
                        new(0.7f, 0.6f),
                        new(0.0f, 1f)
                    }
                );
                colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

                // 配置渲染模块
                ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                {
                    Shader additiveShader = Shader.Find("Legacy Shaders/Particles/Additive");
                    Material particleMaterial;

                    if (additiveShader != null)
                    {
                        particleMaterial = new Material(additiveShader);
                    }
                    else
                    {
                        particleMaterial = new Material(Shader.Find("Sprites/Default"));
                    }

                    if (particleTexture != null)
                    {
                        particleMaterial.mainTexture = particleTexture;
                    }
                    renderer.material = particleMaterial;
                    renderer.sortingLayerName = "FXFront";
                    renderer.sortingOrder = 2000; // 大幅提高排序顺序，确保在最前面
                    renderer.renderMode = ParticleSystemRenderMode.Billboard;
                    renderer.maxParticleSize = 10f; // 增加最大粒子大小
                }
            }
        }

        // 计算激光发射位置和方向
        private void CalculateLaserParameters()
        {
            // 计算1x2小人的中间位置
            gunBasePosition = gameObject.transform.position;

            gunBasePosition.y += 1f; // 向上偏移1单位，使其位于1x2大小小人的中间
            gunBasePosition.z = Grid.GetLayerZ(Grid.SceneLayer.FXFront);

            // 检查物体的朝向，使用Facing组件
            float facingDirection = FacingCom.GetFacing() ? -1f : 1f; // Facing组件的GetFacing()返回true表示向左

            // 计算光束方向（直线型）
            beamDirection = new Vector3(facingDirection, 0, 0);
            beamDistance = beamLength;

            // 计算枪末端位置
            gunEndPosition = gunBasePosition + beamDirection * beamDistance;
        }

        // 激活激光束
        public void ActiveParticle()
        {
            if (AnimController == null)  return;
            TbbDebuger.LogDebug($"激活激光束");
            // 计算激光发射参数
            CalculateLaserParameters();

            // 标记为激活状态
            isSkillActive = true;
        }

        // 停用激光束
        public void DeactiveParticle()
        {
            if (beamInstance != null)
            {
                if (particleSystem != null)
                {
                    particleSystem.Stop(true); // 立即停止粒子系统
                    particleSystem.Clear(); // 清除所有粒子
                }
                beamInstance.SetActive(false); // 禁用对象，而不是销毁
            }
            isSkillActive = false;
        }

        public bool isRotating = false;
        private float rotationProgress = 0f;
        private float rotationDuration = 80f; // 30帧完成旋转
        private float targetRotation = 180f; // 目标旋转角度
        private Vector3 initialDirection;
        private float delayPlayTime = 0.5f;
        private float currentDelayTime = 0f;
        private static float StartAngle = 60f;

        // 开始光束旋转
        public void StartBeamRotation()
        {
            if (!isSkillActive || beamInstance == null || particleSystem == null)
            {
                TbbDebuger.LogWarning("[LaserBeamController] 激光束未激活，无法开始旋转");
                return;
            }
            rotationProgress = 0f;
            isRotating = true;
            currentDelayTime = Time.time;
            // 计算激光发射参数
            CalculateLaserParameters();
            float facingDirection = FacingCom.GetFacing() ? -1f : 1f; // Facing组件的GetFacing()返回true表示向左
            initialDirection = new Vector3(facingDirection * Mathf.Cos(Mathf.Deg2Rad * StartAngle), Mathf.Sin(Mathf.Deg2Rad * StartAngle), 0f).normalized;
        }

        public void SimEveryTick(float dt)
        {
            if (AnimController == null || !isSkillActive) return;

            if (beamInstance == null) CreateBeamInstance();
            if (particleSystem == null && beamInstance != null) particleSystem = beamInstance.GetComponent<ParticleSystem>();

            // 确保所有组件都初始化完成
            if (beamInstance == null || particleSystem == null)
            {
                TbbDebuger.LogWarning($"[LaserBeamController] 组件未初始化完成: beamInstance={beamInstance}, particleSystem={particleSystem}");
                return;
            }

            // 非旋转模式下，每帧更新激光参数
            if (!isRotating)
            {
                CalculateLaserParameters();
            }

            beamInstance.transform.position = gunBasePosition;
            if (beamDirection != Vector3.zero)
            {
                beamInstance.transform.rotation = Quaternion.LookRotation(beamDirection, Vector3.up);
            }

            if (!isRotating && !particleSystem.isPlaying)
            {
                beamInstance.SetActive(true);
                particleSystem.Play();
            }

            // 处理光束旋转
            if (isRotating)
            {
                HandleBeamRotation();
            }

            // 更新粒子系统的生命周期，根据光束长度
            ParticleSystem.MainModule mainModule = particleSystem.main;
            mainModule.startLifetime = beamLength / 5f; // 与初始化时保持一致
        }

        // 处理光束旋转
        private void HandleBeamRotation()
        {
            if (Time.time - currentDelayTime < delayPlayTime)
            {
                return;
            }
            if (!particleSystem.isPlaying)
            {
                beamInstance.SetActive(true);
                particleSystem.Play();
            }
            // 增加旋转进度
            rotationProgress += 1f;

            float facingDirection = FacingCom.GetFacing() ? -1f : 1f;
            // 确保初始方向不为零
            if (initialDirection == Vector3.zero)
            {
                initialDirection = new Vector3(facingDirection * Mathf.Cos(Mathf.Deg2Rad * StartAngle), Mathf.Sin(Mathf.Deg2Rad * StartAngle), 0f).normalized;
            }
            // 计算当前旋转角度，顺时针旋转
            float currentRotation = (rotationProgress / rotationDuration) * targetRotation * facingDirection * -1;
            // 围绕Z轴旋转初始方向
            Quaternion rotation = Quaternion.Euler(0, 0, currentRotation);
            // 应用旋转
            beamDirection = rotation * initialDirection;

            // 确保方向向量归一化
            if (beamDirection.magnitude > 0.01f)
            {
                beamDirection = beamDirection.normalized;
            }

            // 更新光束实例的位置和旋转
            beamInstance.transform.position = gunBasePosition;
            if (beamDirection != Vector3.zero)
            {
                // 调整旋转，使粒子系统沿光束方向发射
                // 由于粒子系统默认沿Z轴发射，我们需要将Z轴对准光束方向
                beamInstance.transform.rotation = Quaternion.LookRotation(beamDirection, Vector3.up);
            }

            // 检查是否完成旋转
            if (rotationProgress >= rotationDuration)
            {
                isRotating = false;
                rotationProgress = 0f;
                particleSystem.Stop();
            }
        }
    }
}