using System;
using System.Collections.Generic;
using System.Linq;
using TBB.He.TbbLib.Debuger;
using static MutantContainmentProject.MutanterComponent.MutanterSkillComponent;

namespace MutantContainmentProject.MutanterComponent.VFXController
{
    public class VFXManager : KMonoBehaviour
    {
        private Dictionary<string, Type> VFXList = new();
        protected override void OnSpawn()
        {
            base.OnSpawn();
            RegisterVFX();
        }

        private void RegisterVFX()
        {
            var VFXTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(IVFXController).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);

            foreach (var type in VFXTypes)
            {
                var attribute = type.GetCustomAttributes(typeof(VFXAttribute), false)
                    .FirstOrDefault() as VFXAttribute;

                if (attribute != null)
                {
                    try
                    {
                        VFXList.Add(attribute.Name, type);
                        var VFXController = gameObject.GetComponent(type) ?? gameObject.AddComponent(type);
                        TbbDebuger.LogDebug($"注册 VFX: {attribute.Name} ({type.Name}) to {gameObject.name} at {VFXController}");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to create trigger instance: {e.Message}");
                    }
                }
            }
        }
        public IVFXController GetVFXController(SkillData Skill)
        {
            if (Skill.VFXName != null && VFXList.TryGetValue(Skill.VFXName, out var vfx))
            {
                return gameObject.GetComponent(vfx) as IVFXController;
            }else{
                TbbDebuger.LogWarning($"未找到 VFX: [{Skill.VFXName}]实体：[{gameObject?.name}] 技能:[{Skill.name}]");
            }
            return null;
        }
    }
}
