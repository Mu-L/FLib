using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace FLib.Unity
{
    [ExecuteInEditMode]
    public class RenderEnvironment : MonoBehaviour
    {
        public bool IsRecordLastSetting;
        public SettingData Setting;
        [NonSerialized]
        public SettingData LastSetting;

        [Serializable]
        public class SettingData
        {
            public AmbientMode EnvMode;
            public Color EnvAmbientColor;
            public Color EnvEquatorColor;
            public Color EnvGroundColor;
            public Color EnvSkyColor;
            public Color ShadowColor;
            public float EnvIntensityMultiplier;

            public FogMode FogMode;
            public bool IsFogEnable;
            public Color FogColor;
            public float FogDensity;
            public float FogBeginDensity;
            public float FogEndDensity;

            public Material SkyboxMaterial;
            public Light Sun;


            public void CopyFromUnity()
            {
                EnvMode = RenderSettings.ambientMode;
                EnvIntensityMultiplier = RenderSettings.ambientIntensity;
                EnvAmbientColor = RenderSettings.ambientLight;
                EnvEquatorColor = RenderSettings.ambientEquatorColor;
                EnvGroundColor = RenderSettings.ambientGroundColor;
                EnvSkyColor = RenderSettings.ambientSkyColor;
                ShadowColor = RenderSettings.subtractiveShadowColor;
                IsFogEnable = RenderSettings.fog;
                FogMode = RenderSettings.fogMode;
                FogColor = RenderSettings.fogColor;
                FogDensity = RenderSettings.fogDensity;
                FogBeginDensity = RenderSettings.fogStartDistance;
                FogEndDensity = RenderSettings.fogEndDistance;
                SkyboxMaterial = RenderSettings.skybox;
                Sun = RenderSettings.sun;
            }

            public void ApplyToUnity()
            {
                RenderSettings.ambientMode = EnvMode;
                RenderSettings.ambientIntensity = EnvIntensityMultiplier;
                RenderSettings.ambientLight = EnvAmbientColor;
                RenderSettings.ambientEquatorColor = EnvEquatorColor;
                RenderSettings.ambientGroundColor = EnvGroundColor;
                RenderSettings.ambientSkyColor = EnvSkyColor;
                RenderSettings.subtractiveShadowColor = ShadowColor;
                RenderSettings.fog = IsFogEnable;
                RenderSettings.fogMode = FogMode;
                RenderSettings.fogColor = FogColor;
                RenderSettings.fogDensity = FogDensity;
                RenderSettings.fogStartDistance = FogBeginDensity;
                RenderSettings.fogEndDistance = FogEndDensity;
                RenderSettings.skybox = SkyboxMaterial;
                RenderSettings.sun = Sun;
            }
        }


        private void Awake()
        {
            if (Setting == null)
            {
                Setting = new();
                Setting.CopyFromUnity();
            }
        }

        private void OnEnable()
        {
            PushToUnity();
        }

        private void OnDisable()
        {
            PopFromUnity();
        }

        public void PushToUnity()
        {
#if !UNITY_EDITOR
            if (IsRecordLastSetting)
#endif
            {
                (LastSetting ??= new SettingData()).CopyFromUnity();
            }
            Setting.ApplyToUnity();
        }

        public void PopFromUnity()
        {
#if !UNITY_EDITOR
            if (IsRecordLastSetting)
#endif
            {
                if (LastSetting != null)
                {
                    LastSetting.ApplyToUnity();
                    LastSetting = null;
                }
            }
        }

#if UNITY_EDITOR
        private void CopyFromUnity()
        {
            Setting.CopyFromUnity();
        }

        private void OnValidate()
        {
            if (LastSetting == null)
                return;
            Setting.ApplyToUnity();
        }
#endif
    }
}
