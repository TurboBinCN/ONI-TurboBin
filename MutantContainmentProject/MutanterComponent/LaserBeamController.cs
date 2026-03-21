using TBB.He.TbbLib.Debuger;
using UnityEngine;
using static Grid;

namespace MutantContainmentProject.MutanterComponent
{
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
        private bool foundedPlaySymbol;
        private KBatchedAnimController animController;
        private GameObject beamInstance;
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

            TbbDebuger.LogDebug($"[LaserBeamController] OnSpawn 开始，游戏对象：{gameObject?.name}");

            if (AnimController == null)
            {
                TbbDebuger.LogError($"在[{gameObject?.name}]上未找到KBatchedAnimController组件！");
                return;
            }
            TbbDebuger.LogDebug("[LaserBeamController] 找到KBatchedAnimController组件");

            // 生成粒子纹理
            particleTexture = GenerateCircleTexture();
            TbbDebuger.LogDebug("[LaserBeamController] 生成粒子纹理完成");

            // 创建并配置光束实例和粒子系统
            CreateBeamInstance();

            TbbDebuger.LogDebug("[LaserBeamController] 激光束控制器初始化完成");
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
                TbbDebuger.LogDebug($"[LaserBeamController] 光束实例已设置到FXFront层，Z位置：{beamInstance.transform.position.z}");

                // 添加粒子系统组件
                particleSystem = beamInstance.AddComponent<ParticleSystem>();
                TbbDebuger.LogDebug("[LaserBeamController] 添加ParticleSystem组件");

                // 配置粒子系统
                ParticleSystem.MainModule mainModule = particleSystem.main;
                mainModule.startColor = beamColor;
                mainModule.startSize = beamWidth * 3f; // 大幅增加粒子大小，使光束更明显
                mainModule.startSpeed = 50f; // 大幅增加速度，使粒子快速向前移动，形成射线效果
                mainModule.maxParticles = 10000; // 大幅增加粒子数量，使射线更密集
                mainModule.duration = 0f;
                mainModule.loop = true;
                mainModule.playOnAwake = false; // 不自动播放，需要手动激活
                mainModule.gravityModifier = 0f;
                mainModule.simulationSpace = ParticleSystemSimulationSpace.Local;
                mainModule.startLifetime = beamLength / 5f; // 调整生命周期，使粒子在光束长度范围内消失

                // 配置发射模块
                ParticleSystem.EmissionModule emissionModule = particleSystem.emission;
                emissionModule.enabled = true;
                emissionModule.rateOverTime = 1000f; // 大幅增加发射速率，使激光束更明显

                // 配置形状模块
                ParticleSystem.ShapeModule shapeModule = particleSystem.shape;
                shapeModule.shapeType = ParticleSystemShapeType.Cone;
                shapeModule.radius = 0.01f; // 更小的半径，使光束更集中
                shapeModule.angle = 0.5f; // 极小的角度，形成高度集中的光束
                shapeModule.rotation = new Vector3(0, 0, 0); // 调整旋转，使粒子沿Z轴发射

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
                    // 尝试使用Particles/Additive材质，如果找不到则使用默认材质
                    Shader additiveShader = Shader.Find("Particles/Additive");
                    Material particleMaterial;
                    
                    if (additiveShader != null)
                    {
                        particleMaterial = new Material(additiveShader);
                        TbbDebuger.LogDebug("[LaserBeamController] 使用Particles/Additive材质");
                    }
                    else
                    {
                        // 使用默认材质
                        particleMaterial = new Material(Shader.Find("Sprites/Default"));
                        TbbDebuger.LogWarning("[LaserBeamController] 未找到Particles/Additive材质，使用默认材质");
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
                    TbbDebuger.LogDebug("[LaserBeamController] 已设置粒子系统材质和排序层");
                }
            }
        }

        // 找到枪的两个标记点
        public enum MarkerFounderFlag { 
            Founded = 0,
            NotFounded = 1,
            Suppose = 2
        }
        private MarkerFounderFlag FindGunMarkers()
        {
            MarkerFounderFlag hasValidPosition = MarkerFounderFlag.NotFounded;

            // 检查物体的朝向，通过localScale.x的符号判断
            float facingDirection = transform.localScale.x > 0 ? 1f : -1f;

            // 找到枪底座标记点
            Matrix4x4 baseMatrix = AnimController.GetSymbolTransform(gunBaseSymbolName, out bool baseFound);
            if (baseFound)
            {
                Vector3 localPos = baseMatrix.GetColumn(3);
                gunBasePosition = AnimController.transform.TransformPoint(localPos);
                gunBasePosition.z = Grid.GetLayerZ(Grid.SceneLayer.FXFront);
                //TbbDebuger.LogDebug($"[LaserBeamController] 找到枪底座标记点：{gunBasePosition}");
                hasValidPosition = MarkerFounderFlag.Founded;
            }
            else
            {
                // 只有在首次找不到标记点时才使用默认位置
                // 如果之前已经找到过标记点，保持之前的位置
                if (gunBasePosition == Vector3.zero)
                {
                    //TbbDebuger.LogWarning($"[LaserBeamController] 未找到标记点：{gunBaseSymbolName}，使用默认位置");
                    // 使用对象中心作为默认位置，调整为畸变体（1x2）的中间位置
                    gunBasePosition = AnimController.transform.position;
                    gunBasePosition.y += 0.5f; // 向上偏移0.5单位，使其位于1x2大小畸变体的中间
                    gunBasePosition.z = Grid.GetLayerZ(Grid.SceneLayer.FXFront);
                    hasValidPosition = MarkerFounderFlag.Suppose;
                }
                else
                {
                    //TbbDebuger.LogWarning($"[LaserBeamController] 未找到标记点：{gunBaseSymbolName}，使用之前的位置");
                }
            }

            // 找到枪末端标记点
            Matrix4x4 endMatrix = AnimController.GetSymbolTransform(gunEndSymbolName, out bool endFound);
            if (endFound)
            {
                Vector3 localPos = endMatrix.GetColumn(3);
                gunEndPosition = AnimController.transform.TransformPoint(localPos);
                gunEndPosition.z = Grid.GetLayerZ(Grid.SceneLayer.FXFront);
                //TbbDebuger.LogDebug($"[LaserBeamController] 找到枪末端标记点：{gunEndPosition}");
                hasValidPosition = MarkerFounderFlag.Founded;
            }
            else
            {
                // 只有在首次找不到标记点时才使用默认位置
                // 如果之前已经找到过标记点，保持之前的位置
                if (gunEndPosition == Vector3.zero || gunBasePosition != Vector3.zero)
                {
                    //TbbDebuger.LogWarning($"[LaserBeamController] 未找到标记点：{gunEndSymbolName}，使用默认位置");
                    // 使用水平方向（x轴）作为默认方向，根据朝向调整方向
                    float beamDistance = 20f; // 合理的固定距离，确保在相机视野内可见
                    gunEndPosition = new Vector3(gunBasePosition.x + (beamDistance * facingDirection), gunBasePosition.y, gunBasePosition.z);
                    gunEndPosition.z = Grid.GetLayerZ(Grid.SceneLayer.FXFront);
                    hasValidPosition = MarkerFounderFlag.Suppose;
                }
                else
                {
                    //TbbDebuger.LogWarning($"[LaserBeamController] 未找到标记点：{gunEndSymbolName}，使用之前的位置");
                }
            }

            // 计算光束方向和距离
            Vector3 direction = gunEndPosition - gunBasePosition;
            if (direction.magnitude > 0.01f) // 避免除以零
            {
                beamDirection = direction.normalized;
            }
            else
            {
                // 如果两个位置相同，使用固定的默认方向，根据朝向调整
                beamDirection = new Vector3(facingDirection, 0, 0);
                // 手动设置end位置，确保有一个有效的方向
                gunEndPosition = gunBasePosition + beamDirection * 5f;
                //TbbDebuger.LogWarning($"[LaserBeamController] 枪标记点位置相同，使用固定默认方向：{beamDirection}，并调整end位置");
                hasValidPosition = MarkerFounderFlag.Founded;
            }
            beamDistance = Vector3.Distance(gunBasePosition, gunEndPosition);

            //TbbDebuger.LogDebug($"[LaserBeamController] 找到枪标记点：base={gunBasePosition}, end={gunEndPosition}, direction={beamDirection}, distance={beamDistance}");
            //TbbDebuger.LogDebug($"[LaserBeamController] 粒子生成坐标：{gunBasePosition}");

            if(!foundedPlaySymbol && hasValidPosition == MarkerFounderFlag.Founded) foundedPlaySymbol = true;
            return hasValidPosition; // 只有当有有效位置时才返回true
        }

        // 激活激光束
        public void ActiveParticle()
        {
            TbbDebuger.LogDebug("[LaserBeamController] 开始激活激光束");

            if (AnimController == null)
            {
                TbbDebuger.LogWarning("[LaserBeamController] 动画控制器为空，无法激活激光束");
                return;
            }

            // 标记为激活状态，即使当前找不到标记点
            isSkillActive = true;
            foundedPlaySymbol = false;
            TbbDebuger.LogDebug("[LaserBeamController] 激光束已标记为激活状态");
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
            foundedPlaySymbol = false;
            TbbDebuger.LogDebug("[LaserBeamController] 激光束已关闭");
        }

        public bool isRotating = false;
        private float rotationProgress = 0f;
        private float rotationDuration = 60f; // 30帧完成旋转
        private float targetRotation = 180f; // 目标旋转角度
        private Vector3 initialDirection;



        // 开始光束旋转
        public void StartBeamRotation()
        {
            TbbDebuger.LogDebug("[LaserBeamController] 开始光束旋转");
            
            if (!isSkillActive || beamInstance == null || particleSystem == null)
            {
                TbbDebuger.LogWarning("[LaserBeamController] 激光束未激活，无法开始旋转");
                return;
            }
            rotationProgress = 0f;
            isRotating = true;

            if (FindGunMarkers() == MarkerFounderFlag.Founded)
            {
                //初始化旋转初始位置，前方斜上方45度
                float facingDirection = transform.localScale.x > 0 ? 1f : -1f;
                initialDirection = new Vector3(facingDirection * Mathf.Cos(Mathf.Deg2Rad * 45), Mathf.Sin(Mathf.Deg2Rad * 45), 0f).normalized;
                
                TbbDebuger.LogDebug("[LaserBeamController] 找到标记点，开始旋转");
            }
            else
            {
                TbbDebuger.LogWarning("[LaserBeamController] 未找到标记点，无法开始旋转");
            }
            particleSystem.Simulate(0f, true, true);
        }

        // 每帧更新
        public void SimEveryTick(float dt)
        {
            if (AnimController == null || !isSkillActive) return;

            if (beamInstance == null)  CreateBeamInstance();
            // 激活光束实例
            beamInstance.SetActive(true);

            if (particleSystem == null && beamInstance != null) particleSystem = beamInstance.GetComponent<ParticleSystem>();

            // 确保所有组件都初始化完成
            if (beamInstance == null || particleSystem == null)
            {
                TbbDebuger.LogWarning($"[LaserBeamController] 组件未初始化完成: beamInstance={beamInstance}, particleSystem={particleSystem}");
                return;
            }
            
            // 尝试找到标记点
            MarkerFounderFlag foundMarkers = FindGunMarkers();
            if(!foundedPlaySymbol || foundMarkers == MarkerFounderFlag.NotFounded) return;


            // 更新光束实例的位置和旋转
            beamInstance.transform.position = gunBasePosition;
            if (beamDirection != Vector3.zero)
            {
                // 计算2D旋转角度（绕Z轴）
                float angle = Mathf.Atan2(beamDirection.y, beamDirection.x) * Mathf.Rad2Deg;
                // 调整旋转，使粒子系统沿光束方向发射
                // 由于粒子系统默认沿Z轴发射，我们需要将Z轴对准光束方向
                beamInstance.transform.rotation = Quaternion.LookRotation(beamDirection, Vector3.up);
            }

            // 开始播放粒子系统
            if (!particleSystem.isPlaying)
            {
                TbbDebuger.LogDebug("[LaserBeamController] 开始播放粒子系统...");
                particleSystem.Play();
                TbbDebuger.LogDebug("[LaserBeamController] 粒子系统已开始播放");
            }

            // 处理光束旋转
            if (isRotating)
            {
                HandleBeamRotation();
            }

            // 更新粒子系统的生命周期，根据光束长度
            ParticleSystem.MainModule mainModule = particleSystem.main;
            mainModule.startLifetime = beamLength / 5f; // 与初始化时保持一致

            //TbbDebuger.LogDebug($"[LaserBeamController] 每帧更新激光束位置：{gunBasePosition}，方向：{beamDirection}");
        }

        // 处理光束旋转
        private void HandleBeamRotation()
        {
            // 增加旋转进度
            rotationProgress += 1f;
            // 检查物体的朝向，通过localScale.x的符号判断
            float facingDirection = transform.localScale.x > 0 ? 1f : -1f;
            // 计算当前旋转角度，根据朝向调整旋转方向
            // 反转旋转方向，确保旋转方向正确
            float currentRotation = (rotationProgress / rotationDuration) * targetRotation * -facingDirection;
            
            // 围绕Z轴旋转初始方向
            Quaternion rotation = Quaternion.Euler(0, 0, currentRotation);
            // 设置初始方向为朝向的斜上方45度 向右（或向左）和向上的单位向量组合
            if(initialDirection == Vector3.zero)
                initialDirection = new Vector3(facingDirection * Mathf.Cos(Mathf.Deg2Rad * 45), Mathf.Sin(Mathf.Deg2Rad * 45), 0f).normalized;
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
                // 计算2D旋转角度（绕Z轴）
                float angle = Mathf.Atan2(beamDirection.y, beamDirection.x) * Mathf.Rad2Deg;
                // 调整旋转，使粒子系统沿光束方向发射
                // 由于粒子系统默认沿Z轴发射，我们需要将Z轴对准光束方向
                beamInstance.transform.rotation = Quaternion.LookRotation(beamDirection, Vector3.up);
            }
            TbbDebuger.LogDebug($"[LaserBeamController] 光束方向：{beamDirection} 旋转角度：{currentRotation} 旋转进度：{rotationProgress}");
            // 检查是否完成旋转
            if (rotationProgress >= rotationDuration)
            {
                isRotating = false;
                rotationProgress = 0f;
                TbbDebuger.LogDebug("[LaserBeamController] 光束旋转完成");
            }
        }
    }
}