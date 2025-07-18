using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using Unity.EditorCoroutines.Editor;
#endif
namespace Kit2.Tasks
{
	/// <summary>
	/// task can be execute without GameObject and Editor
	/// </summary>
	public static class MyEditorTaskHandler
	{
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void Add(MyTaskBase task)
		{
#if UNITY_EDITOR
			if (s_EditorProgress == null)
				s_EditorProgress = EditorCoroutineUtility.StartCoroutineOwnerless(EditorLoop());
			s_EditorTasks.Add(task);
#endif
		}
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void ClearUp()
		{
#if UNITY_EDITOR
			Editor_CleanUp();
#endif
		}

#if UNITY_EDITOR

		public static int TaskCount => s_EditorTasks?.Count ?? 0;
		public static int Executing => TaskCount > 0 ? m_ExecuteIndex : -1;
#else
		public static int TaskCount => 0;
		public static int Executing => -1;
#endif

#if UNITY_EDITOR
		private static List<MyTaskBase> s_EditorTasks = new List<MyTaskBase>(8);
		private static int m_ExecuteIndex;
		private static EditorCoroutine s_EditorProgress = null;
		private static IEnumerator EditorLoop()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode ||
				s_EditorTasks == null ||
				s_EditorTasks.Count == 0)
			{
				Editor_CleanUp();
				yield break;
			}

			int MaxQuota = 10000;

			while (s_EditorTasks.Count > 0)
			{
				var anchor = Time.realtimeSinceStartupAsDouble;
				int quota = MaxQuota;
				int i = s_EditorTasks.Count;
				if (i == 0)
				{
					yield return null;
					continue;
				}

				while (i-- > 0)
				{
					m_ExecuteIndex = i;
					if (s_EditorTasks.Count <= i)
						break; // clean up, or modify by others.

					var task = s_EditorTasks[i];

					if (task is null ||
						(task is MyTask t0 && t0.isDisposed))
					{
						s_EditorTasks.RemoveAt(i);
						continue;
					}

					try
					{
						if (!task.Execute())
						{
							if (task is MyTask t1)
								t1.Abort();
							s_EditorTasks.RemoveAt(i);
						}
					}
					catch (Exception ex)
					{
						ex.DeepLogInvocationException($"TaskError:{task}");
						s_EditorTasks.RemoveAt(i); // broken fail to execute on next cycle.
					}

					var diff = Time.realtimeSinceStartupAsDouble - anchor;
					if (diff >= 1f)
					{
						// timeout
						anchor = Time.realtimeSinceStartupAsDouble;
						yield return null;
					}
					if (--quota <= 0 || // out of quota
						i == 0) // out of tasks
						yield return null;
				}

			}

			// Clean up handling.
			if (s_EditorTasks.Count == 0)
			{
				Editor_CleanUp();
			}
		}

		private static void Editor_CleanUp()
		{
			Debug.Log("EditorTaskHandler completed.");
			s_EditorTasks.Clear();
			EditorUtility.ClearProgressBar();
			if (s_EditorProgress != null)
				EditorCoroutineUtility.StopCoroutine(s_EditorProgress);
			s_EditorProgress = null;
		}
#endif
	}
}
