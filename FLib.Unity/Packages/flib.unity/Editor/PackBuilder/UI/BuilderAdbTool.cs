using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using FLib.Net;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor.PackBuilder.UI
{
    public class BuilderAdbTool : EditorWindow
    {
        public VisualTreeAsset UIAsset;

        private void Awake()
        {
            titleContent.text = "ADB Tool";
        }


        private void CreateGUI()
        {
            var root = rootVisualElement;
            UIAsset.CloneTree(root);

            root.Q<TextField>("Ip").BindDataWithUI(v => PlayerPrefs.SetString("AdbIp", v), () => PlayerPrefs.GetString("AdbIp", GetLocalHostAddress().ToString()));
            root.Q<Button>("Devices").clicked += () => Cmd("devices");
            root.Q<Button>("Restart").clicked += () => Cmd("kill-server", "start-server");
            root.Q<Button>("Forward").clicked += () => Cmd($"forward tcp:34999 localabstract:Unity-{Application.identifier}");
            root.Q<Button>("InstallApk").clicked += () =>
            {
                var path = EditorFLibUtility.OpenFilePanel("APK Path", $"{Utility.PublishPath}/{nameof(BuildTarget.Android)}", "apk");
                if (string.IsNullOrEmpty(path)) return;
                var deviceName = root.Q<TextField>("DeviceName").value;
                if (!string.IsNullOrWhiteSpace(deviceName))
                    Cmd($"-s {deviceName} install \"{path}\"");
                else
                    Cmd($"install \"{path}\"");
            };

            root.Q<Button>("Pair").clicked += () => Cmd($"pair {root.Q<TextField>("Ip").value}:{root.Q<IntegerField>("Port").value}");
            root.Q<Button>("Connect").clicked += () => Cmd($"connect {root.Q<TextField>("Ip").value}:{root.Q<IntegerField>("Port").value}");
        }


        private void Cmd(params string[] args)
        {
            var cmd = string.Join('&', args.Select(a => $"adb {a}"));
            Process.Start("cmd", $"/c {cmd} & pause");
        }

        public static IPAddress GetLocalHostAddress()
        {
            foreach (var network in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (network.NetworkInterfaceType != NetworkInterfaceType.Ethernet && network.NetworkInterfaceType != NetworkInterfaceType.Wireless80211) continue;
                foreach (var ip in network.GetIPProperties().UnicastAddresses)
                {
                    var address = ip.Address;
                    if (address.AddressFamily == AddressFamily.InterNetwork)
                        return address;
                }
            }
            return null;
        }
    }
}
