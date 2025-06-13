using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Kit2.Task
{
    /// <summary>
    /// task can be execute without GameObject
    /// <seealso cref="MyEditorTaskHandler"/> it's design for UNITY_EDITOR usage.
    /// </summary>
    public static class MyTaskHandler
    {
        /// <summary>
        /// task should able to abort or dispose any time by it self.
        /// handle it manually via <see cref="MyTask.Abort()"/> <see cref="MyTask.Dispose()"/>
        /// </summary>
        /// <param name="task">any type of task.</param>
        public static void Add(MyTaskBase task)
        {
            if (!Application.isPlaying)
                throw new Exception($"Task should not be run in editor mode. please use <MyEditorTaskHandler> instead.");
            if (tasks == null)
                throw new Exception();
            
            s_Tasks.Add(task);
        }

        public static void ClearUp()
        {
            if (!Application.isPlaying)
                throw new Exception($"Task should not be run in editor mode. please use <MyEditorTaskHandler> instead.");
            if (tasks == null)
                throw new Exception();

            for (int i = 0; i < tasks.Count; ++i)
            {
                if (tasks[i] == null)
                    continue;
                if (tasks[i] is MyTask t)
                {
                    try
                    {
                        t.Abort();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
                // else, don't care.
            }

            s_Tasks.Clear();
        }

        #region Runtime
        [RuntimeInitializeOnLoadMethod]
        private static void RuntimeInit()
        {
            if (s_Tasks != null)
                return;
            s_Tasks = new List<MyTaskBase>(8);
            UpdateManager.instance.Register(RuntimeUpdate);
        }

        private static List<MyTaskBase> s_Tasks = null;
        public static IReadOnlyList<MyTaskBase> tasks
        {
            get
            {
                if (s_Tasks == null)
                    RuntimeInit();
                return s_Tasks;
            }
        }
        public static int TaskCount => s_Tasks?.Count ?? 0;
        private static int m_ExecuteIndex;
        public static int Executing => TaskCount > 0 ? m_ExecuteIndex : -1;
        private static void RuntimeUpdate() => ManualParallelUpdate(s_Tasks);

        /// <summary>Allow implement task update on custom timing.</summary>
        /// <param name="_tasks"></param>
        public static void ManualParallelUpdate(List<MyTaskBase> _tasks)
        {
			if (_tasks == null || _tasks.Count == 0)
				return;

			var markDel = new List<int>(Mathf.RoundToInt((float)_tasks.Count / 2f));
			for (int i = 0; i < _tasks.Count; ++i)
			{
				var task = _tasks[i];
				if (task is null || (task is MyTask t0 && t0.isDisposed))
				{
					markDel.Add(i);
					continue;
				}

				try
				{
					if (!task.Execute())
					{
						markDel.Add(i);
                        if (task is MyTask t1)
                        {
                            // internal dispose task on completed.
                            t1.Abort();
                        }
					}
				}
				catch (Exception ex)
				{
					ex.DeepLogInvocationException($"TaskError:{task}");
					markDel.Add(i);
				}
			}

			if (markDel.Count > 0)
			{
				var i = markDel.Count;
				while (i-- > 0)
				{
					var idx = markDel[i];
					_tasks.RemoveAt(idx);
				}
			}
            markDel.Clear();
		}
		#endregion Runtime
	}
}