using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace FLib.Gen
{
    /// <summary>
    /// 处理 [BytesPackGenCustomCode] 标记的类型。
    /// 这类类型的序列化逻辑完全由 Attribute 中的 ReadCode/WriteCode 字符串模板控制，
    /// 模板中可用 ${FieldName}、${FieldType}、${Gen} 占位符。
    /// </summary>
    internal static class CustomCodeEmitter
    {
        /// <summary>
        /// 尝试为类型生成自定义序列化代码。沿继承链向上查找 [BytesPackGenCustomCode]。
        /// 返回 true 表示该类型由自定义代码接管，调用方不再走默认分派。
        /// </summary>
        public static bool TryEmit(ITypeSymbol? type, string field, StringBuilder sb, ref int uid, bool isRead)
        {
            var found = false;
            while (type != null)
            {
                if (!TypeHelper.HasCustomCodeAttr(type))
                {
                    type = type.BaseType;
                    continue;
                }

                foreach (var member in type.GetMembers())
                {
                    if (member is not IFieldSymbol customField) continue;

                    if (!found)
                    {
                        if (isRead && TypeHelper.IsNullable(type))
                            sb.Append(field).Append(" ??= new(); ");
                        found = true;
                    }

                    var attr = TypeHelper.GetCustomCodeAttr(customField);
                    if (attr == null) continue;

                    var codeKey = isRead ? "ReadCode" : "WriteCode";
                    string? code = null;
                    foreach (var named in attr.NamedArguments)
                        if (named.Key == codeKey)
                            code = named.Value.Value?.ToString();

                    if (string.IsNullOrEmpty(code)) continue;

                    code = TypeHelper.ReplaceTemplate(code!, type, field);

                    // ${Gen} 占位符：自动生成该字段的标准读写代码，外层包裹 bool 标记（用于 nullable 字段的存在性判断）
                    if (code.Contains("${Gen}"))
                        code = code.Replace("${Gen}", GenInlineCode(customField, field, ref uid, isRead));

                    sb.Append(code).Append(' ');
                }

                type = type.BaseType;
            }
            return found;
        }

        /// <summary>生成 ${Gen} 展开后的代码：写入时先 null 检查再 Push(true)+值，读取时 if(Read bool) 再读值</summary>
        private static string GenInlineCode(IFieldSymbol field, string parentField, ref int uid, bool isRead)
        {
            var fullField = parentField + "." + field.Name;
            var buf = new StringBuilder();

            if (isRead)
            {
                buf.Append("if (reader.Read<bool>()){");
                ReadEmitter.Emit(field.Type, fullField, buf, ref uid);
                buf.Append('}');
            }
            else
            {
                var hasCheck = WriteEmitter.EmitNullCheck(buf, fullField, field.Type, true);
                buf.Append("writer.Push(true);");
                WriteEmitter.Emit(field.Type, fullField, buf, ref uid);
                if (hasCheck)
                    buf.Append("} else writer.Push(false);\n");
            }

            return buf.ToString();
        }
    }
}
