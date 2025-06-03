using UnityEngine;
using UnityEditor;
namespace Kit2
{
	[CustomPropertyDrawer(typeof(MinMaxSliderAttribute))]
	class MinMaxSliderDrawer : PropertyDrawer
	{
		MinMaxSliderAttribute attr => (MinMaxSliderAttribute)attribute;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			switch(property.propertyType)
			{
				case SerializedPropertyType.Vector2: OnVector2(position, property, label); break;
				case SerializedPropertyType.Float: OnFloat(position, property, label); break;
				case SerializedPropertyType.Integer: OnInt(position, property, label); break;
				default:
					EditorGUI.LabelField(position, label, $"Not support {property.propertyType}");
				break;
			}
		}

		private void OnVector2(Rect position, SerializedProperty property, GUIContent label)
		{
			using (var checker = new EditorGUI.ChangeCheckScope())
			{
				Vector2 range = property.vector2Value;
				float min = range.x;
				float max = range.y;

				Rect context = EditorGUI.PrefixLabel(position, label);
				Rect[] cols = context.SplitLeft(50f);
				min = EditorGUI.DelayedFloatField(cols[0], min);
				cols = cols[1].SplitRight(50f);
				EditorGUI.MinMaxSlider(cols[0], ref min, ref max, attr.min, attr.max);
				max = EditorGUI.DelayedFloatField(cols[1], max);

				if (checker.changed)
				{
					range.x = min;
					range.y = max;
					property.vector2Value = range;
				}
			}
		}

		private void OnFloat(Rect position, SerializedProperty property, GUIContent label)
		{
			using (var checker = new EditorGUI.ChangeCheckScope())
			{
				var val = property.floatValue;
				
				Rect context = EditorGUI.PrefixLabel(position, label);
				val = EditorGUI.Slider(context, val, attr.min, attr.max);
				if (checker.changed)
				{
					property.floatValue = val;
				}
			}
		}
		private void OnInt(Rect position, SerializedProperty property, GUIContent label)
		{
			using (var checker = new EditorGUI.ChangeCheckScope())
			{
				var val = property.intValue;

				Rect context = EditorGUI.PrefixLabel(position, label);
				val = EditorGUI.IntSlider(context, val, (int)attr.min, (int)attr.max);
				if (checker.changed)
				{
					property.intValue = (int)val;
				}
			}
		}
	}

	[CustomPropertyDrawer(typeof(MinMaxSliderIntAttribute))]
	class MinMaxSliderIntDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.propertyType != SerializedPropertyType.Vector2Int)
			{
				EditorGUI.LabelField(position, label, "Use only with Vector2Int");
				return;
			}

			using (var checker = new EditorGUI.ChangeCheckScope())
			{
				Vector2Int range = property.vector2IntValue;
				float min = range.x;
				float max = range.y;
				var attr = (MinMaxSliderIntAttribute)attribute;
				
				Rect context = EditorGUI.PrefixLabel(position, label);
				Rect[] cols = context.SplitLeft(50f);
				min = EditorGUI.DelayedIntField(cols[0], (int)min);
				cols = cols[1].SplitRight(50f);
				EditorGUI.MinMaxSlider(cols[0], ref min, ref max, attr.min, attr.max);
				max = EditorGUI.DelayedIntField(cols[1], (int)max);

				if (checker.changed)
				{
					range.x = (int)min;
					range.y = (int)max;
					property.vector2IntValue = range;
				}
			}
		}
	}
}