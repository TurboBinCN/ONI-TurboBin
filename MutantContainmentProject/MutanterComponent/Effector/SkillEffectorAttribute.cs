using System;

namespace MutantContainmentProject.MutanterComponent.Effector
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class SkillEffectorAttribute : Attribute
    {
        public string Name { get; }
        public int Priority { get; }
        public SkillEffectorAttribute(string name, int priority)
        {
            Name = name;
            Priority = priority;
        }
    }
}
