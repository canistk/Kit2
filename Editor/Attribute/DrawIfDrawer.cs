using System;
using UnityEditor;
using UnityEngine;

namespace Kit2
{
	[CustomPropertyDrawer(typeof(DrawIfAttribute))]
	public class DrawIfPropertyDrawer : PropertyDrawer
	{
		// Is the condition met? Should the field be drawn?
		private bool m_ConditionMet = false;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
            if (m_ConditionMet || ((DrawIfAttribute)attribute).disablingType == DisablingType.ReadOnly)
                return base.GetPropertyHeight(property, label);
            else
                return 0f;
		}

        private bool Compare(int lhs, ComparisonType oper, int rhs)
        {
            // Compare the values to see if the condition is met.
            switch (oper)
            {
                case ComparisonType.Equals: return lhs == rhs;
                case ComparisonType.NotEqual: return lhs != rhs;
                case ComparisonType.GreaterThan: return lhs > rhs;
                case ComparisonType.SmallerThan: return lhs < rhs;
                case ComparisonType.SmallerOrEqual: return lhs <= rhs;
                case ComparisonType.GreaterOrEqual: return lhs >= rhs;
                default: throw new NotImplementedException();
            }
        }
        private bool Compare(float lhs, ComparisonType oper, float rhs)
        {
            // Compare the values to see if the condition is met.
            switch (oper)
            {
                case ComparisonType.Equals: return lhs == rhs;
                case ComparisonType.NotEqual: return lhs != rhs;
                case ComparisonType.GreaterThan: return lhs > rhs;
                case ComparisonType.SmallerThan: return lhs < rhs;
                case ComparisonType.SmallerOrEqual: return lhs <= rhs;
                case ComparisonType.GreaterOrEqual: return lhs >= rhs;
                default: throw new NotImplementedException();
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DrawIfAttribute drawIf = (DrawIfAttribute)attribute;
            SerializedProperty comparedField = property.serializedObject.FindProperty(drawIf.comparedPropertyName);

            // Get the value of the compared field.
            switch (comparedField.propertyType)
            {
                case SerializedPropertyType.Enum:
                case SerializedPropertyType.Integer:
                    {
                        m_ConditionMet = Compare(
                            comparedField.intValue,
                            drawIf.comparisonType,
                            (int)drawIf.comparedValue);
                    }
                    break;
                case SerializedPropertyType.Float:
                    {
                        m_ConditionMet = Compare(
                            comparedField.floatValue,
                            drawIf.comparisonType,
                            (float)drawIf.comparedValue);
                    }
                    break;
                case SerializedPropertyType.Boolean:
                    if (drawIf.comparedValue is bool)
                    {
                        bool lhs = comparedField.boolValue;
                        bool rhs = (bool)drawIf.comparedValue;
                        switch (drawIf.comparisonType)
                        {
                            case ComparisonType.Equals: m_ConditionMet = lhs == rhs; break;
                            case ComparisonType.NotEqual: m_ConditionMet = lhs != rhs; break;
                            default:
                                m_ConditionMet = true;
                                EditorGUI.HelpBox(position, $"Boolean type can only compare with Equals/NotEqual\n{property.propertyPath}", MessageType.Error);
                                return;
                        }
                    }
                    else
                    {
                        m_ConditionMet = true;
                        EditorGUI.HelpBox(position, $"Boolean type can only compare with boolean type.", MessageType.Error);
                        return;
                    }
                    break;
                case SerializedPropertyType.ObjectReference:
                    {
                        object lhs = comparedField.objectReferenceValue;
                        object rhs = drawIf.comparedValue;
                        switch (drawIf.comparisonType)
                        {
                            case ComparisonType.Equals: m_ConditionMet = lhs == rhs; break;
                            case ComparisonType.NotEqual: m_ConditionMet = lhs != rhs; break;
                            default:
                                m_ConditionMet = true;
                                EditorGUI.HelpBox(position, $"Object type can only compare with Equals/NotEqual\n{property.propertyPath}", MessageType.Error);
                                return;
                        }
                    }
                    break;
                default:
                    m_ConditionMet = true;
                    EditorGUI.HelpBox(position, $"{nameof(DrawIfAttribute)} Only support NumericType & Boolean", MessageType.Error);
                    return;
            }


            // If the condition is met, simply draw the field. Else...
            if (m_ConditionMet)
            {
                EditorGUI.PropertyField(position, property, label);
            }
            else if (drawIf.disablingType == DisablingType.ReadOnly)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.PropertyField(position, property, label);
                }
            }
        }
    }
}