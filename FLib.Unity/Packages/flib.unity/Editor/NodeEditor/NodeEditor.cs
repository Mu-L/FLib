//==================={By Qcbf|qcbf@qq.com|6/4/2021 12:29:29 AM}===================

using FLib;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
#pragma warning disable CS0618 // Type or member is obsolete

namespace FLib.Unity.Editor
{
    public class NodeEditor : VisualElement, INodeSelectableEditor
    {
        public readonly List<NodeLineEditor> Lines = new();

        public uint Uid;

        public Label BodyUI;
        public Label TopFloatUI;
        public Label ArrowUI;
        public Label CommentUI;

        public string Title
        {
            get => BodyUI.text;
            set => BodyUI.text = value;
        }

        public NodeStageEditor Stage { get; private set; }

        public Vector2 Position
        {
            get => transform.position;
            set => transform.position = new Vector3((int)value.x * 0.1f * 10, (int)value.y * 0.1f * 10);
        }

        public Vector2 FormatPosition
        {
            get => new(Mathf.RoundToInt(Position.x * 0.1f) * 10, Mathf.RoundToInt(Position.y * 0.1f) * 10);
            set => Position = new Vector2(Mathf.RoundToInt(value.x * 0.1f) * 10, Mathf.RoundToInt(value.y * 0.1f) * 10);
        }


        public bool IsSelected
        {
            get => BodyUI.ClassListContains("NodeBodySelected");
            set => BodyUI.EnableInClassList("NodeBodySelected", value);
        }

        protected virtual StyleColor BackgroundColor => StyleKeyword.Null;

        public NodeEditor(NodeStageEditor stage, uint uid)
        {
            focusable = true;
            name = "Node";
            Stage = stage;
            Uid = uid;
            var container = new VisualElement { name = "BodyContainer" };
            Add(container);
            container.Add(BodyUI = new Label { name = "Body" });
            container.Add(ArrowUI = new Label(">") { name = "Arrow" });
            container.Add(TopFloatUI = new Label { name = nameof(TopFloatUI) }.Color(EditorGUIUtility.isProSkin ? Color.white : Color.black));
            TopFloatUI.style.position = new StyleEnum<Position>(UnityEngine.UIElements.Position.Absolute);
            TopFloatUI.style.fontSize = 32;
            TopFloatUI.style.top = 0;
            TopFloatUI.style.backgroundColor = EditorGUIUtility.isProSkin ? new Color(0, 0, 0, 0.7f) : new Color(1, 1, 1, 0.7f);
            TopFloatUI.style.width = TopFloatUI.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            TopFloatUI.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            TopFloatUI.visible = false;

            container = new VisualElement() { name = "Comment", pickingMode = PickingMode.Ignore };
            Add(container);
            container.Add(CommentUI = new Label { pickingMode = PickingMode.Ignore });

            BodyUI.style.backgroundColor = BackgroundColor;

            Title = "New Node";
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void AddLine(NodeLineEditor line)
        {
            Lines.Add(line);
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void RemoveLine(NodeLineEditor line)
        {
            Lines.Remove(line);
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void Flicker(in Color col)
        {
            FlickerProcess(col).Forget();
        }

        protected virtual async UniTaskVoid FlickerProcess(Color col)
        {
            BodyUI.style.backgroundColor = col;
            var t = 1f;
            while (true)
            {
                await UniTask.Yield();
                t -= 0.033f;
                col.a -= 0.033f;
                BodyUI.style.backgroundColor = col;
                if (t <= 0)
                {
                    break;
                }
            }

            BodyUI.style.backgroundColor = BackgroundColor;
        }

        public virtual NodeEditor Clone()
        {
            var node = new NodeEditor(Stage, ++Stage.UidGen);
            return node;
        }

        public override string ToString()
        {
            return Uid + "#" + Title;
        }

        public virtual string GetCommentAttributeNames()
        {
            return null;
        }
    }
}
