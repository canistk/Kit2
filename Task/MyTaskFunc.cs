using UnityEngine;

namespace Kit2.Task
{
	public class MyTaskFunc : MyTask
	{
		private readonly System.Func<bool> func;
		public MyTaskFunc(System.Func<bool> action)
		{
			this.func = action;
		}

		protected override bool InternalExecute()
		{
			try
			{
				var rst = func.Invoke();
				return rst;
			}
			catch (System.Exception ex)
			{
				Debug.LogException(ex);
				return false;
			}
		}
	}
}
