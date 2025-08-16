using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Kit2
{
	[CustomPropertyDrawer(typeof(OnValueChangeAttribute))]
	public class OnValueChangeAttributeDrawer : PropertyDrawer
	{
		OnValueChangeAttribute onValueChangeAttribute => (OnValueChangeAttribute)attribute;

		private KeyValuePair<bool, Action> valueChangeAction; // ensure init once.
		private int previousHash = -1; // avoid common default value 0. e.g. false == 0 might not trigger change action.

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			using (var checker = new EditorGUI.ChangeCheckScope())
			{
				EditorGUI.PropertyField(position, property, label);

				if (checker.changed)
				{
					property.serializedObject.ApplyModifiedProperties();
					HandleValueChangedAction(property);
				}
			}
		}

		private void InitializeValueChangeAction(SerializedProperty property, UnityEngine.Object targetObject,
			MethodInfo methodInfo)
		{
			if (methodInfo == null)
			{
				Debug.LogError("Fail to initialize with invalid method");
				valueChangeAction = new KeyValuePair<bool, Action>(true, null);
				return;
			}
			if (property == null || targetObject == null)
			{
				Debug.LogError("Fail to initialize with null objects");
				valueChangeAction = new KeyValuePair<bool, Action>(true, null);
				return;
			}

			var _action = (Action)Delegate.CreateDelegate(typeof(Action), targetObject, methodInfo);
			valueChangeAction = new KeyValuePair<bool, Action>(true, _action);

			HandleValueChangedAction(property);
		}

		private const BindingFlags s_Rules = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase;

		private void HandleValueChangedAction(SerializedProperty property)
		{
			// when delegate not exist, create one.
			if (!valueChangeAction.Key)
			{
				var targetObject = property?.serializedObject?.targetObject;
				if (targetObject == null)
					throw new NullReferenceException();
				var _type = targetObject.GetType();
				var methodInfo = _type.GetMethod(onValueChangeAttribute.methodName, s_Rules);
				if (methodInfo == null)
				{
					Debug.LogError($"Fail to locate method = \'{onValueChangeAttribute.methodName}\', on {property.serializedObject}");
					return;
				}
				InitializeValueChangeAction(property, targetObject, methodInfo);
			}
			if (valueChangeAction.Value == null)
				return; // fail to create delegate, assume already fire error message on console.

			int hash = property.propertyType switch
			{
				SerializedPropertyType.Boolean => property.boolValue ? 1 : 0,
				SerializedPropertyType.String => property.stringValue.GetHashCode(),
				SerializedPropertyType.Float => property.floatValue.GetHashCode(),
				SerializedPropertyType.Integer => property.intValue,
				SerializedPropertyType.Enum => property.enumValueFlag,
				SerializedPropertyType.Vector2 => property.vector2Value.GetHashCode(),
				SerializedPropertyType.Vector3 => property.vector3Value.GetHashCode(),
				SerializedPropertyType.Vector4 => property.vector4Value.GetHashCode(),
				SerializedPropertyType.Vector2Int => property.vector2IntValue.GetHashCode(),
				SerializedPropertyType.Vector3Int => property.vector3IntValue.GetHashCode(),
				SerializedPropertyType.Color => property.colorValue.GetHashCode(),
				SerializedPropertyType.ObjectReference => property.objectReferenceValue ? property.objectReferenceValue.GetHashCode() : 0,
				SerializedPropertyType.Quaternion => property.quaternionValue.GetHashCode(),

				_ => throw new System.NotImplementedException($"[WIP] TODO: implement {property.propertyType.ToString()} flow.")
			};

			if (hash == previousHash)
				return;
			valueChangeAction.Value.TryCatchDispatchEventError(o => o.Invoke());
			previousHash = hash;
		}
	}
}