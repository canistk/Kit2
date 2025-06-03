using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Kit2
{
	[CustomPropertyDrawer(typeof(MaskFieldAttribute))]
	public class MaskFieldDrawer : PropertyDrawer
	{
		MaskFieldAttribute maskFieldAttribute { get { return (MaskFieldAttribute)attribute; } }

		string[] m_Options = null;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			if (property.propertyType == SerializedPropertyType.Enum)
			{
				if (m_Options == null)
				{
					string[] labels = maskFieldAttribute.type.GetEnumNames();
					var values = System.Enum.GetValues(maskFieldAttribute.type).GetEnumerator();
					List<string> tmp = new List<string>();
					int index = 0;
					while (values.MoveNext())
					{
						int enumValue = (int)values.Current;
						if (EnumExtend.IsPowerOfTwo(enumValue))
							tmp.Add(labels[index]);
						index++;
					}
					m_Options = tmp.ToArray();
				}
				EditorGUI.BeginChangeCheck();
				int rst = EditorGUI.MaskField(position, label, property.intValue, m_Options);
				if (EditorGUI.EndChangeCheck())
				{
					property.intValue = rst;
				}
			}
			else
			{
				EditorGUI.LabelField(position, label, typeof(MaskFieldAttribute).Name + " only allow to use with { Enum }.");
			}
			EditorGUI.EndProperty();
		}
	}
}