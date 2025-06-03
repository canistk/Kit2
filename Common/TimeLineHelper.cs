using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
namespace Kit2
{
	[RequireComponent(typeof(PlayableDirector))]
	public class TimeLineHelper : MonoBehaviour
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

		private struct PlayInfo
		{
			public readonly bool valid;
			public readonly DirectorWrapMode extrapolationMode;
			public readonly DirectorUpdateMode timeUpdateMode;
			public bool isPlayedOnce;
			public bool pass50;
			public double lastTime;

			public PlayInfo(PlayableDirector pd)
			{
				this.valid = true;
				this.extrapolationMode = pd.extrapolationMode;
				this.timeUpdateMode = pd.timeUpdateMode;
				this.isPlayedOnce = false;
				this.pass50 = false;
				this.lastTime = -1f;
			}
		}
		private PlayInfo m_PlayInfo = default;
		public event System.Action EVENT_PlayedOneCycle;

		private void Reset()
		{
			ReferenceEquals(playableDirector, null);
		}

		private void Awake()
		{
			playableDirector.played += OnPlay;
			playableDirector.paused += OnPaused;
			playableDirector.stopped += OnStopped;
			if (playableDirector.playOnAwake)
			{
				
				OnPlay(playableDirector);
			}
		}
		private void OnDestroy()
		{
			playableDirector.played -= OnPlay;
			playableDirector.paused -= OnPaused;
			playableDirector.stopped -= OnStopped;
		}

		private void Update()
		{
			DefinePlayOnceCycle();
		}

		private void OnPlay(PlayableDirector obj)
		{
			m_PlayInfo = new PlayInfo(obj);
		}

		private void OnPaused(PlayableDirector obj)
		{
		}

		private void OnStopped(PlayableDirector obj)
		{
			m_PlayInfo = default;
		}

		private void DefinePlayOnceCycle()
		{
			var p = playableDirector;
			var isPlaying = p.state switch
			{
				PlayState.Paused => false,
				PlayState.Playing => true,
				PlayState.Delayed => false,
				_ => throw new System.NotImplementedException(),
			};

			if (m_PlayInfo.isPlayedOnce)
				return;

			if (m_PlayInfo.lastTime == p.time)
				return;

			var isRewind = m_PlayInfo.lastTime > p.time;
			if (isRewind)
			{
				m_PlayInfo.lastTime = p.time;
				return;
			}

			var pt = Mathf.Clamp01(System.Convert.ToSingle(p.time / p.duration));
			if (!m_PlayInfo.pass50 && pt >= 0.5f)
			{
				m_PlayInfo.pass50 = true;
			}

			var halfSecondB4End = p.time >= (p.duration - 0.5f);
			switch (p.extrapolationMode)
			{
				case DirectorWrapMode.Hold:
					if (halfSecondB4End)
						_ReachTheEnd();
					break;
				case DirectorWrapMode.Loop:
					if (m_PlayInfo.pass50 && isRewind)
						_ReachTheEnd();
					break;
				case DirectorWrapMode.None:
					if (m_PlayInfo.pass50 && isRewind)
						_ReachTheEnd();
					break;
				default:
					throw new System.NotImplementedException();
			}
			
			void _ReachTheEnd()
			{
				m_PlayInfo.isPlayedOnce = true;
				EVENT_PlayedOneCycle?.TryCatchDispatchEventError(o => o?.Invoke());
			}
		}

		public void GotoNormalize(float normalizedTime01)
		{
			var pt = Mathf.Clamp01(normalizedTime01);
			var time = playableDirector.duration * pt;
			Goto(time);
		}

		public void Goto(double time)
		{
			playableDirector.time = time;
			if (!playableDirector.gameObject.activeInHierarchy)
				return;
			playableDirector.Evaluate();
		}
	}
}