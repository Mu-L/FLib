// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using FLib.WorldCores.TimeLogic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor.TimeLogic
{
    public class Window : EditorWindow
    {
        public object UserData;
        public StagePanel Stage;
        public TimeLogicRuntime Runtime => Stage.Runtime;
        public Action OnDestroyHandler;


        private void OnDestroy()
        {
            OnDestroyHandler?.Invoke();
        }

        private void CreateGUI()
        {
            rootVisualElement.RegisterKeyDown(_ => Close(), KeyCode.Escape).Focus();
        }

        /// <summary>
        /// 
        /// </summary>
        public Window Set(TimeLogicRuntime runtime, bool isAllowPlay = true)
        {
            rootVisualElement.Clear();
            Stage = new StagePanel(runtime, isAllowPlay) { name = titleContent.text, Window = this };
            rootVisualElement.Add(Stage);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public Window SetTitle(string titleText)
        {
            titleContent.text = titleText;
            if (Stage != null)
                Stage.name = titleText;
            return this;
        }
    }
}
