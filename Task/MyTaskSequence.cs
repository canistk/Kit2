using UnityEngine;
namespace Kit2.Task
{

	public class MyTaskSequence : MyTaskWithState
	{
		private MyTask[] subTasks;
		private int index;
		public MyTaskSequence(params MyTask[] subTasks)
		{
			this.subTasks = subTasks;
		}

		protected override bool ContinueOnNextCycle()
		{
			if (subTasks.Length == 0)
				return false;

			try
			{
				var t = subTasks[index];
				if (t.Execute())
					return true;

				if (index < subTasks.Length)
					++index;
			}
			catch (System.Exception ex)
			{
				Debug.LogException(ex);
				++index;
			}
			return index < subTasks.Length;
		}
		protected override void OnEnter() { index = 0; }

		protected override void OnComplete() { }

		public override void Reset()
		{
			base.Reset();
			index = 0;
			for (int i = 0; i < subTasks.Length; ++i)
			{
				if (subTasks[i] == null)
					continue;
				subTasks[i].Reset();
			}
		}
	}

}
