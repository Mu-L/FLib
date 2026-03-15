// ==================== qcbf@qq.com | 2025-09-22 ====================

using FLib;
using UnityEngine;

namespace Utilities
{
    public class AnimatorSetParamRandom : MonoBehaviour
    {
        public string ParamName = "RandomValue";
        public float Interval = 1f;
        public int Min = 0;
        public int Max = 100;
        public Animator Target;

        private float _nextTriggerTime;
        private int _paramNameId;


        private void OnEnable()
        {
            _paramNameId = Animator.StringToHash(ParamName);
        }

        private void Update()
        {
            var t = Time.time;
            if (t < _nextTriggerTime)
                return;
            _nextTriggerTime = t + Interval;
            var val = Random.Range(Min, Max);
#if UNITY_EDITOR
            _paramNameId = Animator.StringToHash(ParamName);
#endif
            Target.SetInteger(_paramNameId, val);
        }


#if UNITY_EDITOR
        private void OnValidate()
        {
            Target ??= GetComponentInChildren<Animator>();
        }
#endif
    }
}
