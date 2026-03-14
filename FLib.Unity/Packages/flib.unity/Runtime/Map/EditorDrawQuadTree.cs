// =================================================={By Qcbf|qcbf@qq.com|2024-2-19}==================================================

#if UNITY_EDITOR
using System;
using FLib.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace FLib.Unity
{
    public static class EditorDrawQuadTree
    {
        /// <summary>
        /// 
        /// </summary>
        public static void Draw<T>(QuadTree<T> tree, Color? color = null)
        {
            if (!tree.Root.HasChild)
                return;
            EditorDrawUtility.TryInitialize();
            GL.PushMatrix();
            GL.MultMatrix(UnityEditor.Handles.matrix);
            GL.Begin(GL.LINES);
            GL.Color(color ?? Color.red);
            foreach (var childIdx in tree.Root.Children)
                DrawArea(ref tree.GetNode(childIdx));
            GL.End();
            GL.PopMatrix();
        }

        public static void DrawArea<T>(ref QuadTree<T>.Node node)
        {
            var rect = node.Rect;
            EditorDrawUtility.GLDrawQuadLines(rect.Min.AsVec(), rect.Max.AsVec());
            if (node.HasChild)
            {
                foreach (var childIdx in node.Children)
                    DrawArea(ref node.Tree.GetNode(childIdx));
            }
        }
    }
}

#endif
