// ==================== qcbf@qq.com | 2025-07-01 ====================

using UnityEngine;

namespace FLib.Unity.Editor
{
    public class PlayerPrefsIntField
    {
        public int DefaultValue;
        public readonly string Key;

        private int _cacheValue;
        private bool _isCached;

        public override string ToString() => Get().ToString();

        public PlayerPrefsIntField(string key, in int defaultValue = 0)
        {
            DefaultValue = defaultValue;
            Key = key;
        }

        public int Get()
        {
            if (!_isCached)
            {
                _isCached = true;
                _cacheValue = PlayerPrefs.GetInt(Key, DefaultValue);
            }
            return _cacheValue;
        }

        public void Set(int value) => PlayerPrefs.SetInt(Key, _cacheValue = value);

        public void Set(bool value) => PlayerPrefs.SetInt(Key, _cacheValue = value ? 1 : 0);
        public static implicit operator int(PlayerPrefsIntField v) => v.Get();
        public static implicit operator bool(PlayerPrefsIntField v) => v.Get() == 1;
    }

    public class PlayerPrefsStringField
    {
        public string DefaultValue;
        public readonly string Key;

        private string _cacheValue;
        public override string ToString() => Get();

        public PlayerPrefsStringField(string key, in string defaultValue = "")
        {
            DefaultValue = defaultValue;
            Key = key;
        }

        public string Get() => _cacheValue ??= PlayerPrefs.GetString(Key, DefaultValue);
        public void Set(string value) => PlayerPrefs.SetString(Key, _cacheValue = value);
        public static implicit operator string(PlayerPrefsStringField v) => v.Get();
    }
}
