//==================={By Qcbf|qcbf@qq.com|5/26/2023 3:16:48 PM}===================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using FLib;
using TMPro;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor
{
    [CustomEditor(typeof(TextFieldLang))]
    public class TextFieldLangEditor : BaseEditor<TextFieldLang>
    {
        public const string LANG_FILE_PATH = "../Config/Game/语言包.ini";

        public static Dictionary<string, string> mLangs = new();

        public static Dictionary<string, string> Langs =>
            //if (mLangs.Count == 0)
            //    Lang.ParseTextFile(LANG_FILE_PATH, mLangs);
            mLangs;


        protected override void OnEnable()
        {
            base.OnEnable();
            if (target.TextUI == null)
            {
                target.TextUI = target.GetComponent<TMP_Text>();
            }
            if (!string.IsNullOrEmpty(target.Lang))
            {
                if (!Langs.ContainsKey(target.Lang))
                {
                    Log.Error?.Write($"{target} not found Lang: {target.Lang}");
                }
            }
        }

        public override void CreateUI(TextFieldLang targetObject)
        {
            RootUI.Add(new ObjectField("Text") { objectType = typeof(TMP_Text), bindingPath = nameof(TextFieldLang.TextUI) }.ShortFieldLabel());

            var bar = new VisualElement().FlexDirection(FlexDirection.Row);
            RootUI.Add(bar);
            bar.Add(new TextField("Lang") { bindingPath = nameof(TextFieldLang.Lang) }.FlexGrow(1).ShortFieldLabel());
            bar.Add(new ToolbarButton(() =>
                {
                    if (Langs.ContainsKey(target.Lang))
                    {
                        Log.Info?.Write("exist");
                        return;
                    }
                    if (EditorFLibUtility.AlertSure($"Add Language: {target.Lang}={target.TextUI.text} ?"))
                    {
                        AppendLang(target.Lang, target.TextUI.text);
                    }
                })
                { text = "Add" }.MinWidth(36));
            bar.Add(new ToolbarButton(() =>
                {
                    EditorListChooser.OpenWithDisplay(Langs.Select(v => $"{v.Key}={v.Value}").Take(20).ToArray(), (index, text) =>
                    {
                        if (!string.IsNullOrEmpty(text))
                        {
                            Undo.RecordObject(target, "set lang");
                            target.Lang = text[..text.IndexOf('=')];
                        }
                    });
                })
                { text = "Open" }.MinWidth(42));
        }

        private static void AppendLang(string key, string value)
        {
            Langs.Add(key, value);
            File.AppendAllText(Path.GetFullPath(LANG_FILE_PATH), $"{key}={value}{Environment.NewLine}");
        }
    }
}
