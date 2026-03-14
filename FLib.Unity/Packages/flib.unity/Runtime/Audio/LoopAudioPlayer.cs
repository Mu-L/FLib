//==================={By Qcbf|qcbf@qq.com|8/22/2022 10:44:40 AM}===================

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FLib;
using UnityEngine;

namespace FLib.Unity
{
    public class LoopAudioPlayer : AudioPlayer
    {
        public float Interval = 1;
        public int Count;

        private float _nextTriggerTime;
        private int _count;

        protected override void OnDisable()
        {
            base.OnDisable();
            _count = 0;
            _nextTriggerTime = Time.time + Interval;
        }

        private void Update()
        {
            if (Count > 0 && _count >= Count)
                return;
            var t = Time.time;
            if (t < _nextTriggerTime)
                return;
            _nextTriggerTime = t + Interval;
            Play();
            ++_count;
        }
    }
}
