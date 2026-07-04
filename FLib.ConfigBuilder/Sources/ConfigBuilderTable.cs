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

        /// <summary> 所有配置数据 </summary>
        public List<IBytesPackable> AllConfigs { get; set; } = new(512);

        /// <summary> 所有配置数据索引 </summary>
        public Dictionary<uint, List<int>> AllConfigIdIndexes { get; set; } = new(512);

        private SpinLock _locker;
        private readonly TypeCode _indexIdTypeCode;

        public int ConfigCount => AllConfigIdIndexes.Count;
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
#if NET6_0_OR_GREATER
                AllConfigs.EnsureCapacity(capacity);
#else
            if (capacity > AllConfigs.Capacity)
                AllConfigs.Capacity = capacity;
#endif
            AllConfigIdIndexes.EnsureCapacity(capacity);
        }

        /// <summary>
        /// 
        /// </summary>0
        public (uint Id, int Index)? AddConfig(object objId, IBytesPackable config, TypeCode overrideTypeCode = TypeCode.Empty)
        {
            if (objId == null || config == null)
                return null;
            var isLocking = false;
            _locker.Enter(ref isLocking);
            try
            {
                var index = AllConfigs.Count;
                if (overrideTypeCode == TypeCode.Empty)
                    overrideTypeCode = _indexIdTypeCode;
                var id = overrideTypeCode >= TypeCode.SByte && overrideTypeCode <= TypeCode.UInt64 ? Convert.ToUInt32(objId) : ConfigHelper.StringToUniqueId(objId.ToString());
                if (!AllConfigIdIndexes.TryGetValue(id, out var indexes))
                    AllConfigIdIndexes.Add(id, indexes = new List<int>());
                indexes.Add(index);
                AllConfigs.Add(config);
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