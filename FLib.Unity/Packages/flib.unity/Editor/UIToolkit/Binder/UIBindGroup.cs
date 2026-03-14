// ==================== qcbf@qq.com | 2025-09-02 ====================

using System.Collections.Generic;
using FLib;

namespace FLib.Unity.Editor
{
    public struct UIBindGroup
    {
        public List<UIBindData> BindDatas;

        public void Add(UIBindData data)
        {
            (BindDatas ??= new List<UIBindData>()).Add(data);
        }

        public void Dirty()
        {
            if (BindDatas == null) return;
            foreach (var data in BindDatas) data.Dirty();
        }
    }
}
