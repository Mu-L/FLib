//==================={By Qcbf|qcbf@qq.com|9/9/2022 3:49:01 PM}===================

using System;
using System.Collections.Generic;
using FLib;
using UnityEngine;

namespace FLib.Unity
{
    public struct FpsStatistic
    {
        public int Interval;
        private float mFPSTime;
        private int mFPSTemp;
        private string _ValueStr;
        public int Value { get; private set; }

        public string ValueStr => _ValueStr ??= Value.ToString();

        public static FpsStatistic Create(int interval = 1)
        {
            return new FpsStatistic { Interval = interval };
        }

        public override string ToString() => ValueStr;

        public bool Update()
        {
            if (mFPSTime < Interval)
            {
                mFPSTime += Time.unscaledDeltaTime;
                mFPSTemp++;
                return false;
            }
            if (Value != mFPSTemp)
            {
                Value = mFPSTemp / Interval;
                _ValueStr = null;
            }
            mFPSTime = mFPSTemp = 0;
            return true;
        }

        public static implicit operator string(in FpsStatistic v) => v.Value.ToString();
        public static implicit operator int(in FpsStatistic v) => v.Value;
    }
}
