using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using FLib;
using FLib.Unity;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Utilities;

namespace Modules
{
    public class UIMgr : UIRoot
    {
        private static Dictionary2<string, Type> _uiNameTypes;

        protected override void Awake()
        {
            LayerDefines = Enum.GetNames(typeof(EUILayer))
                .Select((v, i) => new LayerDefine() { Name = $"{v}s", EnableHistory = (EUILayer)i is EUILayer.Page or EUILayer.PopupPage }).ToArray();
            base.Awake();
            var page = Layer(EUILayer.Page);
            page.HiddenContainers = new[] { Layer(EUILayer.Background) };
            page.OnOpenUIEvent += (_, _) => UIUtility.UICameraOnly(250, static () => Layer(EUILayer.Page).OpenedUIs.Count > 0).Forget();
        }

        /// <summary>
        /// 
        /// </summary>
        private static Type GetUIType(string name) => (_uiNameTypes ??= new Dictionary2<string, Type>()).GetValueOrAdd(name) ??= UIMeta.UITypeMetas.SingleOrDefault(v => v.Key.Name == name).Key ?? throw new KeyNotFoundException(name);

        /// <summary>
        /// 
        /// </summary>
        public static UIContainer Layer(EUILayer layer) => Layers[(int)layer];

        /// <summary>
        /// 
        /// </summary>
        public static UIContainer Layer(Type uiType, EUILayer? layer) => Layers[(int)(layer ?? (EUILayer)UIMeta.UITypeMetas[uiType].DefaultLayer)];

        /// <summary>
        /// 
        /// </summary>
        public static UIContext Open<T>(EUILayer? layer = null) where T : UIBase => Layer(typeof(T), layer).Open<T>();

        /// <summary>
        /// 
        /// </summary>
        public static UIContext Open(string name, EUILayer? layer = null)
        {
            var uiType = GetUIType(name);
            return Layer(uiType, layer).Open(uiType);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Close<T>(EUILayer? layer = null) where T : UIBase => Layer(typeof(T), layer).Close(typeof(T));

        /// <summary>
        /// 
        /// </summary>
        public static void Close(Type type, EUILayer? layer = null) => Layer(type, layer).Close(type);

        /// <summary>
        /// 
        /// </summary>
        public static void CloseAll(bool containPopup = false)
        {
            if (containPopup)
                Layer(EUILayer.Popup).CloseAll();
            Layer(EUILayer.PopupPage).CloseAll();
            Layer(EUILayer.Page).CloseAll();
            Layer(EUILayer.Background).CloseAll();
        }

        /// <summary>
        /// 
        /// </summary>
        public static T GetUI<T>(EUILayer? layer = null, ELogLevel logLevel = ELogLevel.Fatal) where T : UIBase => Layer(typeof(T), layer).GetUI<T>(logLevel);

        /// <summary>
        /// 
        /// </summary>
        public static UIContext GetContext<T>(EUILayer? layer = null, ELogLevel logLevel = ELogLevel.Fatal) where T : UIBase => Layer(typeof(T), layer).Get<T>(logLevel);

        /// <summary>
        /// 
        /// </summary>
        public static bool IsOpen<T>(EUILayer? layer = null) where T : UIBase => Layer(typeof(T), layer).OpenedUIs.ContainsKey(typeof(T));
    }

    public enum EUILayer : byte
    {
        Background,
        Page,
        PopupPage,
        Popup,
        SystemPopup,
    }
}
