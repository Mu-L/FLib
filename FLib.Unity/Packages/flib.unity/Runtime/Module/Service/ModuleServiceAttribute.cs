using System;

namespace FLib.Unity
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class ModuleServiceAttribute : ObjectInjectToAttribute
    {
        public ModuleServiceAttribute() : base(nameof(ServiceMgr))
        {
        }
    }
}
