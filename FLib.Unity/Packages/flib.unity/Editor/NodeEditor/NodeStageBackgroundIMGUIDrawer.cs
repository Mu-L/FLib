//==================={By Qcbf|qcbf@qq.com|6/5/2021 9:04:15 PM}===================

using FLib;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor
{
    public class NodeStageBackgroundIMGUIDrawer : ImmediateModeElement
    {
        private static readonly Material mLineMaterial = new(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave };
        public NodeStageEditor Stage;
        public Color LineColor = EditorGUIUtility.isProSkin ? new Color(0.15f, 0.15f, 0.15f, 0.3f) : new Color(0.85f, 0.85f, 0.85f, 0.3f);
        public Vector2 CellSize = new(10, 10);

        public NodeStageBackgroundIMGUIDrawer(NodeStageEditor stage)
        {
            Stage = stage;
            pickingMode = PickingMode.Ignore;
            this.StretchToParentSize();
        }

        protected override void ImmediateRepaint()
        {
            DrawBackground();
        }


        private void DrawBackground()
        {
            var size = Stage.layout.size;
            var cellWidth = CellSize.x * Stage.NodeLayer.Scale;
            var cellHeight = CellSize.y * Stage.NodeLayer.Scale;

            mLineMaterial.SetPass(0);
            GL.Begin(GL.LINES);
            GL.Color(LineColor);
            var from = new Vector2(1, 0);
            var to = new Vector2(1, size.y);
            while (from.x < size.x)
            {
                GL.Vertex(from);
                GL.Vertex(to);
                from.x += cellWidth;
                to.x += cellWidth;
            }
            from = new Vector2(0, 1);
            to = new Vector2(size.x, 1);
            while (from.y < size.y)
            {
                GL.Vertex(from);
                GL.Vertex(to);
                from.y += cellHeight;
                to.y += cellHeight;
            }
            GL.End();
        }
    }
}
