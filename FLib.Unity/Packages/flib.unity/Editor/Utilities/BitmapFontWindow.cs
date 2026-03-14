//==================={By Qcbf|qcbf@qq.com|2/18/2022 2:50:58 PM}===================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor
{
    public class BitmapFontWindow : EditorWindow
    {
        private Texture2D mMainTex;
        private CharacterInfo[] mChtInfos;
        private UIBindData mImage;
        private UIBindData mImageInfo;

        private void OnEnable()
        {
            AddUI();
        }

        private void AddUI()
        {
            var menuBar = new Toolbar();
            rootVisualElement.Add(menuBar);
            menuBar.Add(new ToolbarButton(OnClickOpenImages) { text = "选择图片字素材文件夹" });
            menuBar.Add(new ToolbarButton(OnClickExport) { text = "导出字体" });

            mImage = new Image() { scaleMode = ScaleMode.ScaleToFit }.BindDataToUI(ui => ui.image = mMainTex);
            rootVisualElement.Add(mImage.UI);

            mImageInfo = new Label().BindDataToUI(ui => ui.text = mMainTex == null ? "未选择" : mMainTex.width + "," + mMainTex.height);
            mImageInfo.UI.TextAlign(TextAnchor.MiddleLeft);
            menuBar.Add(mImageInfo);
        }

        private void OnClickOpenImages()
        {
            mMainTex = null;
            var dir = EditorUtility.OpenFolderPanel("", PlayerPrefs.GetString(nameof(BitmapFontWindow) + "path", "assets"), "");
            if (string.IsNullOrEmpty(dir))
            {
                return;
            }
            PlayerPrefs.SetString(nameof(BitmapFontWindow) + "path", dir);
            var files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
            List<Texture2D> tiles = new();
            foreach (var item in files)
            {
                if (!(item.EndsWith(".png") || item.EndsWith(".jpg") || item.EndsWith(".tga") || item.EndsWith(".gif")))
                {
                    continue;
                }

                var tex = (Texture2D)AssetDatabase.LoadAssetAtPath(EditorFLibUtility.TrimToUnityAssetPath(item), typeof(Texture2D));
                tiles.Add(tex);
            }
            mMainTex = new Texture2D(64, 64, TextureFormat.RGBA32, false)
            {
                name = Path.GetFileName(dir),
            };
            mImage.Dirty();
            mImageInfo.Dirty();
            var rects = mMainTex.PackTextures(tiles.ToArray(), 1, 1024);
            var texW = mMainTex.width;
            var texH = mMainTex.height;

            mChtInfos = new CharacterInfo[rects.Length];
            for (var i = 0; i < rects.Length; i++)
            {
                var r = rects[i];
                mChtInfos[i] = new CharacterInfo
                {
                    glyphHeight = texH,
                    glyphWidth = texW,
                    index = Encoding.ASCII.GetBytes(tiles[i].name)[0],
                    uvTopLeft = r.position,
                    uvTopRight = new Vector2(r.x + r.width, r.y),
                    uvBottomLeft = new Vector2(r.x, r.y + r.height),
                    uvBottomRight = new Vector2(r.x + r.width, r.y + r.height),
                };
                mChtInfos[i].minX = 0;
                mChtInfos[i].minY = (int)(r.height * texH * 0.5f);
                mChtInfos[i].maxX = (int)(r.width * texW);
                mChtInfos[i].maxY = (int)(r.height * texH * -0.5f); ;
                mChtInfos[i].advance = mChtInfos[i].maxX;
            }

        }


        private void OnClickExport()
        {
            if (mMainTex == null)
            {
                return;
            }
            var fontPath = EditorUtility.SaveFilePanelInProject("", mMainTex.name, "fontsettings", "save font files", PlayerPrefs.GetString(nameof(BitmapFontWindow) + "path", "assets"));
            if (string.IsNullOrEmpty(fontPath))
            {
                return;
            }

            var name = Path.GetFileNameWithoutExtension(fontPath);

            var texPath = fontPath.Replace("fontsettings", "png");
            File.WriteAllBytes(texPath, mMainTex.EncodeToPNG());
            AssetDatabase.Refresh();
            if (!File.Exists(texPath))
            {
                var texSettings = (TextureImporter)AssetImporter.GetAtPath(texPath);
                texSettings.mipmapEnabled = false;
                texSettings.alphaIsTransparency = true;
                texSettings.wrapMode = TextureWrapMode.Clamp;
            }

            var matPath = fontPath.Replace("fontsettings", "mat");
            if (!File.Exists(matPath))
            {
                var mat = new Material(Shader.Find("GUI/Text Shader"))
                {
                    mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath)
                };
                AssetDatabase.CreateAsset(mat, matPath);
            }

            Font font;
            if (!File.Exists(fontPath))
            {
                font = new Font(name)
                {
                    material = AssetDatabase.LoadAssetAtPath<Material>(matPath),
                };
                AssetDatabase.CreateAsset(font, fontPath);
            }
            font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            font.characterInfo = mChtInfos;
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }




    }
}
