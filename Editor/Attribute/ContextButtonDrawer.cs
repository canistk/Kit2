using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace Kit2
{
	[CustomPropertyDrawer(typeof(ContextButtonAttribute))]
	public sealed class ContextButtonDrawer : PropertyDrawer
	{
		ContextButtonAttribute buttonAttribute { get { return (ContextButtonAttribute)attribute; } }

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			if (property.propertyType == SerializedPropertyType.Boolean)
			{
				if (GUI.Button(position, label))
				{
					if (!string.IsNullOrEmpty(buttonAttribute.Callback))
					{
						Type type = property.serializedObject.targetObject.GetType();
						MethodInfo methodInfo = type.GetMethod(buttonAttribute.Callback,
							BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
						if (methodInfo != null)
						{
							methodInfo.Invoke(property.serializedObject.targetObject, null);
						}
						else
						{
							EditorGUI.HelpBox(position, "Only support boolean type.", MessageType.Error);
						}
					}
					property.boolValue = false;
				}
			}
			else
			{
				EditorGUI.LabelField(position, label, "ContextButton only allow to use on Boolean.");
			}
			EditorGUI.EndProperty();
		}
	}
}