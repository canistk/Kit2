using UnityEngine;
using UnityEditor;
namespace Kit2
{
	public abstract class EditorBase : Editor
	{
		SerializedProperty scriptProp;
		protected virtual void OnEnable()
		{
			scriptProp = this.serializedObject.FindProperty("m_Script");
		}

		protected virtual void OnDisable() { }

		public sealed override void OnInspectorGUI()
		{
			if (OnInspectorInput(Event.current))
				return; // by pass this frame. to override system reaction.

			serializedObject.UpdateIfRequiredOrScript();
			SerializedProperty iter = serializedObject.GetIterator();
			iter.NextVisible(true); // enter children.

			using (new EditorGUI.DisabledGroupScope(true))
			{
				EditorGUILayout.PropertyField(scriptProp, includeChildren: true);
			}

			using (var checker = new EditorGUI.ChangeCheckScope())
            {
                OnBeforeDrawGUI();
                if (checker.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }

            using (var checker = new EditorGUI.ChangeCheckScope())
			{
				do
				{
					if (scriptProp != null && iter.propertyPath == scriptProp.propertyPath)
					{
						// do nothing.
					}
					else
					{
						OnDrawProperty(iter);
					}
				}
				while (iter.NextVisible(false));

				if (checker.changed)
				{
					serializedObject.ApplyModifiedProperties();
				}
			}

            using (var checker = new EditorGUI.ChangeCheckScope())
            {
                OnAfterDrawGUI();
                if (checker.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

		protected bool IsMultipleSelection => serializedObject.targetObjects.Length > 1;
		protected virtual void OnBeforeDrawGUI() { }
		protected virtual void OnAfterDrawGUI() { }

		/// <summary>intercept the key input before draw any GUI</summary>
		/// <param name="e"><see cref="Event.current"/></param>
		/// <returns>true = by pass this frame draw, false = draw inspector this frame.</returns>
		protected virtual bool OnInspectorInput(Event e) { return false; }

		protected virtual void OnDrawProperty(SerializedProperty property)
		{
			EditorGUILayout.PropertyField(property, includeChildren: true);
		}
	}
}