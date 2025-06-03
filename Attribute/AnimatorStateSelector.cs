using System.Collections.Generic;
using UnityEngine;

namespace Kit2
{
	public class RefAnimator : PropertyAttribute
	{
		public string path;
		public RefAnimator(string path)
		{
			this.path = path;
		}
	}

	/// <summary>
	/// Use <see cref="RefAnimator"/> to pre-defined the animator in script.
	/// </summary>
	[System.Serializable]
	public class AnimatorStateSelector
	{
		public Animator m_Animator;
		public int		m_SelectedHash;
		public int		m_LayerIndex;
		public string	m_DisplayPath;
		public bool		Valid
		{
			get
			{
				return m_Animator != null &&
					m_Animator.HasState(m_LayerIndex, m_SelectedHash);
			}
		}
	}

	public static class AnimatorStateSelectorUtils
	{
		public static void CrossFade(this Animator animator, AnimatorStateSelector selector, AnimatorCrossFadeSetting setting)
		{
			if (selector != null &&
				animator == selector.m_Animator)
			{
				animator.CrossFade(selector.m_SelectedHash, selector.m_LayerIndex, setting);
			}
		}

		public static void CrossFadeInFixedTime(this Animator animator, AnimatorStateSelector selector, AnimatorCrossFadeFixedSetting setting)
		{
			if (selector != null &&
				animator == selector.m_Animator)
			{
				animator.CrossFadeInFixedTime(selector.m_SelectedHash, selector.m_LayerIndex, setting);
			}
		}
	}


	[System.Serializable]
	public class AnimatorStateCache : AnimatorStateCacheLite
	{
		public Motion motion;
	}
	[System.Serializable]
	public class AnimatorStateCacheLite
	{
		public string displayPath;
		public int layer;
		public string stateName;
		public int shortNameHash;
		public string path;
		public int fullNameHash; // Key

		public override string ToString()
		{
			return $"[{layer}]\"{displayPath}\", F={fullNameHash},S={shortNameHash}";
		}
	}

	public struct AnimatorStateStruct
	{
		public string name;
		public int shortNameHash;
		public int layerIndex;
	}

	[System.Serializable]
	public struct AnimatorCrossFadeSetting
	{
		[Range(0f, 1f)] public float normalizedTransitionDuration;
		[Range(0f, 1f)] public float normalizedTimeOffset;
		[Range(0f, 1f)] public float normalizedTransitionTime;
	}
	[System.Serializable]
	public struct AnimatorCrossFadeFixedSetting
	{
		public float fixedTransitionDuration;
		public float fixedTimeOffset;
		public float normalizedTransitionTime;
	}
}