//==================={By Qcbf|qcbf@qq.com|6/13/2021 11:44:29 AM}===================

using FLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor
{
    public class NodeLineEditor : INodeSelectableEditor, ICloneable
    {
        public const int LINE_WIDTH = 3;

        public NodeStageEditor Stage;
        public NodeEditor Left;
        public bool IsInvertResult;
        private NodeEditor mRightCached;
        private uint mRightUid;

        protected float mCommentWidth = 0;

        private string mComment = string.Empty;

        public string Comment
        {
            get => mComment;
            set => mCommentWidth = ((GUIStyle)"Label").CalcSize(new GUIContent(mComment = value)).x;
        }

        public struct BezierData
        {
            public Vector2 Begin;
            public Vector2 BeginTan;
            public Vector2 End;
            public Vector2 EndTan;
        }


        public virtual uint RightUid
        {
            get => mRightUid;
            set
            {
                mRightCached = null;
                mRightUid = value;
            }
        }


        public virtual NodeEditor Right
        {
            get
            {
                if (RightUid == 0) return null;
                if (mRightCached == null)
                {
                    Stage.Nodes.TryGetValue(RightUid, out mRightCached);
                }
                else if (mRightCached.parent == null)
                {
                    mRightCached = null;
                }

                return mRightCached;
            }
        }

        public bool IsSelected { get; set; }


        public NodeLineEditor(NodeStageEditor stage, NodeEditor left)
        {
            Stage = stage;
            Left = left;
        }

        public virtual void DrawLine()
        {
            var bezier = GetBezierData(Stage.WorldToLocal(Left.ArrowUI.worldBound.center), Stage.WorldToLocal(Right.BodyUI.worldBound));
            Handles.DrawBezier(bezier.Begin, bezier.End, bezier.BeginTan, bezier.EndTan, GetBezierColor(bezier), null, LINE_WIDTH);

            var labelPos = bezier.Begin + (bezier.End - bezier.Begin) * 0.5f;
            labelPos.x -= mCommentWidth * 0.5f;
            DrawLabel(labelPos);
        }

        protected virtual void DrawLabel(Vector2 pos)
        {
            Handles.Label(pos, mComment);
        }


        public virtual Color GetBezierColor(in BezierData bezier)
        {
            return IsSelected
                ? new Color(0.3f, 0.8f, 0.45f, 1f)
                : new Color(0.3f, 0.4f, 0.45f, Mathf.Lerp(0.8f, 0.2f, (bezier.Begin.x - bezier.End.x) / 300f));
        }


        public void AddToLeft()
        {
            Left.AddLine(this);
        }


        public void RemoveFromLeft()
        {
            Left.RemoveLine(this);
        }


        public bool IsContainPoint(Vector2 point)
        {
            if (Right == null) return false;
            var bezier = GetBezierData(Left.ArrowUI.worldBound.center, Right.BodyUI.worldBound);
            var dist = Mathf.RoundToInt(Vector2.Distance(bezier.Begin, bezier.End) * 0.5f);
            var points = Handles.MakeBezierPoints(bezier.Begin, bezier.End, bezier.BeginTan, bezier.EndTan, dist);
            var widthPowerTwo = LINE_WIDTH * LINE_WIDTH * 4;
            return points.Any(v => Mathf.Abs((point - (Vector2)v).sqrMagnitude) <= widthPowerTwo);
        }

        public static BezierData GetBezierData(Vector2 leftPoint, Rect rightRect)
        {
            var data = new BezierData { Begin = leftPoint };
            data.End.y = Mathf.Clamp(data.Begin.y, rightRect.min.y, rightRect.max.y);
            data.End.x = Mathf.Clamp(data.Begin.x, rightRect.min.x, rightRect.max.x);
            var tan = new Vector2(Mathf.Clamp(data.Begin.x - data.End.x, -60f, 90f), 0);
            data.BeginTan = data.Begin.x >= data.End.x ? data.Begin + tan : data.Begin - tan;
            data.EndTan = data.End + tan;
            return data;
        }


        public virtual object Clone()
        {
            return new NodeLineEditor(Stage, Left)
            {
                RightUid = RightUid
            };
        }

        public virtual string GetCommentAttributeNames()
        {
            return null;
        }
    }
}