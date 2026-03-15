using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sources.Game.Editor.Misc
{
    /// <summary>
    /// Allow users to access Playmode 'step by step' button even if MultiplayerPlaymode is active.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeStepButtonFix
    {
        private static VisualElement _cachedToolbar;

        public static VisualElement Toolbar
        {
            get
            {
                if (_cachedToolbar == null)
                {
                    FetchToolbar();
                }

                return _cachedToolbar;
            }
        }

        static PlayModeStepButtonFix()
        {
            EditorApplication.playModeStateChanged += OnPlaymodeChanged;
        }

        private static void OnPlaymodeChanged(PlayModeStateChange mode)
        {
            if (mode == PlayModeStateChange.EnteredPlayMode)
            {
                EditorApplication.delayCall += EnableStepPlaymodeButton;
            }
        }

        public static void EnableStepPlaymodeButton()
        {
            if (Toolbar == null)
                return;

            var playmodeButtons = Toolbar.Q<VisualElement>("PlayMode");
            var multiplayerDropdown = playmodeButtons.Q<VisualElement>("playmode-dropdown");

            VisualElement stepButton = multiplayerDropdown?.parent.Query<VisualElement>("Step");

            if (stepButton == null)
                return;

            if (stepButton.ClassListContains("unity-disabled"))
            {
                stepButton.RemoveFromClassList("unity-disabled");
            }

            stepButton.SetEnabled(true);
        }

        private static void FetchToolbar()
        {
            var unityEditorAssembly = typeof(EditorWindow).Assembly;
            var guiViewType = unityEditorAssembly.GetType("UnityEditor.Toolbar");

            if (guiViewType == null)
            {
                Debug.LogError("Could not load Toolbar type through reflection.");
                return;
            }

            var allToolbars = Resources.FindObjectsOfTypeAll(guiViewType);

            if (allToolbars == null)
            {
                Debug.LogError("Could not find any 'Toolbar' instances.");
                return;
            }

            var toolbarRootAccessor = guiViewType.GetField("m_Root", BindingFlags.Instance | BindingFlags.NonPublic);
            if (toolbarRootAccessor == null)
            {
                Debug.LogError("Could not access to 'm_Root' member of Toolbar type.");
                return;
            }

            var root = (VisualElement)toolbarRootAccessor.GetValue(allToolbars[0]);

            if (root == null)
            {
                Debug.LogError("Could not access to Toolbar root object");
                return;
            }

            _cachedToolbar = root;
        }
    }
}
