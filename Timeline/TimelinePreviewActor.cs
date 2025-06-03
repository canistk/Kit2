using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Kit2
{
	[RequireComponent(typeof(PlayableDirector))]
	public class TimelinePreviewActor : MonoBehaviour
	{
		[SerializeField] PlayableDirector m_PlayableDirector = null;
		public PlayableDirector playableDirector
		{
			get
			{
				if (m_PlayableDirector == null)
					m_PlayableDirector = GetComponent<PlayableDirector>();
				return m_PlayableDirector;
			}
		}

		[Header("Preview")]
		public Animator m_ActorPrefab = null;
		public Transform m_PreviewRoot = null;

		[Header("Rules")]
		public string m_ContainName = "";
		public bool m_OnlyEmptyTrack = true;



		[NonSerialized] public GameObject actor;

		private void Reset()
		{
			ReferenceEquals(playableDirector, null); // force init
		}
	}
}