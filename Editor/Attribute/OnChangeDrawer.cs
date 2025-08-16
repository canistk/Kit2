using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace Kit2
{
	[System.Obsolete("Use OnValueChange instead", true)]
	[CustomPropertyDrawer(typeof(OnChangeAttribute))]
	public sealed class OnChangeDrawer : PropertyDrawer
	{
		OnChangeAttribute onChangeAttribute => (OnChangeAttribute)attribute;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			using (new EditorGUI.PropertyScope(position, label, property))
			using (var checker = new EditorGUI.ChangeCheckScope())
			{
				EditorGUI.PropertyField(position, property, label, true);
				if (!checker.changed)
					return;
				try
				{
					Type type = property.serializedObject.targetObject.GetType();
					MethodInfo methodInfo = type.GetMethod(onChangeAttribute.callbackMethodName);
					if (methodInfo != null)
						methodInfo.Invoke(property.serializedObject.targetObject, null);
					else if (property.isArray)
					{
						Debug.LogError($"Type = {type.Name}, Arrary detected, fail to locate {onChangeAttribute.callbackMethodName} are those the right path ?");
					}
					else
						Debug.LogError($"Type = {type.Name} ~ MethodInfo {onChangeAttribute.callbackMethodName} not found.");
				}
				catch
				{
					throw new NullReferenceException($"That method {onChangeAttribute.callbackMethodName}() not exist.");
				}
				finally
				{
					property.serializedObject.ApplyModifiedProperties();
				}
			}
		}
	}
}