using System.Collections;
using UnityEngine;
using Action = System.Action;
using System.Collections.Generic;
#if UNITY_EDITOR
using PrefabUtility = UnityEditor.PrefabUtility;
using PrefabType = UnityEditor.PrefabType;
using EditorUtility = UnityEditor.EditorUtility;
#endif

namespace Kit2
{
	public static class GameObjectExtend
	{
		#region GetComponent
		/// <summary>
		/// Memory vs performance trade off, assume component will not destroy on gameobject,
		/// optimize cache the component on gameobject via dictionary
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="go"></param>
		/// <param name="dictionary"></param>
		/// <returns></returns>
		public static T GetComponentCache<T>(this GameObject go, Dictionary<GameObject, T> dictionary) where T : class
		=> GetOrCacheComponentInDict<T>(0, go, dictionary);

		/// <summary>
		/// Memory vs performance trade off, assume component will not destroy on gameobject,
		/// optimize cache the component on gameobject via dictionary
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="go"></param>
		/// <param name="dictionary"></param>
		/// <returns></returns>
		public static T GetComponentInChildCache<T>(this GameObject go, Dictionary<GameObject, T> dictionary) where T : class
		=> GetOrCacheComponentInDict<T>(1, go, dictionary);

		/// <summary>
		/// Memory vs performance trade off, assume component will not destroy on gameobject,
		/// optimize cache the component on gameobject via dictionary
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="go"></param>
		/// <param name="dictionary"></param>
		/// <returns></returns>
		public static T GetComponentInParentCache<T>(this GameObject go, Dictionary<GameObject, T> dictionary) where T : class
		=> GetOrCacheComponentInDict<T>(2, go, dictionary);

		private static T GetOrCacheComponentInDict<T>(int method, GameObject go, Dictionary<GameObject, T> dictionary)
			where T : class
		{
			T rst = null;
			if (go != null && !dictionary.TryGetValue(go, out rst))
			{
				switch(method)
				{
					case 0: rst = go.GetComponent<T>(); break;
					case 1: rst = go.GetComponentInChildren<T>(); break;
					case 2: rst = go.GetComponentInParent<T>(); break;
					default: throw new System.NotImplementedException();
				}
				if (rst != null)
					dictionary.Add(go, rst);
			}
			return rst; // could be null.
		}

		/// <summary>
		/// Memory vs performance trade off, assume component will not destroy on gameobject,
		/// optimize cache the component on gameobject via dictionary
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="self"></param>
		/// <param name="dictionary"></param>
		/// <returns></returns>
		public static T[] GetComponentsCache<T>(this GameObject self, Dictionary<GameObject, T[]> dictionary) where T : class
		=> GetOrCacheComponentsInDict<T>(0, self, dictionary, false);

		/// <summary>
		/// Memory vs performance trade off, assume component will not destroy on gameobject,
		/// optimize cache the component on gameobject via dictionary
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="self"></param>
		/// <param name="dictionary"></param>
		/// <returns></returns>
		public static T[] GetComponentsInChildCache<T>(this GameObject self, Dictionary<GameObject, T[]> dictionary, bool includeInActive = false) where T : class
		=> GetOrCacheComponentsInDict<T>(1, self, dictionary, includeInActive);

		/// <summary>
		/// Memory vs performance trade off, assume component will not destroy on gameobject,
		/// optimize cache the component on gameobject via dictionary
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="self"></param>
		/// <param name="dictionary"></param>
		/// <returns></returns>
		public static T[] GetComponentsInParentCache<T>(this GameObject self, Dictionary<GameObject, T[]> dictionary, bool includeInActive = false) where T : class
		=> GetOrCacheComponentsInDict<T>(2, self, dictionary, includeInActive);

		private static T[] GetOrCacheComponentsInDict<T>(int method, GameObject go, Dictionary<GameObject, T[]> dictionary, bool includeInActive)
			where T : class
		{
			T[] rst = null;
			if (go != null && !dictionary.TryGetValue(go, out rst))
			{
				switch (method)
				{
					case 0: rst = go.GetComponents<T>(); break;
					case 1: rst = go.GetComponentsInChildren<T>(includeInActive); break;
					case 2: rst = go.GetComponentsInParent<T>(includeInActive); break;
					default: throw new System.NotImplementedException();
				}
				if (rst != null)
					dictionary.Add(go, rst);
			}
			return rst; // could be null.
		}
		#endregion // GetComponent

		#region Coroutine
		/// <summary>Start multiple coroutine and wait until all of them are finished.</summary>
		/// <param name="self"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public static Coroutine StartMultiCoroutine(this MonoBehaviour self, params IEnumerator[] args)
		{
			return self.StartCoroutine(_NestCoroutine(self, false, true, args));
		}

		/// <summary>Start multiple coroutine, and wait for *ANY* of them finished.</summary>
		/// <param name="self"></param>
		/// <param name="stopCoroutineOnExit">
		/// True = the Non-finished coroutine will be terminate, when *ANY* of this finished early.
		/// False = the couroutine will end, but the Non-finished coroutine will continue to process.
		/// </param>
		/// <param name="args">Coroutine ienumerator</param>
		/// <returns></returns>
		public static Coroutine WaitForAnyCoroutine(this MonoBehaviour self, bool stopCoroutineOnExit, params IEnumerator[] args)
		{
			return self.StartCoroutine(_NestCoroutine(self, true, stopCoroutineOnExit, args));
		}

		private static IEnumerator _NestCoroutine(this MonoBehaviour self, bool waitForAny, bool stopCoOnExit, params IEnumerator[] args)
		{
			int cnt = args.Length;
			int flag = cnt;
			Coroutine[] arr = new Coroutine[cnt];
			for (int i = 0; i < cnt; i++)
			{
				arr[i] = self.StartCoroutine(CoroutineWrapper(args[i], () => { flag--; }));
			}

			if (waitForAny)
				yield return new WaitUntil(() => flag != cnt);
			else
				yield return new WaitUntil(() => flag == 0);

			if (stopCoOnExit)
			{
				for (int i = 0; i < cnt; i++)
					self.StopCoroutine(arr[i]);
			}
			arr = null;
		}

		/// <summary>A wrapper to allow return a callback after the coroutine was finished.
		/// StopCoroutine() will break this feature.</summary>
		/// <param name="self"></param>
		/// <param name="args"></param>
		/// <param name="callback"></param>
		/// <returns></returns>
		public static Coroutine StartCoroutine(this MonoBehaviour self, IEnumerator args, Action callback)
		{
			return self.StartCoroutine(CoroutineWrapper(args, callback));
		}

		private static IEnumerator CoroutineWrapper(IEnumerator ienumerator, Action callback)
		{
			while (ienumerator.MoveNext())
				yield return ienumerator.Current;
			callback?.Invoke();
		}
		#endregion // Coroutine
	}
}