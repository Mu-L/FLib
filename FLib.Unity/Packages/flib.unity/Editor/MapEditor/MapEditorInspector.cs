// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using System.Buffers.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor
{
    [CustomEditor(typeof(MapEditorHelper), true)]
    public class MapEditorInspector : BaseEditor<MapEditorHelper>
    {
        public QuadMap Map;
        public int EditLayer;
        private MapEditorSceneView _sceneView;
        public bool IsChanged;

        private void Awake()
        {
            ReadMap();
            _sceneView = new MapEditorSceneView(this);
        }

        private void OnDestroy()
        {
            if (IsChanged)
            {
                if (EditorFLibUtility.AlertSure("Save Changed?"))
                    WriteMap();
            }
            Map = null;
        }

        private void OnSceneGUI() => (_sceneView ??= new MapEditorSceneView(this)).OnSceneGUI();

        public override void RefreshUI()
        {
            base.RefreshUI();
            SceneView.lastActiveSceneView.Repaint();
        }

        public override void CreateUI(MapEditorHelper targetObject)
        {
            if (Map == null)
                ReadMap();

            RootUI.schedule.Execute(() => { SceneView.lastActiveSceneView.Repaint(); }).Every(0);

            var bar = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
            RootUI.Add(bar);
            bar.Add(new Button(() =>
            {
                target.RawData = Convert.FromBase64String(EditorFLibUtility.ClipboardTxt);
                ReadMap();
                RefreshUI();
            }) { text = "import" });
            bar.Add(new Button(() =>
            {
                WriteMap();
                var data = Convert.ToBase64String(target.RawData);
                EditorFLibUtility.ClipboardTxt = data;
                Log.Info?.Write($"Data Copied Clipboard[{data.Length}]:\n{data}");
            }) { text = "Export" });
            bar.Add(new Button(() =>
            {
                ReadMap();
                RefreshUI();
            }) { text = "Revert", style = { flexGrow = 1 } });
            bar.Add(new Button(WriteMap) { text = "Save", style = { flexGrow = 1 } });

            new FloatField("TileSize").BindDataWithUI(v =>
            {
                IsChanged = true;
                Map.TileSize = (FNum)v;
                SceneView.lastActiveSceneView.Repaint();
            }, () => Map.TileSize).AddToUI(RootUI);

            new Vector2Field("Offset").BindDataWithUI(v =>
            {
                IsChanged = true;
                Map.Offset = v.AsFVec2();
                SceneView.lastActiveSceneView.Repaint();
            }, () => Map.Offset.AsVec()).AddToUI(RootUI);

            new Vector2IntField("Size").BindDataWithUI(v =>
            {
                IsChanged = true;
                Map.SetSize(new FVector2Int(v.x, v.y));
                SceneView.lastActiveSceneView.Repaint();
            }, () => new Vector2Int(Map.TerrainSize.X, Map.TerrainSize.Y), true).AddToUI(RootUI);

            new IntegerField("LayerCount").BindDataWithUIVerify(v =>
            {
                if (v < 1) return false;
                IsChanged = true;
                Map.SetLayers(v);
                RefreshLayerItems();
                SceneView.lastActiveSceneView.Repaint();
                return true;
            }, () => Map.LayerCount, true).AddToUI(RootUI);

            RefreshLayerItems();
        }

        private void RefreshLayerItems()
        {
            var root = RootUI.Q("LayerRoot");
            if (root == null)
                RootUI.Add(root = new VisualElement() { name = "LayerRoot" });
            else
                root.Clear();
            for (var i = 0; i < Map.LayerCount; i++)
            {
                var bar = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
                root.Add(bar);
                bar.Add(new Label(i.ToString()));
                var active = new Toggle() { text = "Active", style = { flexGrow = 1 }, value = i == 0 };
                bar.Add(active);
                bar.Add(new Button() { text = "Delete" });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void WriteMap()
        {
            IsChanged = false;
            var writer = BytesWriter.CreateFromPool(4096);
            Map?.Z_BytesWrite(ref writer);
            target.RawData = Compressor.Compress(writer.Span).ToArray();
            EditorUtility.SetDirty(target);
        }

        /// <summary>
        /// 
        /// </summary>
        public void ReadMap()
        {
            Map = new QuadMap();
            if ((target.RawData?.Length).GetValueOrDefault() <= 0)
            {
                Map.SetSize(new FVector2Int(100, 100), 1);
                return;
            }
            BytesReader reader = Compressor.Uncompress(target.RawData).AsSpan();
            Map.Z_BytesRead(ref reader);
        }
    }
}
