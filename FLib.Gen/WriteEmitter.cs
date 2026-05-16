using System.Text;
using Microsoft.CodeAnalysis;

namespace FLib.Gen
{
    /// <summary>
    /// 生成字段的序列化(Write)代码。
    /// 按类型分派：VInt > CustomCode > List/Dict > BytesPack > Array > BytesSerializable > Relocate > 基础类型
    /// </summary>
    internal static class WriteEmitter
    {
        /// <summary>生成单个字段的写入代码，uid 用于生成不重名的临时变量</summary>
        public static void Emit(ITypeSymbol type, string field, StringBuilder sb, ref int uid)
        {
            if (CustomCodeEmitter.TryEmit(type, field, sb, ref uid, isRead: false))
                goto additional;

            if (TypeHelper.IsVInt(type))
            {
                sb.Append("writer.PushVInt(").Append(field).Append(");");
                goto additional;
            }

            ++uid;

            if (TypeHelper.IsListOrDict(type, out var args, out var isList))
            {
                EmitCollection(field, sb, ref uid, args, isList);
            }
            else if (TypeHelper.IsBytesPack(type))
            {
                EmitBytesPack(type, field, sb);
            }
            else if (type is IArrayTypeSymbol arr && (!arr.ElementType.IsUnmanagedType || TypeHelper.IsBytesSerializable(type)))
            {
                EmitManagedArray(arr, field, sb, ref uid);
            }
            else if (TypeHelper.IsBytesSerializable(type))
            {
                if (type.TypeKind != TypeKind.Array && TypeHelper.IsNullable(type))
                    sb.Append(field).Append(" ??= new(); ");
                sb.Append(field).Append(".Z_BytesWrite(ref writer); ");
            }
            else if (type.SpecialType == SpecialType.System_Nullable_T ||
                     (type.TypeKind > TypeKind.Enum && type.NullableAnnotation == NullableAnnotation.Annotated))
            {
                // Nullable<T> 取 .Value 写入
                sb.Append("writer.Push(").Append(field).Append(".Value);");
            }
            else if (TypeHelper.IsRelocate(type, out var relocField))
            {
                // [BytesPackGenRelocate] 重定向到内部字段
                Emit(relocField!.Type, field + "." + relocField.Name, sb, ref uid);
            }
            else
            {
                sb.Append("writer.Push(").Append(field).Append(");");
            }

            additional:
            EmitAdditionalCode(type, field, sb);
        }

        /// <summary>
        /// 生成 null/default 检查的 if 语句头。返回 true 表示生成了 if，调用方需要补 "}"。
        /// 跳过 null/default 值的写入可以节省带宽。
        /// </summary>
        public static bool EmitNullCheck(StringBuilder sb, string field, ITypeSymbol type, bool ignoreDefault)
        {
            var special = type.SpecialType;
            var nullable = TypeHelper.IsNullable(type);
            var numeric = special >= SpecialType.System_Boolean && special <= SpecialType.System_Double;

            if (!nullable && !numeric) return false;

            var isStr = special == SpecialType.System_String;
            if (!ignoreDefault && (numeric || isStr)) return false;

            sb.Append("if (");
            if (isStr)
                sb.Append("!string.IsNullOrEmpty(").Append(field).Append(")");
            else if (nullable)
                sb.Append(field).Append(" != null");
            else
                sb.Append(field).Append(" != default");
            sb.Append(") {\n");
            return true;
        }

        private static void EmitCollection(string field, StringBuilder sb, ref int uid,
            System.Collections.Immutable.ImmutableArray<ITypeSymbol> args, bool isList)
        {
            sb.Append('\n');
            sb.Append("writer.PushLength(").Append(field).Append(".Count);\n");
            var item = "item" + uid;
            sb.Append("foreach (var ").Append(item).Append(" in ").Append(field).Append(") {\n");
            if (isList)
            {
                Emit(args[0], item, sb, ref uid);
            }
            else
            {
                Emit(args[0], item + ".Key", sb, ref uid);
                Emit(args[1], item + ".Value", sb, ref uid);
            }
            sb.Append("}\n");
        }

        private static void EmitBytesPack(ITypeSymbol type, string field, StringBuilder sb)
        {
            if (type is IArrayTypeSymbol arr && TypeHelper.IsNullable(arr.ElementType))
                sb.Append("BytesPack.PackNullableElement(").Append(field).Append(", ref writer);\n");
            else
                sb.Append("BytesPack.Pack(").Append(field).Append(", ref writer);\n");
        }

        /// <summary>非 unmanaged 的数组需要逐元素写入</summary>
        private static void EmitManagedArray(IArrayTypeSymbol arr, string field, StringBuilder sb, ref int uid)
        {
            var iVar = "i" + uid;
            sb.Append('\n');
            sb.Append("writer.PushLength(").Append(field).Append(".Length);\n");
            sb.Append("for (var ").Append(iVar).Append(" = 0; ").Append(iVar).Append(" < ").Append(field).Append(".Length; ").Append(iVar).Append("++) {");
            Emit(arr.ElementType, field + "[" + iVar + "]", sb, ref uid);
            sb.Append("}\n");
        }

        /// <summary>追加 [BytesPackGenAdditionalCode(WriteCode = "...")] 中的自定义代码</summary>
        private static void EmitAdditionalCode(ITypeSymbol type, string field, StringBuilder sb)
        {
            var attr = TypeHelper.FindAdditionalCodeAttr(type);
            if (attr == null) return;
            foreach (var named in attr.NamedArguments)
            {
                if (named.Key == "WriteCode" && named.Value.Value is string code)
                {
                    sb.Append(TypeHelper.ReplaceTemplate(code, type, field)).Append(' ');
                    return;
                }
            }
        }
    }
}
