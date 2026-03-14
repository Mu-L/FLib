// =================================================={By Qcbf|qcbf@qq.com|2024-11-07}==================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FLib.Unity
{
    public class SimpleRendererFeature : ScriptableRendererFeature
    {
        public static Dictionary<string, SimpleRendererFeature> Features = new();

        public string Type = "Default";
        public List<Pass> Passes = new();

        private void OnEnable()
        {
            Features[Type] = this;
        }

        private void OnDisable()
        {
            Features.Remove(Type);
        }

        public override void Create()
        {
        }

        public static bool AddPass(string type, Pass pass, ELogLevel logLevel = ELogLevel.Fatal)
        {
            if (Features.TryGetValue(type, out var feature))
            {
                feature.Passes.Add(pass);
                return true;
            }
            Log.Get(logLevel)?.Write($"not found type {type}");
            return false;
        }

        public static bool RemovePass(string type, Pass pass)
        {
            return Features.TryGetValue(type, out var feature) && feature.Passes.Remove(pass);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            for (var i = Passes.Count - 1; i >= 0; i--)
            {
                var pass = Passes[i];
                renderer.EnqueuePass(pass);
                if (!pass.IsEveryFrame)
                    Passes.RemoveAt(i);
            }
        }


        public abstract class Pass : ScriptableRenderPass
        {
            public virtual string Name => nameof(SimpleRendererFeature);
            public virtual bool IsEveryFrame => false;
        }
    }
}
