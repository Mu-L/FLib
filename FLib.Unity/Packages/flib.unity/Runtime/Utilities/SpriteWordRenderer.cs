// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FLib;
using FLib.Unity;
using UnityEngine;

namespace FLib.Unity
{
    /// <summary>
    /// sprite 图片文字
    /// </summary>
    public class SpriteWordRenderer : MonoBehaviour
    {
        public SpriteRenderer[] WordRenderers;
        public SpriteWordRendererData[] WordDatas = Array.Empty<SpriteWordRendererData>();

        protected virtual void Awake()
        {
            for (var i = 0; i < WordDatas.Length; i++)
                SpriteWordRendererCache.Register(WordDatas[i]);
        }

        private void OnDestroy()
        {
            for (var i = 0; i < WordDatas.Length; i++)
                SpriteWordRendererCache.Unregister(WordDatas[i].TemplateName);
        }

        /// <summary>
        /// 
        /// </summary>
        public float Set(string words) => Set(words, WordDatas[0].TemplateName);

        /// <summary>
        /// 
        /// </summary>
        [MethodButton]
        public float Set(string words, string templateName)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                words ??= string.Empty;
                templateName ??= WordDatas[0].TemplateName;
                for (var i = 0; i < WordDatas.Length; i++)
                    SpriteWordRendererCache.Register(WordDatas[i]);
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
            var wordSize = words.Length;
            if (wordSize > WordRenderers.Length)
            {
                var oldSize = WordRenderers.Length;
                Array.Resize(ref WordRenderers, wordSize);
                var template = WordRenderers[0];
                var templateTf = template.transform;
                for (var i = oldSize; i < WordRenderers.Length; i++)
                    WordRenderers[i] = Instantiate(template, templateTf.parent);
            }
            ref readonly var cache = ref SpriteWordRendererCache.GetCache(templateName);
            var width = 0f;
            for (var i = 0; i < WordRenderers.Length; i++)
            {
                if (i < wordSize)
                {
                    var sprite = cache.Sprites[words[i]];
                    WordRenderers[i].sprite = sprite;
                    WordRenderers[i].transform.localPosition = new Vector3(width, 0, 0);
                    width += sprite.rect.width * cache.PixelToWorldUnit;
                }
                else
                {
                    WordRenderers[i].sprite = null;
                }
            }
            return width;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Clear()
        {
            foreach (var item in WordRenderers)
                item.sprite = null;
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            WordRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        [MethodButton]
        protected virtual void AddDataFromSpriteAsset(Sprite sprite)
        {
            var dataIndexDict = WordDatas.Select((data, i) => (data, i)).ToDictionary(k => k.data.TemplateName, v => v.i);
            foreach (var item in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(UnityEditor.AssetDatabase.GetAssetPath(sprite)).OfType<Sprite>().GroupBy(v =>
                     {
                         var index = v.name.IndexOf('_');
                         return index > 0 ? v.name[..index] : string.Empty;
                     }))
            {
                var templateName = item.Key;
                if (string.IsNullOrEmpty(templateName))
                    templateName = sprite.texture.name;
                if (!dataIndexDict.TryGetValue(templateName, out var dataIndex))
                {
                    dataIndex = WordDatas.Length;
                    ArrayFLibUtility.Add(ref WordDatas, default);
                }
                WordDatas[dataIndex] = new SpriteWordRendererData()
                {
                    TemplateName = templateName,
                    PixelToWorldUnit = 1f / sprite.pixelsPerUnit,
                    Sprites = item.ToArray(),
                };
                Log.Info?.Write(WordDatas[dataIndex]);
            }
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable] public struct SpriteWordRendererData
    {
        public string TemplateName;
        public float PixelToWorldUnit;
        public Sprite[] Sprites;
        public override string ToString() => $"[{TemplateName}]{string.Join(',', Sprites.Select(v => v.name[^1]))}";
    }

    /// <summary>
    /// 
    /// </summary>
    public struct SpriteWordRendererCache
    {
        public static Dictionary2<string, SpriteWordRendererCache> Caches = new();
        public int RefCount;
        public float PixelToWorldUnit;
        public ReadOnlyDictionary<char, Sprite> Sprites;

        /// <summary>
        /// 
        /// </summary>
        public static void Register(in SpriteWordRendererData data)
        {
            ref var cache = ref Caches.GetValueOrAdd(data.TemplateName);
            cache.Sprites ??= new ReadOnlyDictionary<char, Sprite>(data.Sprites.ToDictionary(v => v.name[^1]));
            cache.PixelToWorldUnit = data.PixelToWorldUnit;
            ++cache.RefCount;
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Unregister(string templateName)
        {
            var index = Caches.GetEntryIndex(templateName);
            if (index < 0)
                return;
            if (--Caches.GetEntryValue(index).RefCount <= 0)
                Caches.Remove(templateName);
        }

        /// <summary>
        /// 
        /// </summary>
        public static ref SpriteWordRendererCache GetCache(string templateName)
        {
            var index = Caches.GetEntryIndex(templateName);
            if (index >= 0)
                return ref Caches.GetEntryValue(index);
            throw new Exception($"not found word: {templateName}");
        }

    }
}
