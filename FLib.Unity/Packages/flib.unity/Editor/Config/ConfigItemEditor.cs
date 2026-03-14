// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace FLib.Unity.Editor
{
    [CustomEditor(typeof(ConfigItemEditorHelper)), CanEditMultipleObjects]
    public class ConfigItemEditor : BaseEditor<ConfigItemEditorHelper>
    {
        private static Regex _idNameMatch = new(@"\[[0-9]+\]");

        public EditorSmallTipsUI Tips;
        public ShortcutKeyManager Shortcut;
        public FieldInfo IdField;
        public FieldInfo NameField;

        public override bool SupportUndo => false;


        public interface ICustomUI
        {
            void CreateUI(ConfigItemEditor editor);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Shortcut = new ShortcutKeyManager().Register("save", KeyCode.S, ShortcutKeyManager.EModifier.Ctrl, () => SetState(Save), inputFocusStillProcess: true, sureFocusElement: true);

            var assetPath = AssetDatabase.GetAssetPath(target);
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (guid != target.AssetGuid)
            {
                if (!string.IsNullOrEmpty(target.AssetGuid))
                {
                    Log.Info?.Write($"find new config file, auto generate id, {assetPath}");
                    RenameSelf(TypeAssistant.GetType(target.ConfigType));
                }
                SetState(val => val.AssetGuid = guid, false, false);
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            Shortcut.RegisterKeyEvent(root, true);
            if (!string.IsNullOrEmpty(target.ConfigType) && FLibCustomEditorAttribute.TryGetEditor<ICustomUI>(TypeAssistant.GetType(target.ConfigType, isThrowOnError: false), out var customEditor))
                customEditor.CreateUI(this);
            else
                root.Add(base.CreateInspectorGUI());
            root.Add(Tips = new EditorSmallTipsUI());
            return root;
        }

        public override void CreateUI(ConfigItemEditorHelper targetObject)
        {
            if (string.IsNullOrEmpty(targetObject.ConfigType))
            {
                RootUI.Add(new Button(OnClickSetConfigType) { text = "设置配置表类型" });
                return;
            }
            var type = TypeAssistant.GetType(targetObject.ConfigType);
            if (IdField == null)
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance).OrderBy(v => v.MetadataToken).Take(2).ToArray();
                IdField = fields[0];
                NameField = fields[1];
                if (NameField.FieldType != typeof(string) || NameField.Name != "Name")
                    NameField = null;
            }

            targetObject.Instance = (IBytesPackable)TypeAssistant.New(type);
            if (targetObject.Bytes?.Length > 0)
                BytesPack.Unpack(ref targetObject.Instance, Compressor.Uncompress(targetObject.Bytes));
            VisualElement bar = new Toolbar();
            RootUI.Add(bar);
            bar.Add(new ToolbarButton(OnClickSetConfigType) { text = CommentAttribute.TryGetLabel(type), style = { color = Color.blue } });
            bar.Add(new ToolbarButton(Revert) { text = "还原", tooltip = "" });
            var saveBtn = new ToolbarButton(() => SetState(Save)) { style = { flexGrow = 1 }, text = "保存", tooltip = "ctrl+s" };
            saveBtn.RegisterCallback<ContextClickEvent>(evt => OnClickSaveBtnContext(targetObject, evt));
            bar.Add(saveBtn);
            bar = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
            RootUI.Add(bar);
            new TextField(CommentAttribute.TryGetLabel(IdField)) { style = { flexGrow = 1 } }.ShortFieldLabel()
                .BindDataWithUI(s => SetAssetNameId(targetObject, s), () => GetAssetNameId(targetObject.name).ToString(), true).AddToUI(bar);
            bar.Add(new ToolbarButton(() => GenId(targetObject)) { text = "生成" });
            RootUI.Add(new AnyObjectField(CommentAttribute.TryGetLabel(type), null, targetObject.Instance, new AnyObjectField.OptionData()
            {
                Flags = AnyObjectField.EOptionFlag.ObjectMustComment | AnyObjectField.EOptionFlag.SkipRootObjectFold,
                Hook = HookObjectField
            }, this) { name = nameof(ConfigItemEditor) });
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual UIBindData HookObjectField(AnyObjectField objUI, Type valueType, Action<object> setValue, Func<object> getValue)
        {
            if (ReferenceEquals(objUI.userData, IdField))
                return new UIBindData() { UI = new VisualElement() };
            if (NameField != null && ReferenceEquals(objUI.userData, NameField))
                return new Label().BindDataToUI(ui => ui.text = $"{CommentAttribute.TryGetLabel(NameField)}: {_idNameMatch.Replace(target.name, "")}");
            // if (valueType == typeof(WorldEffectConfigPack))
            //     return CreateEffectField(objUI, valueType, setValue, getValue);
            return null;
        }

        // /// <summary>
        // /// 
        // /// </summary>
        // private static UIBindData CreateEffectField(AnyObjectField objUI, Type valueType, Action<object> setValue, Func<object> getValue)
        // {
        //     var val = (WorldEffectConfigPack)getValue();
        //     var root = new TitleAreaUI(nameof(ConfigItemEditor) + "Effect").SetTitle(objUI.Label, valueType.FullName);
        //     root.AddToMenuBar(new ToolbarButton(() =>
        //     {
        //         TypeChooserWindow.Open(typeof(WorldEffect), val.Instance?.GetType(), selectType =>
        //         {
        //             val.Instance = selectType == null ? null : (WorldEffect)TypeAssistant.New(selectType);
        //             setValue(val);
        //             objUI.BindData.Dirty();
        //         }, options: TypeChooserWindow.EOption.MustComment);
        //     }) { text = "选择脚本" });
        //     return root.BindDataToUI(_ =>
        //     {
        //         root.Clear();
        //         if (val.Instance != null)
        //             root.Add(new AnyObjectField(null, null, val.Instance, new AnyObjectField.OptionData() { Flags = AnyObjectField.EOptionFlag.ObjectMustComment | AnyObjectField.EOptionFlag.SkipRootObjectFold }));
        //         var label = CommentAttribute.TryGetLabel(val.Instance?.GetType(), out var detail);
        //         root.Title = $"{objUI.Label}-{label}";
        //         root.TitleUI.tooltip = detail;
        //     });
        // }

        /// <summary>
        /// 
        /// </summary>
        private void GenId(ConfigItemEditorHelper val)
        {
            if (IdField.FieldType == typeof(string))
            {
                SetAssetNameId(val, GuidHelper.Create32().ToString());
            }
            else
            {
                ConfigToolEditor.BuildConfig();
                ConfigToolEditor.LoadConfig();
                var metas = (IReadOnlyDictionary<uint, int>)typeof(Config<>).MakeGenericType(TypeAssistant.GetType(val.ConfigType)).GetField("IdMetas", BindingFlags.Static | BindingFlags.Public)!.GetValue(null);
                var id = 1u;
                while (metas?.ContainsKey(id) == true) id++;
                SetAssetNameId(val, id.ToString());
                RefreshUI();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Save(ConfigItemEditorHelper val)
        {
            NameField?.SetValue(val.Instance, _idNameMatch.Replace(target.name, ""));
            val.SetConfig(val.Instance);
            Tips.Show("保存成功");
            if (ConfigToolEditor.CheckIsAutoRebuild())
                ConfigToolEditor.BuildConfig();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Revert()
        {
            RefreshUI();
            Tips.Show("还原到最初数据");
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnClickSetConfigType()
        {
            TypeChooserWindow.Open(typeof(IBytesPackable), TypeAssistant.GetType(target.ConfigType ?? string.Empty, false, false), type => SetState(val =>
            {
                if (val.ConfigType == null && !val.name.Contains('[', StringComparison.Ordinal))
                    RenameSelf(type);
                val.Bytes = null;
                val.ConfigType = TypeAssistant.GetTypeName(type);
            }), type => type.IsDefined(typeof(ConfigAttribute)), TypeChooserWindow.EOption.HideSetNull);
        }

        /// <summary>
        /// 
        /// </summary>
        private void RenameSelf(Type type)
        {
            var newName = target.name;
            if (_idNameMatch.IsMatch(newName))
            {
                if (newName.EndsWith(" 1"))
                    newName = newName[..^2];
                newName = _idNameMatch.Replace(newName, $"[{GuidHelper.Create32().ToString()}]");
            }
            else
            {
                newName = $"{CommentAttribute.TryGetLabel(type)}[{GuidHelper.Create32().ToString()}]";
            }
            AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(target), newName);
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnClickSaveBtnContext(ConfigItemEditorHelper targetObject, ContextClickEvent evt)
        {
            var type = TypeAssistant.GetType(targetObject.ConfigType);
            var typeName = CommentAttribute.TryGetLabel(type);
            var contextMenu = new GenericMenu();
            contextMenu.AddItem(new GUIContent($"Save All Config {typeName}"), false, () =>
            {
                foreach (var assetGuid in AssetDatabase.FindAssets("t:ConfigItemEditorHelper"))
                {
                    var item = AssetDatabase.LoadAssetAtPath<ConfigItemEditorHelper>(AssetDatabase.GUIDToAssetPath(assetGuid));
                    if (targetObject.ConfigType != item.ConfigType)
                        continue;
                    item.Bytes = Compressor.Compress(BytesPack.Pack(item.CreateConfig())).ToArray();
                    EditorUtility.SetDirty(item);
                }
                AssetDatabase.SaveAssets();
            });
            contextMenu.AddItem(new GUIContent($"Select All Config {typeName}"), false, () =>
            {
                Selection.objects = AssetDatabase.FindAssets("t:ConfigItemEditorHelper")
                    .Select(assetGuid => AssetDatabase.LoadAssetAtPath<ConfigItemEditorHelper>(AssetDatabase.GUIDToAssetPath(assetGuid)))
                    .Where(v => targetObject.ConfigType == v.ConfigType).ToArray<Object>();
            });
            contextMenu.ShowAsContext();
            evt.StopImmediatePropagation();
        }

        /// <summary>
        /// 
        /// </summary>
        public static void ProcessAllConfig<T>(Func<T, bool> handler) where T : IBytesPackable
        {
            var match = typeof(T).ToString();
            foreach (var assetGuid in AssetDatabase.FindAssets("t:ConfigItemEditorHelper"))
            {
                var item = AssetDatabase.LoadAssetAtPath<ConfigItemEditorHelper>(AssetDatabase.GUIDToAssetPath(assetGuid));
                if (item.ConfigType != match)
                    continue;
                var inst = (T)item.CreateConfig();
                if (handler(inst))
                {
                    item.Bytes = Compressor.Compress(BytesPack.Pack(inst)).ToArray();
                    EditorUtility.SetDirty(item);
                }
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 获取资源名称的id比如，资源可以命名abc[101]或者101。就获取101
        /// </summary>
        public static ReadOnlySpan<char> GetAssetNameId(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            var right = name.LastIndexOf(']');
            if (right == -1) return name;
            var left = name.LastIndexOf('[', right);
            if (left == -1 || left >= right) return name;
            return name.AsSpan(left + 1, right - left - 1);
        }

        /// <summary>
        /// 设置资源名称的id比如，输入123，把资源名称改为abc[123]或者123
        /// </summary>
        public static void SetAssetNameId(Object target, string id)
        {
            var oldStr = target.name;
            string newStr;
            if (string.IsNullOrEmpty(oldStr))
            {
                newStr = $"[{id ?? string.Empty}]";
            }
            else
            {
                var right = oldStr.LastIndexOf(']');
                if (right == -1)
                    newStr = $"[{id ?? string.Empty}]{oldStr}";
                else
                {
                    var left = oldStr.LastIndexOf('[', right);
                    if (left == -1 || left >= right)
                        newStr = $"{oldStr}[{id ?? string.Empty}]";
                    else
                        newStr = oldStr[..(left + 1)] + (id ?? string.Empty) + oldStr[right..];
                }
            }
            AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(target), newStr);
        }
    }
}
