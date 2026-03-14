// =================================================={By Qcbf|qcbf@qq.com|2024-2-3}==================================================

#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace FLib.Unity
{
    public class AnimatorTemplateData : ScriptableObject
    {
        public LayerData[] Layers;
        public Animator[] Links;

        [Serializable]
        public struct LayerData
        {
            public string Name;
            public StateData[] States;
            public string[] ObsoleteNames;
        }

        [Serializable]
        public struct StateData
        {
            public string Name;
            public string[] ObsoleteNames;
        }


        [MethodButton]
        public void Make()
        {
            var log = new StringBuilder(256).AppendLine();
            foreach (var link in Links = Links.Where(v => v != null).ToArray())
            {
                if (link != null)
                {
                    if (link.runtimeAnimatorController == null)
                    {
                        var path = FIO.PathRename(AssetDatabase.GetAssetPath(link), "Animator.controller", true, false);
                        link.runtimeAnimatorController = AnimatorController.CreateAnimatorControllerAtPath(path);
                        AssetDatabase.SaveAssetIfDirty(link);
                        log.AppendLine($"Create New Animator: {path}");
                    }

                    MakeAnimatorLayers((AnimatorController)link.runtimeAnimatorController, Layers, log);
                }
            }

            var str = log.ToString();
            if (!string.IsNullOrWhiteSpace(str))
                Log.Info?.Write(log);
        }

        private static void MakeAnimatorLayers(AnimatorController ac, in LayerData[] cfgLayers, StringBuilder log)
        {
            var curLayers = ac.layers;
            var curLayerDict = curLayers.ToDictionary(v => v.name);
            for (var i = cfgLayers.Length - 1; i >= 0; i--)
            {
                var cfgLayerName = cfgLayers[i].Name;
                if (!curLayerDict.TryGetValue(cfgLayerName, out var curLayer))
                {
                    ac.AddLayer(cfgLayerName);
                    curLayer = curLayerDict[cfgLayerName];
                    log.AppendLine($"add layer: {cfgLayerName}");
                }

                MakeAnimatorLayerStates(curLayer, cfgLayers[i].States, log);

                foreach (var obsoleteName in cfgLayers[i].ObsoleteNames)
                {
                    if (curLayerDict.Remove(cfgLayerName, out curLayer))
                    {
                        for (var j = curLayers.Length - 1; j >= 0; j--)
                        {
                            if (curLayers[j] == curLayer)
                            {
                                log.AppendLine($"delete layer: {obsoleteName}");
                                ac.RemoveLayer(j);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private static void MakeAnimatorLayerStates(AnimatorControllerLayer curLayer, StateData[] cfgStates, StringBuilder log)
        {
            var curStates = curLayer.stateMachine.states.ToDictionary(v => v.state.name, v => v.state);
            for (var i = cfgStates.Length - 1; i >= 0; i--)
            {
                var stateName = cfgStates[i].Name;
                if (!curStates.ContainsKey(stateName))
                {
                    curStates.Add(stateName, curLayer.stateMachine.AddState(stateName));
                    log.AppendLine($"add state: {stateName}");
                }

                foreach (var obsoleteName in cfgStates[i].ObsoleteNames)
                {
                    if (curStates.Remove(obsoleteName, out var curState))
                    {
                        curLayer.stateMachine.RemoveState(curState);
                        log.AppendLine($"delete state: {stateName}");
                    }
                }
            }

            curLayer.stateMachine.defaultState = curStates[cfgStates[0].Name];
        }
    }
}
#endif
