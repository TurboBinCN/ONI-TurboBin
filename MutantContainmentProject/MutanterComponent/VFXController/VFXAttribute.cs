using System;

namespace MutantContainmentProject.MutanterComponent.VFXController
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class VFXAttribute : Attribute
    {
        public string Name { get; }
        public VFXAttribute(string name)
        {
            Name = name;
        }
    }
}
