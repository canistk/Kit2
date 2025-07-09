using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kit2
{
    public class TimeLockHelper : MonoBehaviour
    {
        private TimeLock m_TimeLock;

        [SerializeField] float duration = 1f;

		[ContextMenu("Time Set")]
        private void SetTimeLock()
        {
            if (m_TimeLock == null)
            {
                m_TimeLock = new TimeLock();
				m_TimeLock.EVENT_Locked += M_TimeLock_EVENT_Locked;
				m_TimeLock.EVENT_Released += M_TimeLock_EVENT_Released;
			}

			m_TimeLock.Add(this, duration);
		}

		[ContextMenu("Cancel Time Set")]
		private void CancelTimeLock()
		{
			m_TimeLock.Remove(this);
		}

		private void M_TimeLock_EVENT_Released()
		{
			Debug.LogWarning($"TimeLock released, {m_TimeLock.Count} locks left");
		}

		private void M_TimeLock_EVENT_Locked()
		{
			Debug.LogWarning($"TimeLock locked, {m_TimeLock.Count} locks acquired");
		}
	}
}
