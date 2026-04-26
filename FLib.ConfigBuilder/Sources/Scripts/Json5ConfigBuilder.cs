// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FLib
{
    /// <summary>
    /// <code>
    /// {
    ///     meta: {Name: {Sign: 'c',Default: "not found"}},
    ///     $1001: {Name: "abc"},
    ///     $1002: {Name: "XXX"},
    /// }
    /// </code>
    /// </summary>
    public class Json5ConfigBuilder : ConfigBuilder.IBuildable
    {
        public string Extension => ".json5";


        public class Value : IJson5Deserializable
        {
            public IBytesPackable CfgData;
            public string KeyValue;

            public Json5CustomDeserializeResult JsonDeserialize(ref Json5SyntaxNodes nodes, object otherData, in Json5DeserializeOptionData options)
            {
                var ctx = (ConfigBuilder.TableContext)options.UserData;
                var indexIdFieldName = ctx.IndexIdField.Name;
                for (var i = 0; i < nodes.Nodes.Count; i++)
                {
                    if (nodes.Nodes[i].Token != EJson5Token.Value) continue;
                    var key = nodes.Nodes[i].ContentSpan;
                    if (key.SequenceEqual(indexIdFieldName))
                    {
                        KeyValue = nodes.Nodes[i + 1].ContentCopyString;
                        break;
                    }
                }

                CfgData = (IBytesPackable)nodes.To(ctx.ConfigType);
                return true;
            }
        }

        public void Build(in ConfigBuilder.TableContext ctx)
        {
            foreach (var item in Json5.Deserialize<Value[]>(File.ReadAllText(ctx.SourceFilePath), new Json5DeserializeOptionData { UserData = ctx }))
            {
                var cfg = item.CfgData;
                // var rt = ctx.AddConfig(key, cfg);
                // if (rt.HasValue)
                //     ctx.IndexIdField.SetValue(cfg, ConfigBuilderUtility.ConvertObjectToType(key, ctx.IndexIdField.FieldType));
            }
        }
    }
}