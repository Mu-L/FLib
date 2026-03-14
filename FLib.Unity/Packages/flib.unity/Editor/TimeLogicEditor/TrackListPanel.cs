// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FLib.WorldCores.TimeLogic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor.TimeLogic
{
    public class TrackListPanel : ScrollView, IContextMenuProcessable
    {
        public readonly StagePanel Stage;

        public TimeLogicRuntime Runtime => Stage.Runtime;


        public TrackListPanel(StagePanel stage)
        {
            Stage = stage;
            this.FlexGrow(1);
            RefreshTracks();
        }

        /// <summary>
        /// 
        /// </summary>
        public void RefreshTracks()
        {
            var tracks = Runtime.Tracks.Select((v, i) => (v, i)).ToDictionary(k => k.v.GetHashCode() << 16 | k.i, v => v.v);
            for (var i = 0; i < tracks.Count; i++)
            {
                var track = Runtime.Tracks[i];
                if (childCount <= i || ((TrackItemPanel)ElementAt(i)).Track != track)
                {
                    var type = FLibCustomEditorAttribute.CustomEditors.GetValueOrDefault(track.GetType(), typeof(TrackItemPanel));
                    if (childCount > i)
                        RemoveAt(i);
                    Insert(i, (TrackItemPanel)TypeAssistant.New(type, this, track));
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void ContextMenuProcess(MouseUpEvent evt, GenericMenu menu)
        {
            menu.AddItem(new GUIContent("添加轨道"), false, () =>
            {
                var runtimeType = Runtime.GetType();
                if (!TypeChooserWindow.Open(typeof(TimeLogicTrack), options: TypeChooserWindow.EOption.ContainBaseType | TypeChooserWindow.EOption.HideSetNull, filter: t =>
                    {
                        var op = t.GetCustomAttribute<TimeLogicEditorAttribute>();
                        return op == null || op.RequiredRuntime?.IsAssignableFrom(runtimeType) != false;
                    }).TryGetSelected(out var type))
                    return;
                var track = (TimeLogicTrack)TypeAssistant.New(type);
                track.Runtime = Runtime;
                track.Name = CommentAttribute.TryGetLabel(type);
                ArrayFLibUtility.Add(ref Runtime.Tracks, track);
                RefreshTracks();
            });

            menu.AddItem(new GUIContent("刷新(R)"), false, () => Stage.Refresh(true));
        }
    }
}
