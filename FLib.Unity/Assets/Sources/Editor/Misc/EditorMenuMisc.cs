//==================={By Qcbf|qcbf@qq.com|8/22/2021 1:31:29 PM}===================

using Cysharp.Threading.Tasks;
using FLib;
using FLib.Unity;
using HybridCLR.Editor.Settings;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Configs;
using FLib.Unity.Editor;
using FLib.Unity.Editor.PackBuilder;
using FLib.Unity.Editor.PackBuilder.Task.Script;
using Launcher;
using Modules.Dialog;
using Nets;
using TMPro;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Cinemachine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build.Profile;
using UnityEditor.EditorTools;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Utilities;
using Worlds;
using Log = FLib.Log;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Game.Editor
{
    public static class EditorMenuMisc
    {
        [MenuItem("Tools/Misc/测试代码X #%&t", priority = 100000)]
        public static void Test1()
        {
        }

        [MenuItem("Tools/Misc/测试代码Y #%&y", priority = 100000)]
        public static void Test2()
        {
        }
    }
}
