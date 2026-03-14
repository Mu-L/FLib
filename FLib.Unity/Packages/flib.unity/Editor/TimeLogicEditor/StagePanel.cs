// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FLib.WorldCores;
using FLib.WorldCores.TimeLogic;
using OfficeOpenXml.FormulaParsing.Exceptions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor.TimeLogic
{
    public class StagePanel : VisualElement
    {
        public EditorWindow Window;
        public const float ClipStartSpacer = 10;
        public Action<StagePanel> OnSaveHandler;
        public Action OnExternalReferenceValueChange;

        public TimeLogicRuntime Runtime;
        public InspectorPanel Inspector;
        public EditorSmallTipsUI Tips;

        public FEventValue<ISelectable> Selected;
        public FEventValue<float> TrackListWidth = new(130);
        public FEventValue<int> EndFrame;
        public float FrameGraphicInterval;
        public ShortcutKeyManager ShortcutKey;
        public FEventValue<int> CurrentFrame = new(0);
        private readonly IVisualElementScheduledItem _playingSchedule;
        private bool _isEditorLoop;

        public bool IsAllowPlay { get; private set; }

        public StagePanel(TimeLogicRuntime runtime, bool isAllowPlay)
        {
            IsAllowPlay = isAllowPlay && !Application.isPlaying;
            Runtime = runtime;
            Runtime.ExecuteVerifyHandler += ExecuteVerifyHandler;
            if (IsAllowPlay)
            {
                (_playingSchedule = schedule.Execute(PlayNextFrame)).Pause();
                EndFrame.Value = Runtime.EndFrame;
                EndFrame.ListenEvent((object _, in FEventValue<int>.ChangeEvent value) => Runtime.EndFrame = value);
                CurrentFrame.Value = Runtime.CurrentFrame;
                CurrentFrame.ListenEvent((object _, in FEventValue<int>.ChangeEvent value) =>
                {
                    Runtime.CurrentFrame = value;
                    SceneView.RepaintAll();
                });
            }

            RefreshEndFrame();

            this.FlexGrow(1);
            var menuArea = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
            Add(menuArea);
            menuArea.Add(new LeftMenuPanel(this));
            menuArea.Add(new RightMenuPanel(this));
            Add(new TrackListPanel(this));
            if (IsAllowPlay)
                Add(new CurrentFramePanel(this));

            IContextMenuProcessable.RegisterRightContextMenu(this);
            (ShortcutKey = new ShortcutKeyManager()
                // .Register("undo", KeyCode.Z, ShortcutKeyManager.EModifier.Ctrl, () => EditorFLibUtility.Alert("撤销"))
                // .Register("redo", KeyCode.Y, ShortcutKeyManager.EModifier.Ctrl, () => EditorFLibUtility.Alert("重做"))
                // .Register("copy", KeyCode.C, ShortcutKeyManager.EModifier.Ctrl, () => EditorFLibUtility.Alert("复制"))
                // .Register("paste", KeyCode.V, ShortcutKeyManager.EModifier.Ctrl, () => EditorFLibUtility.Alert("粘贴"))
                // .Register("delete", KeyCode.Delete, ShortcutKeyManager.EModifier.None, () => EditorFLibUtility.Alert("删除"))
                // .Register("select all", KeyCode.A, ShortcutKeyManager.EModifier.Ctrl, () => EditorFLibUtility.Alert("全选"))
                .Register("refresh", KeyCode.R, ShortcutKeyManager.EModifier.None, () => Refresh(true))
                .Register("replay", KeyCode.P, ShortcutKeyManager.EModifier.None, () => PlayOrStop(true))
                .Register("loop replay", KeyCode.P, ShortcutKeyManager.EModifier.Shift, () => PlayOrStop(true, true))
                .Register("previous frame", KeyCode.A, ShortcutKeyManager.EModifier.None, () => AddFrame(-1))
                .Register("next frame", KeyCode.D, ShortcutKeyManager.EModifier.None, () => AddFrame(1))
                .Register("previous frame", KeyCode.LeftArrow, ShortcutKeyManager.EModifier.None, () => AddFrame(-1))
                .Register("next frame", KeyCode.RightArrow, ShortcutKeyManager.EModifier.None, () => AddFrame(1))
                .Register("save", KeyCode.S, ShortcutKeyManager.EModifier.Ctrl, Save, inputFocusStillProcess: true)
                .Register("play or stop", KeyCode.Space, ShortcutKeyManager.EModifier.None, () => PlayOrStop())
                .Register("loop play or stop", KeyCode.Space, ShortcutKeyManager.EModifier.Shift, () => PlayOrStop(isEditorLoop: true))).RegisterKeyEvent(this);

            Selected.ListenEvent((object _, in FEventValue<ISelectable>.ChangeEvent value) =>
            {
                value.OldValue?.OnSelectChange(false);
                value.NewValue?.OnSelectChange(true);
            });
            RegisterCallback<WheelEvent>(evt =>
            {
                EditorPrefs.SetFloat($"{nameof(TimeLogicClip)}.Scale", FrameGraphicInterval = Mathf.Clamp(FrameGraphicInterval - evt.delta.y, 3, 100));
                Refresh();
            });
            FrameGraphicInterval = EditorPrefs.GetFloat($"{nameof(TimeLogicClip)}.Scale", 8f);

            Add(Tips = new EditorSmallTipsUI());
            if (IsAllowPlay)
            {
                RegisterCallback<DetachFromPanelEvent>(OnDestroy);
                schedule.Execute(Runtime.UpdateCurrentFrame);
            }

            Resources.FindObjectsOfTypeAll<InspectorPanel>().FirstOrDefault()?.Close();
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnDestroy(DetachFromPanelEvent evt)
        {
            Runtime.Stop(false);
        }

        /// <summary>
        /// 
        /// </summary>
        public void PlayOrStop(bool isReplay = false, bool isEditorLoop = false)
        {
            if (_playingSchedule == null)
                return;
            if (_playingSchedule.isActive)
            {
                this.Q<LeftMenuPanel>().PlayBtn.style.backgroundColor = StyleKeyword.Null;
                _playingSchedule.Pause();
            }
            else
            {
                _isEditorLoop = isEditorLoop;
                if (isReplay)
                    CurrentFrame.Value = 0;
                this.Q<LeftMenuPanel>().PlayBtn.style.backgroundColor = Color.green;
                PlayNextFrame();
                _playingSchedule.Every((long)(1f / Runtime.FrameRate * 1000)).Resume();
            }

            //用作特效预览 
            if (Selection.activeGameObject != null)
            {
                var playableDirector = Selection.activeGameObject.GetComponent<PlayableDirector>();
                if (playableDirector)
                {
                    playableDirector.Stop();
                    playableDirector.Play();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void OpenInspectorPanel(ISelectable target)
        {
            var inspector = (InspectorPanel)EditorWindow.GetWindow(FLibCustomEditorAttribute.CustomEditors.GetValueOrDefault(target.GetType(), typeof(InspectorPanel)));
            if (inspector.Stage != this)
                inspector.SetStage(this);
            Selected.Value = target;
        }

        /// <summary>
        /// 
        /// </summary>
        private bool ExecuteVerifyHandler(object arg)
        {
            var op = arg.GetType().GetCustomAttribute<TimeLogicEditorAttribute>();
            return op == null || op.IsAllowPreview;
        }

        /// <summary>
        /// 
        /// </summary>
        public void PlayNextFrame()
        {
            if (!IsAllowPlay) return;
            if (_isEditorLoop && !Runtime.IsLoop && Runtime.IsEndFrameOver)
                CurrentFrame.Value = 0;
            Runtime.UpdateNextFrame(FNum.One / Runtime.FrameRate);
            CurrentFrame.Value = Runtime.CurrentFrame;
        }

        /// <summary>
        /// 
        /// </summary>
        public void AddFrame(int delta)
        {
            if (!IsAllowPlay) return;
            var f = Mathf.Clamp(CurrentFrame + delta, 0, 255);
            if (f != CurrentFrame)
                CurrentFrame.Value = f;
        }

        /// <summary>
        /// 
        /// </summary>
        public void RefreshEndFrame()
        {
            var endFrameTemp = 0;
            foreach (var track in Runtime.Tracks)
            {
                foreach (var clip in track.Clips)
                {
                    if (clip.EndFrame > endFrameTemp)
                        endFrameTemp = clip.EndFrame;
                }
            }

            if (endFrameTemp != EndFrame)
                EndFrame.Value = endFrameTemp;
            if (IsAllowPlay)
                Runtime.UpdateCurrentFrame();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Refresh(bool isTips = false)
        {
            if (isTips)
                Tips.Show("刷新");
            foreach (var item in this.Query<ImmediateModeElement>().Build())
                item.MarkDirtyRepaint();
            foreach (var item in this.Query<ClipItemPanel>().Build())
                item.RefreshUI();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Save()
        {
            RefreshEndFrame();
            foreach (var track in Runtime.Tracks)
            {
                var trackType = track.GetType();
                if (trackType != typeof(TimeLogicTrack))
                    VerifyFields(track, track.Name);
                foreach (var clip in track.Clips)
                    VerifyFields(clip, clip.Name);
            }

            var items = this.Query<ClipItemPanel>().Build();
            foreach (var clip in items)
                clip.OnSave(true);
            OnSaveHandler?.Invoke(this);
            foreach (var clip in items)
                clip.OnSave(false);
        }


        /// <summary>
        /// 
        /// </summary>
        private void VerifyFields(object obj, string objName)
        {
            foreach (var field in obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
                VerifyField(obj, field, objName);
        }

        /// <summary>
        /// 
        /// </summary>
        private void VerifyField(object obj, FieldInfo field, string objName)
        {
            var comment = field.GetCustomAttribute<CommentAttribute>(false);
            if (comment != null && comment.Name.EndsWith('*'))
            {
                var val = field.GetValue(obj);
                if (val == null || (val is IExternalReferenceField unityObjectField && unityObjectField.Index < 0))
                {
                    var str = $"必填字段没有设置 {Runtime.Name}>{objName}>{comment.Name}";
                    Tips.Show(str);
                    throw new Exception(str);
                }
            }
        }
    }
}
