using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Kit2
{
	/// <summary>
	/// Allow to drag humanoid skeleton in scene view directly.
	/// <see cref="HumanoidHandlerEditor"/> for more detail.
	/// </summary>
	[RequireComponent(typeof(Animator))]
    public class HumanoidHandler : MonoBehaviour
    {
		public Animator m_Animator;
		public bool m_EnableHandler = true;

		private void Reset()
		{
			m_Animator = GetComponent<Animator>();
		}

		private void Awake()
		{
			if (m_Animator == null)
				m_Animator = GetComponent<Animator>();
		}
	}
}