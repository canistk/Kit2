using UnityEngine;
using UnityEditor;

namespace Kit2
{
	[CustomPropertyDrawer(typeof(AnimatorParam), true)]
	public class AnimatorParamDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			using (new EditorGUI.PropertyScope(position, label, property))
			using (var checker = new EditorGUI.ChangeCheckScope())
			{
				var paramProp	= property.FindPropertyRelative(nameof(AnimatorParam.m_ParamString));
				var hashProp	= property.FindPropertyRelative(nameof(AnimatorParam.m_ParamId));
				EditorGUI.DelayedTextField(position, paramProp, label); // new GUIContent(hashProp.intValue.ToString()));
				if (checker.changed)
				{
					hashProp.intValue = Animator.StringToHash(paramProp.stringValue);
				}
			}
		}
	}
}