// ==================== qcbf@qq.com | 2025-07-01 ====================

namespace FLib.Unity.Editor.TimeLogic
{
    public interface ISelectable
    {
        public object InspectorValue { get; }
        void OnSelectChange(bool value);
        void RefreshUI();
    }
}
