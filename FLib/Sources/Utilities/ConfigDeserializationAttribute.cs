public static class ConfigDeserializationAttribute
{
    public enum EType
    {
        /// <summary> 自定义反序列化。方法签名：<c>static int Method(in Memory&lt;byte&gt; buffer)</c>。 </summary>
        CustomDeserialize,

        /// <summary> 全部配置表反序列化完成后执行。方法签名：<c>static void Method()</c>。 </summary>
        AllConfigDeserializeFinish,
    }
}