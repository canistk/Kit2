using System.Collections.Generic;
using UnityEngine;

namespace Kit2.Tasks
{
	public class MyTaskDelay : MyTask
	{
		private readonly float m_Delay;
		private readonly bool m_RealTime;
		private System.Action m_Callback;
		private bool m_Started;
		private float m_GameTime;
		private System.DateTime m_DateTime;
		public MyTaskDelay(float delay, System.Action callback, bool realTime = false)
		{
			this.m_RealTime = realTime;
			// this.m_StartTime = realTime ? Time.realtimeSinceStartup : Time.timeSinceLevelLoad;
			this.m_Delay = Mathf.Max(0f, delay);
			this.m_Callback = callback;
		}

		protected override bool InternalExecute()
		{
			if (!m_Started)
			{
				m_Started = true;
				m_DateTime = System.DateTime.UtcNow;
				m_GameTime = Time.timeSinceLevelLoad;
			}

			double duration;
			if (m_RealTime)
			{
				duration = (System.DateTime.UtcNow - m_DateTime).TotalSeconds ;
			}
			else
			{
				duration = Time.timeSinceLevelLoad - m_GameTime;
			}

			
			var keepRun = duration < m_Delay;
			if (!keepRun)
			{
				// special handling for task reuseable.
				Reset();
			}
			return keepRun;
		}
		protected override void OnDisposing()
		{
			if (isCompleted)
			{
				// Only execute callback when task is completed.
				m_Callback?.Invoke();
			}
			base.OnDisposing();
		}

		public override void Reset()
		{
			base.Reset();
			m_Started = false;
			m_DateTime = default;
			m_GameTime = default;
		}
	}
}
