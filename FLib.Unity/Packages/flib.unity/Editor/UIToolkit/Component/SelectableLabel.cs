//==================={By Qcbf|qcbf@qq.com|7/3/2021 11:15:19 AM}===================

using FLib;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor
{
    public class SelectableLabel : TextField
    {

        public SelectableLabel(string label)
        {
            text = label;
            isReadOnly = true;
            style.borderTopWidth = style.borderRightWidth = style.borderBottomWidth = style.borderLeftWidth = 0;
            var child = ElementAt(0);
            child.style.borderTopWidth = child.style.borderRightWidth = child.style.borderBottomWidth = child.style.borderLeftWidth = 0;
            child.style.flexGrow = 0;
        }



    }
}
