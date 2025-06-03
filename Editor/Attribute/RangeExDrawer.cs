using UnityEngine;
using UnityEditor;
namespace Kit2
{
	[CustomPropertyDrawer(typeof(RangeExAttribute))]
	public class RangeExDrawer : PropertyDrawer
	{
		RangeExAttribute target { get { return (RangeExAttribute)attribute; } }

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			float min = (target.config & RangeExAttribute.eConfig.MinNamed) == 0 ?
				target.min :
				property.serializedObject.FindProperty(target.minName).floatValue;

			float max = (target.config & RangeExAttribute.eConfig.MaxNamed) == 0 ?
				target.max :
				property.serializedObject.FindProperty(target.maxName).floatValue;

			using (var checker = new EditorGUI.ChangeCheckScope())
			{
				float tmp = EditorGUI.Slider(position, label, property.floatValue, min, max);
				if (checker.changed)
				{
					property.floatValue = tmp;
				}
			}
		}
	}
}