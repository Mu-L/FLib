using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FLib.Unity
{
    [ObjectInjectionReceiver(nameof(UIMeta), nameof(ReceiveInjection))]
    public class UIMeta
    {
        public static ReadOnlyDictionary<Type, UIMeta> UITypeMetas;
        public string AssetPath;
        public Type ContextType;
        public int DefaultLayer;
        public byte DestroyImmediate;

        public string SecondaryAssetPath => FIO.PathRename(AssetPath, UIRoot.Inst.Secondary.PrefabSuffix, true);
        public override string ToString() => AssetPath;

        public string GetCurrentAssetPath()
        {
            if (!UIRoot.Inst.Secondary.IsActivated || string.IsNullOrEmpty(UIRoot.Inst.Secondary.PrefabSuffix))
                return AssetPath;
            var secondaryPath = SecondaryAssetPath;
            return AssetLoader.ExistsAsset(secondaryPath) ? secondaryPath : AssetPath;
        }

        private static void ReceiveInjection(List<(object info, ObjectInjectToAttribute attr)> list)
        {
            var uiMetas = new Dictionary<Type, UIMeta>(list.Count);
            foreach (var item in list)
            {
                var type = (Type)item.info;
                var attr = (ModuleUIAttribute)item.attr;
                var meta = TypeAssistant.New(attr.MetaType ?? typeof(UIMeta)) as UIMeta ?? throw new Exception($"{type} need base type {nameof(UIMeta)}");

                if (attr.ContextType == null)
                {
                    var baseType = type.BaseType;
                    while (baseType != null && baseType != typeof(UIBase))
                    {
                        if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(UIBase<>))
                        {
                            attr.ContextType = baseType.GetGenericArguments()[0];
                            break;
                        }
                        baseType = baseType.BaseType;
                    }
                }
                meta.ContextType = attr.ContextType ?? typeof(UIContext);
                meta.DefaultLayer = attr.DefaultLayer;
                meta.DestroyImmediate = attr.DestroyImmediate;
                if (meta.AssetPath == null)
                {
                    string moduleName = null;
                    if (type.Namespace != null)
                    {
                        moduleName = type.Namespace;
                        var theLastNameIdx = moduleName.LastIndexOf('.') + 1;
                        if (theLastNameIdx > 0)
                            moduleName = moduleName[theLastNameIdx..];
                    }
                    meta.AssetPath = $"UI/{moduleName ?? type.Name}/{type.Name}.prefab";
                }
                uiMetas.Add(type, meta);
            }
            UITypeMetas = new ReadOnlyDictionary<Type, UIMeta>(uiMetas);
        }

        /// <summary>
        /// 
        /// </summary>
        public static UIMeta Get(Type uiType)
        {
            return !UITypeMetas.TryGetValue(uiType, out var meta) ? throw new Exception($"not found ui: {uiType}") : meta;
        }
    }
}
