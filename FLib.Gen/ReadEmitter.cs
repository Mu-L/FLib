using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace FLib.Gen
{
    /// <summary>
    /// 生成字段的反序列化(Read)代码。
    /// 分派逻辑与 WriteEmitter 对称：VInt > CustomCode > List/Dict > BytesPack > Array > BytesSerializable > Relocate > 基础类型
    /// </summary>
    internal static class ReadEmitter
    {
        /// <summary>生成单个字段的读取代码</summary>
        public static void Emit(ITypeSymbol type, string field, StringBuilder sb, ref int uid, int options = 0)
        {
            if (CustomCodeEmitter.TryEmit(type, field, sb, ref uid, isRead: true))
                goto additional;

            if (TypeHelper.IsVInt(type) || (options & FieldOption.VInt) != 0)
            {
                sb.Append(field).Append(" = (").Append(type.Name).Append(")reader.ReadVInt();");
                goto additional;
            }

            ++uid;
            var isBytesSerializable = TypeHelper.IsBytesSerializable(type);

            if (TypeHelper.IsListOrDict(type, out var args, out var isList))
            {
                EmitCollection(type, field, sb, ref uid, args, isList);
            }
            else if (TypeHelper.IsBytesPack(type))
            {
                EmitBytesPack(type, field, sb);
            }
            else if (type is IArrayTypeSymbol arr && (!type.IsUnmanagedType || isBytesSerializable))
            {
                EmitArray(arr, field, sb, ref uid, isBytesSerializable);
            }
            else if (isBytesSerializable)
            {
                if (type.TypeKind != TypeKind.Array && TypeHelper.IsNullable(type))
                    sb.Append(field).Append(" ??= new(); ");
                sb.Append(field).Append(".Z_BytesRead(ref reader); ");
            }
            else if (type.SpecialType == SpecialType.System_String)
            {
                sb.Append(field).Append(" = reader.ReadString(); ");
            }
            else if (TypeHelper.IsRelocate(type, out var relocField))
            {
                Emit(relocField!.Type, field + "." + relocField.Name, sb, ref uid);
            }
            else
            {
                // unmanaged 基础类型直接 Read<T>
                sb.Append(field).Append(" = reader.Read<").Append(TypeHelper.ToTypeString(type, true)).Append(">(); ");
            }

            additional:
            EmitAdditionalCode(type, field, sb);
        }

        /// <summary>读取 List/Dictionary：先读 count，再逐元素反序列化</summary>
        private static void EmitCollection(ITypeSymbol type, string field, StringBuilder sb,
            ref int uid, ImmutableArray<ITypeSymbol> args, bool isList)
        {
            var countVar = "__count" + uid;
            var typeStr = TypeHelper.ToTypeString(type, true);

            sb.Append('\n');
            sb.Append("var ").Append(countVar).Append(" = reader.ReadLength();\n");
            // ??= new 保证非 null，Clear 复用已有容量
            sb.Append('(').Append(field).Append(" ??= new ").Append(typeStr).Append('(').Append(countVar).Append(")).Clear();\n");

            var iVar = "i" + uid;
            sb.Append("for (var ").Append(iVar).Append(" = 0; ").Append(iVar).Append(" < ").Append(countVar).Append("; ").Append(iVar).Append("++) {\n");

            if (isList)
            {
                var v = "v" + uid;
                EmitVarDef(sb, args[0], v);
                Emit(args[0], v, sb, ref uid);
                sb.Append(field).Append(".Add(").Append(v).Append(");\n");
            }
            else
            {
                var k = "k" + uid;
                var v = "v" + uid;
                sb.Append("var ");
                Emit(args[0], k, sb, ref uid);
                EmitVarDef(sb, args[1], v);
                Emit(args[1], v, sb, ref uid);
                sb.Append(field).Append(".Add(").Append(k).Append(", ").Append(v).Append(");\n");
            }

            sb.Append("}\n");
        }

        private static void EmitBytesPack(ITypeSymbol type, string field, StringBuilder sb)
        {
            if (type is IArrayTypeSymbol arr && TypeHelper.IsNullable(arr.ElementType))
            {
                sb.Append("BytesPack.UnpackNullableElement(ref ").Append(field).Append(", ref reader); ");
            }
            else
            {
                if (type.TypeKind != TypeKind.Array && TypeHelper.IsNullable(type))
                    sb.Append(field).Append(" ??= new(); ");
                sb.Append("BytesPack.Unpack(ref ").Append(field).Append(", ref reader); ");
            }
        }

        /// <summary>数组读取：string[] 有快捷方法，其余逐元素或整块读取</summary>
        private static void EmitArray(IArrayTypeSymbol arr, string field, StringBuilder sb,
            ref int uid, bool isBytesSerializable)
        {
            var elem = arr.ElementType;

            if (elem.SpecialType == SpecialType.System_String)
            {
                sb.Append(field).Append(" = reader.ReadStrings(); ");
            }
            else if (elem is IArrayTypeSymbol inner)
            {
                // 二维数组
                if (inner.ElementType.SpecialType == SpecialType.System_String)
                    sb.Append(field).Append(" = reader.ReadStrings2(); ");
                else
                    sb.Append(field).Append(" = reader.ReadArray2<").Append(TypeHelper.ToTypeString(inner.ElementType, true)).Append(">(); ");
            }
            else if (!elem.IsUnmanagedType || isBytesSerializable)
            {
                // 非 unmanaged 元素需要逐个读取
                var countVar = "__count" + uid;
                var iVar = "i" + uid;
                var elemStr = TypeHelper.ToTypeString(elem, true);
                sb.Append("var ").Append(countVar).Append(" = reader.ReadLength();\n");
                sb.Append(field).Append(" = new ").Append(elemStr).Append('[').Append(countVar).Append("];\n");
                sb.Append("for (var ").Append(iVar).Append(" = 0; ").Append(iVar).Append(" < ").Append(countVar).Append("; ").Append(iVar).Append("++) {");
                Emit(elem, field + "[" + iVar + "]", sb, ref uid);
                sb.Append("}\n");
            }
            else
            {
                // unmanaged 元素可以整块 memcpy 读取
                sb.Append(field).Append(" = reader.ReadArray<").Append(TypeHelper.ToTypeString(elem, true)).Append(">(); ");
            }
        }

        /// <summary>生成临时变量定义：值类型用 default，引用类型用 new()</summary>
        private static void EmitVarDef(StringBuilder sb, ITypeSymbol type, string varName)
        {
            var typeStr = TypeHelper.ToTypeString(type, true);
            if (type.IsValueType || type.SpecialType == SpecialType.System_String || type.TypeKind == TypeKind.Array)
                sb.Append(typeStr).Append(' ').Append(varName).Append(" = default; ");
            else
                sb.Append("var ").Append(varName).Append(" = new ").Append(typeStr).Append("(); ");
        }

        private static void EmitAdditionalCode(ITypeSymbol type, string field, StringBuilder sb)
        {
            var attr = TypeHelper.FindAdditionalCodeAttr(type);
            if (attr == null) return;
            foreach (var named in attr.NamedArguments)
            {
                if (named.Key == "ReadCode" && named.Value.Value is string code)
                {
                    sb.Append(TypeHelper.ReplaceTemplate(code, type, field)).Append(' ');
                    return;
                }
            }
        }
    }
}
