using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace Kit2
{
	[CustomEditor(typeof(HumanoidHandler))]
    public class HumanoidHandlerEditor : EditorBase
    {
		protected override void OnEnable()
		{
			base.OnEnable();
			SceneView.duringSceneGui += OnDrawSceneView;
		}
		protected override void OnDisable()
		{
			base.OnDisable();
			SceneView.duringSceneGui -= OnDrawSceneView;
		}

		private void OnDrawSceneView(SceneView obj)
		{
			// var targets = serializedObject.targetObjects;
			foreach (var t in targets)
			{
				if (t is not HumanoidHandler h ||
					h.m_Animator == null ||
					!h.m_Animator.isHuman)
					continue;
				DrawSkeleten(h, h.m_Animator);
			}
		}

		private static readonly Dictionary<HumanBodyBones, HumanBodyBones> s_ParentBoneDict = new Dictionary<HumanBodyBones, HumanBodyBones>
		{
			{ HumanBodyBones.Head,			HumanBodyBones.Neck },
			{ HumanBodyBones.Neck,			HumanBodyBones.UpperChest },
			{ HumanBodyBones.UpperChest,	HumanBodyBones.Chest },
			{ HumanBodyBones.Chest,			HumanBodyBones.Spine },
			{ HumanBodyBones.Spine,			HumanBodyBones.Hips },
			{ HumanBodyBones.LeftUpperArm,	HumanBodyBones.Chest },
			{ HumanBodyBones.RightUpperArm,	HumanBodyBones.Chest },
			{ HumanBodyBones.LeftLowerArm,	HumanBodyBones.LeftUpperArm },
			{ HumanBodyBones.RightLowerArm,	HumanBodyBones.RightUpperArm },
			{ HumanBodyBones.LeftHand,		HumanBodyBones.LeftLowerArm },
			{ HumanBodyBones.RightHand,		HumanBodyBones.RightLowerArm },
			{ HumanBodyBones.LeftUpperLeg,	HumanBodyBones.Hips },
			{ HumanBodyBones.RightUpperLeg,	HumanBodyBones.Hips },
			{ HumanBodyBones.LeftLowerLeg,	HumanBodyBones.LeftUpperLeg },
			{ HumanBodyBones.RightLowerLeg,	HumanBodyBones.RightUpperLeg },
			{ HumanBodyBones.LeftFoot,		HumanBodyBones.LeftLowerLeg },
			{ HumanBodyBones.RightFoot,		HumanBodyBones.RightLowerLeg },
			{ HumanBodyBones.LeftToes,		HumanBodyBones.LeftFoot },
			{ HumanBodyBones.RightToes,		HumanBodyBones.RightFoot },
			{ HumanBodyBones.LeftThumbDistal, HumanBodyBones.LeftThumbIntermediate },
			{ HumanBodyBones.LeftThumbIntermediate, HumanBodyBones.LeftThumbProximal },
			{ HumanBodyBones.LeftThumbProximal, HumanBodyBones.LeftHand },
			{ HumanBodyBones.LeftIndexDistal, HumanBodyBones.LeftIndexIntermediate },
			{ HumanBodyBones.LeftIndexIntermediate, HumanBodyBones.LeftIndexProximal },
			{ HumanBodyBones.LeftIndexProximal, HumanBodyBones.LeftHand },
			{ HumanBodyBones.LeftMiddleDistal, HumanBodyBones.LeftMiddleIntermediate },
			{ HumanBodyBones.LeftMiddleIntermediate, HumanBodyBones.LeftMiddleProximal },
			{ HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftHand },
			{ HumanBodyBones.LeftRingDistal, HumanBodyBones.LeftRingIntermediate },
			{ HumanBodyBones.LeftRingIntermediate, HumanBodyBones.LeftRingProximal },
			{ HumanBodyBones.LeftRingProximal, HumanBodyBones.LeftHand },
			{ HumanBodyBones.LeftLittleDistal, HumanBodyBones.LeftLittleIntermediate },
			{ HumanBodyBones.LeftLittleIntermediate, HumanBodyBones.LeftLittleProximal },
			{ HumanBodyBones.LeftLittleProximal, HumanBodyBones.LeftHand },
			{ HumanBodyBones.RightThumbDistal, HumanBodyBones.RightThumbIntermediate },
			{ HumanBodyBones.RightThumbIntermediate, HumanBodyBones.RightThumbProximal },
			{ HumanBodyBones.RightThumbProximal, HumanBodyBones.RightHand },
			{ HumanBodyBones.RightIndexDistal, HumanBodyBones.RightIndexIntermediate },
			{ HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexProximal },
			{ HumanBodyBones.RightIndexProximal, HumanBodyBones.RightHand },
			{ HumanBodyBones.RightMiddleDistal, HumanBodyBones.RightMiddleIntermediate },
			{ HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleProximal },
			{ HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightHand },
			{ HumanBodyBones.RightRingDistal, HumanBodyBones.RightRingIntermediate },
			{ HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingProximal },
			{ HumanBodyBones.RightRingProximal, HumanBodyBones.RightHand },
			{ HumanBodyBones.RightLittleDistal, HumanBodyBones.RightLittleIntermediate },
			{ HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleProximal },
			{ HumanBodyBones.RightLittleProximal, HumanBodyBones.RightHand }
		};
		private KeyValuePair<Animator, HumanBodyBones> m_Selected;
		private void DrawSkeleten(HumanoidHandler obj, Animator animator)
		{
			if (animator == null)
				return;
			if (!obj.m_EnableHandler)
				return;

			using (new HandlesColorScope(Color.cyan))
			for (var i = HumanBodyBones.Hips; i < HumanBodyBones.LastBone; ++i)
			{
				var bone = animator.GetBoneTransform(i);
				if (bone == null)
					continue;
				var wasSelected = m_Selected.Key == animator && m_Selected.Value == i;
				var hSize = HandleUtility.GetHandleSize(bone.position);
				var p = bone.position;
				var r = bone.rotation;
				using (var checker = new EditorGUI.ChangeCheckScope())
				{
					if (wasSelected)
					{
						HandlesExtend.DrawHandleBaseOnTools(ref p, ref r);
						if (checker.changed)
						{
							
							var grpName = $"Change Bone {bone.name} Coordinate";
							Undo.RecordObject(bone, grpName);
							bone.SetPositionAndRotation(p, r);
							if (wasSelected)
							{
								Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
							}
							EditorUtility.SetDirty(bone);
						}
					}
					else
					{
						HandlesExtend.DrawSelectableDot(p, r, hSize * 0.2f,
							out var isHover, out var isSelected);
						if (checker.changed || isSelected)
						{
							m_Selected = new KeyValuePair<Animator, HumanBodyBones>(animator, i);
						}
					}
				}

				if (s_ParentBoneDict.TryGetValue(i, out var parentBone) &&
					animator.GetBoneTransform(parentBone) is Transform parent)
				{
					Handles.DrawDottedLine(parent.position, bone.position, hSize * 0.02f);
				}
			}
		}
	}

}