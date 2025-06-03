using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kit2.Task
{
	/// <summary>
	/// A simple wrapper to wrap event base callback into task method.
	/// Therefore this task can only complete based on the giving event callback.
	/// </summary>
	/// <example>
	/// new MyTaskEventWrapper((self) => AnotherManager.EventOnTrigger => () => self.Completed());
	/// </example>
	public class MyTaskEventWrapper : MyTaskWithState
	{
		private bool m_Completed = false;
		public MyTaskEventWrapper(System.Action<MyTaskEventWrapper> waitForTrigger)
		{
			if (waitForTrigger == null)
				throw new System.ArgumentNullException();
			waitForTrigger.Invoke(this);
		}
		/// <summary>
		/// Callback function for external source.
		/// </summary>
		public void Complete()
		{
			if (m_Completed)
				return;
			m_Completed = true;
		}
		public override void Reset()
		{
			base.Reset();
			m_Completed = false;
		}
		protected override void OnEnter() { }
		protected override bool ContinueOnNextCycle() => !m_Completed;
		protected override void OnComplete() { }
	}
}
