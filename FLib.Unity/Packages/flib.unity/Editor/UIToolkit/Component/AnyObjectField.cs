// =================================================={By Qcbf|qcbf@qq.com|2024-2-3}==================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace FLib.Unity.Editor
{
    public class AnyObjectField : VisualElement, INotifyValueChanged<object>
    {
        public string Label;
        public Type ValueType;
        public OptionData Option;
        public UIBindData BindData;
        protected object _value;

        public virtual object value
        {
            get => _value;
            set
            {
                if (!ValueType.IsClass && value is null)
                    value = ValueType.DefaultValue();
                Log.Assert(_value == null || _value.GetType() == ValueType);
                var oldVal = _value;
                SetValueWithoutNotify(value);
                NotifyEvent(oldVal, value);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public struct OptionData
        {
            public EOptionFlag Flags;
            public Func<AnyObjectField, Type, Action<object>, Func<object>, UIBindData> Hook;
            public static implicit operator OptionData(in EOptionFlag optionsFlag) => new() { Flags = optionsFlag };
        }

        public interface ICustomEditor
        {
            UIBindData CreateUI(AnyObjectField field, Type type, Action<object> setValue, Func<object> getValue);
        }

        [Flags]
        public enum EOptionFlag
        {
            None,

            /// <summary>
            /// 对象必须有Comment属性才绘制
            /// </summary>
            ObjectMustComment = 0x1,

            // /// <summary>
            // /// 对象必须有Comment属性才绘制，额外包含serializable的对象
            // /// </summary>
            // ObjectMustCommentIncludeSerializable = 0x2,

            /// <summary>
            /// 对象不绘制为UI，而是绘制为一个简单的文本框
            /// </summary>
            ObjectAsJsonTextField = 0x4,

            /// <summary>
            /// 默认折叠数组
            /// </summary>
            FoldArray = 0x8,

            /// <summary>
            /// 默认折叠对象字段
            /// </summary>
            FoldObject = 0x10,

            /// <summary>
            /// 跳过根对象折叠
            /// </summary>
            SkipRootObjectFold = 0x20,

            FoldAll = FoldArray | FoldObject,
        }

        public AnyObjectField()
        {
        }

        public AnyObjectField(string label, Type valueType, in OptionData option = default, object userData = null)
        {
            this.userData = userData;
            Option = option;
            ValueType = valueType;
            Label = label;
            _value = valueType.DefaultValue();
            AddValueUI(ValueType, InternalSetValue, () => value);
        }

        public AnyObjectField(string label, Type valueType, object data, in OptionData option = default, object userData = null)
        {
            this.userData = userData;
            Option = option;
            ValueType = valueType ?? data.GetType();
            Label = label;
            _value = data;
            AddValueUI(ValueType, InternalSetValue, () => value);
            SetValueWithoutNotify(_value);
        }

        public AnyObjectField Set(string label, Type valueType, Action<object> setValue, Func<object> getValue, in OptionData option = default)
        {
            Option = option;
            ValueType = valueType;
            Label = label;
            _value = valueType.DefaultValue();
            AddValueUI(ValueType, setValue, getValue);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void InternalSetValue(object newVal)
        {
            var oldVal = _value;
            _value = newVal;
            NotifyEvent(oldVal, newVal);
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void NotifyEvent(object oldVal, object newVal)
        {
            using var e = ChangeEvent<object>.GetPooled(oldVal, newVal);
            e.target = this;
            SendEvent(e);
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void SetValueWithoutNotify(object newValue)
        {
            _value = newValue;
            BindData.Dirty();
        }

        /// <summary>
        /// 
        /// </summary>
        private static T T<T>(object v)
        {
            if (v == null)
                return default;
            return (T)v;
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void AddValueUI(Type type, Action<object> setValue, Func<object> getValue)
        {
            BindData = Option.Hook?.Invoke(this, type, setValue, getValue) ??
                       FLibCustomEditorAttribute.GetEditor<ICustomEditor>(type)?.CreateUI(this, type, setValue, getValue);
            if (BindData != null)
            {
                BindData.AddToUI(this);
                return;
            }

            if (type.IsEnum)
            {
                var vEnum = (Enum)getValue() ?? (Enum)Enum.ToObject(type, 0);
                BindData = new SelectBoxField().Set(Label, vEnum)
                    .BindDataWithUI(v => setValue(Enum.ToObject(type, v)), () => Convert.ToInt32(getValue() ?? 0));
                // BindData = type.IsDefined(typeof(FlagsAttribute))
                //     ? new EnumFlagsField(Label, (Enum)getValue()).ShortFieldLabel().BindDataWithUI(setValue, () => (Enum)getValue())
                //     : new EnumField(Label, (Enum)getValue()).ShortFieldLabel().BindDataWithUI(setValue, () => (Enum)getValue());
            }
            else
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Byte:
                        BindData = new IntegerField(Label).ShortFieldLabel().BindDataWithUI(v => setValue((byte)v), () => T<byte>(getValue()));
                        break;
                    case TypeCode.SByte:
                        BindData = new IntegerField(Label).ShortFieldLabel().BindDataWithUI(v => setValue((sbyte)v), () => T<sbyte>(getValue()));
                        break;
                    case TypeCode.Int16:
                        BindData = new IntegerField(Label).ShortFieldLabel().BindDataWithUI(v => setValue((short)v), () => T<short>(getValue()));
                        break;
                    case TypeCode.UInt16:
                        BindData = new IntegerField(Label).ShortFieldLabel().BindDataWithUI(v => setValue((ushort)v), () => T<ushort>(getValue()));
                        break;
                    case TypeCode.Int32:
                        BindData = CreateStructValueBinder(new IntegerField(Label));
                        break;
                    case TypeCode.UInt32:
                        BindData = new LongField(Label).ShortFieldLabel().BindDataWithUI(v => setValue((uint)v), () => T<uint>(getValue()));
                        break;
                    case TypeCode.Int64:
                        BindData = CreateStructValueBinder(new LongField(Label));
                        break;
                    case TypeCode.Single:
                        BindData = CreateStructValueBinder(new FloatField(Label));
                        break;
                    case TypeCode.Double:
                        BindData = CreateStructValueBinder(new DoubleField(Label));
                        break;
                    case TypeCode.Boolean:
                        BindData = CreateStructValueBinder(new Toggle(Label));
                        break;
                    case TypeCode.Char:
                        BindData = new TextField(Label).ShortFieldLabel().FlexGrow(1)
                            .BindDataWithUI(v => setValue(v.FirstOrDefault()), () => getValue().ToString());
                        break;
                    case TypeCode.String:
                        BindData = new TextField(Label).ShortFieldLabel().BindDataWithUI(setValue, () => T<string>(getValue()), true);
                        break;
                    case TypeCode.Object:
                        if (typeof(FNum) == type)
                            BindData = new FloatField(Label).ShortFieldLabel().FlexGrow(1)
                                .BindDataWithUI(v => setValue((FNum)v), () => (FNum)getValue());
                        else if (typeof(Vector2).IsAssignableFrom(type))
                            BindData = CreateStructValueBinder(new Vector2Field(Label));
                        else if (typeof(Vector3).IsAssignableFrom(type))
                            BindData = CreateStructValueBinder(new Vector3Field(Label));
                        else if (typeof(Rect).IsAssignableFrom(type))
                            BindData = CreateStructValueBinder(new RectField(Label));
                        else if (typeof(Bounds).IsAssignableFrom(type))
                            BindData = CreateStructValueBinder(new BoundsField(Label));
                        else if (typeof(BoundsInt).IsAssignableFrom(type))
                            BindData = CreateStructValueBinder(new BoundsIntField(Label));
                        else if (typeof(Vector2Int).IsAssignableFrom(type))
                            BindData = CreateStructValueBinder(new Vector2IntField(Label));
                        else if (typeof(Vector3Int).IsAssignableFrom(type))
                            BindData = CreateStructValueBinder(new Vector3IntField(Label));
                        else if (typeof(RectInt).IsAssignableFrom(type))
                            BindData = CreateStructValueBinder(new RectIntField(Label));
                        else if (typeof(Color).IsAssignableFrom(type))
                            BindData = CreateStructValueBinder(new ColorField(Label));
                        else if (typeof(Color32).IsAssignableFrom(type))
                            BindData = new ColorField(Label).ShortFieldLabel().FlexGrow(1)
                                .BindDataWithUI(v => setValue((Color32)v), () => (Color)getValue());
                        else if (typeof(Object).IsAssignableFrom(type))
                            BindData = new ObjectField(Label) { objectType = type }.ShortFieldLabel()
                                .BindDataWithUI(setValue, () => T<Object>(getValue()));
                        else if (typeof(IList).IsAssignableFrom(type))
                            BindData = AddArrayField(type, setValue, getValue);
                        else if (typeof(BitFlags) == type)
                            DrawWorldFlags(type, setValue, getValue);
                        else if (typeof(ScriptPack).IsAssignableFrom(type))
                            DrawScriptPack(type, setValue, getValue);
                        else if (typeof(IEditableConfigId).IsAssignableFrom(type))
                            BindData = CreateConfigIdField(this, type, setValue, getValue);
                        else
                        {
                            if ((Option.Flags & EOptionFlag.ObjectAsJsonTextField) != 0)
                                BindData = new TextField(Label).ShortFieldLabel().BindDataWithUI(v => setValue(Json5.Deserialize(v, type)),
                                    () => Json5.Serialize(getValue()), true);
                            else
                                BindData = typeof(IDictionary).IsAssignableFrom(type)
                                    ? DrawDictionaryField(type, setValue, getValue)
                                    : DrawObjField(type, setValue, getValue);
                        }

                        break;
                }
            }

            if (BindData == null)
                throw new NotSupportedException(TypeAssistant.GetTypeName(type));
            BindData.AddToUI(this);
            return;

            UIBindData CreateStructValueBinder<T>(BaseField<T> ui) =>
                ui.ShortFieldLabel().BindDataWithUI(v => setValue(v), () => T<T>(getValue()));
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void DrawScriptPack(Type type, Action<object> setValue, Func<object> getValue)
        {
            var val = (ScriptPack)getValue() ?? (ScriptPack)TypeAssistant.New(type);
            var root = new TitleAreaUI(nameof(AnyObjectField) + type.FullName + userData).SetTitle(Label, nameof(AnyObjectField) + type.FullName);
            root.AddToMenuBar(new ToolbarButton(() =>
            {
                TypeChooserWindow.EOption op = default;
                if ((Option.Flags & EOptionFlag.ObjectMustComment) != 0)
                    op |= TypeChooserWindow.EOption.MustComment;
                if (!val.BaseType.IsAbstract && !val.BaseType.IsInterface)
                    op |= TypeChooserWindow.EOption.ContainBaseType;
                TypeChooserWindow.Open(val.BaseType, val.Type, selectType =>
                {
                    val.Set(selectType == null ? null : (IBytesPackable)TypeAssistant.New(selectType));
                    setValue(val);
                    BindData.Dirty();
                }, options: op);
            }) { text = "选择脚本" });
            BindData = root.BindDataToUI(_ =>
            {
                root.Clear();
                if (val.Type != null)
                {
                    Option.Flags |= EOptionFlag.SkipRootObjectFold;
                    new AnyObjectField(null, val.Type, Option).BindDataWithUI(v =>
                    {
                        val.Set((IBytesPackable)v);
                        setValue(val);
                    }, () => val.Create()).AddToUI(root);
                }

                var label = CommentAttribute.TryGetLabel(val.Type, out var detail);
                root.Title = string.IsNullOrEmpty(Label) ? label : $"{Label}-{label}";
                root.TitleUI.tooltip = detail;
            });
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void DrawWorldFlags(Type type, Action<object> setValue, Func<object> getValue)
        {
            var val = (BitFlags)getValue();
            var bar = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
            bar.Add(new Label(Label) { style = { left = 2 } });
            bar.Add(new SelectBoxField() { name = "type" }.Set(null, val.Group,
                BitFlags.FlagGroupNames.Select((_, flagType) => new SelectBoxField.ItemData(flagType.ToString(), flagType)), true)
            );
            bar.Add(new SelectBoxField() { name = "mask" });

            bar.Q<SelectBoxField>("type").RegisterValueChangedCallback(v =>
            {
                val.Group = (byte)v.newValue;
                setValue(val);
                BindData.Dirty();
            });
            bar.Q<SelectBoxField>("mask").RegisterValueChangedCallback(v =>
            {
                val.Mask = (uint)v.newValue;
                setValue(val);
            });

            BindData = bar.BindDataToUI(_ =>
            {
                var selectBox = bar.Q<SelectBoxField>("mask");
                var names = BitFlags.FlagGroupNames.ElementAtOrDefault(val.Group);
                if (names != null)
                    selectBox.Set(null, (int)val.Mask, names.Select((v, i) => new SelectBoxField.ItemData(v, 1 << i)));
                else
                    selectBox.Set(null, Array.Empty<SelectBoxField.ItemData>());
            });
        }

        /// <summary>
        /// 
        /// </summary>
        private UIBindData CreateConfigIdField(AnyObjectField objUI, Type valueType, Action<object> setValue, Func<object> getValue)
        {
            var val = (IEditableConfigId)getValue();
            var root = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
            return root.BindDataToUI(_ =>
            {
                root.Clear();
                if (val.EditUseId)
                {
                    new LongField(objUI.Label) { style = { flexGrow = 1 } }.ShortFieldLabel().BindDataWithUI(id =>
                    {
                        val.Id = (uint)id;
                        setValue(val);
                    }, () => val.Id).AddToUI(root);
                    root.Add(new ToolbarButton(() => SetState(() => val.EditUseId = false)) { text = "R" });
                }
                else
                {
                    var ui = new ObjectField(objUI.Label) { objectType = typeof(ConfigItemEditorHelper), style = { flexGrow = 1 } }.ShortFieldLabel();
                    var label = ui.Q<Label>(classes: "unity-object-field-display__label");
                    label.style.display = DisplayStyle.None;
                    label.parent.Add(label = new Label());
                    ui.BindDataWithUIVerify(obj =>
                    {
                        if (obj != null && val.ConfigType.ToString() != ((ConfigItemEditorHelper)obj).ConfigType)
                            return false;
                        SetState(() =>
                        {
                            if (obj != null)
                            {
                                var guid = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(obj));
                                val.EditExternalGuid = Unsafe.As<GUID, Guid>(ref guid);
                                val.Id = ConfigItemEditor.GetAssetNameId(obj.name).ToUInt();
                                setValue(val);
                                label.text = obj.name;
                            }
                            else
                            {
                                val.EditExternalGuid = Guid.Empty;
                                val.Id = 0;
                                label.text = "空";
                                setValue(val);
                            }
                        });
                        return true;
                    }, () =>
                    {
                        var v = val.EditExternalGuid;
                        var obj = AssetDatabase.LoadAssetAtPath<ConfigItemEditorHelper>(AssetDatabase.GUIDToAssetPath(Unsafe.As<Guid, GUID>(ref v)));
                        label.text = obj?.name ?? "空";
                        return obj;
                    }).AddToUI(root);

                    root.Add(new ToolbarButton(() => SetState(() =>
                    {
                        val.EditUseId = true;
                        setValue(val);
                    })) { text = "V" });
                }
            });

            void SetState(Action action)
            {
                action();
                setValue(val);
                objUI.BindData.Dirty();
            }
        }

        /// <summary>
        /// 绘制字典
        /// </summary>
        /// <returns></returns>
        protected virtual UIBindData DrawDictionaryField(Type type, Action<object> setValue, Func<object> getValue)
        {
            var v = (getValue() ?? TypeAssistant.New(type)) as IDictionary;
            var ui = new IDictionaryAreaUI((_, e) =>
            {
                var root = new VisualElement();
                object key = null;
                if (e.userData != null)
                {
                    key = e.userData.GetType().GetProperty("Key")!.GetValue(e.userData);
                }

                var anyField = new AnyObjectField();
                anyField.Set("key", type.GetGenericArguments()[0], (o =>
                {
                    v![o] ??= TypeAssistant.New(type.GetGenericArguments()[1]);
                    e.userData = v;
                    key = o;
                    value = v;
                }), () => key, Option);
                root.Add(anyField);
                anyField = new AnyObjectField();
                anyField.Set("value", type.GetGenericArguments()[1], (obj) =>
                {
                    if (key == null)
                    {
                        Debug.LogError("请先填写Key");
                        return;
                    }

                    v![key] = obj;
                    value = v;
                }, () =>
                {
                    if (key != null && v!.Contains(key))
                    {
                        return v[key];
                    }

                    return null;
                }, Option);
                root.Add(anyField);
                return root;
            }, e =>
            {
                if (e.userData != null)
                {
                    v!.Remove(e.userData.GetType().GetProperty("Key")!.GetValue(e.userData));
                }

                value = v;
                return true;
            });
            foreach (var item in (IEnumerable)v!)
            {
                ui.CreateItem(false, item);
            }
            if ((Option.Flags & EOptionFlag.FoldObject) != 0)
                ui.value = false;
            return new UIBindData { UI = ui, Dirty = () => { } };
        }

        protected virtual UIBindData DrawObjField(Type type, Action<object> setValue, Func<object> getValue)
        {
            VisualElement root;
            if ((Option.Flags & EOptionFlag.SkipRootObjectFold) == 0)
            {
                var titleArea = new TitleAreaUI(type.FullName + userData).SetTitle(Label);
                root = titleArea;
                titleArea.TitleUI.RegisterCallback<ContextClickEvent>(_ =>
                {
                    var defaultField = type.GetField("Default", BindingFlags.Public | BindingFlags.Static);
                    if (defaultField != null)
                    {
                        var menu = new GenericMenu();
                        menu.AddItem(new GUIContent("设置默认值"), false, () =>
                        {
                            setValue?.Invoke(defaultField.GetValue(null));
                            BindData.Dirty();
                        });
                        menu.ShowAsContext();
                    }
                });
            }
            else
            {
                root = new VisualElement();
            }
            Option.Flags &= ~EOptionFlag.SkipRootObjectFold;
            var isSerializableType = type.IsDefined(typeof(SerializableAttribute));
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            return root.BindDataToUI(_ =>
            {
                root.Clear();
                var v = getValue() ?? TypeAssistant.New(type);
                foreach (var field in fields)
                {
                    if (field.IsDefined(typeof(NonSerializedAttribute)) ||
                        (!((Option.Flags & EOptionFlag.ObjectMustComment) == 0 || field.IsDefined(typeof(CommentAttribute))) && !isSerializableType))
                        continue;
                    var label = CommentAttribute.TryGetLabel(field, out var detail);
                    var anyField = new AnyObjectField() { tooltip = $"{field.Name}\n{detail}", userData = field };
                    anyField.Set(label, field.FieldType, obj =>
                    {
                        field.SetValue(v, obj);
                        setValue?.Invoke(v);
                    }, () => field.GetValue(v), Option);
                    root.Add(anyField);
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual UIBindData AddArrayField(Type type, Action<object> setValue, Func<object> getValue)
        {
            var v = getValue();
            var elType = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
            //if (v is not IList listBuffer)
            //{
            var length = 0;
            if (v is Array array)
            {
                length = array.Length;
            }
            else
            {
                if (v is IList list)
                {
                    length = list.Count;
                }
            }

            //var ar = (v as Array)?.Length(v as IList)?.Count;
            var p = v ?? Array.CreateInstance(elType!, length);
            var listBuffer = (IList)TypeAssistant.New(typeof(List<>).MakeGenericType(elType), p);
            //}

            Log.AssertNotNull(elType)?.Write(nameof(elType) + " != null");

            var ui = new ListAreaUI((isNew, container) =>
            {
                if (isNew)
                {
                    listBuffer.Add(elType.DefaultValue());
                    RefreshValue();
                }
                // return new AnyObjectField(string.Empty, elType.GetGenericArguments().Length > 0 ? elType.GetGenericArguments()[0] : elType, // 这里以前写法有问题，Obj<int>[] 会获取元素的int类型来创建而不是Obj<int>
                return new AnyObjectField(string.Empty, elType, listBuffer[container.Index], Option, container.Index).FlexGrow(1).BindDataWithUI(elVal =>
                {
                    listBuffer[container.Index] = elVal;
                    RefreshValue();
                }, () => listBuffer[container.Index]);
            }, container =>
            {
                listBuffer.RemoveAt(container.Index);
                RefreshValue();
                return true;
            }, autoFoldoutKey: nameof(AnyObjectField) + type.FullName + userData)
            {
                Title = Label, IsDisplayUp = true,
                OnSwapItem = (from, to) =>
                {
                    var temp = listBuffer[from];
                    listBuffer.RemoveAt(from);
                    listBuffer.Insert(to, temp);
                    RefreshValue();
                }
            }.AcceptDrag(elType);

            if (v != null)
            {
                foreach (var item in (IEnumerable)v)
                    ui.CreateItem(false, item);
            }
            if ((Option.Flags & EOptionFlag.FoldArray) != 0)
                ui.value = false;
            return new UIBindData { UI = ui, Dirty = () => { } };

            void RefreshValue()
            {
                var result = listBuffer;
                if (type.IsArray)
                {
                    var arr = (Array)(result = Array.CreateInstance(elType, listBuffer.Count));
                    for (var i = arr.Length - 1; i >= 0; i--)
                        arr.SetValue(listBuffer[i], i);
                }

                setValue(result);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void AddObjectField()
        {
        }
    }
}
