using KSerialization;
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
        public void SetParameters(MutanterDangerLevel level, int maxSize, float speedFactor = 1f, float successFactor = 0.3f)
        {
            dangerLevel = level;
            maxColonySize = maxSize;
            workingSpeedFactor = speedFactor;
            successRateFactor = successFactor;
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
    }
}