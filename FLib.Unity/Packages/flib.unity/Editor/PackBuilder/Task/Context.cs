// ==================== qcbf@qq.com | 2025-07-01 ====================

using System.IO;
using UnityEditor;

namespace FLib.Unity.Editor.PackBuilder.Task
{
    public class Context
    {
        public TaskSchedule Schedule;
        public AssetLoaderInfo Info = new(2048);

        public Context(TaskSchedule schedule)
        {
            Schedule = schedule;
        }


        /// <summary>
        /// 
        /// </summary>
        public byte[] GetInfoBytes()
        {
            return Compressor.Compress(BytesPack.Pack(Info)).ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        public ref AssetLoaderInfo.Meta GetInfoAssetMeta(string path)
        {
            return ref Info.AssetMetas.GetOrAddValueRef(path);
        }
    }
}
