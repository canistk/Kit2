using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if USE_ADDRESSABLE
using UnityEngine.AddressableAssets;
#endif
using UnityEngine.ResourceManagement.AsyncOperations;
using Kit2.Tasks;
using System;

namespace Kit2.ObjectPool
{
	public interface ISelfDespawnable
	{
		public void SelfDespawn();
	}

	public interface ISpawnToken
	{
		public void OnSpawn(ISpawner pool);
		public void OnDespawn();
	}

	public interface ISpawner
	{
		public bool IsSpawned(GameObject token);

		public bool Despawn(GameObject go);
	}

	public enum eSrcType
	{
		GameObject = 0, // for runtime spawn
		Resources = 1,
		Addressable = 2,
		// StreamingAssets, TODO: implement custom loading from StreamingAssets.
	}

	[System.Obsolete("Use AsyncObjectPool instead.")]
	public class KxObjectPool : MonoBehaviour, System.IDisposable, ISpawner
	{
		#region Event
		public delegate void TokenEvent(GameObject token);
		public event TokenEvent Event_Spawn;
		public event TokenEvent Event_Despawn;
		#endregion Event

		public static List<KxObjectPool> Instances { get; } = new List<KxObjectPool>(10);

		#region System
		protected virtual bool ShouldAutoRegisterPool() => false;
		protected virtual void Awake()
		{
			PreloadOnDemend();
			if (ShouldAutoRegisterPool())
			{
				Instances.Add(this);
			}
		}
		public bool IsDestroy { get; private set; } = false;
		protected virtual void OnDestroy()
		{
			if (IsDestroy)
				return;
			Instances.Remove(this);
			Dispose();
			IsDestroy = true; // last step, so we can reparent all child
		}

		private List<MyTaskBase> m_Tasks = null;
		protected List<MyTaskBase> tasks
		{
			get
			{
				if (m_Tasks == null)
					m_Tasks = new List<MyTaskBase>(8);
				return m_Tasks;
			}
		}
		protected virtual void Update()
		{
			MyTaskHandler.ManualParallelUpdate(tasks);
		}

		protected static bool IsAppQuit { get; private set; } = false;
		protected void OnApplicationQuit()
		{
			if (!IsAppQuit)
				IsAppQuit = true;
		}
		#endregion System

		#region Preload
		[System.Serializable]
		public struct PreloadInfo
		{
			[Tooltip("Prefab")]
			public GameObject prefab;
			[Tooltip("Delay before start preload, useful for scene load, so that the scene can be loaded first.")]
			public float delayPreload;
			[Tooltip("The interval between each preload elements, distribute the performance overhead during GameObject.Instantiate")]
			public float interval;
			[Tooltip("Auto preload prefab(s) base on giving amount")]
			public int count;
		}

		[SerializeField] protected PreloadInfo[] m_PreloadConfig = { };

		protected void PreloadOnDemend()
		{
			if (m_Category != null)
				return;
			m_Category = new Dictionary<CombineKey, PrefabCategory>(10);
			if (m_PreloadConfig == null)
				m_PreloadConfig = new PreloadInfo[0];
			for (int i = 0; i < m_PreloadConfig.Length; ++i)
			{
				if (m_PreloadConfig[i].prefab == null)
					continue;
				Preload(m_PreloadConfig[i]);
			}
		}

		public void Preload(GameObject prefab, int preloadAmount, float interval)
			=> Preload(new PreloadInfo { prefab = prefab, interval = interval, count = preloadAmount });

		public void Preload(PreloadInfo preloadInfo)
		{
			var cat = GetOrAddCategory(preloadInfo.prefab, eSrcType.GameObject);
			var task = new PreloadTask(cat, preloadInfo, transform);
#if false
			tasks.Add(task);
#else
			MyTaskHandler.AsyncWrap(task);
#endif
		}

		private class PreloadTask : MyTaskWithState
		{
			private readonly Transform parent;
			private readonly PreloadInfo preloadInfo;
			private readonly PrefabCategory spawner;
			private float m_Last;
			private KeyValuePair<bool, float> m_FirstStart;
			public PreloadTask(PrefabCategory prefabInfo, PreloadInfo preloadInfo, Transform parent)
			{
				this.spawner = prefabInfo;
				this.preloadInfo = preloadInfo;
				this.parent = parent;
				this.m_FirstStart = default;
			}

			protected override void OnEnter() { }
			protected override bool ContinueOnNextCycle()
			{
				if (spawner == null)
					return false; // fatel error

				if (spawner.total >= preloadInfo.count)
				{
					Debug.Log($"[ObjectPool] Preload complete {preloadInfo.prefab.name}, amount = {preloadInfo.count}");
					return false; // early end for enough token spawn.
				}

				if (WaitForDelay())
					return true;
				
				var diff = Time.realtimeSinceStartup - m_Last;
				if (diff < preloadInfo.interval)
					return true; // wait for interval.

				m_Last = Time.realtimeSinceStartup;
				spawner.ReturnToken(spawner.NewToken(parent));
				return spawner.total < preloadInfo.count;
			}
			protected override void OnComplete() { }

			bool WaitForDelay()
			{
				if (!m_FirstStart.Key)
				{
					// first time, reference start time.
					m_FirstStart = new KeyValuePair<bool, float>(true, Time.realtimeSinceStartup);
					m_Last = Time.realtimeSinceStartup;
				}

				if (Time.realtimeSinceStartup - m_FirstStart.Value <= preloadInfo.delayPreload)
				{
					return true; // wait for delay.
				}
				return false;
			}
		}

		#endregion Preload

		#region Pooling
		private struct CombineKey: IEquatable<CombineKey>
		{
			public readonly eSrcType srcType;
			public readonly object stringOrObject;
			public readonly int hashCode;
			public CombineKey(eSrcType srcType, object stringOrObject)
			{
				this.srcType = srcType;
				this.stringOrObject = stringOrObject ?? throw new System.ArgumentNullException(nameof(stringOrObject), "stringOrObject cannot be null");
				this.hashCode = System.HashCode.Combine(srcType, stringOrObject.GetHashCode());
			}
			public override int GetHashCode() => hashCode;

			public override bool Equals(object obj)
			{
				return obj is CombineKey other && Equals(other);
			}
			public bool Equals(CombineKey other)
			{
				if (srcType != other.srcType)
					return false;
				var x = stringOrObject;
				var y = other.stringOrObject;
				if (x == null && y == null)
					return true;
				return x.Equals(y);
			}
		}
		private class PrefabCategory : System.IDisposable
		{
			public readonly CombineKey key;
			public eSrcType srcType => key.srcType;
			public readonly GameObject prefab;
			public AsyncOperationHandle<GameObject> handle;
			public readonly Transform parent;
			public HashSet<GameObject> activeObjs;
			public Queue<GameObject> deactiveObjs;
			private bool isDisposed;

			public PrefabCategory(CombineKey key, GameObject prefab, Transform _parent)
			{
				this.parent = _parent;
				this.activeObjs = new HashSet<GameObject>(10);
				this.deactiveObjs = new Queue<GameObject>(10);
				this.key = key;
				this.handle = default;
				this.prefab = prefab;

				if (prefab == null)
					throw new System.ArgumentNullException(nameof(prefab), "Prefab cannot be null");
			}

			public int total => activeObjs.Count + deactiveObjs.Count;

			public GameObject NewToken(Transform parent)
			{
				if (isDisposed)
					throw new System.Exception("kObjectPool was Disposed");
				bool oldState = prefab.activeSelf;
				prefab.SetActive(false);
				var token = Instantiate(prefab, parent);

				// U3D bug, enable this will also leave the token in scene
				//token.hideFlags = HideFlags.DontSave;

				token.name = token.name.Replace("Clone", $"#{total}");
				prefab.SetActive(oldState);
				return token;
			}

			public void GetOrAddToken(out GameObject token, in Transform parent, bool worldStay)
			{
				if (isDisposed)
					throw new System.Exception("kObjectPool was Disposed");
				token = null;
				while (deactiveObjs.Count > 0)
				{
					token = deactiveObjs.Dequeue();
					if (token != null)
					{
						if (token.transform.parent != parent)
							token.transform.SetParent(parent, worldStay);
						break;
					}
				}

				if (token == null)
				{
					// not enough
					token = NewToken(parent);
				}
				activeObjs.Add(token);
			}

			public void ReturnToken(GameObject token)
			{
				if (isDisposed)
					throw new System.Exception("kObjectPool was Disposed");
				if (token == null)
					return;
				try
				{
					if (token.activeSelf)
					{
						token.SetActive(false);
					}

					if (!token.transform.IsChildOf(parent))
						token.transform.SetParent(parent, true);
				}
				catch
				{ }
				finally
				{
					activeObjs.Remove(token);
					deactiveObjs.Enqueue(token);
				}
			}

			protected virtual void Dispose(bool disposing)
			{
				if (isDisposed)
					return;

				if (disposing)
				{
					// dispose managed state (managed objects)
					/// Should reparent all tokens to this parent.
					/// but always fail when the object pool being destroy.
					/// <see cref="ReturnToken(GameObject)"/>
					//foreach (var go in gos)
					//{
					//	if (go) ReturnToken(go);
					//}

					/// instead of reparenting and then destroy all tokens. directly destroy all tokens.
					var tokens = activeObjs.ToList().Concat(deactiveObjs).ToArray();
					activeObjs.Clear();
					deactiveObjs.Clear();
					for (int i = 0; i < tokens.Length; ++i)
					{
						if (tokens[i] == null)
							continue;
						try
						{
							GameObject.Destroy(tokens[i]);
						}
						catch { }
					}
					tokens = null; // clear reference
					if (srcType == eSrcType.Addressable)
					{
#if USE_ADDRESSABLE
						Addressables.Release(handle);
#endif
					}
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				activeObjs = null;
				deactiveObjs = null;
				isDisposed = true;
			}

			// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
			~PrefabCategory()
			{
				// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
				Dispose(disposing: false);
			}

			public void Dispose()
			{
				// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
				Dispose(disposing: true);
				System.GC.SuppressFinalize(this);
			}
		}

		private Dictionary<GameObject /*token*/, CombineKey> m_ActiveTokens = new Dictionary<GameObject, CombineKey>(100);
		private Dictionary<CombineKey, PrefabCategory> m_Category = null;
		private Dictionary<CombineKey, PrefabCategory> category
		{
			get
			{
				if (m_Category == null)
				{
					PreloadOnDemend();
				}
				return m_Category;
			}
		}

		public IEnumerable<GameObject> prefabs
		{
			get
			{
				foreach (var val in category.Values)
				{
					yield return val.prefab;
				}
			}
		}
		protected IEnumerable<GameObject> spawned => m_ActiveTokens.Keys;

		public bool TryGetPrefab(object prefabOrString, eSrcType srcType, out GameObject prefab)
		{
			prefab = null;
			if (prefabOrString == null)
			{
				Debug.LogError("invalid prefab to spawn.", this);
				return false;
			}
			if (prefabOrString is GameObject go)
			{
				if (srcType != eSrcType.GameObject)
				{
					Debug.LogWarning($"[{nameof(KxObjectPool)}] GameObject type should only be used with eSrcType.GameObject, but got {srcType}.", this);
				}
				srcType = eSrcType.GameObject; // force to GameObject type
			}
			var key = new CombineKey(srcType, prefabOrString);
			if (!m_Category.TryGetValue(key, out var category))
				return false;
			prefab = category.prefab;
			return prefab != null;
		}

		private PrefabCategory GetOrAddCategory(object prefabOrString, eSrcType srcType)
		{
			if (prefabOrString == null)
			{
				Debug.LogError("invalid prefab to spawn.", this);
				return null;
			}

			if (prefabOrString is GameObject go)
			{
				if (srcType != eSrcType.GameObject)
				{
					Debug.LogWarning($"[{nameof(KxObjectPool)}] GameObject type should only be used with eSrcType.GameObject, but got {srcType}.", this);
				}
				srcType = eSrcType.GameObject; // force to GameObject type
			}

			var key = new CombineKey(srcType, prefabOrString);


			// Quick check if prefab already exists in category
			if (!category.TryGetValue(key, out PrefabCategory info))
			{
				// TODO: locate prefab based on srcType
				GameObject prefab = null;
				switch (srcType)
				{
					case eSrcType.GameObject:
					{
						prefab = prefabOrString as GameObject;
						if (prefab == null)
						{
							Debug.LogError($"[{nameof(KxObjectPool)}] GameObject type requires a valid GameObject, but got {prefabOrString}.", this);
							return null;
						}
					}
					break;
					case eSrcType.Resources:
					{
						var path = prefabOrString as string;
						if (string.IsNullOrEmpty(path))
						{
							Debug.LogError($"[{nameof(KxObjectPool)}] Addressable path cannot be null or empty.", this);
							return null;
						}
						prefab = Resources.Load<GameObject>(path);
					}
					break;
					//case eSrcType.StreamingAssets:
					//{
					//	var path = prefabOrString as string;
					//	if (string.IsNullOrEmpty(path))
					//	{
					//		Debug.LogError($"[{nameof(KxObjectPool)}] Addressable path cannot be null or empty.", this);
					//		return null;
					//	}
					//	// TODO: try load file from StreamingAssets
					//	if (path.StartsWith(Application.streamingAssetsPath))
					//	{
					//		KxFile.Read()
					//	}
					//	else if (path.StartsWith(Application.persistentDataPath))
					//	{
					//	}
					//	else if (path.StartsWith(Application.dataPath))
					//	{
					//		path = path.Substring(Application.dataPath.Length - Application.streamingAssetsPath.Length);
					//	}
					//	else
					//	{
					//		Debug.LogError($"[{nameof(KxObjectPool)}] StreamingAssets path must start with {Application.streamingAssetsPath} or {Application.persistentDataPath} or {Application.dataPath}/StreamingAssets/.", this);
					//		return null;
					//	}
					//}
					//break;
					case eSrcType.Addressable:
					{
#if USE_ADDRESSABLE
						var path = prefabOrString as string;
						if (string.IsNullOrEmpty(path))
						{
							Debug.LogError($"[{nameof(KxObjectPool)}] Addressable path cannot be null or empty.", this);
							return null;
						}
						var handle = Addressables.LoadAssetAsync<GameObject>(path);
						prefab = handle.WaitForCompletion();
#else
						Debug.LogError($"[{nameof(kObjectPool)}] Addressable ({stringOrPrefab}) is not supported in this build, please enable USE_ADDRESSABLE define symbol.");
#endif
					}
					break;
					default:
					throw new System.NotImplementedException($"Invalid Source Type: {srcType}");
				}

				if (prefab == null)
				{
					throw new System.NullReferenceException($"Prefab {prefabOrString as string} cannot be null.");
				}

				category.Add(key, info = new PrefabCategory(key, prefab, transform));
			}
			return info;
		}

		Dictionary<GameObject, ISpawnToken[]> m_TokenDict = new Dictionary<GameObject, ISpawnToken[]>(8);
		protected GameObject InternalSpawn(object prefabOrString, eSrcType srcType, Vector3 position, Quaternion rotation, Transform parent, bool worldStay)
		{
			if (IsAppQuit)
				return null;
#if !USE_ADDRESSABLE
			if (srcType == eSrcType.Addressable)
			{
				Debug.LogError($"[{nameof(kObjectPool)}] Addressable is not supported in this build, please enable USE_ADDRESSABLE define symbol.", this);
				return null;
			}
#endif
			var info = GetOrAddCategory(prefabOrString, srcType);
			if (info == null)
				return null;
			info.GetOrAddToken(out var token, parent, worldStay); //parent == null means scene root 
			m_ActiveTokens.Add(token, info.key);
			if (!worldStay)
			{
				token.transform.SetPositionAndRotation(position, rotation);
			}
			token.SetActive(true);
			Event_Spawn.TryCatchDispatchEventError(o => o?.Invoke(token));
			var arr = token.GetComponentsInChildCache<ISpawnToken>(m_TokenDict, true);
			foreach (var o in arr)
			{
				o.OnSpawn(this);
			}
			return token;
		}

		protected bool InternalDespawn(GameObject token)
		{
			if (IsAppQuit)
				return false;
			if (IsDestroy && token == null)
				return false;
			if (!m_ActiveTokens.TryGetValue(token, out var prefab))
			{
				Debug.LogWarning($"{token} isn't spawned by spawn pool :{name}.", token);
				// could be deactive in m_Category > PrefabInfo.deactiveObjs, skip search due to performance issue.
				return false;
			}

			var arr = token.GetComponentsInChildCache<ISpawnToken>(m_TokenDict, true);
			foreach (var o in arr)
			{
				o.OnDespawn();
			}
			Event_Despawn.TryCatchDispatchEventError(o => o?.Invoke(token));

			if (!IsDestroy)
			{
				m_ActiveTokens.Remove(token);
				category[prefab].ReturnToken(token);
			}
			return true;
		}

		private GameObject ResetLocalPosRot(bool worldStay_, GameObject go)
		{
			if (!worldStay_)
			{
				go.transform.localPosition = Vector3.zero;
				go.transform.localRotation = Quaternion.identity;
			}
			return go;
		}

		public void DespawnAll()
		{
			// category.Values.Select(cat => cat.activeObjs).Select(o => o.ToArray()).SelectMany(o => o).ToArray();
			foreach (var cat in category.Values)
			{
				var arr = cat.activeObjs.ToArray();
				foreach (var a in arr)
				{
					if (a == null)
						continue;
					InternalDespawn(a);
				}
			}
		}
		#endregion Pooling

		#region Public API
		public GameObject Spawn(GameObject prefab, Transform parent, bool worldStay = false)
			=> ResetLocalPosRot(worldStay, InternalSpawn(prefab, eSrcType.GameObject, Vector3.zero, Quaternion.identity, parent, true));
		
		public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, bool worldStay)
			=> InternalSpawn(prefab, eSrcType.GameObject, position, rotation, parent, worldStay);

		public GameObject Spawn(string prefabPath, eSrcType type, Transform parent, bool worldStay = false)
			=> InternalSpawn(prefabPath, type, Vector3.zero, Quaternion.identity, parent, worldStay);

		public GameObject Spawn(GameObject prefab, eSrcType type, Transform parent, bool worldStay = false)
			=> ResetLocalPosRot(worldStay, InternalSpawn(prefab, type, Vector3.zero, Quaternion.identity, parent, true));

		public GameObject Spawn(string prefabPath, eSrcType type, Vector3 position, Quaternion rotation, Transform parent, bool worldStay)
			=> InternalSpawn(prefabPath, type, position, rotation, parent, worldStay);

		public GameObject Spawn(GameObject prefab, eSrcType type, Vector3 position, Quaternion rotation, Transform parent, bool worldStay)
			=> InternalSpawn(prefab, type, position, rotation, parent, worldStay);

		public T Spawn<T>(T prefab, Transform parent, bool worldStay = false) where T : Component
		{
			var obj = Spawn(prefab.gameObject, parent, worldStay);
			if (obj.TryGetComponent<T>(out var component))
			{
				return component;
			}
			else
			{
				throw new System.Exception($"[{nameof(KxObjectPool)}] Spawned object contains no {typeof(T).Name}");
			}
		}

		public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent, bool worldStay) where T : Component
		{
			var obj = Spawn(prefab.gameObject, position, rotation, parent, worldStay);
			if (obj.TryGetComponent<T>(out var component))
			{
				return component;
			}
			else
			{
				throw new System.Exception($"[{nameof(KxObjectPool)}] Spawned object contains no {typeof(T).Name}");
			}
		}

		public bool IsSpawned(GameObject token)
		{
			if (token == null || m_ActiveTokens == null) return false;
			return m_ActiveTokens.ContainsKey(token);
		}

		public bool Despawn(GameObject token)
			=> InternalDespawn(token);

		public IEnumerable<GameObject> GetSpawnedObjects()
			=> m_ActiveTokens.Keys;
		#endregion Public API

		#region Tools
		public static string GetPrefabPath(GameObject prefab)
		{
#if UNITY_EDITOR
			const string resourcesPath = "resources/";
			string path = UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefab);
			if (string.IsNullOrEmpty(path))
				throw new System.Exception($"{prefab} is not a prefab");
			int index = path.ToLower().IndexOf(resourcesPath);
			if (index == -1)
				throw new System.Exception($"{prefab} didn't contain \"{resourcesPath}\" path.\n{path}");
			else
				index += resourcesPath.Length;
			path = path.Substring(index, path.Length - index);
			return path;
#else
            throw new System.NotImplementedException("'GetPrefabPath' Feature not support in release.");
#endif
		}
		#endregion Tools

		#region Disposable
		public bool Isdisposed { get; private set; } = false;

		protected virtual void Dispose(bool disposing)
		{
			if (!Isdisposed)
			{
				if (disposing)
				{
					// dispose managed state (managed objects)
					m_Tasks.Clear();
					m_ActiveTokens.Clear();
					m_TokenDict.Clear();
					var arr = m_Category.Values.ToArray();
					for (int i = 0; i < arr.Length; ++i)
					{
						try
						{
							arr[i].Dispose();
						}
						catch (System.Exception ex)
						{
							Debug.LogWarning(ex, this);
							// ex.DeepLogInvocationException($"{nameof(kObjectPool)}-{name} : {nameof(Dispose)}");
						}
					}
					m_Category.Clear();
				}

				m_PreloadConfig = null;
				m_Category = null;
				m_Tasks = null;
				m_ActiveTokens = null;
				m_TokenDict = null;
				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				Isdisposed = true;
			}
		}

		~KxObjectPool()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: false);
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			System.GC.SuppressFinalize(this);
		}
		#endregion Disposable
	}
}
