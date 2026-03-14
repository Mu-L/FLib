// ==================== qcbf@qq.com | 2025-08-28 ====================

using FLib;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor
{
    [CustomPropertyDrawer(typeof(FNum))]
    public class FNumEditor : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return new FloatField(property.displayName).BindDataWithUI(v =>
            {
                property.boxedValue = (FNum)v;
                property.serializedObject.ApplyModifiedProperties();
            }, () => (FNum)property.boxedValue);
        }
    }

    [CustomPropertyDrawer(typeof(FVector2))]
    public class FVector2Editor : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return new Vector2Field(property.displayName).BindDataWithUI(v =>
            {
                property.boxedValue = v.AsFVec2();
                property.serializedObject.ApplyModifiedProperties();
            }, () => ((FVector2)property.boxedValue).AsVec());
        }
    }

    [CustomPropertyDrawer(typeof(FVector2Int))]
    public class FVector2IntEditor : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return new Vector2IntField(property.displayName).BindDataWithUI(v =>
            {
                property.boxedValue = new FVector2Int(v.x, v.y);
                property.serializedObject.ApplyModifiedProperties();
            }, () =>
            {
                var vec = (FVector2Int)property.boxedValue;
                return new Vector2Int(vec.X, vec.Y);
            });
        }
    }

    [CustomPropertyDrawer(typeof(FVector3))]
    public class FVector3Editor : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return new Vector3Field(property.displayName).BindDataWithUI(v =>
            {
                property.boxedValue = new FVector3((FNum)v.x, (FNum)v.y, (FNum)v.z);
                property.serializedObject.ApplyModifiedProperties();
            }, () => ((FVector3)property.boxedValue).AsVec());
        }
    }
}
