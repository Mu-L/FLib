// ==================== qcbf@qq.com | 2025-09-04 ====================

using FLib;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Utilities
{
    public class RawImageUvMove : MonoBehaviour
    {
        public RawImage Target;
        public float Speed = 0.006f;

        private void Update()
        {
            var uv = Target.uvRect;
            uv.x -= Speed * Time.deltaTime;
            Target.uvRect = uv;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Target == null)
            {
                Target = GetComponent<RawImage>();
                EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}
