using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
namespace Kit2
{
    public static class ThreadExtend
    {
		/// <summary>
		/// Wrapa System.Action into a Callback method
        /// and run it asynchronously.
		/// </summary>
		/// <param name="action"></param>
		/// <param name=""></param>
		public static void Run(System.Action action,
            System.Action onComplete = null,
            System.Action<System.Exception> onError = null)
        {
            if (action == null)
            {
                Debug.LogError("Action cannot be null.");
                return;
            }

            Task.Run(action).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    var ex = t.Exception?.GetBaseException();
                    onError?.Invoke(ex);
                }
                else if (t.IsCanceled)
                {
                    var ex = new System.OperationCanceledException("Task was canceled.");
                    onError?.Invoke(ex);
				}
                else
                {
                    onComplete?.Invoke();
				}
            });
        }
    }
}
