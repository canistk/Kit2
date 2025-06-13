using UnityEngine;
namespace Kit2.Tasks
{
	public class MyTaskAction : MyTask
	{
		private readonly System.Action callback;
		public MyTaskAction(System.Action action)
		{
			this.callback = action;
		}

		protected override bool InternalExecute()
		{
			try
			{
				callback.Invoke();
				// only execute one
				return false;
			}
			catch (System.Exception ex)
			{
				Debug.LogError(ex);
				return false;
			}
		}
	}

}
