using UnityEngine;
using UnityEditor;

namespace Kit2
{
	[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
	public class ReadOnlyDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			using (new EditorGUI.PropertyScope(position, label, property))
			{
				using (new EditorGUI.DisabledGroupScope(true))
				{
					EditorGUI.PropertyField(position, property, label, true);
				}
			}
		}
	}
}