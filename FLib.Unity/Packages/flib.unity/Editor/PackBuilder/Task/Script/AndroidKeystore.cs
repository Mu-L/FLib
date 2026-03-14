// ==================== qcbf@qq.com | 2025-07-01 ====================

using UnityEditor;

namespace FLib.Unity.Editor.PackBuilder.Task.Script
{
    public class AndroidKeystore : TaskBase
    {
        public string KeystorePassword;
        public string KeyaliasName;
        public string KeyaliasPassword;

        public override void Execute(Context ctx)
        {
            PlayerSettings.Android.keystorePass = KeystorePassword;
            PlayerSettings.Android.keyaliasName = KeyaliasName;
            PlayerSettings.Android.keyaliasPass = KeyaliasPassword;
        }
    }
}
