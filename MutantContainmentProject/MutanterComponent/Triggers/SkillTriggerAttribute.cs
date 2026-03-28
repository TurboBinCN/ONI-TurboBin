using System;

namespace MutantContainmentProject.MutanterComponent.Triggers
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class SkillTriggerAttribute : Attribute
    {
        public string Name { get; }
        public int Priority { get; }
        public bool IsPassive { get; }
        
        public SkillTriggerAttribute(string name, int priority, bool isPassive = false)
        {
            Name = name;
            Priority = priority;
            IsPassive = isPassive;
        }
    }
}
