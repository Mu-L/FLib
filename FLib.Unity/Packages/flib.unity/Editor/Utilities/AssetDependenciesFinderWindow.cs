//==================={By Qcbf|qcbf@qq.com|10/25/2021 5:42:04 PM}===================

using FLib;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FLib.Unity.Editor
{
    public class AssetDependenciesFinderWindow : EditorWindow
    {
        private readonly List<AssetInfo> mDependencies = new(32);
        private readonly List<AssetInfo> mReverseDependencies = new(32);
        private readonly Vector2[] mScrollsPos = new Vector2[2];

        private Object mTarget = null;
        private AssetDependenciesFinder mDepsFinder;
        protected Vector2 mScrollPos;


        public struct AssetInfo
        {
            public string Path;
            public Object Obj;
        }


        public static void Open()
        {
            GetWindow<AssetDependenciesFinderWindow>();
        }

        private void OnGUI()
        {
            //var e = Event.current;
            //if (e.type == EventType.MouseDown)
            //{
            //    GUI.FocusControl(null);
            //    Repaint();
            //}


            GUILayout.BeginHorizontal("Toolbar", GUILayout.Width(Screen.width));
            OnDrawMenus();
            GUILayout.EndHorizontal();
            mScrollPos = EditorGUILayout.BeginScrollView(mScrollPos);
            OnDrawContent();
            EditorGUILayout.EndScrollView();

            //如果鼠标正在拖拽中或拖拽结束时，并且鼠标所在位置在文本输入框内  
            if (Event.current.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
            }
            else if (Event.current.type == EventType.DragExited)
            {
                if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                {
                    mTarget = AssetDatabase.LoadMainAssetAtPath(DragAndDrop.paths[0]);
                    Find();
                }
            }

        }



        private void OnDrawMenus()
        {
            if (mDepsFinder == null)
            {
                return;
            }
            EditorGUI.BeginChangeCheck();
            mTarget = EditorGUILayout.ObjectField(mTarget, typeof(Object), false);
            if (EditorGUI.EndChangeCheck())
            {
                Find();
            }
            if (GUILayout.Button("重新生成依赖关系", "ToolbarButton", GUILayout.ExpandWidth(false)))
            {
                FinderGenerate();
                Find();
            }
            if (GUILayout.Button("清除", "ToolbarButton"))
            {
                mTarget = null;
                mDependencies.Clear();
                mReverseDependencies.Clear();
            }

        }


        private void OnDrawContent()
        {
            if (mDepsFinder == null)
            {
                if (GUILayout.Button("初始化", GUILayout.Width(Screen.width), GUILayout.Height(Screen.height / 2f)))
                {
                    mDepsFinder = new AssetDependenciesFinder();
                    FinderGenerate();
                }
                return;
            }
            GUILayout.BeginHorizontal();
            DrawList(ref mScrollsPos[0], "被这些对象依赖", mReverseDependencies);
            DrawList(ref mScrollsPos[1], "依赖这些对象", mDependencies);
            GUILayout.EndHorizontal();
        }


        private void DrawList(ref Vector2 scrollPos, string name, List<AssetInfo> list)
        {
            GUILayout.BeginVertical("GroupBox", GUILayout.Width(Screen.width / 2f - 12));
            GUILayout.Label(name + " 数量:" + list.Count);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            for (var i = list.Count - 1; i >= 0; i--)
            {
                GUILayout.BeginHorizontal("box");
                EditorGUILayout.ObjectField(list[i].Obj, typeof(Object), false, GUILayout.Width(50));
                EditorGUILayout.SelectableLabel(list[i].Path, GUILayout.Height(18));
                GUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();
        }


        private void FinderGenerate()
        {
            mDepsFinder.Generate((progress, path) =>
            {
                return EditorUtility.DisplayCancelableProgressBar("waiting...", path, progress);
            });
            EditorUtility.ClearProgressBar();
        }

        private void Find()
        {
            mDependencies.Clear();
            mReverseDependencies.Clear();
            var path = AssetDatabase.GetAssetPath(mTarget);

            if (mDepsFinder.AllDependencies.TryGetValue(path, out var list))
            {
                foreach (var item in list)
                {
                    mDependencies.Add(new AssetInfo { Obj = AssetDatabase.LoadMainAssetAtPath(item), Path = item });
                }
            }

            if (mDepsFinder.AllReverseDependencies.TryGetValue(path, out list))
            {
                foreach (var item in list)
                {
                    mReverseDependencies.Add(new AssetInfo { Obj = AssetDatabase.LoadMainAssetAtPath(item), Path = item });
                }
            }

        }



    }
}
