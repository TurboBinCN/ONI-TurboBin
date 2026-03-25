using System;
using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;
using UnityEngine;
using static MutantContainmentProject.MutanterComponent.MutanterSkillComponent;

namespace MutantContainmentProject.MutanterComponent
{
    public class FixerWhiteLaserEffect : IExtraAnimationEffect
    {
        private FixerWhiteLaserController laser;

        public FixerWhiteLaserEffect(FixerWhiteLaserController laser)
        {
            this.laser = laser;
        }

        public void Activate()
        {
            laser?.ActivateLaser();
        }

        public void Deactivate()
        {
            laser?.DeactivateLaser();
        }
        public List<KPrefabID> GetAttackTargets()
        {
            return laser?.GetAttackTargets() ?? new List<KPrefabID>();
        }
    }
    public class FixerWhiteLaserController : KMonoBehaviour, ISimEveryTick
    {
        private ParticleSystem ParticleSystemInstance;
        public GameObject LaserInstance;
        private Facing facing;
        public Facing FacingCom => facing ??= GetComponent<Facing>();

        public bool isSkillActive;

        private float PlayLaserDelay = 0.7f;
        private float PlayLaserDelayTime = 0;
        public float BeamLength = 30f;

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

            // 检查是否完成旋转
            if (rotationProgress >= RotationFrameCount)
            {
                isRotating = false;
                rotationProgress = 0f;
                ParticleSystemInstance.Stop();
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

            // 计算旋转过程中所有可能的路径
            for (int i = 0; i <= RotationFrameCount; i++)
            {
                // 计算当前旋转角度
                float rotationProgress = i;
                float currentRotation = (rotationProgress / RotationFrameCount) * RotationAngle * FacingDirection * -1;

                // 计算当前方向
                Vector3 initialDirection = new Vector3(FacingDirection * Mathf.Cos(Mathf.Deg2Rad * StartAngle), Mathf.Sin(Mathf.Deg2Rad * StartAngle), 0f).normalized;
                Quaternion rotation = Quaternion.Euler(0, 0, currentRotation);
                Vector3 currentDirection = rotation * initialDirection;
                currentDirection = currentDirection.normalized;

                // 计算激光终点位置
                Vector3 endPosition = BasePosition + currentDirection * BeamLength;

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
        public void ActivateLaser()
        {
            TbbDebuger.LogDebug($"FixerWhiteLaserController激活!");
            isSkillActive = true;
            rotationProgress = 0f;

            InitializeDefaultParams();

            LaserInstance.SetActive(true);
            ParticleSystemInstance?.Play();
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

            beamDirection = new Vector3(FacingDirection, 0, 0);

            // 激光光束旋转需要保证模拟空间使用local
            ParticleSystem.MainModule mainModule = ParticleSystemInstance.main;
            mainModule.startSpeed = 50f;
            mainModule.simulationSpace = ParticleSystemSimulationSpace.Local;

            ParticleSystemInstance?.Play();

            // 检测旋转激光路径上的碰撞
            CheckRotatingLaserCollision();

        }
    }
}
