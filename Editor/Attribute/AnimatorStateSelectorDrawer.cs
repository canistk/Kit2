using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Reflection;

namespace Kit2
{
	[CustomPropertyDrawer(typeof(AnimatorStateSelector))]
	public class AnimatorStateSelectorDrawer : PropertyDrawer
	{
		static readonly float m_LineH = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		private RuntimeAnimatorController m_CacheController = null;
		private List<AnimatorStateCacheLite> m_AnimStateList = null;
		// private AnimatorStateSelector m_Self = null;
		private string[] pathsCache;
		private int[] str2hashs;

		SerializedProperty animProp = null;
		SerializedProperty selectProp = null;
		SerializedProperty layerIndexProp = null;
		SerializedProperty displayPathProp = null;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			//property.serializedObject.FindProperty()
			if (selectProp == null) selectProp = property.FindPropertyRelative(nameof(AnimatorStateSelector.m_SelectedHash));
			if (layerIndexProp == null) layerIndexProp = property.FindPropertyRelative(nameof(AnimatorStateSelector.m_LayerIndex));
			if (displayPathProp == null) displayPathProp = property.FindPropertyRelative(nameof(AnimatorStateSelector.m_DisplayPath));
			Rect line = position.Clone(height: m_LineH);

			Animator animator = null;
			if (TryGetRefAnimator(property, out var refAnimator))
			{
				animProp = property.serializedObject.FindProperty(refAnimator.path);
				animator = animProp?.objectReferenceValue as Animator;
				m_RenderPath = 0;
			}
			if (animator == null)
			{
				animProp = property.FindPropertyRelative(nameof(AnimatorStateSelector.m_Animator));
				EditorGUI.PropertyField(line, animProp, true);
				animator = (Animator)animProp.objectReferenceValue;
				line = line.GetRectBottom();
				m_RenderPath = 1;
			}

			if (animator == null)
			{
				EditorGUI.HelpBox(line, "Missing Animator", MessageType.Error);
				return;
			}

			var runtimeController = animator.runtimeAnimatorController;
            if (runtimeController == null)
            {
				EditorGUI.HelpBox(line, "it's not a valid AnimatorController", MessageType.Error);
				return;
			}

			UpdateController(animator.runtimeAnimatorController);

			if (pathsCache == null || str2hashs == null)
			{
				EditorGUI.HelpBox(line, "Cache information broken.", MessageType.Error);
				return;
			}

			int val = selectProp.intValue;
			var rest = EditorGUI.PrefixLabel(line, label);
			using (var checker = new EditorGUI.ChangeCheckScope())
			{
				val = EditorGUI.IntPopup(rest, val, pathsCache, str2hashs);
				if (checker.changed)
				{
					selectProp.intValue = val;
					int index = -1, cnt = m_AnimStateList.Count;
					for (int i = 0; index == -1 && i < cnt; i++)
					{
						if (m_AnimStateList[i].fullNameHash == val)
						{
							index = i;
							layerIndexProp.intValue = m_AnimStateList[index].layer;
							displayPathProp.stringValue = m_AnimStateList[index].displayPath;
						}
					}
					// don't cache index, since animator will change afterward.
					// use hash result directly.
					Debug.Log($"Change to : Element[{index}], hash = {val}, Path={m_AnimStateList[index].displayPath}");//, Motion={m_AnimStateList[index].motion}", m_AnimStateList[index].motion);
					property.serializedObject.ApplyModifiedProperties();
				}
			}
			
			// 	base.OnGUI(position, property, label);
		}

		private int m_RenderPath;
		private bool TryGetRefAnimator(SerializedProperty property, out RefAnimator refAnimator)
		{
			refAnimator = null;
			var target = property?.serializedObject?.targetObject;
			if (target == null)
				return false;
			var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			FieldInfo fieldInfo = null;
			if (property.propertyPath.Contains('.'))
			{
				fieldInfo = target.GetType().GetField(property.propertyPath.Split('.')[0], flags);
			}
			else
			{
				fieldInfo = target.GetType().GetField(property.name, flags);
			}
			if (fieldInfo == null)
				return false;
			refAnimator = fieldInfo.GetCustomAttribute<RefAnimator>();
			return refAnimator != null;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return
				m_RenderPath == 0 ?
					base.GetPropertyHeight(property, label) :
					m_LineH * 2f;
		}

		private void UpdateController(RuntimeAnimatorController controller)
		{
			if (m_CacheController != controller || (pathsCache == null && str2hashs == null))
			{
				m_CacheController = controller;
				UpdateState(controller);
			}
		}

		private void UpdateState(RuntimeAnimatorController controller)
		{
			if (m_CacheController == null || !(m_CacheController is AnimatorController))
			{
				m_AnimStateList = null;
				return;
			}

			m_CacheController = controller;
			AnimatorController ac = (AnimatorController)controller;
			Editor_BuildStateInfoList(ac, out m_AnimStateList);

			// reflection hack : put reference back into variables
			int cnt = m_AnimStateList.Count;
			pathsCache = new string[cnt];
			str2hashs = new int[cnt];
			for (int i = 0; i < cnt; i++)
			{
				pathsCache[i] = m_AnimStateList[i].displayPath;
				str2hashs[i] = m_AnimStateList[i].fullNameHash;
			}
		}
		public static bool Editor_TryGetMotion<T>(List<T> stateList, int hash, int layer, out Motion motion)
			where T : AnimatorStateCache
		{
			for (int i = 0; i < stateList.Count; i++)
			{
				if (stateList[i].layer == layer &&
					stateList[i].fullNameHash == hash)
				{
					motion = stateList[i].motion;
					return true;
				}
			}
			motion = null;
			return false;
		}

		/// <summary>
		/// In order to fetch state information from AnimatorController
		/// </summary>
		/// <param name="animatorController"></param>
		/// <param name="stateList"></param>
		private static void Editor_BuildStateInfoList(RuntimeAnimatorController animatorController, out List<AnimatorStateCacheLite> stateList)
		{
			AnimatorController ac = animatorController as AnimatorController;
			if (ac == null)
				throw new UnityException($"Require {nameof(AnimatorController)}");
			AnimatorControllerLayer[] layers = ac.layers;
			stateList = new List<AnimatorStateCacheLite>(100);
			int layerCnt = layers.Length;
			for (int i = 0; i < layerCnt; i++)
			{
				string layerName = layers[i].name;
				Editor_AddStateFromStateMachine(stateList, layerName, layerName, i, layers[i].stateMachine);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="stateList"></param>
		/// <param name="prefix"></param>
		/// <param name="displayPrefix"></param>
		/// <param name="layerIndex"></param>
		/// <param name="animatorStateMachine">Is <see cref="AnimatorStateMachine"/> in object type, to allow broken U3D build pipeline</param>
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		private static void Editor_AddStateFromStateMachine(List<AnimatorStateCacheLite> stateList, string prefix, string displayPrefix, int layerIndex, Object animatorStateMachine)
		{
			// In order to get rid of compiler error (No editor)
			AnimatorStateMachine asm = animatorStateMachine as AnimatorStateMachine;

			ChildAnimatorState[] childs = asm.states;
			int childCnt = childs.Length;
			for (int x = 0; x < childCnt; x++)
			{
				AnimatorState state = childs[x].state;
				string path = prefix + "." + state.name;
				string displayPath = prefix + "/" + state.name;
				stateList.Add(new AnimatorStateCacheLite
				{
					displayPath = displayPath,
					path = path,
					layer = layerIndex,
					fullNameHash = Animator.StringToHash(path),
					stateName = state.name,
					shortNameHash = Animator.StringToHash(state.name),
				});
				// Debug.Log(path + ", hash = "+ state.nameHash + " == "+ Animator.StringToHash(path));
			}

			ChildAnimatorStateMachine[] subStateMachine = asm.stateMachines;
			int subStateMachineCnt = subStateMachine.Length;
			for (int y = 0; y < subStateMachineCnt; y++)
			{
				ChildAnimatorStateMachine childStateMachine = subStateMachine[y];
				string subLayerName = prefix + "." + childStateMachine.stateMachine.name;
				string subLayerDisplayName = prefix + "/" + childStateMachine.stateMachine.name;
				Editor_AddStateFromStateMachine(stateList, subLayerName, subLayerDisplayName, layerIndex, childStateMachine.stateMachine);
			}
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		private static void Editor_AddStateFromStateMachine(List<AnimatorStateCache> stateList, string prefix, string displayPrefix, int layerIndex, Object animatorStateMachine)
		{
			// In order to get rid of compiler error (No editor)
			AnimatorStateMachine asm = animatorStateMachine as AnimatorStateMachine;

			ChildAnimatorState[] childs = asm.states;
			int childCnt = childs.Length;
			for (int x = 0; x < childCnt; x++)
			{
				AnimatorState state = childs[x].state;
				string path = prefix + "." + state.name;
				string displayPath = prefix + "/" + state.name;
				stateList.Add(new AnimatorStateCache
				{
					motion = state.motion,
					displayPath = displayPath,
					path = path,
					layer = layerIndex,
					fullNameHash = Animator.StringToHash(path),
					stateName = state.name,
					shortNameHash = Animator.StringToHash(state.name),
				});
				// Debug.Log(path + ", hash = "+ state.nameHash + " == "+ Animator.StringToHash(path));
			}

			ChildAnimatorStateMachine[] subStateMachine = asm.stateMachines;
			int subStateMachineCnt = subStateMachine.Length;
			for (int y = 0; y < subStateMachineCnt; y++)
			{
				ChildAnimatorStateMachine childStateMachine = subStateMachine[y];
				string subLayerName = prefix + "." + childStateMachine.stateMachine.name;
				string subLayerDisplayName = prefix + "/" + childStateMachine.stateMachine.name;
				Editor_AddStateFromStateMachine(stateList, subLayerName, subLayerDisplayName, layerIndex, childStateMachine.stateMachine);
			}
		}

	}
}