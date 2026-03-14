//==================={By Qcbf|qcbf@qq.com|11/5/2020 5:59:45 PM}===================

using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace FLib.Unity
{
    /// <summary>
    ///     附带atlas信息的image
    /// </summary>
    [ExecuteAlways]
    public class UIAtlasSpriteImage : MonoBehaviour
    {
        public Image ImageUI;
        public SpriteAtlas Atlas;
        public bool IsAutoNativeSize;

        private void Start()
        {
            if (ImageUI == null && transform.TryGetComponent(out Image img))
                ImageUI = img;
        }

        /// <summary>
        ///     根据spriteName刷新sprite
        /// </summary>
        public void SetSpriteFromName(string spriteName)
        {
            if (Atlas == null)
            {
                Log.Warn?.Write("not found atlas");
                return;
            }

            if (string.IsNullOrEmpty(spriteName))
            {
                ImageUI.sprite = null;
                ImageUI.enabled = false;
            }
            else
            {
                ImageUI.enabled = true;
                var temp = Atlas.GetSprite(spriteName);
                if (temp == null)
                    Log.Warn?.Write($"atlas [{Atlas.name}] not found sprite name [{spriteName}]");
                else
                    ImageUI.sprite = temp;
            }

            if (IsAutoNativeSize) ImageUI.SetNativeSize();
        }


#if UNITY_EDITOR
        private void OnValidate()
        {
            if (ImageUI == null)
                ImageUI = GetComponentInChildren<Image>();
            if (Atlas == null && ImageUI != null && ImageUI.sprite != null)
            {
                var path = FIO.GetFileDirectory(UnityEditor.AssetDatabase.GetAssetPath(ImageUI.sprite));
                var atlasPathGuid = UnityEditor.AssetDatabase.FindAssets("t:SpriteAtlas", new[] { path }).FirstOrDefault();
                if (atlasPathGuid != null)
                    Atlas = UnityEditor.AssetDatabase.LoadAssetAtPath<SpriteAtlas>(UnityEditor.AssetDatabase.GUIDToAssetPath(atlasPathGuid));
            }
        }
#endif
    }
}
