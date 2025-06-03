using UnityEngine;
using UnityEngine.Animations;

namespace Kit2.SMB
{
    public class RandomStateSMB : BaseSMB
	{
		public enum eRandomTiming
		{
			OnEnter = 0,
			EachCycle = 1,
			OnExit = 2,
		}

		[SerializeField] string m_ParameterName = "Random";
		[Tooltip("in outputInteger mode, Not include max")]
		[SerializeField] int m_NumberOfStates = 7;
		[SerializeField] bool m_OutputInteger = false;
		[Tooltip("The random trigger timing.")]
		[SerializeField] eRandomTiming m_RandomTiming = eRandomTiming.OnEnter;

		public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
		{
			if (m_RandomTiming == eRandomTiming.OnEnter)
				SetRandom(animator);
			base.OnStateMachineEnter(animator, stateMachinePathHash);
		}
		public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
		{
			if (m_RandomTiming == eRandomTiming.OnExit)
				SetRandom(animator);
			base.OnStateMachineExit(animator, stateMachinePathHash);
		}

		public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
		{
			// Set RandomIdle based on how many states there are.
			if (m_RandomTiming == eRandomTiming.OnEnter)
				SetRandom(animator);
			base.OnStateEnter(animator, stateInfo, layerIndex);
		}
		public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
		{
			if (m_RandomTiming == eRandomTiming.OnExit)
				SetRandom(animator);
			base.OnStateExit(animator, stateInfo, layerIndex);
		}

		public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
		{
			HandleRandomEachCycle(animator, stateInfo, layerIndex);
		}

		/// <summary>Skip trigger random, and wait until current cycle end.</summary>
		private bool m_WaitCycleEnd = false;
		private void HandleRandomEachCycle(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
		{
			if (m_RandomTiming == eRandomTiming.EachCycle)
			{
				if (!m_WaitCycleEnd && stateInfo.normalizedTime >= 0f && stateInfo.normalizedTime <= 0.5f)
				{
					if (m_WaitCycleEnd)
						return;
					m_WaitCycleEnd = true;
					SetRandom(animator);
				}
				else if (stateInfo.normalizedTime > 0.5f)
				{
					m_WaitCycleEnd = false;
				}
			}
		}

		private void SetRandom(Animator animator)
		{
			var hasParam = animator.HasParameter(m_ParameterName, out var pType);
			if (!hasParam)
			{
				Debug.LogError($"{GetType()}, parameter {m_ParameterName} not exist.");
				return;
			}
			if (m_OutputInteger)
			{
				// Return a random int within [minInclusive..maxExclusive)
				var rnd = Random.Range(0, m_NumberOfStates);
				if (pType == AnimatorControllerParameterType.Int)
					animator.SetInteger(m_ParameterName, rnd);
				if (pType == AnimatorControllerParameterType.Float)
					animator.SetFloat(m_ParameterName, (float)rnd);
			}
			else
			{
				// Returns a random float within [minInclusive..maxInclusive] (range is inclusive).
				var rnd = Random.Range(0f, (float)m_NumberOfStates);
				if (pType == AnimatorControllerParameterType.Int)
					animator.SetInteger(m_ParameterName, (int)rnd);
				if (pType == AnimatorControllerParameterType.Float)
					animator.SetFloat(m_ParameterName, rnd);
			}
		}
	}
}