// ==================== qcbf@qq.com | 2025-07-01 ====================

#if DEBUG

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace FLib.Unity.Editor.HierarchyDebugger
{
    public static class Runtime
    {
        public static Guid GetHierarchyGuid = new("362298BB-401E-500B-A054-E23C42BD1668");
        public static Guid SetGameObjectActiveGuid = new("15214B31-14E2-F4E8-959A-67D65C49AF7E");
        public static HierarchyData HierarchyData;
        public static Dictionary<uint, Object> HierarchyIdMap = new();


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void Initialize()
        {
            PlayerConnection.instance.RegisterConnection(arg0 => Log.Info?.Write($"Connection: {arg0}"));
            PlayerConnection.instance.RegisterDisconnection(arg0 => Log.Info?.Write($"Disconnection: {arg0}"));
            PlayerConnection.instance.Register(GetHierarchyGuid, OnReceiveMessage);
            PlayerConnection.instance.Register(SetGameObjectActiveGuid, OnSetGameObjectActive);
        }

        /// <summary>
        /// 
        /// </summary>
        private static void OnSetGameObjectActive(MessageEventArgs arg0)
        {
            var data = BytesPack.Unpack<SetActiveData>(arg0.data);
            if (data.Version != HierarchyData.Version)
                throw new Exception($"version error {HierarchyData.Version}/{data.Version}");
            if (!HierarchyIdMap.TryGetValue(data.NodeId, out var obj))
                throw new Exception($"not found {data.NodeId}");
            if (obj is GameObject go)
                go.SetActive(data.Value);
            else if (obj is Transform tf)
                tf.gameObject.SetActive(data.Value);
            else if (obj is Behaviour comp)
                comp.enabled = data.Value;
        }

        /// <summary>
        /// 
        /// </summary>
        private static void OnReceiveMessage(MessageEventArgs arg0)
        {
            HierarchyData.Sample(HierarchyIdMap);
            PlayerConnection.instance.Send(GetHierarchyGuid, BytesPack.Pack(HierarchyData).ToArray());
        }
    }

    [BytesPackGen]
    public partial struct SetActiveData
    {
        public uint Version;
        public uint NodeId;
        public bool Value;
    }

    [BytesPackGen]
    public partial struct HierarchyData
    {
        public uint Version;
        public HierarchyNode[] Nodes;

        public void Sample(Dictionary<uint, Object> map)
        {
            map.Clear();
            ++Version;
            var id = 0u;
            var nodes = new List<HierarchyNode>();
            var count = SceneManager.sceneCount;
            for (var i = 0; i < count; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                var children = new List<HierarchyNode>();
                var roots = scene.GetRootGameObjects();
                foreach (var root in roots)
                    Add(root.transform, children);
                nodes.Add(new HierarchyNode() { Type = HierarchyNode.EType.Scene, Name = scene.name, Children = children.ToArray() });
            }
            Nodes = nodes.ToArray();
            return;

            void Add(Transform root, List<HierarchyNode> list)
            {
                List<HierarchyNode> children = null;
                if (root.childCount > 0)
                {
                    children = new List<HierarchyNode>();
                    foreach (Transform child in root)
                        Add(child, children);
                }
                var comps = new List<HierarchyNode>();
                foreach (var comp in root.GetComponents<Component>())
                {
                    ++id;
                    map.Add(id, comp);
                    comps.Add(new HierarchyNode()
                    {
                        Id = id,
                        Type = HierarchyNode.EType.Component, Name = comp.name,
                        IsActive = comp is not Behaviour bComp || bComp.enabled,
                    });
                }
                ++id;
                map.Add(id, root);
                list.Add(new HierarchyNode()
                {
                    Id = id,
                    Type = HierarchyNode.EType.GameObject, Name = root.name, IsActive = root.gameObject.activeSelf,
                    Children = children?.ToArray(), Components = comps.ToArray()
                });
            }
        }
    }

    [BytesPackGen, StructLayout(LayoutKind.Auto)]
    public partial struct HierarchyNode
    {
        public enum EType : byte { None, Scene, GameObject, Component }

        public uint Id;
        public EType Type;
        public bool IsActive;
        public string Name;
        public HierarchyNode[] Components;
        public HierarchyNode[] Children;
    }
}
#endif
