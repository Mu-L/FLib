//==================={By Qcbf|qcbf@qq.com|6/7/2021 11:38:17 PM}===================

using FLib;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor
{
    public class NodeStageFrontIMGUIDrawer : ImmediateModeElement
    {
        public NodeStageEditor Stage;

        public Rect RectSelector;


        public bool IsHaveRectSelector => RectSelector.size.sqrMagnitude != 0;


        public NodeStageFrontIMGUIDrawer(NodeStageEditor stage)
        {
            Stage = stage;
            pickingMode = PickingMode.Ignore;
            this.StretchToParentSize();
        }


        protected override void ImmediateRepaint()
        {
            //DrawLines();
            if (IsHaveRectSelector)
            {
                EditorGUI.DrawRect(RectSelector, EditorGUIUtility.isProSkin ? new Color(0.1f, 0.1f, 0.2f, 0.25f) : new Color(0.9f, 0.9f, 0.8f, 0.25f));
            }
        }
    }
}
