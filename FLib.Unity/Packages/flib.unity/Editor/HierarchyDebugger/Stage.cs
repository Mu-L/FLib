// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;

namespace FLib.Unity.Editor.HierarchyDebugger
{
    public class Stage : EditorWindow
    {
        private IConnectionState _connection;
        private HierarchyData _data;
        private Vector2 _scrollPos;
        private string _search = string.Empty;
        private Dictionary<uint, bool> _expandFolds = new();

        [MenuItem("Window/Analysis/Hierarchy Debugger")]
        public static void Open() => GetWindow<Stage>("Hierarchy Debugger");

        private void OnEnable()
        {
            _connection = PlayerConnectionGUIUtility.GetConnectionState(this);
            EditorConnection.instance.Initialize();
            EditorConnection.instance.Register(Runtime.GetHierarchyGuid, OnGetHierarchy);
        }

        private void OnDisable()
        {
            _connection.Dispose();
            EditorConnection.instance.DisconnectAll();
            EditorConnection.instance.Unregister(Runtime.GetHierarchyGuid, OnGetHierarchy);
        }

        private void OnGUI()
        {
            PlayerConnectionGUILayout.ConnectionTargetSelectionDropdown(_connection, EditorStyles.toolbarDropDown);
            var builder = StringFLibUtility.GetStrBuf();
            builder.AppendLine($"{EditorConnection.instance.ConnectedPlayers.Count} players connected.");
            var i = 0;
            foreach (var p in EditorConnection.instance.ConnectedPlayers)
                builder.AppendLine($"[{i++}] - {p.name} {p.playerId}");
            EditorGUILayout.HelpBox(builder.ToString(), MessageType.Info);
            if (GUILayout.Button(new GUIContent("Show Hierarchy Snapshot", "Send hierarchy information from the Player to the Editor and display it as preview stage.\nRequires an active EditorConnection, select a target in the dropdown above.")))
            {
                _expandFolds.Clear();
                if (EditorConnection.instance.ConnectedPlayers.Count == 0)
                {
                    Runtime.HierarchyData.Sample(Runtime.HierarchyIdMap);
                    _data = Runtime.HierarchyData;
                }
                else
                {
                    EditorConnection.instance.Send(Runtime.GetHierarchyGuid, Array.Empty<byte>());
                }
            }

            _search = EditorGUILayout.TextField(_search, (GUIStyle)"SearchTextField");
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);
            DrawNodes(_data.Nodes);
            GUILayout.EndScrollView();
        }

        /// <summary>
        /// 
        /// </summary>
        private void DrawNodes(in HierarchyNode[] nodes)
        {
            if (nodes == null)
                return;
            for (var i = 0; i < nodes.Length; i++)
            {
                if (!nodes[i].Name.Contains(_search))
                    continue;
                nodes[i].IsActive = EditorGUILayout.ToggleLeft(nodes[i].Name, nodes[i].IsActive);
                EditorGUI.BeginChangeCheck();
                var fold = EditorGUILayout.Foldout(_expandFolds.GetValueOrDefault(nodes[i].Id), nodes[i].Name);
                if (EditorGUI.EndChangeCheck())
                    _expandFolds[nodes[i].Id] = fold;
                if (fold)
                {
                    ++EditorGUI.indentLevel;
                    DrawNodes(nodes[i].Children);
                    --EditorGUI.indentLevel;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnGetHierarchy(MessageEventArgs arg0)
        {
            BytesPack.Unpack(ref _data, arg0.data);
        }
    }
}
