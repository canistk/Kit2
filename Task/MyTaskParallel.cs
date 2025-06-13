using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kit2.Tasks
{
	public class MyTaskParallel : MyTaskWithState
	{
		private bool[] markCompleted;
		private MyTask[] subTasks;
		public MyTaskParallel(params MyTask[] subTasks)
		{
			this.subTasks = subTasks;
			this.markCompleted = new bool[subTasks.Length]; // default false
		}

		protected override bool ContinueOnNextCycle()
		{
			for (int i = 0; i < markCompleted.Length; ++i)
			{
				if (markCompleted[i])
					continue;
				try
				{
					markCompleted[i] = !subTasks[i].Execute();
				}
				catch (System.Exception ex)
				{
					Debug.LogException(ex);
					markCompleted[i] = true;
				}
			}

			int x = markCompleted.Length, completed = 0;
			while (x-- > 0)
			{
				if (markCompleted[x])
					++completed;
			}

			return completed < subTasks.Length;
		}

		protected override void OnComplete() { }

		protected override void OnEnter() { Init(); }

		public override void Reset()
		{
			base.Reset();
			Init();
		}

		private void Init()
		{
			for (int i = 0; i < markCompleted.Length; ++i)
			{
				markCompleted[i] = false;
			}
		}
	}


}
