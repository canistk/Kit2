using UnityEngine;
using System.Collections.Generic;
namespace Kit2
{
	public static class AnimatorExtend
	{
		public static bool TryGetState(this Animator animator, string stateName, out AnimatorStateStruct state)
		{
			if (animator.isInitialized && !string.IsNullOrEmpty(stateName))
			{
				int hash = Animator.StringToHash(stateName);
				for (int i = 0; i < animator.layerCount; i++)
				{
					if (animator.HasState(i, hash))
					{
						state.layerIndex = i;
						state.name = stateName;
						state.shortNameHash = hash;
						return true;
					}
				}
			}
			state = default;
			return false;
		}

		public static void CrossFade(this Animator animator,int stateHashName, int layer, AnimatorCrossFadeSetting setting)
        {
			animator.CrossFade(stateHashName,
				setting.normalizedTransitionDuration,
				layer,
				setting.normalizedTimeOffset,
				setting.normalizedTransitionTime);
		}

		public static void CrossFadeInFixedTime(this Animator animator, int stateHashName, int layer, AnimatorCrossFadeFixedSetting setting)
		{
			animator.CrossFadeInFixedTime(stateHashName,
				setting.fixedTransitionDuration,
				layer,
				setting.fixedTimeOffset,
				setting.normalizedTransitionTime);
		}
		
		public static bool HasParameter(this Animator animator, string parameterName)
			=> animator.HasParameter(parameterName, out _);

		public static bool HasParameter(this Animator animator, string parameterName, out AnimatorControllerParameterType type)
		{
			int hash = Animator.StringToHash(parameterName);
			foreach (AnimatorControllerParameter param in animator.parameters)
			{
				if (param.nameHash != hash)
					continue;
				type = param.type;
				return true;
			}
			type = (AnimatorControllerParameterType) 0;
			return false;
		}
		
		public static bool HasParameter(this Animator animator, string parameterName, AnimatorControllerParameterType type)
		{
			int hash = Animator.StringToHash(parameterName);
			foreach (AnimatorControllerParameter param in animator.parameters)
			{
				if (param.type == type && param.nameHash == hash)
					return true;
			}
			return false;
		}

		public static bool HasState(this Animator animator, string stateName, int layerIndex)
		{
			int stateId = Animator.StringToHash(stateName);
			return animator.HasState(layerIndex, stateId);
		}

		public static bool IsInvalid(this AnimatorStateInfo o)
		{
			return
				o.loop			!= false &&
				o.tagHash		!= 0 &&
				o.shortNameHash	!= 0 &&
				o.fullPathHash	!= 0;
				//o.length		!= 0;
		}
	}
}