using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
namespace Kit2
{
	/// <summary>
	/// Editor : "Window/UI Toolkit > Sample" had sample of visualElement.
	/// </summary>
    public static class VisualElementExtend
    {
        public static VisualElement SplitVertical(float topHeight, out VisualElement top, float bottomHeight, out VisualElement bottom)
        {
			var vertical = new VisualElement()
			{
				style =
				{
					flexGrow = 1,
					flexDirection = FlexDirection.Column,
				},
			};
			top = new VisualElement();
			if (topHeight < 0f)
			{
				top.style.flexGrow = 1;
			}
			else
			{
				top.style.height = topHeight;
			}
			bottom = new VisualElement();
			if (bottomHeight < 0f)
			{
				bottom.style.flexGrow = 1;
			}
			else
			{
				bottom.style.height = bottomHeight;
			}
			vertical.Add(top);
			vertical.Add(bottom);
			return vertical;
		}

		public static VisualElement Splithorizontal(float leftWidth, out VisualElement left, float rightWidth, out VisualElement right)
		{
			var horizon = new VisualElement()
			{
				style =
				{
					flexGrow = 1,
					flexDirection = FlexDirection.Row,
				},
			};
			left = new VisualElement();
			if (leftWidth < 0f)
			{
				left.style.flexGrow = 1;
			}
			else
			{
				left.style.width = leftWidth;
			}
			right = new VisualElement();
			if (rightWidth < 0f)
			{
				right.style.flexGrow = 1;
			}
			else
			{
				right.style.height = rightWidth;
			}
			horizon.Add(left);
			horizon.Add(right);
			return horizon;
		}

		public static ObjectField CachePrefabField<OBJ>(string fieldName, string saveKey)
			where OBJ : UnityEngine.Object
		{
			var field = new ObjectField(fieldName)
			{
				objectType = typeof(OBJ),
				allowSceneObjects = false,

			};
			field.RegisterValueChangedCallback(_OnValueChanged);
			var path = EditorPrefs.GetString(saveKey, string.Empty);
			if (!string.IsNullOrEmpty(path))
			{
				var prefab = AssetDatabase.LoadAssetAtPath<OBJ>(path);
				if (prefab != null)
				{
					field.value = prefab;
				}
			}
			return field;

			void _OnValueChanged<T>(ChangeEvent<T> evt)
				where T : UnityEngine.Object
			{
				if (evt.newValue == null)
				{
					EditorPrefs.DeleteKey(saveKey);
				}
				else
				{
					var go = evt.newValue as T;
					//var inScene = go.scene.IsValid();
					//if (inScene)
					//{
					//	Debug.LogWarning("Prefab field should not be a scene object.", go);
					//	field.value = null;
					//	return;
					//}
					//if (!AssetDatabase.IsMainAsset(go))
					if (!EditorUtility.IsPersistent(go))
					{
						Debug.LogWarning("Prefab field should be a main asset.", go);
						field.value = null;
						return;
					}
					EditorPrefs.SetString(saveKey, AssetDatabase.GetAssetPath(evt.newValue));
				}
			}
		}
	}
}