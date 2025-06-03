using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Kit2
{
	[System.Serializable]
	public abstract class AnimatorParam
	{
		public string m_ParamString;
		
		/// Auto fill in by <see cref="AnimatorParamDrawer"/>
		[ReadOnly] public int m_ParamId;

		private bool m_InitHash = false;
		public int hash
		{
			get
			{
				if (!m_InitHash)
				{
					m_InitHash = true;
					m_ParamId = Animator.StringToHash(m_ParamString);
				}
				return m_ParamId;
			}
		}

		public virtual void InitAnimator(Animator animator)
		{
			if (!m_InitAnimator)
			{
				m_InitAnimator = true;
				m_Animator = animator;
				HasParam = m_Animator.HasParameter(m_ParamString, parameterType);
				if (!HasParam)
					Debug.LogWarning($"Missing paramter {m_ParamString} on animator", animator);
			}
			else if (m_Animator != animator)
				throw new UnityException("Double init " + GetType().Name);
		}

		protected abstract AnimatorControllerParameterType parameterType { get; }
		protected bool m_InitAnimator = false;
		protected Animator m_Animator = null;
		public bool HasParam { get; set; } = false;

		
		public static implicit operator int (AnimatorParam param) => param.hash;

		public static implicit operator string (AnimatorParam param) => param.m_ParamString;

		public static implicit operator bool(AnimatorParam param) => param.HasParam;
	}

	[System.Serializable]
	public class AnimatorStateParam : AnimatorParam
	{
		protected AnimatorStateStruct state;
		public int layerIndex => state.layerIndex;
		protected override AnimatorControllerParameterType parameterType => throw new NotImplementedException("convert hash only");

		public override void InitAnimator(Animator animator)
		{
			// base.InitAnimator(animator);
			if (!m_InitAnimator)
			{
				m_InitAnimator = true;
				m_Animator = animator;
				HasParam = animator.TryGetState(m_ParamString, out state);
				if (!HasParam)
					Debug.LogWarning($"Missing state {m_ParamString} on animator", animator);
			}
			else if (m_Animator != animator)
				throw new UnityException("Double init " + GetType().Name);
		}

		public static implicit operator AnimatorStateParam(string paramString)
			=> new AnimatorStateParam() { m_ParamString = paramString, m_ParamId = Animator.StringToHash(paramString) };
	}
	[System.Serializable]
	public class AnimatorFloatParam : AnimatorParam
	{
		protected override AnimatorControllerParameterType parameterType => AnimatorControllerParameterType.Float;

		public void Set(float value)
		{
			if (!m_InitAnimator)
				throw new UnityException($"Require to Call {nameof(InitAnimator)}() before assign any value.");
			if (HasParam)
				m_Animator.SetFloat(hash, value);
		}

		public void Set(float value, float dampTime, float deltaTime)
		{
			if (!m_InitAnimator)
				throw new UnityException($"Require to Call {nameof(InitAnimator)}() before assign any value.");
			if (HasParam)
				m_Animator.SetFloat(hash, value, dampTime, deltaTime);
		}

		public static implicit operator AnimatorFloatParam(string paramString)
			=> new AnimatorFloatParam() { m_ParamString = paramString, m_ParamId = Animator.StringToHash(paramString) };
	}
	[System.Serializable]
	public class AnimatorBoolParam : AnimatorParam
	{
		protected override AnimatorControllerParameterType parameterType => AnimatorControllerParameterType.Bool;

		public void Set(bool value)
		{
			if (!m_InitAnimator)
				throw new UnityException($"Require to Call {nameof(InitAnimator)}() before assign any value.");
			if (HasParam)
				m_Animator.SetBool(hash, value);
		}

		public static implicit operator AnimatorBoolParam(string paramString)
			=> new AnimatorBoolParam() { m_ParamString = paramString, m_ParamId = Animator.StringToHash(paramString) };
	}
	[System.Serializable]
	public class AnimatorIntParam : AnimatorParam
	{
		protected override AnimatorControllerParameterType parameterType => AnimatorControllerParameterType.Int;

		public void Set(int value)
		{
			if (!m_InitAnimator)
				throw new UnityException($"Require to Call {nameof(InitAnimator)}() before assign any value.");
			if (HasParam)
				m_Animator.SetInteger(hash, value);
		}

		public static implicit operator AnimatorIntParam(string paramString)
			=> new AnimatorIntParam() { m_ParamString = paramString, m_ParamId = Animator.StringToHash(paramString) };
	}
	[System.Serializable]
	public class AnimatorTriggerParam : AnimatorParam
	{
		protected override AnimatorControllerParameterType parameterType => AnimatorControllerParameterType.Trigger;

		public void Set()
		{
			if (!m_InitAnimator)
				throw new UnityException($"Require to Call {nameof(InitAnimator)}() before assign any value.");
			if (HasParam)
				m_Animator.SetTrigger(hash);
		}

		public void Reset()
		{
			if (!m_InitAnimator)
				throw new UnityException($"Require to Call {nameof(InitAnimator)}() before assign any value.");
			if (HasParam)
				m_Animator.ResetTrigger(hash);
		}

		public static implicit operator AnimatorTriggerParam(string paramString)
			=> new AnimatorTriggerParam() { m_ParamString = paramString, m_ParamId = Animator.StringToHash(paramString) };
	}
}