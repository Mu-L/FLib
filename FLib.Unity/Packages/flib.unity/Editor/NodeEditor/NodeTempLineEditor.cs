//==================={By Qcbf|qcbf@qq.com|6/13/2021 12:29:31 PM}===================

using FLib;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor
{
    public class NodeTempLineEditor
    {
        public NodeLineEditor Line;
        private Vector2 mTempPoint;


        public NodeStageEditor Stage => Line.Stage;

        public NodeEditor Left => Line.Left;

        public Vector2 RightPoint
        {
            get
            {
                if (Line.RightUid == 0) return mTempPoint;
                return Line.Right.BodyUI.worldBound.center;
            }
            set
            {
                Line.RightUid = 0;
                mTempPoint = value;
            }
        }

        public NodeTempLineEditor(NodeEditor left)
        {
            Line = left.Stage.CreateLine(left);
        }

        public virtual void DrawLine()
        {
            if (Line.Right == null)
            {
                var bezier = NodeLineEditor.GetBezierData(Stage.WorldToLocal(Left.ArrowUI.worldBound.center), new Rect(Stage.WorldToLocal(mTempPoint), Vector2.zero));
                Handles.DrawBezier(bezier.Begin, bezier.End, bezier.BeginTan, bezier.End, Line.GetBezierColor(bezier), null, 3);
            }
            else
            {
                Line.DrawLine();
            }
        }

    }
}
