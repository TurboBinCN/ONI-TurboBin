using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent.VFXController
{
    [VFXAttribute("LaserBeamVFX")]
    public class LaserBeamVFXController : KMonoBehaviour, ISimEveryTick, IVFXController
    {
        public static string ID = "LaserBeam";
        [Header("激光束配置")]
        public string gunBaseSymbolName = "snapto_gun_base";
        public string gunEndSymbolName = "snapto_gun_end";
        public float beamLength = 15f;
        public float beamWidth = 0.3f;
        public Color beamColor = Color.red;

        // LOD配置
        [Header("LOD配置")]
        public float lodDistance1 = 5f; // 高细节距离
        public float lodDistance2 = 10f; // 中等细节距离
        public int maxParticlesHigh = 10000;
        public int maxParticlesMedium = 5000;
        public int maxParticlesLow = 1000;
        public float emissionRateHigh = 1000f;
        public float emissionRateMedium = 500f;
        public float emissionRateLow = 100f;

        private Vector3 gunBasePosition;
        private Vector3 beamDirection;
        private float beamDistance;
        private bool isSkillActive = false;
        private GameObject beamInstance;
        private ParticleSystem particleSystem;
        private Texture2D particleTexture;
        // 存储检测到的攻击目标
        private List<KPrefabID> attackTargets = new List<KPrefabID>();
        private int currentLODLevel = 0;

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
        }

        // 激活激光束
        public void ActiveParticle()
        {
            if (AnimController == null) return;
            TbbDebuger.LogDebug($"激活激光束");
            CalculateLaserParameters();

            isSkillActive = true;

            // 检测直线激光路径上的碰撞
            CheckLaserCollision();
        }

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
        public List<KPrefabID> GetAttackTargets()
        {
            TbbDebuger.LogDebug($"LaserBeamController获取攻击目标! 目标数量: {attackTargets.Count}");
            // 返回检测到的攻击目标列表
            return new List<KPrefabID>(attackTargets);
        }

        private void CheckRotatingLaserCollision()
        {
            // 清空之前的攻击目标列表
            attackTargets.Clear();

            // 获取激光起点的位置
            Vector3 laserPosition = gunBasePosition;

            // 计算扇面的最大半径
            float maxRadius = beamLength;

            // 计算扇面的角度范围
            float facingDirection = FacingCom.GetFacing() ? -1f : 1f;
            float sectorStart = StartAngle;
            float sectorEnd = StartAngle - targetRotation * facingDirection;
            // 确保角度在0-360范围内
            if (sectorStart < 0)
                sectorStart += 360;
            if (sectorEnd < 0)
                sectorEnd += 360;

            // 查找所有小人
            List<GameObject> minions = FindAllMinions();
            foreach (var minion in minions)
            {
                if (IsInSector(minion.transform.position, laserPosition, maxRadius, sectorStart, sectorEnd, facingDirection))
                {
                    var health = minion.GetComponent<Health>();
                    if (health != null && health.hitPoints > 0)
                    {
                        KPrefabID kPrefabID = minion.GetComponent<KPrefabID>();
                        if (kPrefabID != null && !attackTargets.Contains(kPrefabID))
                        {
                            attackTargets.Add(kPrefabID);
                        }
                    }
                }
            }

            // 查找所有生物
            List<GameObject> creatures = FindAllCreatures();
            foreach (var creature in creatures)
            {
                if (IsInSector(creature.transform.position, laserPosition, maxRadius, sectorStart, sectorEnd, facingDirection))
                {
                    var health = creature.GetComponent<Health>();
                    if (health != null && health.hitPoints > 0)
                    {
                        KPrefabID kPrefabID = creature.GetComponent<KPrefabID>();
                        if (kPrefabID != null && !attackTargets.Contains(kPrefabID))
                        {
                            attackTargets.Add(kPrefabID);
                        }
                    }
                }
            }
        }

        // 查找所有小人
        private List<GameObject> FindAllMinions()
        {
            List<GameObject> minions = new List<GameObject>();

            // 使用游戏内置的Components系统获取所有活着的小人
            foreach (var minionIdentity in Components.LiveMinionIdentities.Items)
            {
                if (minionIdentity != null && minionIdentity.gameObject != null)
                {
                    minions.Add(minionIdentity.gameObject);
                }
            }

            return minions;
        }

        // 查找所有生物
        private List<GameObject> FindAllCreatures()
        {
            List<GameObject> creatures = new List<GameObject>();

            // 遍历所有可拾取对象，查找生物
            foreach (var pickupable in Components.Pickupables.Items)
            {
                if (pickupable != null && pickupable.KPrefabID.HasTag(GameTags.Creature) && !pickupable.KPrefabID.HasTag(MutanterTags.Mutanter))
                {
                    creatures.Add(pickupable.gameObject);
                }
            }

            return creatures;
        }

        // 判断位置是否在扇面内
        private bool IsInSector(Vector3 position, Vector3 origin, float maxRadius, float sectorStart, float sectorEnd, float facingDirection)
        {
            // 计算距离
            float distance = Vector3.Distance(position, origin);
            if (distance > maxRadius)
                return false;

            // 计算角度
            Vector3 direction = position - origin;
            float angle = Mathf.Atan2(direction.y, direction.x * facingDirection);
            float angleDeg = Mathf.Rad2Deg * angle;
            if (angleDeg < 0)
                angleDeg += 360;

            // 检查角度是否在扇面范围内
            if (facingDirection > 0) // 面向右
            {
                if (sectorStart >= sectorEnd)
                {
                    return (angleDeg >= sectorEnd && angleDeg <= sectorStart);
                }
                else
                {
                    return (angleDeg >= sectorEnd || angleDeg <= sectorStart);
                }
            }
            else // 面向左
            {
                if (sectorStart <= sectorEnd)
                {
                    // 当面向左且sectorStart <= sectorEnd时，扇面是从sectorStart到360度，再从0度到sectorEnd
                    return (angleDeg >= sectorStart || angleDeg <= sectorEnd);
                }
                else
                {
                    // 当面向左且sectorStart > sectorEnd时，扇面是从sectorStart到sectorEnd
                    return (angleDeg >= sectorStart && angleDeg <= sectorEnd);
                }
            }
        }

        private void CheckLaserCollision()
        {
            // 清空之前的攻击目标列表
            attackTargets.Clear();

            // 计算激光终点位置
            Vector3 endPosition = gunBasePosition + beamDirection * beamLength;

            // 查找所有小人
            List<GameObject> minions = FindAllMinions();
            foreach (var minion in minions)
            {
                if (IsOnLaserPath(minion.transform.position, gunBasePosition, endPosition, beamWidth))
                {
                    var health = minion.GetComponent<Health>();
                    if (health != null && health.hitPoints > 0)
                    {
                        KPrefabID kPrefabID = minion.GetComponent<KPrefabID>();
                        if (kPrefabID != null && !attackTargets.Contains(kPrefabID))
                        {
                            attackTargets.Add(kPrefabID);
                        }
                    }
                }
            }

            // 查找所有生物
            List<GameObject> creatures = FindAllCreatures();
            foreach (var creature in creatures)
            {
                if (IsOnLaserPath(creature.transform.position, gunBasePosition, endPosition, beamWidth))
                {
                    var health = creature.GetComponent<Health>();
                    if (health != null && health.hitPoints > 0)
                    {
                        KPrefabID kPrefabID = creature.GetComponent<KPrefabID>();
                        if (kPrefabID != null && !attackTargets.Contains(kPrefabID))
                        {
                            attackTargets.Add(kPrefabID);
                        }
                    }
                }
            }
        }

        // 判断位置是否在激光路径上
        private bool IsOnLaserPath(Vector3 position, Vector3 start, Vector3 end, float width)
        {
            // 计算点到线段的距离
            float distance = Vector3.Distance(position, ClosestPointOnLine(start, end, position));

            // 检查距离是否在激光宽度范围内
            if (distance > width)
                return false;

            // 检查点是否在线段的延长线上
            Vector3 startToEnd = end - start;
            Vector3 startToPoint = position - start;
            Vector3 endToPoint = position - end;

            // 计算点积，检查点是否在线段范围内
            float dotStart = Vector3.Dot(startToEnd, startToPoint);
            float dotEnd = Vector3.Dot(-startToEnd, endToPoint);

            return dotStart >= 0 && dotEnd >= 0;
        }

        // 计算点到线段的最近点
        private Vector3 ClosestPointOnLine(Vector3 start, Vector3 end, Vector3 point)
        {
            Vector3 startToEnd = end - start;
            float lengthSquared = startToEnd.sqrMagnitude;

            if (lengthSquared == 0)
                return start;

            float t = Mathf.Clamp01(Vector3.Dot(point - start, startToEnd) / lengthSquared);
            return start + t * startToEnd;
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

            // 检测旋转激光路径上的碰撞
            CheckRotatingLaserCollision();
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

        public void Activate(GameObject target = null)
        {
            this.ActiveParticle();
        }

        public void Deactivate()
        {
            this.DeactiveParticle();
        }

        public void UpdateLOD(float distance)
        {
            int newLODLevel = 0;
            if (distance > lodDistance2)
            {
                newLODLevel = 2; // 低细节
            }
            else if (distance > lodDistance1)
            {
                newLODLevel = 1; // 中等细节
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

        public void SetLODLevel(int level)
        {
            currentLODLevel = level;
            
            if (particleSystem == null) return;
            
            ParticleSystem.MainModule mainModule = particleSystem.main;
            ParticleSystem.EmissionModule emissionModule = particleSystem.emission;
            
            switch (level)
            {
                case 0: // 高细节
                    mainModule.maxParticles = maxParticlesHigh;
                    emissionModule.rateOverTime = emissionRateHigh;
                    break;
                case 1: // 中等细节
                    mainModule.maxParticles = maxParticlesMedium;
                    emissionModule.rateOverTime = emissionRateMedium;
                    break;
                case 2: // 低细节
                    mainModule.maxParticles = maxParticlesLow;
                    emissionModule.rateOverTime = emissionRateLow;
                    break;
            }
        }

        public int GetCurrentLODLevel()
        {
            return currentLODLevel;
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

            // 更新LOD
            if (CameraController.Instance != null)
            {
                float distance = Vector3.Distance(transform.position, CameraController.Instance.transform.position);
                UpdateLOD(distance);
            }
        }
    }
}
