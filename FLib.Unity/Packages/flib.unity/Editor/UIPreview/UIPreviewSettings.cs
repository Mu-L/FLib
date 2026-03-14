using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Kamgam.UIPreview
{
    public enum LogLevel
    {
        Log = 0,
        Warning = 1,
        Error = 2,
        NoLogs = 99
    }

    [Serializable]
    public class UIPreviewSettings
    {
        public static UIPreviewSettings Inst = new();
        public const string Version = "1.2.0";
        // public const string SettingsFilePath = "Packages/com.flib.unity/Editor/UIPreview/UIPreviewSettings.asset";

        [SerializeField, Tooltip(_LogLevelTooltip)]
        public LogLevel LogLevel = LogLevel.Warning;

        public const string _LogLevelTooltip = "Log levels to determine how many log messages will be shown (Log = all message, Error = only critical errors).";

        [Tooltip(_IgnoreErrorsTooltip)]
        public bool IgnoreErrors;

        public const string _IgnoreErrorsTooltip = "Should exceptions while rendering the preview be shown in the console or just ignored? Enable if you see error logs by some third party asset during preview.";

        [Tooltip(_DisableCustomScriptsInPreview)]
        public bool DisableCustomScriptsInPreview = true;

        public const string _DisableCustomScriptsInPreview = "Disable custom scripts in while rendering the preview. Helps to avoid side effects of [ExecuteAlways] scripts.";

        [Tooltip(_DisableCustomScriptsExclusions)]
        public string[] DisableCustomScriptsExclusions = new string[] { };

        public const string _DisableCustomScriptsExclusions = "If 'DisableCustomScripts' in ON then you can still exclude some namespaces from being excluded. Any component in any of these namespaces will not be excluded. Any component having this exact name will not be excluded.";

        [Tooltip(_ExecuteInPlayModeTooltip)]
        public bool ExecuteInPlayMode;

        public const string _ExecuteInPlayModeTooltip = "Should the preview be shown during play mode too?\nThe preview has to load and render the asset. Having two versions of an asset loaded can lead to lower performance or side effects. That's why it is disabled by default.";

        [Tooltip(_PreviewTextureResolution)]
        public int PreviewTextureResolution = 256;

        public const string _PreviewTextureResolution = "Side length of the square preview render texture.";

        [Header("Canvas Scaler")]
        [Tooltip(_UseCanvasScalerTooltip)]
        public bool UseCanvasScaler = true;

        public const string _UseCanvasScalerTooltip = "Should a canvas scaler be used for the rendering of the UI?";

        public CanvasScaler.ScaleMode UIScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

        public Vector2 ReferenceResolution = new(800, 600);

        public CanvasScaler.ScreenMatchMode ScreenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        [Range(0f, 1f)]
        public float MatchWidthOrHeight = 0.5f;

        public int ReferencePixelsPerUnit = 100;

        [Range(0f, 20f)]
        public float ScaleFactor = 1f;

        public CanvasScaler.Unit PhysicalUnit = CanvasScaler.Unit.Points;
        public int FallbackScreenDPI = 96;
        public int DefaultSpriteDPI = 96;

        protected static UIPreviewSettings cachedSettings;

        public static UIPreviewSettings GetOrCreateSettings() => Inst;
    }
}
