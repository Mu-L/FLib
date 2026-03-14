//==================={By Qcbf|qcbf@qq.com|6/7/2021 10:53:37 PM}===================

using FLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
#pragma warning disable CS0618 // Type or member is obsolete

namespace FLib.Unity.Editor
{
    public class NodeStageDraggerLayer : VisualElement
    {
        public VisualElement NodeContainer;

        public virtual Vector2 Position
        {
            get => NodeContainer.transform.position;
            set => NodeContainer.transform.position = value;
        }

        public float Scale
        {
            get => NodeContainer.transform.scale.x;
            set => NodeContainer.transform.scale = new Vector3(value, value, 1);
        }

        public override VisualElement contentContainer => NodeContainer;

        public NodeStageDraggerLayer(NodeStageEditor stage)
        {
            this.StretchToParentSize();
            hierarchy.Add(NodeContainer = new VisualElement());
            hierarchy.Add(new NodeStageLineIMGUIDrawer() { Stage = stage });
        }

    }
}
