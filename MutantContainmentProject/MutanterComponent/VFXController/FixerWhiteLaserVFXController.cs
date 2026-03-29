using System;
using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent.VFXController
{
    public class FixerWhiteLaserVFXController : KMonoBehaviour, ISimEveryTick
    {
        private ParticleSystem ParticleSystemInstance;
        public GameObject LaserInstance;
        private Facing facing;
        public Facing FacingCom => facing ??= GetComponent<Facing>();

        public bool isSkillActive;

        private float PlayLaserDelay = 0.7f;
        private float PlayLaserDelayTime = 0;
        public float BeamLength = 30f;

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
        private int currentLODLevel = 0;

        private float FacingDirection = 1;//默认面向右

        protected override void OnSpawn()
        {
            base.OnSpawn();
            try
            {
                GameObject prefab = MutantContainmentProjectMod.MutantContainmentProject.ModAssetBundle.LoadAsset<GameObject>("TheFixerWhiteLaser");
                if (prefab == null) TbbDebuger.LogError($"Failed to load TheFixerWhiteLaser prefab!");
                LaserInstance = Util.KInstantiate(prefab);
                if (LaserInstance != null)
                {
                    // 不设置父对象，使用世界坐标
                    LaserInstance.transform.SetParent(gameObject.transform, false);

                    LaserInstance.transform.localPosition = Vector3.zero;

                    LaserInstance.transform.localScale = Vector3.one;
                    LaserInstance.transform.localRotation = Quaternion.identity;

                    ParticleSystemInstance ??= LaserInstance.GetComponent<ParticleSystem>();
                    if (ParticleSystemInstance == null)
                        TbbDebuger.LogWarning($"Failed to get ParticleSystem component from LaserInstance!");
                    else
                    {
                        // 配置粒子系统，确保只有一条光束
                        //ConfigureParticleSystem();
                    }
                    ParticleSystemInstance?.Stop();
                    isSkillActive = false;
                    LaserInstance.SetActive(false);
                }
                else
                {
                    TbbDebuger.LogError($"Failed to instantiate LaserInstance!");
                }
            }
            catch (Exception e)
            {
                TbbDebuger.LogError($"Error in OnSpawn: {e.Message}");
            }
        }
        override protected void OnCleanUp()
        {
            DeactivateLaser();
            base.OnCleanUp();
        }
        // 计算激光发射位置和方向
        public Vector3 CalculateLaserParameters(float dt)
        {
            // 计算1x2小人的中间位置
            var basePosition = gameObject.transform.position;
            basePosition.y += 1f;
            basePosition.z = Grid.GetLayerZ(Grid.SceneLayer.FXFront);

            // 计算光束方向（直线型）
            var beamDirection = new Vector3(FacingDirection, 0, 0);

            return basePosition + beamDirection * BeamLength;
        }

        //旋转设置
        private Vector3 initialDirection;
        private float StartAngle = 90;
        public bool isRotating = false;
        private float RotationAngle = 240f;
        private float RotationFrameCount = 24f;
        private float rotationProgress = 0f;
        private Vector3 BasePosition;
        private Vector3 beamDirection;
        // 存储检测到的攻击目标
        private List<KPrefabID> attackTargets = new();
        private void HandleBeamRotation()
        {
            // 增加旋转进度
            rotationProgress += 1f;

            // 检查是否完成旋转
            if (rotationProgress >= RotationFrameCount)
            {
                DeactivateLaser();
                return;
            }

            // 确保初始方向不为零
            if (initialDirection == Vector3.zero)
            {
                initialDirection = new Vector3(FacingDirection * Mathf.Cos(Mathf.Deg2Rad * StartAngle), Mathf.Sin(Mathf.Deg2Rad * StartAngle), 0f).normalized;
            }
            // 计算当前旋转角度，顺时针旋转
            float currentRotation = (rotationProgress / RotationFrameCount) * RotationAngle * FacingDirection * -1;
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
            LaserInstance.transform.position = BasePosition;
            if (beamDirection != Vector3.zero)
            {
                // 调整旋转，使粒子系统沿光束方向发射
                // 由于粒子系统默认沿Z轴发射，我们需要将Z轴对准光束方向
                LaserInstance.transform.rotation = Quaternion.LookRotation(beamDirection, Vector3.up);
            }
        }

        private void CheckStraightLaserCollision()
        {
            // 清空之前的攻击目标列表
            attackTargets.Clear();

            // 计算激光终点位置（直线型）
            Vector3 beamDirection = new Vector3(FacingDirection, 0, 0);
            Vector3 endPosition = BasePosition + beamDirection * BeamLength;

            // 检测激光路径上的所有格子
            int startCell = Grid.PosToCell(BasePosition);
            int endCell = Grid.PosToCell(endPosition);

            // 使用Grid.CellToXY获取起始和结束格子的坐标
            int startX, startY, endX, endY;
            Grid.CellToXY(startCell, out startX, out startY);
            Grid.CellToXY(endCell, out endX, out endY);

            // 使用Bresenham算法遍历激光路径上的所有格子
            int dx = Math.Abs(endX - startX);
            int dy = Math.Abs(endY - startY);
            int sx = startX < endX ? 1 : -1;
            int sy = startY < endY ? 1 : -1;
            int err = dx - dy;

            int x = startX;
            int y = startY;

            while (true)
            {
                // 检查当前格子是否有效
                int cell = Grid.XYToCell(x, y);
                if (Grid.IsValidCell(cell))
                {
                    // 检查格子中的小人
                    GameObject minion = Grid.Objects[cell, (int)ObjectLayer.Minion];
                    if (minion != null)
                    {
                        var health = minion.GetComponent<Health>();
                        if (health != null && health.hitPoints > 0)
                        {
                            // 添加到攻击目标列表
                            KPrefabID kPrefabID = minion.GetComponent<KPrefabID>();
                            if (kPrefabID != null && !attackTargets.Contains(kPrefabID))
                            {
                                attackTargets.Add(kPrefabID);
                            }
                        }
                    }

                    // 检查格子中的生物
                    GameObject creature = Grid.Objects[cell, (int)ObjectLayer.Pickupables];
                    if (creature != null)
                    {
                        ObjectLayerListItem objectLayerListItem = creature.GetComponent<Pickupable>().objectLayerListItem;
                        while (objectLayerListItem != null)
                        {
                            Pickupable pickupable = objectLayerListItem.pickupable;
                            objectLayerListItem = objectLayerListItem.nextItem;
                            if (pickupable != null && pickupable.KPrefabID.HasTag(GameTags.Creature) && !pickupable.KPrefabID.HasTag(MutanterTags.Mutanter))
                            {
                                Health health = pickupable.GetComponent<Health>();
                                if (health != null && health.hitPoints > 0)
                                {
                                    // 添加到攻击目标列表
                                    KPrefabID kPrefabID = pickupable.KPrefabID;
                                    if (kPrefabID != null && !attackTargets.Contains(kPrefabID))
                                    {
                                        attackTargets.Add(kPrefabID);
                                    }
                                }
                            }
                        }
                    }
                }

                // 检查是否到达终点
                if (x == endX && y == endY)
                    break;

                // 继续Bresenham算法
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }
        }

        private void CheckRotatingLaserCollision()
        {
            // 清空之前的攻击目标列表
            attackTargets.Clear();

            // 获取激光起点的位置
            Vector3 laserPosition = BasePosition;

            // 计算扇面的最大半径
            float maxRadius = BeamLength;

            // 计算扇面的角度范围
            float sectorStart = StartAngle;
            float sectorEnd = StartAngle - RotationAngle * FacingDirection;
            // 确保角度在0-360范围内
            if (sectorStart < 0)
                sectorStart += 360;
            if (sectorEnd < 0)
                sectorEnd += 360;

            // 查找所有小人
            List<GameObject> minions = FindAllMinions();
            foreach (var minion in minions)
            {
                if (IsInSector(minion.transform.position, laserPosition, maxRadius, sectorStart, sectorEnd, FacingDirection))
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
                if (IsInSector(creature.transform.position, laserPosition, maxRadius, sectorStart, sectorEnd, FacingDirection))
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

        // 检查格子中的其他可能目标，提高检测覆盖率
        private void CheckAdditionalTargets(int cell)
        {
            // 检查格子中的建筑（如果需要）
            GameObject building = Grid.Objects[cell, (int)ObjectLayer.Building];
            if (building != null)
            {
                // 可以根据需要添加建筑检测逻辑
            }

            // 检查格子中的其他物体
            for (int layer = 0; layer < (int)ObjectLayer.NumLayers; layer++)
            {
                // 跳过已经检查过的层
                if (layer == (int)ObjectLayer.Minion || layer == (int)ObjectLayer.Pickupables || layer == (int)ObjectLayer.Building)
                    continue;

                GameObject obj = Grid.Objects[cell, layer];
                if (obj != null)
                {
                    // 检查是否是生物或可攻击目标
                    Health health = obj.GetComponent<Health>();
                    if (health != null && health.hitPoints > 0)
                    {
                        KPrefabID kPrefabID = obj.GetComponent<KPrefabID>();
                        if (kPrefabID != null && !attackTargets.Contains(kPrefabID))
                        {
                            attackTargets.Add(kPrefabID);
                        }
                    }
                }
            }
        }

        private void InitializeDefaultParams()
        {
            //初始化激光起点
            BasePosition = gameObject.transform.position;
            BasePosition.y += 1f;
            BasePosition.z = Grid.GetLayerZ(Grid.SceneLayer.FXFront);
            LaserInstance.transform.position = BasePosition;

            //朝向
            FacingDirection = FacingCom.GetFacing() ? -1f : 1f;
        }

        private void ConfigureParticleSystem()
        {
            // 激光光束需要保证模拟空间使用local
            ParticleSystem.MainModule mainModule = ParticleSystemInstance.main;
            mainModule.startSpeed = 50f;
            mainModule.simulationSpace = ParticleSystemSimulationSpace.Local;
        }

        private void UpdateLaserTransform(Vector3 direction)
        {
            LaserInstance.transform.position = BasePosition;
            if (direction != Vector3.zero)
            {
                LaserInstance.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }

        private void ActivateLaserCommon()
        {
            LaserInstance.SetActive(true);
            ParticleSystemInstance?.Play();
            isSkillActive = true;
        }

        public void ActivateLaser()
        {
            TbbDebuger.LogDebug($"FixerWhiteLaserController激活!");
            rotationProgress = 0f;
            isRotating = false;

            InitializeDefaultParams();
            ConfigureParticleSystem();
            
            // 设置初始方向为直线
            beamDirection = new Vector3(FacingDirection, 0, 0);
            UpdateLaserTransform(beamDirection);
            
            ActivateLaserCommon();
            PlayLaserDelayTime = PlayLaserDelay;

            // 检测直线激光路径上的碰撞
            CheckStraightLaserCollision();
        }

        public void DeactivateLaser()
        {
            TbbDebuger.LogDebug($"FixerWhiteLaserController取消激活!");
            isSkillActive = false;
            isRotating = false;
            rotationProgress = 0f;
            if (ParticleSystemInstance != null) ParticleSystemInstance.Stop();
            if (LaserInstance != null) LaserInstance.SetActive(false);
            PlayLaserDelayTime = 0;
        }
        public List<KPrefabID> GetAttackTargets()
        {
            TbbDebuger.LogDebug($"FixerWhiteLaserController获取攻击目标! 目标数量: {attackTargets.Count}");
            return attackTargets;
        }

        // 开始旋转
        public void StartRotation()
        {
            TbbDebuger.LogDebug("开始激光旋转");
            isRotating = true;
            rotationProgress = 0f;

            InitializeDefaultParams();
            ConfigureParticleSystem();

            // 初始化初始方向为旋转开始角度
            initialDirection = new Vector3(FacingDirection * Mathf.Cos(Mathf.Deg2Rad * StartAngle), Mathf.Sin(Mathf.Deg2Rad * StartAngle), 0f).normalized;
            beamDirection = initialDirection;

            // 设置初始旋转角度
            UpdateLaserTransform(beamDirection);

            ActivateLaserCommon();

            // 检测旋转激光路径上的碰撞
            CheckRotatingLaserCollision();

        }

        // 激活直线型激光
        public void ActivateStraightLaser()
        {
            TbbDebuger.LogDebug("激活直线型激光");
            isRotating = false;
            rotationProgress = 0f;

            InitializeDefaultParams();
            ConfigureParticleSystem();

            // 设置初始方向为直线
            beamDirection = new Vector3(FacingDirection, 0, 0);
            UpdateLaserTransform(beamDirection);

            ActivateLaserCommon();
            PlayLaserDelayTime = PlayLaserDelay;

            // 检测直线激光路径上的碰撞
            CheckStraightLaserCollision();

        }

        public void Deactivate()
        {
            this.DeactivateLaser();
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
            
            if (ParticleSystemInstance == null) return;
            
            ParticleSystem.MainModule mainModule = ParticleSystemInstance.main;
            ParticleSystem.EmissionModule emissionModule = ParticleSystemInstance.emission;
            
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
            if (!isSkillActive || ParticleSystemInstance == null || LaserInstance == null) return;
            if (PlayLaserDelayTime > 0)
            {
                PlayLaserDelayTime -= dt;
                return;
            }
            if (!isRotating)
            {
                LaserInstance.transform.position = CalculateLaserParameters(dt);
            }
            if (isRotating)
            {
                LaserInstance.transform.position = BasePosition;

                HandleBeamRotation();
            }

            // 更新LOD
            if (CameraController.Instance != null)
            {
                float distance = Vector3.Distance(transform.position, CameraController.Instance.transform.position);
                UpdateLOD(distance);
            }
        }
    }
}
