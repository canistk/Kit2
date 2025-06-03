using UnityEngine;
using UnityEditor;

namespace Kit2
{
	[CustomPropertyDrawer(typeof(LayerFieldAttribute))]
	public class LayerFieldDrawer : PropertyDrawer
	{
		LayerFieldAttribute layerFieldAttribute { get { return (LayerFieldAttribute)attribute; } }

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			if (property.propertyType == SerializedPropertyType.LayerMask)
			{
				property.intValue = EditorGUI.LayerField(position, label.text, property.intValue);
			}
			else if (property.propertyType == SerializedPropertyType.String)
            {
				int oldId = LayerMask.NameToLayer(property.stringValue);
				int layerId = EditorGUI.LayerField(position, label.text, oldId);
				property.stringValue = LayerMask.LayerToName(layerId);
			}
			else
			{
				EditorGUI.LabelField(position, label, typeof(TagFieldAttribute).Name + " only allow to use with { String }.");
			}
			EditorGUI.EndProperty();
		}
	}
}