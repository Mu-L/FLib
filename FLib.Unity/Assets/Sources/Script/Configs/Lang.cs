// ==================== qcbf@qq.com | 2025-08-06 ====================

using System.Collections.Generic;
using System.Text.RegularExpressions;
using FLib;

namespace Configs
{
    [BytesPackGen, Config(null)]
    public partial class Lang : IJson5Deserializable
    {
        public static int DefaultConfigIndex;
        
        [BytesPackGenField] public string Type;
        [BytesPackGenField] public Dictionary<string, string> Values;

        private static Regex _langMatch;
        private static MatchEvaluator _langMatchReplacer;

        Json5CustomDeserializeResult IJson5Deserializable.JsonDeserialize(ref Json5SyntaxNodes nodes, object otherData, in Json5DeserializeOptionData options)
        {
            Values = nodes.To<Dictionary<string, string>>();
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public static string ParseText(string str)
        {
            if (_langMatch != null) return _langMatch.Replace(str, _langMatchReplacer);
            _langMatch = new Regex(@"\${(.+?)\}");
            _langMatchReplacer = m => Get(m.Groups[1].Value);
            return _langMatch.Replace(str, _langMatchReplacer);
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool TryGet(string key, out string result)
        {
            return Config<Lang>.Index(DefaultConfigIndex).Values.TryGetValue(key, out result);
        }

        /// <summary>
        /// 获取语言包
        /// </summary>
        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return ""; //默认值返回空
            return Config<Lang>.Index(DefaultConfigIndex).Values.TryGetValue(key, out var value) ? value : $"not found[{key}]";
        }

        /// <summary>
        /// 获取语言包, 配置格式: a{0}
        /// </summary>
        public static string Get(string key, object arg0)
        {
            return StringFLibFormatter.F(Get(key), arg0);
        }

        /// <summary>
        /// 获取语言包, 配置格式: a{0}b{1}
        /// </summary>
        public static string Get(string key, object arg0, object arg1)
        {
            return StringFLibFormatter.F(Get(key), arg0, arg1);
        }

        /// <summary>
        /// 获取语言包, 配置格式: a{0}b{1}c{2}
        /// </summary>
        public static string Get(string key, object arg0, object arg1, object arg2)
        {
            return StringFLibFormatter.F(Get(key), arg0, arg1, arg2);
        }

        /// <summary>
        /// 获取语言包, 配置格式: a{0}b{1}c{2}d{3}
        /// </summary>
        public static string Get(string key, object arg0, object arg1, object arg2, object arg3)
        {
            return StringFLibFormatter.F(Get(key), arg0, arg1, arg2, arg3);
        }

        /// <summary>
        /// 获取语言包, 配置格式: a{0}b{1}c{2}d{3}e{4}
        /// </summary>
        public static string Get(string key, object arg0, object arg1, object arg2, object arg3, object arg4)
        {
            return StringFLibFormatter.F(Get(key), arg0, arg1, arg2, arg3, arg4);
        }

        /// <summary>
        /// 获取语言包, 配置格式: a{0}b{1}c{2}d{3}e{4}f{5}
        /// </summary>
        public static string Get(string key, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5)
        {
            return StringFLibFormatter.F(Get(key), arg0, arg1, arg2, arg3, arg4, arg5);
        }

        /// <summary>
        /// 是否存储语言包
        /// </summary>
        public static bool IsContains(string key)
        {
            return Config<Lang>.Index(DefaultConfigIndex).Values.ContainsKey(key);
        }
    }
}
