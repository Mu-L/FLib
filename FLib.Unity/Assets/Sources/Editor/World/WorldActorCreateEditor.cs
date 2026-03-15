// ==================== qcbf@qq.com | 2025-08-08 ====================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FLib;
using FLib.Unity;
using FLib.Unity.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Worlds.Editor
{
    public class WorldActorCreateEditor : EditorWindow
    {
        [MenuItem("Tools/World/Create Gpu Actor")]
        public static void CreateActor()
        {
            var path = EditorFLibUtility.OpenFilePanel("", "Assets/GameRes/Actors", "fbx");
            if (string.IsNullOrEmpty(path)) return;
            var modelImporter = (ModelImporter)AssetImporter.GetAtPath(path);
            modelImporter.globalScale = 90;
            modelImporter.normalSmoothingAngle = 120;
            modelImporter.importNormals = ModelImporterNormals.Calculate;
            modelImporter.isReadable = true;
            var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(v => !v.name.StartsWith("__")).ToDictionary(k => k.name);
            modelImporter.clipAnimations = modelImporter.defaultClipAnimations.Select(clip =>
            {
                clip.lastFrame = clip.firstFrame + clips[clip.name].length * clips[clip.name].frameRate;
                return clip;
            }).ToArray();
            modelImporter.SaveAndReimport();
            clips = clips.ToDictionary(k =>
            {
                var index = k.Key.LastIndexOf('_');
                if (index < 0)
                    index = k.Key.LastIndexOf('|');
                return index > 0 ? k.Key[(index + 1)..] : k.Key;
            }, v => v.Value);


            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var baker = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadMainAssetAtPath(path), scene);
                baker.AddComponent<Animator>();
                var dir = Path.GetDirectoryName(path)!;
                var name = Path.GetFileName(dir);
                var gpuBaker = baker.AddComponent<GpuAnimBaker>();
                gpuBaker.transform.localEulerAngles = new Vector3(0, 90, 0);
                gpuBaker.Skinned = baker.GetComponentInChildren<SkinnedMeshRenderer>();
                var materials = gpuBaker.Skinned.sharedMaterials;
                var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/GameRes/Renders/InkMatSimple.mat");
                gpuBaker.Skinned.sharedMaterials = Array.ConvertAll(gpuBaker.Skinned.sharedMaterials, _ => mat);
                var meshSimplify = gpuBaker.Skinned.gameObject.AddComponent<MeshSimplify>();
                gpuBaker.AnimRoot = baker;
                gpuBaker.ClipDatas = new GpuAnimBaker.ClipData[]
                {
                    new() { IsLoop = true, Clip = clips.GetValueOrDefault("Stand"), FrameRate = 30 },
                    new() { IsLoop = true, Clip = clips.GetValueOrDefault("Walk"), FrameRate = 30 },
                    new() { Clip = clips.GetValueOrDefault("Hit01"), FrameRate = 30 },
                    new() { Clip = clips.GetValueOrDefault("Die"), FrameRate = 30 },
                    new() { Clip = clips.GetValueOrDefault("Attack01"), FrameRate = 30 },
                };
                var rendererGameObj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadMainAssetAtPath("Assets/GameRes/Actors/Gpu角色模板.prefab"), scene);
                gpuBaker.Renderer = PrefabUtility.SaveAsPrefabAsset(rendererGameObj, Path.Combine(dir, name + ".prefab")).GetComponent<GpuAnimRenderer>();
                PrefabUtility.SaveAsPrefabAsset(gpuBaker.gameObject, Path.Combine(dir, name + "baker.prefab"));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
