using System;
using System.Collections;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace FLib.Gen
{
    /// <summary>
    /// 类型检查与 Attribute 查找工具集。
    /// 所有 Attribute 名称用 const 避免重复分配，查找逻辑用 foreach 替代 LINQ 减少闭包开销。
    /// </summary>
    internal static class TypeHelper
    {
        private const string AttrBytesPack = "BytesPackGenAttribute";
        private const string AttrField = "BytesPackGenFieldAttribute";
        private const string AttrHoldKey = "BytesPackGenHoldKeyAttribute";
        private const string AttrRelocate = "BytesPackGenRelocateAttribute";
        private const string AttrAdditionalCode = "BytesPackGenAdditionalCodeAttribute";
        private const string AttrCustomCode = "BytesPackGenCustomCodeAttribute";
        private const string IFaceBytesPackable = "IBytesPackable";
        private const string IFaceBytesSerializable = "IBytesSerializable";

        public static bool HasBytesPack(ITypeSymbol typeSymbol)
        {
            foreach (var attr in typeSymbol.GetAttributes())
                if (attr.AttributeClass?.Name == AttrBytesPack)
                    return true;
            return false;
        }

        public static bool HasFieldAttr(ISymbol member)
        {
            foreach (var attr in member.GetAttributes())
                if (attr.AttributeClass?.Name == AttrField)
                    return true;
            return false;
        }

        public static AttributeData? GetFieldAttr(ISymbol member)
        {
            foreach (var attr in member.GetAttributes())
                if (attr.AttributeClass?.Name == AttrField)
                    return attr;
            return null;
        }

        /// <summary>从 [BytesPackGenField(key)] 或 [BytesPackGenField(Key = key)] 取 key，取不到则用 fallback</summary>
        public static int GetKeyFromAttr(AttributeData attr, int fallback)
        {
            if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is int ctorKey)
                return ctorKey;
            foreach (var named in attr.NamedArguments)
                if (named.Key == "Key" && named.Value.Value is int namedKey)
                    return namedKey;
            return fallback;
        }

        /// <summary>读取 Options 字段的枚举值（对应 EBytePackGenFieldOption）</summary>
        public static int GetOptionsFromAttr(AttributeData attr)
        {
            foreach (var named in attr.NamedArguments)
                if (named.Key == "Options" && named.Value.Value is int options)
                    return options;
            return 0;
        }

        /// <summary>char ~ uint 范围的整数类型用变长编码(VInt)写入</summary>
        public static bool IsVInt(ITypeSymbol t)
        {
            var s = t.SpecialType;
            return s >= SpecialType.System_Char && s <= SpecialType.System_UInt32;
        }

        public static bool IsFNum(ITypeSymbol t)
        {
            return t.Name == "FNum" && t.ContainingNamespace.ToDisplayString() == "FLib";
        }

        public static bool IsFlagsEnum(ITypeSymbol t)
        {
            foreach (var attr in t.GetAttributes())
                if (attr.AttributeClass?.Name == nameof(FlagsAttribute))
                    return true;
            return false;
        }

        /// <summary>判断类型是否可能为 null（包括引用类型和显式 nullable 标注）</summary>
        public static bool IsNullable(ITypeSymbol t)
        {
            return t.NullableAnnotation == NullableAnnotation.Annotated ||
                   (t.NullableAnnotation == NullableAnnotation.None && t.TypeKind is TypeKind.Class or TypeKind.Array);
        }

        /// <summary>判断类型（含数组元素，最多解4层）是否实现了 IBytesPackable 或标记了 [BytesPackGen]</summary>
        public static bool IsBytesPack(ITypeSymbol? t)
        {
            if (t == null) return false;
            for (var i = 0; i < 4; i++)
            {
                if (t is IArrayTypeSymbol arr) t = arr.ElementType;
                else break;
            }
            while (t != null)
            {
                if (HasInterface(t, IFaceBytesPackable) || HasBytesPack(t))
                    return true;
                t = t.BaseType;
            }
            return false;
        }

        /// <summary>判断类型是否实现了 IBytesSerializable（自定义二进制读写接口）</summary>
        public static bool IsBytesSerializable(ITypeSymbol t)
        {
            for (var i = 0; i < 4; i++)
            {
                if (t is IArrayTypeSymbol arr) t = arr.ElementType;
                else break;
            }
            return HasInterface(t, IFaceBytesSerializable);
        }

        /// <summary>
        /// 判断是否为 List 或 Dictionary。
        /// 注意：Dictionary 也实现了 IEnumerable，所以遇到 IDictionary 立即返回，避免误判为 List。
        /// </summary>
        public static bool IsListOrDict(ITypeSymbol t, out ImmutableArray<ITypeSymbol> args, out bool isList)
        {
            isList = false;
            args = default;
            if (t.IsUnmanagedType) return false;
            if (t is not INamedTypeSymbol named || !named.IsGenericType) return false;

            args = named.TypeArguments;
            foreach (var iface in named.Interfaces)
            {
                switch (iface.Name)
                {
                    case nameof(IDictionary): isList = false; return true;
                    case nameof(IEnumerable): isList = true; break;
                }
            }
            return isList;
        }

        /// <summary>[BytesPackGenRelocate("fieldName")] 将序列化重定向到指定内部字段</summary>
        public static bool IsRelocate(ITypeSymbol t, out IFieldSymbol? field)
        {
            field = null;
            foreach (var attr in t.GetAttributes())
            {
                if (attr.AttributeClass?.Name != AttrRelocate) continue;
                var name = attr.ConstructorArguments[0].Value!.ToString();
                field = t.GetMembers(name).OfType<IFieldSymbol>().SingleOrDefault();
                if (field == null)
                    throw new Exception("relocate field not found: " + t.Name + "." + name);
                return true;
            }
            return false;
        }

        /// <summary>沿继承链查找指定名称的 Attribute</summary>
        public static AttributeData? FindAttr(ITypeSymbol? t, string name, bool recursive = true)
        {
            while (t != null)
            {
                foreach (var attr in t.GetAttributes())
                    if (attr.AttributeClass?.Name == name)
                        return attr;
                if (!recursive) break;
                t = t.BaseType;
            }
            return null;
        }

        public static AttributeData? FindAdditionalCodeAttr(ITypeSymbol t)
        {
            return FindAttr(t, AttrAdditionalCode);
        }

        public static bool HasCustomCodeAttr(ITypeSymbol t)
        {
            foreach (var attr in t.GetAttributes())
                if (attr.AttributeClass?.Name == AttrCustomCode)
                    return true;
            return false;
        }

        public static AttributeData? GetCustomCodeAttr(ISymbol member)
        {
            foreach (var attr in member.GetAttributes())
                if (attr.AttributeClass?.Name == AttrCustomCode)
                    return attr;
            return null;
        }

        /// <summary>
        /// 计算所有父类的最大 key 值之和。子类的 key 在此基础上偏移，保证继承链中 key 不冲突。
        /// [BytesPackGenHoldKey(n)] 可以预留 key 空间给未来扩展。
        /// </summary>
        public static int GetAllParentKeyValue(INamedTypeSymbol? parent)
        {
            var total = 0;
            while (parent != null)
            {
                var fieldKey = 0;
                foreach (var member in parent.GetMembers())
                {
                    var attr = GetFieldAttr(member);
                    if (attr != null)
                        fieldKey = GetKeyFromAttr(attr, fieldKey + 1);
                }

                var holdAttr = FindAttr(parent, AttrHoldKey, false);
                if (holdAttr != null)
                {
                    var holdKey = (holdAttr.ConstructorArguments[0].Value as int?).GetValueOrDefault();
                    if (fieldKey < holdKey) fieldKey = holdKey;
                }

                total += fieldKey;
                parent = parent.BaseType;
            }
            return total;
        }

        /// <summary>获取类型的字符串表示，trimNullable 时去掉 ? 和 Nullable 包装</summary>
        public static string ToTypeString(ITypeSymbol t, bool trimNullable)
        {
            if (trimNullable)
            {
                if (t.NullableAnnotation == NullableAnnotation.Annotated)
                    t = t.WithNullableAnnotation(NullableAnnotation.None);
                if (t.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                    t = ((INamedTypeSymbol)t).TypeArguments[0];
            }
            return t.ToString();
        }

        /// <summary>替换模板中的 ${FieldName} 和 ${FieldType} 占位符</summary>
        public static string ReplaceTemplate(string s, ITypeSymbol type, string fieldName)
        {
            return s.Replace("${FieldName}", fieldName).Replace("${FieldType}", type.ToString());
        }

        private static bool HasInterface(ITypeSymbol t, string name)
        {
            foreach (var iface in t.AllInterfaces)
                if (iface.Name == name)
                    return true;
            return false;
        }
    }
}
