// // ==================== qcbf@qq.com | 2026-07-04 ====================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace FLib
{
    /// <summary> 一张抽象配置表, 不代表实体配置文件. </summary>
    public class ConfigBuilderTable : IConfigTable
    {
        /// <summary> 配置表实际类型 </summary>
        public Type ConfigType { get; set; }

        /// <summary> 配置表名称 </summary>
        public string Name { get; set; }

        /// <summary> 配置表id字段 </summary>
        public FieldInfo IndexIdField { get; }

        /// <summary> 配置表选项 </summary>
        public ConfigHelper.EOption Options { get; set; }

        /// <summary> 所有配置表 </summary>
        public Dictionary<uint, IBytesPackable> AllConfigs { get; set; } = new(512);

        private SpinLock _locker;
        private readonly TypeCode _indexIdTypeCode;
        private Dictionary<string, FieldInfo> _fieldCache;

        public int ConfigCount => AllConfigs.Count;
        public override string ToString() => ConfigType.Name;

        public ConfigBuilderTable(string name, Type type, ConfigHelper.EOption options)
        {
            Name = name;
            ConfigType = type;
            Options = options;
            IndexIdField = ConfigType.GetFields(BindingFlags.Public | BindingFlags.Instance).OrderBy(v => v.MetadataToken).FirstOrDefault();
            _indexIdTypeCode = Type.GetTypeCode(IndexIdField?.FieldType);
        }

        /// <summary>
        /// 
        /// </summary>
        public void EnsureCapacity(int capacity)
        {
            AllConfigs.EnsureCapacity(capacity);
        }

        /// <summary>
        /// 
        /// </summary>0
        public (uint Id, int Index)? AddConfig(object objId, IBytesPackable config, string[] applyFields = null, TypeCode overrideTypeCode = TypeCode.Empty)
        {
            if (objId == null || config == null)
                return null;
            var isLocking = false;
            _locker.Enter(ref isLocking); // 大部分情况只有一个线程执行
            try
            {
                var index = AllConfigs.Count;
                if (overrideTypeCode == TypeCode.Empty)
                    overrideTypeCode = _indexIdTypeCode;
                var id = overrideTypeCode >= TypeCode.SByte && overrideTypeCode <= TypeCode.UInt64 ? Convert.ToUInt32(objId) : ConfigHelper.StringToUniqueId(objId.ToString());
                if (!AllConfigs.TryGetValue(id, out var mainConfig))
                {
                    AllConfigs.Add(id, config);
                }
                else
                {
                    Log.AssertNotNull(applyFields)?.Write($"{TypeAssistant.GetTypeName(ConfigType)}.{objId} addition config not found apply fields");
                    _fieldCache ??= ConfigType.GetFields(BindingFlags.Public | BindingFlags.Instance).ToDictionary(v => v.Name);
                    foreach (var fieldName in applyFields)
                    {
                        var fi = _fieldCache[fieldName];
                        var val = fi.GetValue(config);
                        fi.SetValue(mainConfig, val);
                    }
                }

                return (id, index);
            }
            finally
            {
                if (isLocking)
                    _locker.Exit();
            }
        }
    }
}