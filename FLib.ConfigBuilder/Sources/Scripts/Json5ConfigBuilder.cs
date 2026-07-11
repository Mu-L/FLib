// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FLib
{
    public class Json5ConfigBuilder : IConfigBuildable
    {
        public string Extension => ".json5";

        public class Table<T>
        {
            public string[] Fields;
            public List<T> Values;
        }

        public class Value : IJson5Deserializable
        {
            public IBytesPackable CfgData;
            public string Id = string.Empty;

            public Json5CustomDeserializeResult JsonDeserialize(ref Json5SyntaxNodes nodes, object otherData, in Json5DeserializeOptionData options)
            {
                var table = (ConfigBuilderTable)options.UserData;
                if (table.IndexIdField != null)
                {
                    var indexIdFieldName = table.IndexIdField.Name;
                    for (var i = nodes.Position + 1; i < nodes.Nodes.Count; i++)
                    {
                        if (nodes.Nodes[i].Token != EJson5Token.Value) continue;
                        var key = nodes.Nodes[i].ContentSpan;
                        if (key.SequenceEqual(indexIdFieldName))
                        {
                            Id = nodes.Nodes[i + 1].ContentCopyString;
                            break;
                        }
                    }
                }

                CfgData = (IBytesPackable)nodes.To(table.ConfigType);
                return true;
            }
        }

        public void Build(ConfigBuilderTable table, ConfigBuilderFile file)
        {
            var parsedTable = Json5.Deserialize<Table<Value>>(File.ReadAllText(file.Path), new Json5DeserializeOptionData { UserData = table });
            foreach (var value in parsedTable.Values)
                table.AddConfig(value.Id, value.CfgData, parsedTable.Fields);
        }
    }
}