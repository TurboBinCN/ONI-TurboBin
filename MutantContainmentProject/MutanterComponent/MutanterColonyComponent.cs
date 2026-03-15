using KSerialization;
using MutantContainmentProject.Buildings;
using System.Collections.Generic;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    public class MutanterColonyComponent : KMonoBehaviour
    {
        // 主畸变体信息
        [Serialize]
        private KPrefabID masterInstance;

        // 子畸变体列表
        [Serialize]
        private List<KPrefabID> slaveInstances = new List<KPrefabID>();

        // 物种信息
        [Serialize]
        private Tag speciesID;
        [Serialize]
        private MutanterDangerLevel dangerLevel;
        [Serialize]
        private int maxColonySize = 1;
        [Serialize]
        private float workingSpeedFactor = 1f;
        [Serialize]
        private float successRateFactor = 0.3f;

        // 安全措施偏好值（0~100%）
        // 每个操作类型对应三个等级的偏好值
        [Serialize]
        private Dictionary<SecureAction, float[]> secureActionPreferences = new();

        // 属性
        public KPrefabID MasterInstance
        {
            get { return masterInstance; }
            set { masterInstance = value; }
        }

        public List<KPrefabID> SlaveInstances
        {
            get { return slaveInstances; }
            set { slaveInstances = value; }
        }

        public Tag SpeciesID
        {
            get { return speciesID; }
            set { speciesID = value; }
        }

        public MutanterDangerLevel DangerLevel
        {
            get { return dangerLevel; }
            set { dangerLevel = value; }
        }

        public int MaxColonySize
        {
            get { return maxColonySize; }
            set { maxColonySize = value; }
        }

        public float WorkingSpeedFactor
        {
            get { return workingSpeedFactor; }
            set { workingSpeedFactor = value; }
        }

        public float SuccessRateFactor
        {
            get { return successRateFactor; }
            set { successRateFactor = value; }
        }

        // 只读属性
        public bool IsMaster => masterInstance == null;
        public bool IsSlave => masterInstance != null;
        public int SlaveCount => slaveInstances.Count;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            speciesID = gameObject.GetComponent<KPrefabID>().PrefabID();

            // 初始化安全措施偏好值字典
            InitializeSecureActionPreferences();
        }

        // 初始化安全措施偏好值
        private void InitializeSecureActionPreferences()
        {
            // 为每个操作类型初始化三个等级的偏好值
            secureActionPreferences = new Dictionary<SecureAction, float[]>
            {
                // 本能操作（对应勇气技能）
                [SecureAction.Instinct] = new float[] { 30f, 60f, 60f },
                // 洞察操作（对应防御技能）
                [SecureAction.Reconnaissance] = new float[] { 0f, 20f, 30f },
                // 自律操作（对应沟通技能）
                [SecureAction.Communicate] = new float[] { 20f, 40f, 50f },
                // 压迫操作（对应正义技能）
                [SecureAction.Intimidation] = new float[] { 10f, 30f, 40f }
            };
        }

        protected override void OnCleanUp()
        {
            base.OnCleanUp();

            // 如果是子畸变体，从主畸变体的子列表中移除
            if (IsSlave && masterInstance != null)
            {
                var masterColony = masterInstance.GetComponent<MutanterColonyComponent>();
                if (masterColony != null)
                {
                    masterColony.RemoveSlaveInstance(gameObject.GetComponent<KPrefabID>());
                }
            }

            // 如果是主畸变体，清理所有子畸变体的主引用
            if (IsMaster)
            {
                foreach (var slave in slaveInstances)
                {
                    if (slave != null)
                    {
                        var slaveColony = slave.GetComponent<MutanterColonyComponent>();
                        if (slaveColony != null)
                        {
                            slaveColony.MasterInstance = null;
                        }
                    }
                }
                slaveInstances.Clear();
            }
        }

        // 设置基本参数
        public void SetParameters(MutanterDangerLevel level, int maxSize, float speedFactor = 1f, float successFactor = 0.3f, Dictionary<SecureAction, float[]> preferences = null)
        {
            dangerLevel = level;
            maxColonySize = maxSize;
            workingSpeedFactor = speedFactor;
            successRateFactor = successFactor;

            // 如果提供了偏好值，则覆盖默认值
            if (preferences != null)
            {
                secureActionPreferences = preferences;
            }
        }

        // 添加子畸变体
        public void AddSlaveInstance(KPrefabID slavePrefabID)
        {
            if (slavePrefabID != null && !slaveInstances.Contains(slavePrefabID))
            {
                // 添加到子列表
                slaveInstances.Add(slavePrefabID);

                // 设置子畸变体的主引用
                var slaveColony = slavePrefabID.GetComponent<MutanterColonyComponent>();
                if (slaveColony != null)
                {
                    slaveColony.MasterInstance = gameObject.GetComponent<KPrefabID>();
                }
            }
        }

        // 移除子畸变体
        public void RemoveSlaveInstance(KPrefabID slavePrefabID)
        {
            if (slavePrefabID != null)
            {
                // 从子列表中移除
                slaveInstances.Remove(slavePrefabID);

                // 清除子畸变体的主引用
                var slaveColony = slavePrefabID.GetComponent<MutanterColonyComponent>();
                if (slaveColony != null)
                {
                    slaveColony.MasterInstance = null;
                }
            }
        }

        // 检查是否包含子畸变体
        public bool HasSlaveInstance(KPrefabID slavePrefabID)
        {
            return slaveInstances.Contains(slavePrefabID);
        }

        // 获取主畸变体的群落组件
        public MutanterColonyComponent GetMasterColony()
        {
            if (masterInstance != null)
            {
                return masterInstance.GetComponent<MutanterColonyComponent>();
            }
            return null;
        }

        // 检查是否达到最大子畸变体数量
        public bool IsAtMaxSlaveCount()
        {
            return slaveInstances.Count >= maxColonySize;
        }

        // 获取安全措施偏好值
        public float GetSecureActionPreference(SecureAction action, int level)
        {
            if (secureActionPreferences.TryGetValue(action, out float[] levels) && level >= 0 && level < 3)
            {
                return levels[level];
            }
            return 50f; // 默认值
        }

        // 设置安全措施偏好值
        public void SetSecureActionPreference(SecureAction action, int level, float value)
        {
            if (level >= 0 && level < 3)
            {
                if (!secureActionPreferences.ContainsKey(action))
                {
                    secureActionPreferences[action] = new float[] { 50f, 50f, 50f };
                }
                secureActionPreferences[action][level] = Mathf.Clamp(value, 0f, 100f); // 确保值在0~100之间
            }
        }

        // 获取安全措施偏好值字典
        public Dictionary<SecureAction, float[]> GetSecureActionPreferences()
        {
            return secureActionPreferences;
        }
    }
}