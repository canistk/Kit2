using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Kit2.Tasks
{
	/// <summary>
	/// A task loop through all sub tasks.
	/// this will not end, unless manually call <see cref="MyTask.Abort"/>
	/// </summary>
	public class MyTaskLoop : MyTask
	{
		private MyTask[] subTasks;
		private int index;
		private bool resetAtEnd;
		public MyTaskLoop(bool resetAtEnd, params MyTask[] subTasks)
		{
			this.subTasks = subTasks;
			this.index = 0;
			this.resetAtEnd = resetAtEnd;
		}

		protected override bool InternalExecute()
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

			if (index < subTasks.Length)
				return true;

			// reset index
			if (resetAtEnd)
			{
				Reset();
			}
			else
			{
				index = 0;
			}
			return true;
		}

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
