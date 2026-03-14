// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using FLib.WorldCores.TimeLogic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor.TimeLogic
{
    public class LeftMenuPanel : VisualElement, IContextMenuProcessable
    {
        public readonly StagePanel Stage;
        public Button PlayBtn;

        public LeftMenuPanel(StagePanel stage)
        {
            Stage = stage;
            this.FlexDirection(FlexDirection.Row).Width(Stage.TrackListWidth.Value);
            if (stage.IsAllowPlay)
            {
                Add(PlayBtn = CreateMenuButton("播放", "空格键：继续播放\nP：重新播放\nshift+空格、shift+P:循环播放", () => stage.PlayOrStop()));
                Add(CreateMenuButton("<", "键盘:A,左箭头", () => stage.AddFrame(-1)));
                Add(CreateMenuButton(">", "键盘:D,右箭头", () => stage.AddFrame(1)));
                new IntegerField() { style = { width = 25 } }
                    .BindDataWithUI(v => stage.CurrentFrame.Value = v, () => stage.CurrentFrame.Value)
                    .ListenEvent(ref stage.CurrentFrame)
                    .AddToUI(this);
            }
            else
            {
                Add(new Label("简易模式") { tooltip = "完整模式请通过预制实例打开编辑", style = { color = Color.red, flexGrow = 1, unityTextAlign = TextAnchor.MiddleCenter } });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static Button CreateMenuButton(string text, string tooltip, Action handler)
        {
            return new ToolbarButton(handler) { text = text, tooltip = tooltip, focusable = false };
        }

        public void ContextMenuProcess(MouseUpEvent evt, GenericMenu menu)
        {
            menu.AddItem(new GUIContent($"设置帧率[{Stage.Runtime.FrameRate}]"), false, () =>
            {
                var dialog = DialogWindow.Open(new DialogWindow.OptionData()
                {
                    Title = "设置帧率",
                    Btns = new[] { "确定" },
                    CustomUI = new IntegerField() { value = Stage.Runtime.FrameRate }
                }, DialogWindow.EOpenType.ModalUtility);
                Stage.Runtime.FrameRate = (byte)dialog.GetCustomUI<IntegerField>().value;
                Log.Info?.Write($"set frame to: {Stage.Runtime.FrameRate}");
            });
            menu.AddItem(new GUIContent("循环播放"), Stage.Runtime.IsLoop, () => { Stage.Runtime.IsLoop = !Stage.Runtime.IsLoop; });
            menu.AddItem(new GUIContent("复制"), false, () =>
            {
                var writer = new BytesWriter();
                writer.PushScript(Stage.Runtime);
                EditorFLibUtility.ClipboardTxt = $"{nameof(TimeLogicRuntime)}|{Convert.ToBase64String(writer.Span)}";
                Stage.Tips.Show($"复制 {FIO.FormatSize(writer.Length)}");
            });
            if (EditorFLibUtility.ClipboardTxt.StartsWith($"{nameof(TimeLogicRuntime)}|"))
            {
                menu.AddItem(new GUIContent("粘贴"), false, () =>
                {
                    var reader = new BytesReader(Convert.FromBase64String(EditorFLibUtility.ClipboardTxt[$"{nameof(TimeLogicRuntime)}|".Length..]));
                    var runtime = (TimeLogicRuntime)reader.ReadScript();
                    runtime.ExternalReferences = Stage.Runtime.ExternalReferences;
                    runtime.ExecuteVerifyHandler = Stage.Runtime.ExecuteVerifyHandler;
                    Stage.Runtime = runtime;
                    Stage.Q<TrackListPanel>().RefreshTracks();
                    Stage.CurrentFrame.Value = runtime.CurrentFrame;
                });
            }
        }
    }
}
