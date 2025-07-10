using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.Animations;
using System.Collections.Generic;

namespace Kit2
{
	public static class ViewObjectEditorUtil
	{
		public static void StateButtons(Object[] targets)
		{
			EditorGUILayout.Space();
			EditorGUI.BeginDisabledGroup(!Application.isPlaying);
			GUILayout.BeginHorizontal();
			Color orgColor = GUI.color;
			GUI.color = Color.cyan;
			if (GUILayout.Button("Appear", GUILayout.Height(30f)))
			{
				for (int i = 0; i < targets.Length; i++)
					((ViewObject)targets[i]).Appear();
			}
			GUI.color = Color.yellow;
			if (GUILayout.Button("Disappear", GUILayout.Height(30f)))
			{
				for (int i = 0; i < targets.Length; i++)
					((ViewObject)targets[i]).Disappear();
			}
			GUILayout.EndHorizontal();
			if (targets[0] is ViewObject)
				EditorGUILayout.LabelField(StateReport((ViewObject)targets[0]));
			GUI.color = orgColor;
			EditorGUI.EndDisabledGroup();
		}

		public static string StateReport(ViewObject m_Target)
		{
			string info = (m_Target.IsActionFinish() ? "[-]" : "[" + m_Target.ActionCount() + "]");
			info += " Status: " + m_Target.ViewStatus.ToString("F");
			for (int i = 0; i < m_Target.ActionCount(); i++)
			{
				info += (m_Target.ViewStatus == ViewObject.eViewStatus.Appearing || m_Target.ViewStatus == ViewObject.eViewStatus.Appeared) ?
					((i % 2 == 0) ? ", D" : ", A") :
					((i % 2 == 0) ? ", A" : ", D");
			}
			return info;
		}
	}

	[CustomEditor(typeof(ViewObject), true, isFallback = true), CanEditMultipleObjects]
	public class ViewObjectEditor : EditorBase
	{
		static readonly GUIContent l_ShowEvent = new GUIContent("-- Unity Events --");
		static readonly GUIContent l_HideEvent = new GUIContent("-- Unity Events (Hidden) --");
		protected List<SerializedProperty> props = new List<SerializedProperty>();
		protected List<SerializedProperty> eventsProps = new List<SerializedProperty>();
		private static AnimBool m_ShownEvents;
		protected override void OnEnable()
		{
			base.OnEnable();
			SerializedProperty iter = serializedObject.GetIterator();
			iter.NextVisible(true);
			do
			{
				SerializedProperty prop = iter.Copy();
				if (prop.type.Equals(typeof(UnityEngine.Events.UnityEvent).Name))
					eventsProps.Add(prop);
			}
			while (iter.NextVisible(false));
			if (m_ShownEvents == null)
			{
				m_ShownEvents = new AnimBool(false);
			}
			m_ShownEvents.valueChanged.AddListener(Repaint);
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			m_ShownEvents.valueChanged.RemoveAllListeners();
		}

		protected override void OnBeforeDrawGUI()
		{
			base.OnBeforeDrawGUI();
			DrawStateButtons();
			DrawEventSession();
		}

		//protected override void OnAfterDrawGUI()
		//{
		//	base.OnAfterDrawGUI();
		//}

		protected override void OnDrawProperty(SerializedProperty property)
		{
			if (property.type.Equals(typeof(UnityEngine.Events.UnityEvent).Name))
			{
				/// Skip, handle on <see cref="OnAfterDrawGUI"/>
			}
			else
				base.OnDrawProperty(property);
		}

		protected void DrawStateButtons()
		{
			// Multiple session
			ViewObjectEditorUtil.StateButtons(serializedObject.targetObjects);
			EditorGUILayout.Space();
			serializedObject.UpdateIfRequiredOrScript();
		}

		protected void DrawEventSession()
		{
			if (IsMultipleSelection)
			{
				EditorGUILayout.HelpBox("Multiple Viewobjects cannot config events at the same time.", MessageType.Info);
				return;
			}
			// grouped events
			EditorGUILayout.LabelField("Events:", EditorStyles.boldLabel);
			if (GUILayout.Button(m_ShownEvents.target ? l_ShowEvent : l_HideEvent, EditorStyles.toolbarDropDown))
			{
				m_ShownEvents.target = !m_ShownEvents.target;
			}
			if (EditorGUILayout.BeginFadeGroup(m_ShownEvents.faded))
			{
				EditorGUI.BeginChangeCheck();
				EditorGUI.indentLevel++;
				int cnt = eventsProps.Count;
				for (int i = 0; i < cnt; i++)
				{
					EditorGUILayout.PropertyField(eventsProps[i]);
				}
				EditorGUI.indentLevel--;
				if (EditorGUI.EndChangeCheck())
					serializedObject.ApplyModifiedProperties();
			}
			EditorGUILayout.EndFadeGroup();
		}
	}
}