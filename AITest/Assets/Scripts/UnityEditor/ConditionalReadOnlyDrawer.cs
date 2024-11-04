using UnityEngine;

namespace UnityEditor
{
    [CustomPropertyDrawer(typeof(ConditionalReadOnlyAttribute))]
    public class ConditionalReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ConditionalReadOnlyAttribute conditional = (ConditionalReadOnlyAttribute)attribute;
            SerializedProperty conditionField = property.serializedObject.FindProperty(conditional.conditionField);

            if (conditionField != null && conditionField.boolValue)
            {
                GUI.enabled = true;
            }
            else
            {
                GUI.enabled = false;
            }

            EditorGUI.PropertyField(position, property, label);
            GUI.enabled = true;
        }
    }
}