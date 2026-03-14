//==================={By Qcbf|qcbf@qq.com|10/31/2023 11:56:35 AM}===================

using FLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FLib.Unity
{
    [Conditional("DEBUG"), AttributeUsage(AttributeTargets.Method)]
    public class MethodButtonAttribute : Attribute
    {
        public string Name;

        public MethodButtonAttribute(string name = null)
        {
            Name = name;
        }
    }
}
