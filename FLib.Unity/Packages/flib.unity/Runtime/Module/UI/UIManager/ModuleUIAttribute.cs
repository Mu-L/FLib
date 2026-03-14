using System;

namespace FLib.Unity
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ModuleUIAttribute : ObjectInjectToAttribute
    {
        public Type MetaType;
        public Type ContextType;
        public int DefaultLayer = 1;

        /// <summary>
        /// <para>0: Object.Destroy();</para>
        /// <para>1: LoadedUI.gameObject.SetActive(false); Object.Destroy();</para>
        /// <para>2: Object.DestroyImmediate();</para>
        /// </summary>
        public byte DestroyImmediate;

        public ModuleUIAttribute() : base(nameof(UIMeta))
        {
        }
    }
}
