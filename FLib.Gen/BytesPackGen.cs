using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FLib.Gen
{
    /// <summary>
    /// 为标记了 [BytesPackGen] 的类型生成 IBytesPackable 的序列化/反序列化实现。
    /// 生成的代码通过 key 标识每个字段，支持继承链和版本兼容。
    /// </summary>
    [Generator]
    public class BytesPackGen : ISourceGenerator
    {
        private const string WriteMethodName = "Z_BytesPackWrite";
        private const string ReadMethodName = "Z_BytesPackRead";

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not SyntaxReceiver receiver || receiver.CandidateTypes.Count == 0)
                return;

            var targets = CollectTargets(context.Compilation, receiver.CandidateTypes);
            if (targets.Count == 0)
                return;

            var sb = new StringBuilder(1024);
            foreach (var target in targets)
            {
                EmitSource(sb, target.Key, target.Value);
                context.AddSource(GetFileName(target.Key) + ".g.cs", sb.ToString());
                sb.Clear();
            }
        }

        /// <summary>
        /// 从候选语法节点中筛选出真正标记了 [BytesPackGen] 的类型，收集其字段信息。
        /// partial class 的多个声明会解析为同一个 symbol，用 ContainsKey 去重。
        /// </summary>
        private static Dictionary<INamedTypeSymbol, MemberInfo[]> CollectTargets(
            Compilation compilation, List<TypeDeclarationSyntax> candidates)
        {
            var targets = new Dictionary<INamedTypeSymbol, MemberInfo[]>(SymbolEqualityComparer.Default);
            var semanticModels = new Dictionary<SyntaxTree, SemanticModel>();

            foreach (var candidate in candidates)
            {
                var model = GetSemanticModel(compilation, candidate.SyntaxTree, semanticModels);
                if (model.GetDeclaredSymbol(candidate) is not INamedTypeSymbol symbol)
                    continue;
                if (targets.ContainsKey(symbol))
                    continue;
                if (!TypeHelper.HasBytesPack(symbol))
                    continue;

                targets.Add(symbol, CollectMembers(compilation, symbol, semanticModels));
            }

            return targets;
        }

        /// <summary>
        /// 收集类型中标记了 [BytesPackGenField] 的字段/属性，解析其 key 值。
        /// key 可以显式指定，否则自动递增。
        /// </summary>
        private static MemberInfo[] CollectMembers(
            Compilation compilation,
            INamedTypeSymbol symbol,
            Dictionary<SyntaxTree, SemanticModel> semanticModels)
        {
            var allMembers = symbol.GetMembers();
            var result = new List<MemberInfo>(allMembers.Length);
            var keyOffset = 0;

            foreach (var member in allMembers)
            {
                if (member is not (IFieldSymbol or IPropertySymbol)) continue;
                var attr = TypeHelper.GetFieldAttr(member);
                if (attr == null) continue;

                keyOffset = TypeHelper.GetKeyFromAttr(attr, keyOffset + 1);
                var options = TypeHelper.GetOptionsFromAttr(attr);
                var type = (member as IFieldSymbol)?.Type ?? ((IPropertySymbol)member).Type;
                var defaultValue = GetExplicitDefaultValue(compilation, member, type, semanticModels);
                result.Add(new MemberInfo(member.Name, type, keyOffset, options, defaultValue));
            }

            return result.ToArray();
        }

        private static string? GetExplicitDefaultValue(
            Compilation compilation,
            ISymbol member,
            ITypeSymbol type,
            Dictionary<SyntaxTree, SemanticModel> semanticModels)
        {
            foreach (var syntaxRef in member.DeclaringSyntaxReferences)
            {
                var syntax = syntaxRef.GetSyntax();
                var value = syntax switch
                {
                    VariableDeclaratorSyntax v => v.Initializer?.Value,
                    PropertyDeclarationSyntax p => p.Initializer?.Value,
                    _ => null
                };
                if (value == null) continue;

                var model = GetSemanticModel(compilation, value.SyntaxTree, semanticModels);
                if (type.TypeKind == TypeKind.Enum &&
                    !TypeHelper.IsFlagsEnum(type) &&
                    TryFormatSimpleEnumMemberDefault(model, value, type, out var enumLiteral))
                {
                    return enumLiteral;
                }

                var constant = model.GetConstantValue(value);
                if (constant.HasValue && TryFormatConstantValue(constant.Value, type, out var literal))
                    return literal;

                return value.ToString();
            }

            return null;
        }

        private static SemanticModel GetSemanticModel(
            Compilation compilation,
            SyntaxTree syntaxTree,
            Dictionary<SyntaxTree, SemanticModel> cache)
        {
            if (!cache.TryGetValue(syntaxTree, out var model))
            {
                model = compilation.GetSemanticModel(syntaxTree);
                cache.Add(syntaxTree, model);
            }

            return model;
        }

        private static bool TryFormatSimpleEnumMemberDefault(
            SemanticModel model,
            ExpressionSyntax value,
            ITypeSymbol type,
            out string literal)
        {
            literal = "";
            if (model.GetSymbolInfo(value).Symbol is not IFieldSymbol field ||
                field.ContainingType?.TypeKind != TypeKind.Enum ||
                !SymbolEqualityComparer.Default.Equals(field.ContainingType, type))
            {
                return false;
            }

            literal = field.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) +
                      "." + field.Name;
            return true;
        }

        private static bool TryFormatConstantValue(object? value, ITypeSymbol type, out string literal)
        {
            if (value == null)
            {
                literal = "null";
                return true;
            }

            if (type.TypeKind == TypeKind.Enum)
            {
                literal = "(" + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ")" +
                          FormatIntegralConstant(value);
                return true;
            }

            return TryFormatPrimitiveConstant(value, type.SpecialType, out literal);
        }

        private static bool TryFormatPrimitiveConstant(object value, SpecialType special, out string literal)
        {
            switch (special)
            {
                case SpecialType.System_Boolean:
                    literal = Convert.ToBoolean(value, CultureInfo.InvariantCulture) ? "true" : "false";
                    return true;
                case SpecialType.System_Char:
                    literal = "'" + EscapeChar(Convert.ToChar(value, CultureInfo.InvariantCulture), false) + "'";
                    return true;
                case SpecialType.System_String:
                    literal = "\"" + EscapeString((string)value) + "\"";
                    return true;
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                    literal = Convert.ToString(value, CultureInfo.InvariantCulture)!;
                    return true;
                case SpecialType.System_UInt32:
                    literal = Convert.ToString(value, CultureInfo.InvariantCulture)! + "u";
                    return true;
                case SpecialType.System_Int64:
                    literal = Convert.ToString(value, CultureInfo.InvariantCulture)! + "L";
                    return true;
                case SpecialType.System_UInt64:
                    literal = Convert.ToString(value, CultureInfo.InvariantCulture)! + "UL";
                    return true;
                case SpecialType.System_Single:
                    literal = FormatSingleConstant(Convert.ToSingle(value, CultureInfo.InvariantCulture));
                    return true;
                case SpecialType.System_Double:
                    literal = FormatDoubleConstant(Convert.ToDouble(value, CultureInfo.InvariantCulture));
                    return true;
                case SpecialType.System_Decimal:
                    literal = Convert.ToDecimal(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture) + "m";
                    return true;
                default:
                    literal = "";
                    return false;
            }
        }

        private static string FormatIntegralConstant(object value)
        {
            switch (value)
            {
                case uint v:
                    return v.ToString(CultureInfo.InvariantCulture) + "u";
                case ulong v:
                    return v.ToString(CultureInfo.InvariantCulture) + "UL";
                case long v:
                    return v.ToString(CultureInfo.InvariantCulture) + "L";
                default:
                    return Convert.ToString(value, CultureInfo.InvariantCulture)!;
            }
        }

        private static string FormatSingleConstant(float value)
        {
            if (float.IsNaN(value)) return "float.NaN";
            if (float.IsPositiveInfinity(value)) return "float.PositiveInfinity";
            if (float.IsNegativeInfinity(value)) return "float.NegativeInfinity";
            return value.ToString("R", CultureInfo.InvariantCulture) + "f";
        }

        private static string FormatDoubleConstant(double value)
        {
            if (double.IsNaN(value)) return "double.NaN";
            if (double.IsPositiveInfinity(value)) return "double.PositiveInfinity";
            if (double.IsNegativeInfinity(value)) return "double.NegativeInfinity";
            return value.ToString("R", CultureInfo.InvariantCulture) + "d";
        }

        private static string EscapeString(string value)
        {
            var sb = new StringBuilder(value.Length);
            foreach (var c in value)
                sb.Append(EscapeChar(c, true));
            return sb.ToString();
        }

        private static string EscapeChar(char value, bool inString)
        {
            switch (value)
            {
                case '\0': return "\\0";
                case '\a': return "\\a";
                case '\b': return "\\b";
                case '\f': return "\\f";
                case '\n': return "\\n";
                case '\r': return "\\r";
                case '\t': return "\\t";
                case '\v': return "\\v";
                case '\\': return "\\\\";
                case '"': return inString ? "\\\"" : "\"";
                case '\'': return inString ? "'" : "\\'";
                default:
                    return value < ' ' || value > '~'
                        ? "\\u" + ((int)value).ToString("x4", CultureInfo.InvariantCulture)
                        : value.ToString();
            }
        }

        /// <summary>嵌套类型的文件名用 "Parent.Child" 格式</summary>
        private static string GetFileName(INamedTypeSymbol symbol)
        {
            var name = symbol.Name;
            var parent = symbol.ContainingType;
            while (parent != null)
            {
                name = parent.Name + "." + name;
                parent = parent.ContainingType;
            }
            return name;
        }

        private static void ApplyKeyOffset(MemberInfo[] members, int keyOffset)
        {
            if (keyOffset == 0)
                return;

            for (var i = 0; i < members.Length; i++)
            {
                members[i] = new MemberInfo(
                    members[i].Name,
                    members[i].Type,
                    members[i].Key + keyOffset,
                    members[i].Options,
                    members[i].DefaultValue);
            }
        }

        /// <summary>
        /// 判断是否需要生成 base 调用。
        /// 同一轮 source generator 看不到自己给父类生成的方法，所以“父类没有显式方法”时仍要生成 base 调用。
        /// 只有父类源码里已经声明了同名 abstract 方法时，才跳过调用。
        /// </summary>
        private static bool ShouldCallBasePackMethod(INamedTypeSymbol? baseType, string methodName)
        {
            if (baseType == null)
                return false;

            var foundConcreteMethod = false;
            var foundAbstractMethod = false;

            foreach (var member in baseType.GetMembers(methodName))
            {
                if (member is not IMethodSymbol method)
                    continue;

                if (method.IsAbstract)
                    foundAbstractMethod = true;
                else
                    foundConcreteMethod = true;
            }

            return foundConcreteMethod || !foundAbstractMethod;
        }

        /// <summary>
        /// 生成一个类型的完整源码：namespace、嵌套父类、类型定义、Write/Read 方法。
        /// </summary>
        private static void EmitSource(StringBuilder sb, INamedTypeSymbol symbol, MemberInfo[] members)
        {
            sb.Append("using System;\nusing System.Collections;\nusing System.Collections.Generic;\nusing FLib;\n\n");

            var braceCount = 1;
            var ns = symbol.ContainingNamespace;
            if (!ns.IsGlobalNamespace)
            {
                sb.Append("namespace ").Append(ns).Append(" {\n");
                ++braceCount;
            }

            // 嵌套类型需要逐层写出父类定义
            var parent = symbol.ContainingType;
            while (parent != null)
            {
                ++braceCount;
                AppendTypeDef(sb, parent).Append(" {\n");
                parent = parent.ContainingType;
            }

            AppendTypeDef(sb, symbol).Append(" : IBytesPackable {\n");

            // 子类的 key 需要在父类最大 key 基础上偏移，避免冲突
            var parentKey = TypeHelper.GetAllParentKeyValue(symbol.BaseType);
            var hasParent = TypeHelper.IsBytesPack(symbol.BaseType);

            var mod = "";
            if (symbol.TypeKind == TypeKind.Class)
                mod = hasParent ? " override" : " virtual";

            // 原地偏移避免额外数组分配；targets 中的 MemberInfo[] 每个类型只会生成一次。
            ApplyKeyOffset(members, parentKey);

            EmitWrite(sb, symbol, members, mod, hasParent);
            EmitRead(sb, symbol, members, mod, hasParent);

            sb.Append('}', braceCount);
        }

        /// <summary>
        /// 生成 Z_BytesPackWrite：逐字段写入 key + value，nullable/default 值跳过写入以节省空间。
        /// </summary>
        private static void EmitWrite(StringBuilder sb, INamedTypeSymbol symbol, MemberInfo[] members,
            string mod, bool hasParent)
        {
            sb.Append("public").Append(mod).Append(" void Z_BytesPackWrite(ref BytesPack.KeyHelper key, ref BytesWriter writer) {\n");

            if (hasParent && ShouldCallBasePackMethod(symbol.BaseType, WriteMethodName))
                sb.Append("base.Z_BytesPackWrite(ref key, ref writer);\n");

            var uid = 0;
            foreach (var m in members)
            {
                var disableTrim = m.HasOption(FieldOption.DisableTrim);
                var check = !disableTrim && WriteEmitter.EmitNullCheck(sb, m.Name, m.Type, true, m.DefaultValue);
                sb.Append("key.Push(ref writer, ").Append(m.Key).Append("); ");
                WriteEmitter.Emit(m.Type, m.Name, sb, ref uid, m.Options);
                AppendLineBreakIfNeeded(sb);
                if (check) sb.Append("}\n");
                ++uid;
            }

            sb.Append("}\n");
        }

        private static void AppendLineBreakIfNeeded(StringBuilder sb)
        {
            if (sb.Length == 0 || sb[sb.Length - 1] == '\n')
                return;
            sb.Append('\n');
        }

        /// <summary>
        /// 生成 Z_BytesPackRead：通过 switch(key) 分发到对应字段的反序列化逻辑。
        /// </summary>
        private static void EmitRead(StringBuilder sb, INamedTypeSymbol symbol, MemberInfo[] members,
            string mod, bool hasParent)
        {
            sb.Append("public").Append(mod).Append(" void Z_BytesPackRead(int key, ref BytesReader reader) {\n");

            var canCallBase = hasParent && ShouldCallBasePackMethod(symbol.BaseType, ReadMethodName);

            if (members.Length > 0)
            {
                sb.Append("switch (key) {\n");
                var uid = 0;
                foreach (var m in members)
                {
                    sb.Append("case ").Append(m.Key).Append(": ");
                    ReadEmitter.Emit(m.Type, m.Name, sb, ref uid, m.Options);
                    sb.Append("break;\n");
                    ++uid;
                }
                if (canCallBase)
                    sb.Append("default: base.Z_BytesPackRead(key, ref reader); break;\n");
                sb.Append('}', 2);
            }
            else
            {
                if (canCallBase)
                    sb.Append("base.Z_BytesPackRead(key, ref reader);\n");
                sb.Append('}');
            }
        }

        private static StringBuilder AppendTypeDef(StringBuilder sb, INamedTypeSymbol symbol)
        {
            var kind = symbol.TypeKind == TypeKind.Class ? "class" : "struct";
            sb.Append(symbol.DeclaredAccessibility.ToString().ToLowerInvariant())
              .Append(" partial ").Append(kind).Append(' ')
              .Append(symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            return sb;
        }

        public static void Log(in GeneratorExecutionContext context, object msg,
            DiagnosticSeverity severity = DiagnosticSeverity.Warning)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                "BPG", "BytesPack.Gen", msg.ToString(), severity, severity, true,
                severity == DiagnosticSeverity.Warning ? 1 : 0));
        }

        /// <summary>粗筛：收集所有带 Attribute 的类型声明，后续再精确判断</summary>
        private class SyntaxReceiver : ISyntaxReceiver
        {
            public readonly List<TypeDeclarationSyntax> CandidateTypes = new List<TypeDeclarationSyntax>();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is TypeDeclarationSyntax t && t.AttributeLists.Count > 0)
                    CandidateTypes.Add(t);
            }
        }
    }

    /// <summary>一个待序列化字段的信息：名称、类型、协议 key、选项</summary>
    internal readonly struct MemberInfo
    {
        public readonly string Name;
        public readonly ITypeSymbol Type;
        public readonly int Key;
        public readonly int Options;
        public readonly string? DefaultValue;

        public MemberInfo(string name, ITypeSymbol type, int key, int options = 0, string? defaultValue = null)
        {
            Name = name;
            Type = type;
            Key = key;
            Options = options;
            DefaultValue = defaultValue;
        }

        public bool HasOption(int flag) => (Options & flag) != 0;
    }

    /// <summary>对应 EBytePackGenFieldOption 枚举值</summary>
    internal static class FieldOption
    {
        public const int DisableTrim = 0x1;
        public const int VInt = 0x2;
    }
}
