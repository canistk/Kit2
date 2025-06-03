using UnityEditor;
using UnityEngine;

namespace Kit2
{
	[CustomPropertyDrawer(typeof(EularAnglesAttribute))]
    public class EularAnglesDrawer : PropertyDrawer
    {
        EularAnglesAttribute eularAnglesAttribute => (EularAnglesAttribute)attribute;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			if (property.propertyType == SerializedPropertyType.Quaternion)
			{
				Vector3 eular = property.quaternionValue.eulerAngles;
				EditorGUI.BeginChangeCheck();
				eular = EditorGUI.Vector3Field(position, label, eular);
				if (EditorGUI.EndChangeCheck())
				{
				#if true
					property.quaternionValue = Quaternion.Euler(eular);
				#else
					// Attemp to avoid gimbal lock
					// we should only able to change one axis at a time on inspector.
					Quaternion before = property.quaternionValue.normalized;
					Quaternion after = Quaternion.Euler(eular);
					Quaternion diff = before.Inverse() * after;
					Vector3 beforeV3 = property.quaternionValue.eulerAngles;
					if (beforeV3.x != eular.x)
						diff = diff.Limit1DOF(Vector3.right);
					else if (beforeV3.y != eular.y)
						diff = diff.Limit1DOF(Vector3.up);
					else if (beforeV3.z != eular.z)
						diff = diff.Limit1DOF(Vector3.forward);
					property.quaternionValue = before * diff;
				#endif
				}
			}
			else
			{
				EditorGUI.HelpBox(position, $"{nameof(EularAnglesAttribute)} can only apply on {nameof(Quaternion)} field.", MessageType.Error);
			}
			EditorGUI.EndProperty();
		}
	}
}