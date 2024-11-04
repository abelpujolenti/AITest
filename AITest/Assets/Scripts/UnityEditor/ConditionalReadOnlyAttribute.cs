using UnityEngine;

namespace UnityEditor
{
    public class ConditionalReadOnlyAttribute : PropertyAttribute
    {
        public string conditionField;

        public ConditionalReadOnlyAttribute(string conditionField)
        {
            this.conditionField = conditionField;
        }
    }
}