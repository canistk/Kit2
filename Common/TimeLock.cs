using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Kit2
{
    public class TimeLock
    {
        public bool IsLocked => m_LockRequests.Count > 0;
        public int Count => m_LockRequests.Count;
		public event Action EVENT_Locked, EVENT_Released;

        private class LockRequest
        {
            public readonly object Caller;
            public readonly float StartTime;
            public readonly float Duration;
            public readonly CancellationTokenSource src;
            public bool IsExpired => m_Expired;
            private bool m_Expired;
            private Action m_Callback;
            public LockRequest(object caller, float duration, System.Action cancelCallback)
            {
                this.Caller = caller;
                this.Duration = duration;
                this.StartTime = Time.realtimeSinceStartup;
                this.m_Expired = false;
                this.src = new CancellationTokenSource((int)(duration * 1000));
                this.src.Token.Register(_Completed);
                //this.src.CancelAfter((int)(duration * 1000));
                m_Callback = cancelCallback;
				//Debug.Log("Lock Construct completed.");
			}

            private void _Completed()
            {
                if (m_Expired)
                    throw new System.Exception("Double dispose.");
                m_Expired = true;
                if (m_Callback != null)
                    m_Callback.Invoke();
                //Debug.Log($"Job Completed after {Duration}sec, isExpired = {IsExpired}");
            }

            public void Cancel()
            {
                if (src.IsCancellationRequested)
                    return;
                src.Cancel();
                src.Dispose();
				if (m_Callback != null)
					m_Callback.Invoke();
				//Debug.Log($"Cancel lock {Caller}");
			}
        }

        private List<LockRequest> m_LockRequests = new List<LockRequest>();
        
		public void Add(object caller, float duration)
        {
            var request = new LockRequest(caller, duration, InternalTryRelease);
            var wasEmpty = m_LockRequests.Count == 0;
			m_LockRequests.Add(request);
            if (wasEmpty && m_LockRequests.Count == 1)
            {
                EVENT_Locked?.Invoke();
            }
		}
        public void Remove(object caller)
        {
            int i = m_LockRequests.Count;
			while (i --> 0)
            {
                if (!m_LockRequests[i].Caller.Equals(caller))
                    continue;
                m_LockRequests[i].Cancel();
                /// cancel will trigger <see cref="InternalTryRelease"/>
                /// <seealso cref="LockRequest"/> constructor
                break;
			}
		}

        public void Clear()
        {
            var cnt = m_LockRequests.Count;
            m_LockRequests.Clear();
            if (cnt > 0 && m_LockRequests.Count == 0)
                EVENT_Released?.Invoke();
		}

        private void InternalTryRelease()
        {
			var i = m_LockRequests.Count;
            var before = i;
			while (i-- > 0)
			{
				if (!m_LockRequests[i].IsExpired)
					continue;
				m_LockRequests.RemoveAt(i);
			}
			if (before > 0 && m_LockRequests.Count == 0)
			{
				EVENT_Released?.Invoke();
			}
		}
	}
}
