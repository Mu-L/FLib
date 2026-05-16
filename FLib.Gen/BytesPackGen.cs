using System.Collections.Generic;
using System.Linq;
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

            foreach (var candidate in candidates)
            {
                var model = compilation.GetSemanticModel(candidate.SyntaxTree);
                if (model.GetDeclaredSymbol(candidate) is not INamedTypeSymbol symbol)
                    continue;
                if (targets.ContainsKey(symbol))
                    continue;
                if (!TypeHelper.HasBytesPack(symbol))
                    continue;

                targets.Add(symbol, CollectMembers(symbol));
            }

            return targets;
        }

        /// <summary>
        /// 收集类型中标记了 [BytesPackGenField] 的字段/属性，解析其 key 值。
        /// key 可以显式指定，否则自动递增。
        /// </summary>
        private static MemberInfo[] CollectMembers(INamedTypeSymbol symbol)
        {
            var result = new List<MemberInfo>();
            var keyOffset = 0;

            foreach (var member in symbol.GetMembers())
            {
                if (member is not (IFieldSymbol or IPropertySymbol)) continue;
                var attr = TypeHelper.GetFieldAttr(member);
                if (attr == null) continue;

                keyOffset = TypeHelper.GetKeyFromAttr(attr, keyOffset + 1);
                var options = TypeHelper.GetOptionsFromAttr(attr);
                var type = (member as IFieldSymbol)?.Type ?? ((IPropertySymbol)member).Type;
                result.Add(new MemberInfo(member.Name, type, keyOffset, options));
            }

            return result.ToArray();
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

            for (var i = 0; i < members.Length; i++)
                members[i] = new MemberInfo(members[i].Name, members[i].Type, members[i].Key + parentKey, members[i].Options);

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

            if (hasParent && symbol.BaseType?.GetMembers("Z_BytesPackWrite").SingleOrDefault()?.IsAbstract != true)
                sb.Append("base.Z_BytesPackWrite(ref key, ref writer);\n");

            var uid = 0;
            foreach (var m in members)
            {
                var disableTrim = m.HasOption(FieldOption.DisableTrim);
                var check = !disableTrim && WriteEmitter.EmitNullCheck(sb, m.Name, m.Type, true);
                sb.Append("key.Push(ref writer, ").Append(m.Key).Append("); ");
                WriteEmitter.Emit(m.Type, m.Name, sb, ref uid, m.Options);
                if (check) sb.Append("}\n");
                ++uid;
            }

            sb.Append("}\n");
        }

        /// <summary>
        /// 生成 Z_BytesPackRead：通过 switch(key) 分发到对应字段的反序列化逻辑。
        /// </summary>
        private static void EmitRead(StringBuilder sb, INamedTypeSymbol symbol, MemberInfo[] members,
            string mod, bool hasParent)
        {
            sb.Append("public").Append(mod).Append(" void Z_BytesPackRead(int key, ref BytesReader reader) {\n");

            var canCallBase = hasParent &&
                              symbol.BaseType?.GetMembers("Z_BytesPackRead").SingleOrDefault()?.IsAbstract != true;

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

        public MemberInfo(string name, ITypeSymbol type, int key, int options = 0)
        {
            Name = name;
            Type = type;
            Key = key;
            Options = options;
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
