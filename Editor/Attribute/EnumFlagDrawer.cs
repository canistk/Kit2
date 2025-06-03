using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
namespace Kit2
{
	[CustomPropertyDrawer(typeof(EnumFlagAttribute))]
	public class EnumFlagDrawer : PropertyDrawer
	{
		EnumFlagAttribute enumFlagAttribute { get { return (EnumFlagAttribute)attribute; } }

		GUIContent[] m_Options = null;
		int[] m_Values = null;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			if (property.propertyType == SerializedPropertyType.Enum)
			{
				if (enumFlagAttribute.IsSingleValue)
				{
					if (m_Options == null)
					{
						string[] labels = enumFlagAttribute.type.GetEnumNames();
						var values = System.Enum.GetValues(enumFlagAttribute.type).GetEnumerator();
						int index = 0;
						List<GUIContent> rawOptions = new List<GUIContent>(labels.Length);
						List<int> rawValues = new List<int>(labels.Length);
						while (values.MoveNext())
						{
							int enumValue = (int)values.Current;
							if ((enumFlagAttribute.IsNullable && enumValue == 0) ||
								EnumExtend.IsPowerOfTwo(enumValue))
							{
								rawOptions.Add(new GUIContent(labels[index]));
								rawValues.Add(enumValue);
							}
							index++;
						}
						m_Options = rawOptions.ToArray();
						m_Values = rawValues.ToArray();
					}
					EditorGUI.BeginChangeCheck();
					int rst = EditorGUI.IntPopup(position, label, property.intValue, m_Options, m_Values);
					if (EditorGUI.EndChangeCheck())
					{
						property.intValue = rst;
					}
				}
				else
				{
					EditorGUI.PropertyField(position, property, label, true);
				}
			}
			else
			{
				EditorGUI.LabelField(position, label, typeof(EnumFlagAttribute).Name + " only allow to use with { Enum }.");
			}
			EditorGUI.EndProperty();
		}
	}
}