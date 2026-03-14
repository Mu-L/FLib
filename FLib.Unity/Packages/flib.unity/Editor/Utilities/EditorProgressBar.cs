//==================={By Qcbf|qcbf@qq.com|12/28/2021 5:59:34 PM}===================

using FLib;
using System;
using System.Collections.Generic;
using UnityEditor;

namespace FLib.Unity.Editor
{
    public class EditorProgressBar : IDisposable
    {

        public string Title = string.Empty;

        public EditorProgressBar() { }

        public EditorProgressBar(string title)
        {
            Title = title;
        }

        public void Display(string info, float progress)
        {
            EditorUtility.DisplayProgressBar(Title, info, progress);
        }
        public void Display(string title, string info, float progress)
        {
            EditorUtility.DisplayProgressBar(Title = title, info, progress);
        }


        public bool DisplayCancelable(string info, float progress)
        {
            return EditorUtility.DisplayCancelableProgressBar(Title, info, progress);
        }
        public bool DisplayCancelable(string title, string info, float progress)
        {
            return EditorUtility.DisplayCancelableProgressBar(Title = title, info, progress);
        }


        public void Dispose()
        {
            EditorUtility.ClearProgressBar();
        }
    }
}
